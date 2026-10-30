# GameCI Unity license (Unity CI workflow)

`.github/workflows/unity-ci.yml` (manual `workflow_dispatch`) runs `game-ci/unity-test-runner@v4`
against `unity/ProjectAegis` on Unity **6000.3.22f1** (must match `ProjectSettings/ProjectVersion.txt`).

## Required repo secrets (Settings → Secrets and variables → Actions)

| Secret | Value |
|---|---|
| `UNITY_LICENSE` | Full XML contents of a signed `Unity_lic.ulf` (from `<?xml` to `</root>`, including `<Signature>`) |
| `UNITY_EMAIL` | Unity ID email of the licensed account |
| `UNITY_PASSWORD` | Unity ID password for that account |

The workflow's "Require valid Unity license" step fails fast with an actionable error if any secret is
missing, or if `UNITY_LICENSE` lacks a `<Signature>` element / closing `</root>` (truncated paste or an
`.alf` request file instead of a `.ulf`).

## Re-export a Personal license (.ulf)

1. Install Unity Hub, sign in with the account above, and activate a **Personal** license
   (Hub → Preferences → Licenses → Add → Get a free personal license).
2. Locate the license file:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. Open it in a plain text editor and copy the **entire** file. Do not trim whitespace or lines.
   The easiest safe path is the gh CLI, which avoids clipboard truncation:
   ```bash
   gh secret set UNITY_LICENSE -R drgaciw/cmano-clone < "/path/to/Unity_lic.ulf"
   gh secret set UNITY_EMAIL -R drgaciw/cmano-clone
   gh secret set UNITY_PASSWORD -R drgaciw/cmano-clone
   ```
4. Re-run **Actions → Unity CI (manual) → Run workflow**.

Alternative: follow GameCI's activation guide (https://game.ci/docs/github/activation). Note that Unity
no longer supports manual `.alf` → `.ulf` activation for Personal licenses, so the Hub export above is the
reliable route. Personal `.ulf` files can expire or be invalidated when the machine's license is
returned; repeat these steps if activation starts failing again.

## Known failure (2026-10-29, run 40907516771)

`Cannot load ULF license: Signature element not found in XML document` — the `UNITY_LICENSE` secret
did not contain a signed `.ulf`. The run also pinned `6000.3.14f1` while the project is on `6000.3.22f1`
(fixed in the workflow).
