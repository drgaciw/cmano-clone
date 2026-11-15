# ProjectAegis.Ai.Host

Wave 0 AI host skeleton (DRG-731).

- DI composition via `AiHostComposer.AddProjectAegisAiHost`
- `FakeChatClient` (`IChatClient`) — scripted offline responses only
- `AtomicArtifactStore` — temp write → validate → atomic publish
- Telemetry excludes secrets and full prompt bodies

**Fences:** not in `ProjectAegis.sln` yet (DRG-735). No provider SDKs. No `Process.Start` / `Process.Run*`. No paid network calls.
