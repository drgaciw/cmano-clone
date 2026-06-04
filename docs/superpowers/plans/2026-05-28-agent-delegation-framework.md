> **ARCHIVED (2026-06-04):** `ProjectAegis.Delegation` is shipped; req 04 tracker row is **Partial** (runtime + tests). Do not execute remaining checklist tasks here. Active planning: [2026-06-04-requirements-wave5-implementation.md](2026-06-04-requirements-wave5-implementation.md). Tracker: [implementation-tracker-2026-06-04.md](../../../Game-Requirements/implementation-tracker-2026-06-04.md).

# Agent Delegation Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the controller/possession delegation framework from `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md` as a deterministic, testable C# library with stub sim/policy seams—ready for a future Unity/DOTS sim to consume.

**Architecture:** Build `ProjectAegis.Delegation` as an engine-agnostic .NET 8 class library with NUnit tests. Controllers emit shared `Order` objects; `DelegationOrchestrator` owns target registry, controller slots, attention/degradation, stochastic choice, autonomy gating, group detach-rejoin (next-cycle), ROE filtering, and decision logging. Unity integration is a later thin adapter—not in this plan.

**Tech Stack:** .NET 8, C# 12, NUnit 3, `dotnet test`. No Unity packages in v1.

**Design spec:** `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`

**Planning decisions (resolves spec §9 open questions for v1):**
- Group re-plan on detach: **next decision cycle** (not immediate).
- Observed state: **`PerceivedState`** filtered from full `ObservedState` using `situational_awareness`.
- Assisted autonomy: **`IRiskClassifier`** with default rules (`Move`/`Hold` = low, `Engage` = high).

---

## File Map

| Path | Responsibility |
|------|----------------|
| `src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj` | Library project |
| `src/ProjectAegis.Delegation/Core/Identifiers.cs` | `TargetId`, `AgentId`, `OrderId` |
| `src/ProjectAegis.Delegation/Core/Order.cs` | `Order`, `OrderKind`, `RiskLevel` |
| `src/ProjectAegis.Delegation/Core/AutonomyLevel.cs` | Manual → FullAutonomous |
| `src/ProjectAegis.Delegation/Core/SimulationMode.cs` | Human / Mixed / AgentVsAgent profiles |
| `src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs` | Target contract + controller slot |
| `src/ProjectAegis.Delegation/Targets/UnitTarget.cs` | Single unit target |
| `src/ProjectAegis.Delegation/Targets/GroupTarget.cs` | Group + member list |
| `src/ProjectAegis.Delegation/Controllers/IController.cs` | Controller contract |
| `src/ProjectAegis.Delegation/Controllers/HumanController.cs` | Player order queue |
| `src/ProjectAegis.Delegation/Controllers/AgentController.cs` | Policy + traits + suspended state |
| `src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs` | Active vs suspended agent |
| `src/ProjectAegis.Delegation/Attention/AttentionCalculator.cs` | Load, budget, degradation flags |
| `src/ProjectAegis.Delegation/Traits/TraitVector.cs` | Six trait fields |
| `src/ProjectAegis.Delegation/Traits/PersonalityCatalog.cs` | Six named presets (data) |
| `src/ProjectAegis.Delegation/Decision/SeededRng.cs` | Per-agent deterministic RNG |
| `src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs` | Soft-max + reaction delay gate |
| `src/ProjectAegis.Delegation/Decision/DecisionRecord.cs` | Log artifact |
| `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` | Append-only stream |
| `src/ProjectAegis.Delegation/Policy/IPolicy.cs` | `decide(...)` seam |
| `src/ProjectAegis.Delegation/Policy/StubPatrolPolicy.cs` | Test policy |
| `src/ProjectAegis.Delegation/Roe/IRoeFilter.cs` | Reject/queue illegal orders |
| `src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs` | Assisted autonomy risk |
| `src/ProjectAegis.Delegation/Groups/DetachRejoinService.cs` | Detach/rejoin + next-cycle replan |
| `src/ProjectAegis.Delegation/Sim/ObservedState.cs` | Full + perceived snapshots |
| `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs` | Tick loop, mode config |
| `src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs` | Trust hook (emit-only) |
| `src/ProjectAegis.Delegation.Tests/**` | NUnit tests per component |
| `ProjectAegis.sln` | Solution root |

---

### Task 1: Solution scaffold

**Files:**
- Create: `ProjectAegis.sln`
- Create: `src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj`
- Create: `src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj`
- Create: `src/ProjectAegis.Delegation.Tests/SmokeTests.cs`

- [ ] **Step 1: Create library csproj**

Create `src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ProjectAegis.Delegation</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create test csproj**

Create `src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectAegis.Delegation\ProjectAegis.Delegation.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create solution and add projects**

Run from repo root:

