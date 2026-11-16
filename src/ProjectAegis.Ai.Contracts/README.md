# ProjectAegis.Ai.Contracts

Wave 0 frozen shapes for evidence export and AI artifacts (DRG-730).

## Freeze rule

- Schema version is `AiContractSchema.Version` (`2` after DRG-749 additive `AdviceExplanationContent`).
- After Wave 0 exit, **additive changes only**, with a version bump.
- Do not rename or remove public members without a new major schema version and a coordinated consumer update.
- This project targets `netstandard2.1;net11.0`, **LangVersion 12.0**, and has **zero package references**.
- Do not reference Delegation, Sim, Data, UnityEngine, or Microsoft.Extensions.AI from this project.

## Evidence IDs

`EvidenceId.From(fingerprintPrefix, sequenceId)` → `{fingerprint-prefix}:{SequenceId}`.

## Build

Not in `ProjectAegis.sln` until DRG-735. Build directly:

```bash
dotnet build src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj
dotnet test src/ProjectAegis.Ai.Contracts/test/ProjectAegis.Ai.Contracts.Tests.csproj
```
