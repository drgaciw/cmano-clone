# aar_explain evidence fixture

Test-only synthetic JSONL for headless `aar_explain --evidence` UAT.

This file is not production evidence. It is not a Wave 0 order-log case, and it is not listed in `tests/ai-fixtures/manifest.json`.

The bytes match `AarExplainCommandTests.WriteSampleEvidenceAsync`: build identity `build-cli`, scenario `scenario-cli`, evidence ids `fp:1` (Engagement) and `fp:2` (AgentDecision). They were produced by `JsonlEvidenceWriter.WriteAsync` so `ReadRoundTripAsync` accepts them.

Unit text in the file is untrusted synthetic data.

`.gitattributes` pins `*.jsonl` to LF. A `core.autocrlf=true` checkout must not rewrite this file to CRLF.