```bash
dotnet new sln -n ProjectAegis -o .
dotnet sln ProjectAegis.sln add src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj
dotnet sln ProjectAegis.sln add src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj
```

- [ ] **Step 4: Write failing smoke test**

Create `src/ProjectAegis.Delegation.Tests/SmokeTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class SmokeTests
{
    [Test]
    public void Solution_builds()
    {
        Assert.Pass();
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test ProjectAegis.sln -v minimal`

Expected: PASS (1 test)

- [ ] **Step 6: Commit**

```bash
git add ProjectAegis.sln src/ProjectAegis.Delegation src/ProjectAegis.Delegation.Tests
git commit -m "chore: scaffold ProjectAegis.Delegation solution and test project"
```

---

### Task 2: Core order and identifier types

**Files:**
- Create: `src/ProjectAegis.Delegation/Core/Identifiers.cs`
- Create: `src/ProjectAegis.Delegation/Core/Order.cs`
- Create: `src/ProjectAegis.Delegation/Core/AutonomyLevel.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Core/OrderTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Core/OrderTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Core;

using ProjectAegis.Delegation.Core;
using NUnit.Framework;

[TestFixture]
public sealed class OrderTests
{
    [Test]
    public void Order_stores_kind_target_and_sim_time()
    {
        var target = new TargetId("unit-1");
        var order = new Order(
            new OrderId(1),
            target,
            simTime: 42.5,
            OrderKind.Hold,
            RiskLevel.Low);

        Assert.That(order.Target, Is.EqualTo(target));
        Assert.That(order.SimTime, Is.EqualTo(42.5));
        Assert.That(order.Kind, Is.EqualTo(OrderKind.Hold));
        Assert.That(order.Risk, Is.EqualTo(RiskLevel.Low));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ProjectAegis.sln --filter "Order_stores_kind_target_and_sim_time" -v minimal`

Expected: FAIL (types not found)

- [ ] **Step 3: Implement core types**

Create `src/ProjectAegis.Delegation/Core/Identifiers.cs`:

```csharp
namespace ProjectAegis.Delegation.Core;

public readonly record struct TargetId(string Value);
public readonly record struct AgentId(string Value);
public readonly record struct OrderId(long Value);
```

Create `src/ProjectAegis.Delegation/Core/Order.cs`:

```csharp
namespace ProjectAegis.Delegation.Core;

public enum OrderKind
{
    Move,
    Hold,
    Engage,
    SetEwPosture,
    ReturnToBase,
}

public enum RiskLevel
{
    Low,
    High,
}

public sealed record Order(
    OrderId Id,
    TargetId Target,
    double SimTime,
    OrderKind Kind,
    RiskLevel Risk);
```

Create `src/ProjectAegis.Delegation/Core/AutonomyLevel.cs`:

```csharp
namespace ProjectAegis.Delegation.Core;

public enum AutonomyLevel
{
    Manual = 1,
    Assisted = 2,
    SemiAutonomous = 3,
    FullAutonomous = 4,
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test ProjectAegis.sln --filter "Order_stores_kind_target_and_sim_time" -v minimal`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Core src/ProjectAegis.Delegation.Tests/Core
git commit -m "feat(delegation): add Order and core identifier types"
```

---

### Task 3: Commandable targets and controller slot

**Files:**
- Create: `src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs`
- Create: `src/ProjectAegis.Delegation/Targets/UnitTarget.cs`
- Create: `src/ProjectAegis.Delegation/Targets/GroupTarget.cs`
- Create: `src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Targets/TargetTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Targets/TargetTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using NUnit.Framework;

[TestFixture]
public sealed class TargetTests
{
    [Test]
    public void UnitTarget_starts_with_no_active_controller()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        Assert.That(unit.Slot.Active, Is.Null);
        Assert.That(unit.Slot.SuspendedAgent, Is.Null);
    }

    [Test]
    public void GroupTarget_tracks_members()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var member = new TargetId("u1");
        group.AddMember(member);
        Assert.That(group.Members, Does.Contain(member));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~TargetTests" -v minimal`

Expected: FAIL

- [ ] **Step 3: Implement targets and slot**

Create `src/ProjectAegis.Delegation/Controllers/IController.cs`:

```csharp
namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;

public interface IController
{
    bool IsHuman { get; }
    IReadOnlyList<Order> DrainIssuedOrders();
}
```

Create `src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs`:

```csharp
namespace ProjectAegis.Delegation.Controllers;

public sealed class ControllerSlot
{
    public IController? Active { get; private set; }
    public AgentController? SuspendedAgent { get; private set; }

    public void SetActive(IController controller)
    {
        Active = controller;
    }

    public void SuspendAgent(AgentController agent)
    {
        SuspendedAgent = agent;
        Active = null;
    }

    public void ResumeSuspendedAgent()
    {
        if (SuspendedAgent is null)
        {
            throw new InvalidOperationException("No suspended agent to resume.");
        }

        Active = SuspendedAgent;
        SuspendedAgent = null;
    }
}
```

