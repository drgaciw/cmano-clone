# GameCI Unity license (Unity CI workflow)

`.github/workflows/unity-ci.yml` (manual `workflow_dispatch`) runs `game-ci/unity-test-runner@v4`
against `unity/ProjectAegis` on Unity **6000.3.22f1** (must match `ProjectSettings/ProjectVersion.txt`).

## Required repo secrets (Settings → Secrets and variables → Actions)

| Secret | Value |
|---|---|
| `UNITY_LICENSE` | Full XML contents of a signed `Unity_lic.ulf` (from `<?xml` to `</root>`, including `<Signature>`) |
| `UNITY_EMAIL` | Unity ID email of the licensed account |
| `UNITY_PASSWORD` | Unity ID password for that account |

The workflow's "Check Unity license prerequisites" step fails fast if any secret is missing,
the XML is malformed, or it lacks the legacy `root/License` structure and a `Signature` element.
This rejects `.alf` requests and entitlement XML before starting the Editor. It does not verify the
cryptographic signature, expiry, or activation: only a successful Unity run establishes those.

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
4. Re-run **Actions → Unity CI (manual) → Run workflow**, selecting the PR branch while testing this
   change. Capture the run URL and test results before treating Editor CI as restored.

These steps follow [GameCI's Personal activation guide](https://game.ci/docs/github/activation/).
That guide notes that some Hub installations do not offer `.ulf` generation. Do not assume that a
license displayed in Hub guarantees a `.ulf` exists. If the account has only a named-user entitlement,
this legacy-file workflow remains blocked until a compatible activation method is available.
Do not change or return a working local license merely to retry CI.

## Do not use `UnityEntitlementLicense.xml`

Unity 6 distinguishes [serial-based `.ulf` files from named-user entitlement files](https://docs.unity3d.com/6000.0/Documentation/Manual/ActivationFAQ.html).
The named-user file on Linux is `~/.config/unity3d/Unity/licenses/UnityEntitlementLicense.xml`.
An XML signature alone does not make that file a legacy `.ulf`; do not rename it or upload it as
`UNITY_LICENSE`. The workflow now checks the legacy structure as well as the signature element.
GameCI v4 (test-runner/builder) cannot activate Personal from `UNITY_EMAIL`/`UNITY_PASSWORD` alone;
for Personal it requires `UNITY_LICENSE`.

## Known failure (2026-10-29, run 40907516771)

[`40907516771`](https://github.com/drgaciw/cmano-clone/actions/runs/40907516771) reports
`Cannot load ULF license: Signature element not found in XML document`. This establishes that Unity
could not load the supplied file as a signed `.ulf`; it does not establish whether the secret was
truncated, an activation request, or a different license format. The run also pinned `6000.3.14f1`
while the project is on `6000.3.22f1` (fixed in the workflow). A passing headless build does not prove
that the license issue is resolved.
