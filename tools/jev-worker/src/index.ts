// jev-worker: authenticated forwarder to OpenRouter's Jev decision endpoint.
// Replaces the fixed demo payload with a validated pass-through of { state, questions }.
// Secrets (Worker secrets, never in source): JEV_WORKER_TOKEN, OPENROUTER_API_KEY.
// Optional var: OPENROUTER_MODEL (default typesafe/jev-1.14). Clients cannot override the model.

interface Env {
  JEV_WORKER_TOKEN?: string;
  OPENROUTER_API_KEY?: string;
  OPENROUTER_MODEL?: string;
}

const MAX_BODY_BYTES = 32 * 1024;
const MAX_QUESTIONS = 20;
const KEY_PATTERN = /^[a-z0-9_]{1,40}$/;

type Json = Record<string, unknown>;

function json(body: unknown, status = 200, extra: Record<string, string> = {}): Response {
  return Response.json(body, { status, headers: { "Cache-Control": "no-store", ...extra } });
}

function isRecord(value: unknown): value is Json {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isShortString(value: unknown, max: number): value is string {
  return typeof value === "string" && value.length > 0 && value.length <= max;
}

/** Returns an error string, or null when the question is well formed. */
function validateQuestion(q: unknown): string | null {
  if (!isRecord(q)) return "question must be an object";
  if (!isShortString(q.instructions, 600)) return "instructions required (max 600 chars)";
  switch (q.type) {
    case "choice": {
      if (!isRecord(q.criteria)) return "choice needs a criteria object";
      const entries = Object.entries(q.criteria);
      if (entries.length < 2 || entries.length > 255) return "choice criteria needs 2..255 options";
      return entries.every(([k, v]) => KEY_PATTERN.test(k) && isShortString(v, 400))
        ? null
        : "choice criteria keys must match [a-z0-9_]{1,40} with string descriptions";
    }
    case "score": {
      if (!Array.isArray(q.criteria)) return "score needs a criteria array";
      if (q.criteria.length < 2 || q.criteria.length > 10) return "score criteria needs 2..10 levels";
      return q.criteria.every((c) => isShortString(c, 400)) ? null : "score levels must be strings";
    }
    case "noul": {
      if (q.criteria === undefined) return null;
      if (!isRecord(q.criteria)) return "noul criteria must be an object";
      return Object.values(q.criteria).every((v) => isShortString(v, 400))
        ? null
        : "noul criteria values must be strings";
    }
    default:
      return "type must be choice, score or noul";
  }
}

function validateBody(body: unknown): { error: string } | { state: unknown; questions: Json } {
  if (!isRecord(body)) return { error: "body must be a JSON object" };
  const { state, questions } = body;
  if (!(typeof state === "string" || isRecord(state))) return { error: "state must be a string or object" };
  if (!isRecord(questions)) return { error: "questions must be an object" };
  const names = Object.keys(questions);
  if (names.length === 0 || names.length > MAX_QUESTIONS) return { error: `questions needs 1..${MAX_QUESTIONS} entries` };
  for (const name of names) {
    if (!KEY_PATTERN.test(name)) return { error: `invalid question name: ${name}` };
    const problem = validateQuestion(questions[name]);
    if (problem) return { error: `${name}: ${problem}` };
  }
  return { state, questions };
}

async function tokenMatches(supplied: string, expected: string): Promise<boolean> {
  const enc = new TextEncoder();
  const [a, b] = await Promise.all(
    [supplied, expected].map(async (t) => new Uint8Array(await crypto.subtle.digest("SHA-256", enc.encode(t)))),
  );
  let mismatch = 0;
  for (let i = 0; i < b.length; i++) mismatch |= a[i] ^ b[i];
  return supplied.length > 0 && mismatch === 0;
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const workerToken = env.JEV_WORKER_TOKEN?.trim();
    if (!workerToken) {
      return json({ error: { code: "worker_not_configured", message: "Set the JEV_WORKER_TOKEN secret." } }, 503);
    }

    // Authenticate before reading the body.
    const supplied = request.headers.get("Authorization")?.match(/^Bearer (\S+)$/i)?.[1] ?? "";
    if (!(await tokenMatches(supplied, workerToken))) {
      return json({ error: { code: "unauthorized" } }, 401, { "WWW-Authenticate": "Bearer" });
    }

    if (request.method !== "POST") {
      return json({ error: { code: "method_not_allowed" } }, 405, { Allow: "POST" });
    }

    const apiKey = env.OPENROUTER_API_KEY?.trim();
    if (!apiKey) {
      return json({ error: { code: "openrouter_not_configured", message: "Set the OPENROUTER_API_KEY secret." } }, 503);
    }

    const raw = await request.text();
    if (new TextEncoder().encode(raw).length > MAX_BODY_BYTES) {
      return json({ error: { code: "payload_too_large", max_bytes: MAX_BODY_BYTES } }, 413);
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(raw);
    } catch {
      return json({ error: { code: "invalid_json" } }, 400);
    }

    const checked = validateBody(parsed);
    if ("error" in checked) {
      return json({ error: { code: "invalid_request", message: checked.error } }, 400);
    }

    try {
      const response = await fetch("https://openrouter.ai/api/alpha/decisions", {
        method: "POST",
        headers: { Authorization: `Bearer ${apiKey}`, "Content-Type": "application/json" },
        body: JSON.stringify({
          model: env.OPENROUTER_MODEL || "typesafe/jev-1.14",
          state: checked.state,
          questions: checked.questions,
        }),
        redirect: "manual",
        signal: AbortSignal.timeout(30_000),
      });
      if (!response.ok) {
        return json({ error: { code: "openrouter_request_failed", upstream_status: response.status } }, 502);
      }
      const result: unknown = await response.json();
      if (!isRecord(result) || !isRecord(result.answers)) {
        return json({ error: { code: "openrouter_invalid_response" } }, 502);
      }
      return json(result);
    } catch (error) {
      let detail = error instanceof Error ? error.message : "Unknown transport error";
      for (const secret of [apiKey, workerToken]) detail = detail.split(secret).join("[redacted]");
      console.error("OpenRouter transport failure:", detail.slice(0, 300));
      return json({ error: { code: "openrouter_unavailable" } }, 502);
    }
  },
};