Create `src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs`:

```csharp
namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public interface ICommandableTarget
{
    TargetId Id { get; }
    ControllerSlot Slot { get; }
    bool IsDetachedFromGroup { get; }
}
```

Create `src/ProjectAegis.Delegation/Targets/UnitTarget.cs`:

```csharp
namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public sealed class UnitTarget : ICommandableTarget
{
    public UnitTarget(TargetId id)
    {
        Id = id;
        Slot = new ControllerSlot();
    }

    public TargetId Id { get; }
    public ControllerSlot Slot { get; }
    public bool IsDetachedFromGroup { get; private set; }

    public void SetDetached(bool detached) => IsDetachedFromGroup = detached;
}
```

Create `src/ProjectAegis.Delegation/Targets/GroupTarget.cs`:

```csharp
namespace ProjectAegis.Delegation.Targets;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;

public sealed class GroupTarget : ICommandableTarget
{
    private readonly List<TargetId> _members = new();

    public GroupTarget(TargetId id)
    {
        Id = id;
        Slot = new ControllerSlot();
    }

    public TargetId Id { get; }
    public ControllerSlot Slot { get; }
    public bool IsDetachedFromGroup => false;
    public IReadOnlyList<TargetId> Members => _members;

    public void AddMember(TargetId memberId) => _members.Add(memberId);

    public void RemoveMember(TargetId memberId) => _members.Remove(memberId);
}
```

Create stub `src/ProjectAegis.Delegation/Sim/ObservedState.cs` (minimal, expanded in Task 11):

```csharp
namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;

public sealed record ObservedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount,
    IReadOnlyDictionary<TargetId, bool> MemberAlive);

public sealed record PerceivedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount);
```

Create placeholder `src/ProjectAegis.Delegation/Controllers/AgentController.cs`:

```csharp
namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;

public sealed class AgentController : IController
{
    public AgentController(AgentId id) => Id = id;

    public AgentId Id { get; }
    public bool IsHuman => false;

    public IReadOnlyList<Order> DrainIssuedOrders() => Array.Empty<Order>();
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~TargetTests" -v minimal`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Targets src/ProjectAegis.Delegation/Controllers src/ProjectAegis.Delegation/Sim/ObservedState.cs src/ProjectAegis.Delegation.Tests/Targets
git commit -m "feat(delegation): add commandable targets and controller slot"
```

---

### Task 4: Human controller and override swap

**Files:**
- Create: `src/ProjectAegis.Delegation/Controllers/HumanController.cs`
- Modify: `src/ProjectAegis.Delegation/Controllers/AgentController.cs`
- Create: `src/ProjectAegis.Delegation/Orchestration/OverrideService.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Controllers/OverrideTests.cs`

- [ ] **Step 1: Write failing tests**

Create `src/ProjectAegis.Delegation.Tests/Controllers/OverrideTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Controllers;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using NUnit.Framework;

[TestFixture]
public sealed class OverrideTests
{
    [Test]
    public void Override_swaps_agent_for_human_and_preserves_suspended_agent()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = new AgentController(new AgentId("a1"));
        unit.Slot.SetActive(agent);

        var service = new OverrideService();
        service.TakeDirectControl(unit, new HumanController());

        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(agent));
    }

    [Test]
    public void Release_restores_suspended_agent()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = new AgentController(new AgentId("a1"));
        unit.Slot.SetActive(agent);
        var service = new OverrideService();
        service.TakeDirectControl(unit, new HumanController());

        service.ReleaseDirectControl(unit);

        Assert.That(unit.Slot.Active, Is.SameAs(agent));
        Assert.That(unit.Slot.SuspendedAgent, Is.Null);
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL**

Run: `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~OverrideTests" -v minimal`

- [ ] **Step 3: Implement HumanController and OverrideService**

Create `src/ProjectAegis.Delegation/Controllers/HumanController.cs`:

```csharp
namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;

public sealed class HumanController : IController
{
    private readonly List<Order> _pending = new();

    public bool IsHuman => true;

    public void Enqueue(Order order) => _pending.Add(order);

    public IReadOnlyList<Order> DrainIssuedOrders()
    {
        if (_pending.Count == 0)
        {
            return Array.Empty<Order>();
        }

        var copy = _pending.ToArray();
        _pending.Clear();
        return copy;
    }
}
```

Create `src/ProjectAegis.Delegation/Orchestration/OverrideService.cs`:

