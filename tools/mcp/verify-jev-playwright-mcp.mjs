#!/usr/bin/env node
// Smoke test for the local jev-playwright-mcp install: launches the proxy over
// stdio exactly as an agent would, drives a real browser against a local page,
// and asserts the upstream + Jev tool surface. Exit 0 = healthy.
//
//   node tools/mcp/verify-jev-playwright-mcp.mjs
import { existsSync } from 'node:fs';
import { createServer } from 'node:http';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const installDir = join(here, 'jev-playwright-mcp');
const cliPath = join(installDir, 'cli.js');

if (!existsSync(cliPath)) {
  console.error(`jev-playwright-mcp not installed at ${installDir}`);
  console.error('Run: bash tools/mcp/install-jev-playwright-mcp.sh');
  process.exit(1);
}

// The MCP SDK lives in the gitignored checkout, so it can only be resolved at runtime.
const requireFromInstall = createRequire(join(installDir, 'package.json'));
const importFromInstall = (specifier) =>
  import(pathToFileURL(requireFromInstall.resolve(specifier)).href);
const { Client } = await importFromInstall('@modelcontextprotocol/sdk/client/index.js');
const { StdioClientTransport } = await importFromInstall('@modelcontextprotocol/sdk/client/stdio.js');

const MARKER = 'Aegis jev-playwright smoke page';
const server = createServer((_req, res) => {
  res.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
  res.end(`<!doctype html><title>${MARKER}</title><h1>${MARKER}</h1><button>Acknowledge</button>`);
});
await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
const url = `http://127.0.0.1:${server.address().port}/`;

const textOf = (result) => (result.content ?? []).map((c) => c.text ?? '').join('\n');
let failed = false;
const check = (ok, label) => {
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${label}`);
  if (!ok) failed = true;
};

const transport = new StdioClientTransport({
  command: process.execPath,
  args: [cliPath, '--headless', '--isolated'],
  stderr: 'inherit',
});
const client = new Client({ name: 'cmano-clone-jev-playwright-verify', version: '1.0.0' });

try {
  await client.connect(transport);

  const { tools } = await client.listTools();
  const names = new Set(tools.map((t) => t.name));
  console.log(`tools/list: ${tools.length} tools`);
  for (const name of ['browser_navigate', 'browser_snapshot', 'browser_set_goal', 'browser_jev_status']) {
    check(names.has(name), `tool listed: ${name}`);
  }

  const nav = await client.callTool({ name: 'browser_navigate', arguments: { url } });
  check(!nav.isError && textOf(nav).includes(MARKER), `browser_navigate ${url}`);

  const snap = await client.callTool({ name: 'browser_snapshot', arguments: {} });
  check(!snap.isError && textOf(snap).includes('Acknowledge'), 'browser_snapshot contains page content');

  const status = await client.callTool({ name: 'browser_jev_status', arguments: {} });
  check(!status.isError, 'browser_jev_status responds');
  console.log(`jev status:\n${textOf(status)}`);

  await client.callTool({ name: 'browser_close', arguments: {} }).catch(() => {});
} catch (err) {
  console.error(err);
  failed = true;
} finally {
  await client.close().catch(() => {});
  server.close();
}

console.log(failed ? 'RESULT: FAIL' : 'RESULT: PASS');
process.exit(failed ? 1 : 0);
