# Sprint 7 TDD RED Evidence (`DRG-269`, `DRG-268`)

## Track A & B Headless RED (`PendingApprovalInterruptCoordinatorTests`, `MessageLogExplainDeepLinkCoordinatorTests`)

Command:
```bash
dotnet build src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --no-restore
dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-restore
```

Verbatim failing RED output prior to implementing `PendingApprovalInterruptCoordinator.cs` and `MessageLogExplainDeepLinkCoordinator.cs`:
```text
src/ProjectAegis.Delegation.Tests/Projection/PendingApprovalInterruptCoordinatorTests.cs(65,31): error CS0246: The type or namespace name 'PendingApprovalInterruptCoordinator' could not be found
    0 Warning(s)
    11 Error(s)
```

```text
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/MessageLogExplainDeepLinkCoordinatorTests.cs(28,22): error CS0103: The name 'MessageLogExplainDeepLinkCoordinator' does not exist in the current context
    0 Warning(s)
    4 Error(s)
```