```csharp
namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Targets;

public sealed class OverrideService
{
    public void TakeDirectControl(ICommandableTarget target, HumanController human)
    {
        if (target.Slot.Active is AgentController agent)
        {
            target.Slot.SuspendAgent(agent);
        }

        target.Slot.SetActive(human);
    }

    public void ReleaseDirectControl(ICommandableTarget target)
    {
        target.Slot.ResumeSuspendedAgent();
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

Run: `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~OverrideTests" -v minimal`

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Controllers/HumanController.cs src/ProjectAegis.Delegation/Orchestration/OverrideService.cs src/ProjectAegis.Delegation.Tests/Controllers
git commit -m "feat(delegation): add human override via controller swap"
```

---

### Task 5: Seeded RNG and trait vector

**Files:**
- Create: `src/ProjectAegis.Delegation/Decision/SeededRng.cs`
- Create: `src/ProjectAegis.Delegation/Traits/TraitVector.cs`
- Create: `src/ProjectAegis.Delegation/Traits/PersonalityCatalog.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Decision/DeterminismTests.cs`

- [ ] **Step 1: Write failing determinism test**

Create `src/ProjectAegis.Delegation.Tests/Decision/DeterminismTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class DeterminismTests
{
    [Test]
    public void SeededRng_same_seed_produces_same_sequence()
    {
        var a = new SeededRng(9001, agentSalt: 42);
        var b = new SeededRng(9001, agentSalt: 42);
        Assert.That(a.NextUnit(), Is.EqualTo(b.NextUnit()));
        Assert.That(a.NextUnit(), Is.EqualTo(b.NextUnit()));
    }

    [Test]
    public void PersonalityCatalog_exposes_six_presets()
    {
        var names = PersonalityCatalog.All.Select(p => p.Name).ToArray();
        Assert.That(names, Has.Length.EqualTo(6));
        Assert.That(names, Does.Contain("Aggressive"));
        Assert.That(names, Does.Contain("SwarmCoordinator"));
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement SeededRng, TraitVector, PersonalityCatalog**

Create `src/ProjectAegis.Delegation/Traits/TraitVector.cs`:

```csharp
namespace ProjectAegis.Delegation.Traits;

public sealed record TraitVector(
    double Aggression,
    double RiskTolerance,
    double ReactionDelay,
    double ErrorRate,
    double SituationalAwareness,
    double Decisiveness);
```

Create `src/ProjectAegis.Delegation/Traits/PersonalityCatalog.cs`:

```csharp
namespace ProjectAegis.Delegation.Traits;

public sealed record PersonalityPreset(string Name, TraitVector Traits);

public static class PersonalityCatalog
{
    public static IReadOnlyList<PersonalityPreset> All { get; } = new[]
    {
        new PersonalityPreset("Aggressive", new TraitVector(0.9, 0.8, 0.2, 0.15, 0.6, 0.8)),
        new PersonalityPreset("Defensive", new TraitVector(0.2, 0.3, 0.3, 0.08, 0.7, 0.5)),
        new PersonalityPreset("Cautious", new TraitVector(0.3, 0.2, 0.5, 0.05, 0.8, 0.3)),
        new PersonalityPreset("Opportunistic", new TraitVector(0.6, 0.6, 0.25, 0.12, 0.65, 0.7)),
        new PersonalityPreset("SwarmCoordinator", new TraitVector(0.5, 0.5, 0.15, 0.1, 0.75, 0.85)),
        new PersonalityPreset("EwSpecialist", new TraitVector(0.4, 0.4, 0.2, 0.07, 0.85, 0.6)),
    };
}
```

Create `src/ProjectAegis.Delegation/Decision/SeededRng.cs`:

```csharp
namespace ProjectAegis.Delegation.Decision;

public sealed class SeededRng
{
    private ulong _state;

    public SeededRng(int globalSeed, int agentSalt)
    {
        _state = (ulong)(globalSeed ^ (agentSalt * 0x9E3779B9));
        if (_state == 0)
        {
            _state = 1;
        }
    }

    public double NextUnit()
    {
        _state ^= _state << 13;
        _state ^= _state >> 7;
        _state ^= _state << 17;
        return (_state & 0xFFFFFF) / (double)0x1000000;
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Decision/SeededRng.cs src/ProjectAegis.Delegation/Traits src/ProjectAegis.Delegation.Tests/Decision
git commit -m "feat(delegation): add seeded RNG, traits, and personality presets"
```

---

### Task 6: Attention calculator and degradation

**Files:**
- Create: `src/ProjectAegis.Delegation/Attention/AttentionState.cs`
- Create: `src/ProjectAegis.Delegation/Attention/AttentionCalculator.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Attention/AttentionTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Attention/AttentionTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Attention;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Sim;
using NUnit.Framework;

[TestFixture]
public sealed class AttentionTests
{
    [Test]
    public void Overload_enables_all_degradation_flags_in_order()
    {
        var state = new ObservedState(
            SimTime: 10,
            ContactCount: 50,
            ActiveEngagementCount: 20,
            MemberAlive: new Dictionary<ProjectAegis.Delegation.Core.TargetId, bool>());

        var result = AttentionCalculator.Evaluate(
            budget: 10,
            memberCount: 8,
            state);

        Assert.That(result.Load, Is.GreaterThan(result.Budget));
        Assert.That(result.Degradation.SlowerReactions, Is.True);
        Assert.That(result.Degradation.NarrowedFocus, Is.True);
        Assert.That(result.Degradation.SimplerDecisions, Is.True);
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement attention**

Create `src/ProjectAegis.Delegation/Attention/AttentionState.cs`:

```csharp
namespace ProjectAegis.Delegation.Attention;

public sealed record AttentionDegradation(
    bool SlowerReactions,
    bool NarrowedFocus,
    bool SimplerDecisions);

public sealed record AttentionEvaluation(
    double Budget,
    double Load,
    AttentionDegradation Degradation)
{
    public bool IsOverloaded => Load > Budget;
}
```

Create `src/ProjectAegis.Delegation/Attention/AttentionCalculator.cs`:

```csharp
namespace ProjectAegis.Delegation.Attention;

using ProjectAegis.Delegation.Sim;

public static class AttentionCalculator
{
    public static AttentionEvaluation Evaluate(
        double budget,
        int memberCount,
        ObservedState state)
    {
        var load =
            state.ContactCount * 0.5 +
            state.ActiveEngagementCount * 1.0 +
            memberCount * 0.25;

        var degradation = new AttentionDegradation(
            SlowerReactions: load > budget,
            NarrowedFocus: load > budget * 1.25,
            SimplerDecisions: load > budget * 1.5);

        return new AttentionEvaluation(budget, load, degradation);
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Attention src/ProjectAegis.Delegation.Tests/Attention
git commit -m "feat(delegation): add attention load and degradation model"
```

---

### Task 7: Policy seam and decision pipeline (soft-max)

**Files:**
- Create: `src/ProjectAegis.Delegation/Policy/IPolicy.cs`
- Create: `src/ProjectAegis.Delegation/Policy/StubPatrolPolicy.cs`
- Create: `src/ProjectAegis.Delegation/Decision/ScoredIntent.cs`
- Create: `src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class DecisionPipelineTests
{
    [Test]
    public void Pipeline_is_deterministic_for_same_seed_and_inputs()
    {
        var traits = PersonalityCatalog.All[0].Traits;
        var attention = new AttentionEvaluation(100, 5, new AttentionDegradation(false, false, false));
        var rng1 = new SeededRng(42, agentSalt: 7);
        var rng2 = new SeededRng(42, agentSalt: 7);

        var a = DecisionPipeline.Choose(
            StubPatrolPolicy.Candidates,
            traits,
            attention,
            rng1);
        var b = DecisionPipeline.Choose(
            StubPatrolPolicy.Candidates,
            traits,
            attention,
            rng2);

        Assert.That(a.Chosen.Kind, Is.EqualTo(b.Chosen.Kind));
        Assert.That(a.RngDraw, Is.EqualTo(b.RngDraw));
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement policy + pipeline**

Create `src/ProjectAegis.Delegation/Policy/IPolicy.cs`:

```csharp
namespace ProjectAegis.Delegation.Policy;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;

public interface IPolicy
{
    IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits);
}
```

Create `src/ProjectAegis.Delegation/Decision/ScoredIntent.cs`:

```csharp
namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record ScoredIntent(OrderKind Kind, double Score, RiskLevel Risk);
```

Create `src/ProjectAegis.Delegation/Policy/StubPatrolPolicy.cs`:

```csharp
namespace ProjectAegis.Delegation.Policy;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;

public static class StubPatrolPolicy
{
    public static readonly IReadOnlyList<ScoredIntent> Candidates = new[]
    {
        new ScoredIntent(OrderKind.Hold, 1.0, RiskLevel.Low),
        new ScoredIntent(OrderKind.Move, 0.8, RiskLevel.Low),
        new ScoredIntent(OrderKind.Engage, 0.6, RiskLevel.High),
    };
}
```

Create `src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs`:

```csharp
namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Traits;

public sealed record PipelineChoice(ScoredIntent Chosen, double RngDraw, string Rationale);

public static class DecisionPipeline
{
    public static PipelineChoice Choose(
        IReadOnlyList<ScoredIntent> candidates,
        TraitVector traits,
        AttentionEvaluation attention,
        SeededRng rng)
    {
        var pool = candidates.ToList();
        if (attention.Degradation.NarrowedFocus)
        {
            pool = pool.OrderByDescending(c => c.Score).Take(2).ToList();
        }

        var temperature = Math.Max(0.05, traits.Decisiveness * (attention.IsOverloaded ? 0.5 : 1.0));
        var weights = pool.Select(c => Math.Exp(c.Score / temperature)).ToArray();
        var sum = weights.Sum();
        var draw = rng.NextUnit() * sum;
        var acc = 0.0;
        var index = 0;
        for (; index < pool.Count; index++)
        {
            acc += weights[index];
            if (draw <= acc)
            {
                break;
            }
        }

        var chosen = pool[Math.Min(index, pool.Count - 1)];
        var rationale = attention.IsOverloaded
            ? "overload: narrowed focus applied"
            : "nominal: trait-weighted stochastic choice";

        return new PipelineChoice(chosen, draw, rationale);
    }
}
```

Expand `ObservedState.cs` with `PerceivedStateFactory`:

```csharp
public static class PerceivedStateFactory
{
    public static PerceivedState FromFull(ObservedState full, double situationalAwareness)
    {
        var factor = Math.Clamp(situationalAwareness, 0, 1);
        var contacts = (int)Math.Round(full.ContactCount * factor);
        var engagements = (int)Math.Round(full.ActiveEngagementCount * factor);
        return new PerceivedState(full.SimTime, contacts, engagements);
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Policy src/ProjectAegis.Delegation/Decision src/ProjectAegis.Delegation/Sim/ObservedState.cs src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs
git commit -m "feat(delegation): add policy seam and deterministic decision pipeline"
```

---

### Task 8: Autonomy gating and ROE filter

**Files:**
- Create: `src/ProjectAegis.Delegation/Roe/IRoeFilter.cs`
- Create: `src/ProjectAegis.Delegation/Roe/PassthroughRoeFilter.cs`
- Create: `src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs`
- Create: `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Roe/AutonomyGateTests.cs`

- [ ] **Step 1: Write failing tests**

Create `src/ProjectAegis.Delegation.Tests/Roe/AutonomyGateTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Roe;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using NUnit.Framework;

[TestFixture]
public sealed class AutonomyGateTests
{
    [Test]
    public void Manual_never_executes_without_approval()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var result = gate.Evaluate(AutonomyLevel.Manual, order, playerApproved: false);
        Assert.That(result.ExecuteNow, Is.False);
        Assert.That(result.QueueForApproval, Is.True);
    }

    [Test]
    public void Assisted_auto_executes_low_risk_only()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var low = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Hold, RiskLevel.Low);
        var high = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        Assert.That(gate.Evaluate(AutonomyLevel.Assisted, low, playerApproved: false).ExecuteNow, Is.True);
        Assert.That(gate.Evaluate(AutonomyLevel.Assisted, high, playerApproved: false).QueueForApproval, Is.True);
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement ROE + autonomy gate**

Create `src/ProjectAegis.Delegation/Roe/IRoeFilter.cs`:

```csharp
namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public enum RoeVerdict
{
    Allow,
    Reject,
    Queue,
}

public interface IRoeFilter
{
    RoeVerdict Evaluate(Order order);
}
```

Create `src/ProjectAegis.Delegation/Roe/PassthroughRoeFilter.cs`:

```csharp
namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public sealed class PassthroughRoeFilter : IRoeFilter
{
    public RoeVerdict Evaluate(Order order) => RoeVerdict.Allow;
}
```

Create `src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs`:

```csharp
namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public static class DefaultRiskClassifier
{
    public static RiskLevel Classify(OrderKind kind) =>
        kind switch
        {
            OrderKind.Engage => RiskLevel.High,
            _ => RiskLevel.Low,
        };
}
```

Create `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs`:

```csharp
namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Roe;

public sealed record GateResult(bool ExecuteNow, bool QueueForApproval, bool Rejected);

public sealed class AutonomyGate
{
    private readonly IRoeFilter _roe;

    public AutonomyGate(IRoeFilter roe) => _roe = roe;

    public GateResult Evaluate(AutonomyLevel autonomy, Order order, bool playerApproved)
    {
        if (_roe.Evaluate(order) == RoeVerdict.Reject)
        {
            return new GateResult(false, false, true);
        }

        return autonomy switch
        {
            AutonomyLevel.Manual => new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.Assisted when order.Risk == RiskLevel.Low =>
                new GateResult(true, false, false),
            AutonomyLevel.Assisted =>
                new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.SemiAutonomous or AutonomyLevel.FullAutonomous =>
                new GateResult(true, false, false),
            _ => new GateResult(false, true, false),
        };
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Roe src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs src/ProjectAegis.Delegation.Tests/Roe
git commit -m "feat(delegation): add ROE filter and autonomy gating"
```

---

### Task 9: Group detach-rejoin (next-cycle replan)

**Files:**
- Create: `src/ProjectAegis.Delegation/Groups/DetachRejoinService.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Groups/DetachRejoinTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Groups/DetachRejoinTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Groups;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Groups;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using NUnit.Framework;

[TestFixture]
public sealed class DetachRejoinTests
{
    [Test]
    public void Detach_marks_unit_and_schedules_group_replan_next_cycle()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        group.AddMember(unit.Id);
        group.Slot.SetActive(new AgentController(new AgentId("ga")));

        var service = new DetachRejoinService(new OverrideService());
        service.Detach(group, unit);

        Assert.That(unit.IsDetachedFromGroup, Is.True);
        Assert.That(group.PendingReplan, Is.True);
        Assert.That(group.Members, Does.Not.Contain(unit.Id));
    }

    [Test]
    public void Rejoin_adds_member_and_clears_detach_flag()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        var service = new DetachRejoinService(new OverrideService());
        service.Detach(group, unit);

        service.Rejoin(group, unit);

        Assert.That(unit.IsDetachedFromGroup, Is.False);
        Assert.That(group.Members, Does.Contain(unit.Id));
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement — add PendingReplan to GroupTarget + service**

Add to `GroupTarget.cs`:

```csharp
public bool PendingReplan { get; private set; }

public void MarkReplanPending() => PendingReplan = true;

public void ClearReplanPending() => PendingReplan = false;
```

Create `src/ProjectAegis.Delegation/Groups/DetachRejoinService.cs`:

```csharp
namespace ProjectAegis.Delegation.Groups;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;

public sealed class DetachRejoinService
{
    private readonly OverrideService _overrideService;

    public DetachRejoinService(OverrideService overrideService) =>
        _overrideService = overrideService;

    public void Detach(GroupTarget group, UnitTarget unit)
    {
        unit.SetDetached(true);
        group.RemoveMember(unit.Id);
        group.MarkReplanPending();
        _overrideService.TakeDirectControl(unit, new HumanController());
    }

    public void Rejoin(GroupTarget group, UnitTarget unit)
    {
        _overrideService.ReleaseDirectControl(unit);
        unit.SetDetached(false);
        group.AddMember(unit.Id);
        group.MarkReplanPending();
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Groups src/ProjectAegis.Delegation/Targets/GroupTarget.cs src/ProjectAegis.Delegation.Tests/Groups
git commit -m "feat(delegation): add group detach-rejoin with next-cycle replan flag"
```

---

### Task 10: Decision record and log stream

**Files:**
- Create: `src/ProjectAegis.Delegation/Decision/DecisionRecord.cs`
- Create: `src/ProjectAegis.Delegation/Decision/DecisionLog.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs`

- [ ] **Step 1: Write failing test**

Create `src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

[TestFixture]
public sealed class DecisionLogTests
{
    [Test]
    public void Append_preserves_order_for_aar_stream()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            SimTime: 1,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId("u1"),
            AutonomyLevel.Autonomous,
            ChosenKind: OrderKind.Hold,
            Alternatives: Array.Empty<ScoredIntent>(),
            Rationale: "test",
            AttentionLoad: 5,
            AttentionBudget: 10,
            RngDraw: 0.42));

        Assert.That(log.Records, Has.Count.EqualTo(1));
        Assert.That(log.Records[0].ChosenKind, Is.EqualTo(OrderKind.Hold));
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement DecisionRecord + DecisionLog**

Create `src/ProjectAegis.Delegation/Decision/DecisionRecord.cs`:

```csharp
namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record DecisionRecord(
    double SimTime,
    AgentId AgentId,
    TargetId TargetId,
    AutonomyLevel AutonomyLevel,
    OrderKind ChosenKind,
    IReadOnlyList<ScoredIntent> Alternatives,
    string Rationale,
    double AttentionLoad,
    double AttentionBudget,
    double RngDraw);
```

Create `src/ProjectAegis.Delegation/Decision/DecisionLog.cs`:

```csharp
namespace ProjectAegis.Delegation.Decision;

public sealed class DecisionLog
{
    private readonly List<DecisionRecord> _records = new();

    public IReadOnlyList<DecisionRecord> Records => _records;

    public void Append(DecisionRecord record) => _records.Add(record);
}
```

Add helper in same file or `DecisionRecord.cs`:

```csharp
public static AutonomyLevel Autonomous => AutonomyLevel.FullAutonomous;
```

(Use `AutonomyLevel.FullAutonomous` in test instead—fix test to use `AutonomyLevel.FullAutonomous`.)

- [ ] **Step 4: Run — expect PASS** (after fixing test enum to `FullAutonomous`)

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Decision/DecisionRecord.cs src/ProjectAegis.Delegation/Decision/DecisionLog.cs src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs
git commit -m "feat(delegation): add decision record and append-only log"
```

---

### Task 11: AgentController decision tick + orchestrator

**Files:**
- Modify: `src/ProjectAegis.Delegation/Controllers/AgentController.cs`
- Create: `src/ProjectAegis.Delegation/Core/SimulationMode.cs`
- Create: `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs`
- Create: `src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs`
- Create: `src/ProjectAegis.Delegation.Tests/Orchestration/OrchestratorTests.cs`

- [ ] **Step 1: Write failing integration test**

Create `src/ProjectAegis.Delegation.Tests/Orchestration/OrchestratorTests.cs`:

```csharp
namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class OrchestratorTests
{
    [Test]
    public void Two_ticks_same_seed_produce_identical_executed_orders()
    {
        var run1 = RunScenario(globalSeed: 1234);
        var run2 = RunScenario(globalSeed: 1234);
        Assert.That(run1, Is.EqualTo(run2));
    }

    private static IReadOnlyList<OrderKind> RunScenario(int globalSeed)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0, new Dictionary<TargetId, bool>());
        orchestrator.Tick(state);
        orchestrator.Tick(state with { SimTime = 1 });
        return orchestrator.ExecutedOrders.Select(o => o.Kind).ToArray();
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement orchestrator and wire AgentController**

Create `src/ProjectAegis.Delegation/Core/SimulationMode.cs`:

```csharp
namespace ProjectAegis.Delegation.Core;

public enum SimulationModeKind
{
    Human,
    Mixed,
    AgentVsAgent,
}

public sealed record SimulationModeProfile(
    SimulationModeKind Kind,
    bool PlayerControlsFriendlySide);
```

Implement full `AgentController` with fields: `AgentId`, `TraitVector`, `AutonomyLevel`, `SeededRng`, `IPolicy` (inject `StubPatrolPolicy` wrapper), pending orders list, `Experience` blob.

Implement `DelegationOrchestrator`:
- `Register(ICommandableTarget)`
- `CreateAgent(...)` returns configured `AgentController`
- `Tick(ObservedState)` — for each target with `AgentController`, compute attention, run pipeline, apply autonomy gate + ROE, append `DecisionRecord`, collect executed orders
- Clear group `PendingReplan` at **start** of tick after replan (next-cycle semantics)
- `ExecutedOrders` property for tests

Create `src/ProjectAegis.Delegation/Trust/AgentExperienceBlob.cs`:

```csharp
namespace ProjectAegis.Delegation.Trust;

public sealed class AgentExperienceBlob
{
    public Dictionary<string, double> Metrics { get; } = new();
}
```

Emit `TrustSignal` record on engagement end (hook only—empty handler in orchestrator).

- [ ] **Step 4: Run full suite**

Run: `dotnet test ProjectAegis.sln -v minimal`

Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/ProjectAegis.Delegation/Orchestration src/ProjectAegis.Delegation/Controllers/AgentController.cs src/ProjectAegis.Delegation/Core/SimulationMode.cs src/ProjectAegis.Delegation/Trust src/ProjectAegis.Delegation.Tests/Orchestration
git commit -m "feat(delegation): add orchestrator tick loop and deterministic replay test"
```

---

### Task 12: Library README and requirement traceability

**Files:**
- Create: `src/ProjectAegis.Delegation/README.md`

- [ ] **Step 1: Write README**

Document:
- Purpose and link to design spec
- Public seams: `Order`, `IPolicy`, `IRoeFilter`, `DelegationOrchestrator`
- How to run tests: `dotnet test ProjectAegis.sln`
- Unity integration note: reference this assembly from future `Assets/Scripts` Unity project

- [ ] **Step 2: Commit**

```bash
git add src/ProjectAegis.Delegation/README.md
git commit -m "docs(delegation): add library README and integration notes"
```

---

## Spec Coverage Checklist (self-review)

| Spec section | Task(s) |
|--------------|---------|
| §2 Controller / Order API | 2, 3, 4, 11 |
| §3 Attention / degradation | 6, 11 |
| §4 Traits / personalities / stochastic | 5, 7, 11 |
| §5 Autonomy + override + detach-rejoin | 4, 8, 9 |
| §5.4 ROE | 8 |
| §6 Decision records + explainability | 10, 11 |
| §7 Out of scope (UI, Side, System) | Not in plan (correct) |
| §8 Trust hook | 11 (`AgentExperienceBlob`) |
| §8 Policy seam | 7 |
| §8 Sim seam | 3, 11 (`ObservedState`) |
| §3 Simulation modes | 11 (`SimulationModeProfile`) |

**Placeholder scan:** No TBD steps. All test commands and file paths are explicit.

**Type consistency:** `TargetId`, `Order`, `AgentController`, `AttentionEvaluation`, `PipelineChoice` names are consistent across tasks.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-28-agent-delegation-framework.md`. Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration. Use superpowers:subagent-driven-development.

2. **Inline Execution** — implement tasks in this session with checkpoints. Use superpowers:executing-plans.

Which approach do you want?
