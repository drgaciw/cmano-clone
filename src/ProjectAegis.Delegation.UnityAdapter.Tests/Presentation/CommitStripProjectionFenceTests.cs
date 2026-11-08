using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// S124-02 W3-C2-02: the commit strip and refusal bark bind existing projections only
/// (ADR-010 §2–3, ADR-007, ADR-001) — no sim/session queries, no order-sink writes.
/// </summary>
[TestFixture]
public sealed class CommitStripProjectionFenceTests
{
    private static readonly string[] FencedSources =
    {
        "CommitConstraintStripBinder.cs",
        "CommitRefusalBarkBinder.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "SimulationSession",
        "DelegationOrchestrator",
        "DelegationBridge",
        "ISimWorldSnapshot",
        "IOrderSink",
        "ApplyOrder",
        "TryEnqueue",
        "HumanController",
        "DecisionLog",
        "Append",
        "CatalogWriteGate",
        "MvpEngagementResolver",
        "PolicyEvaluator",
        "BuildLiveEngageContext",
        "UnityEngine",
        "Random",
        "DateTime",
    };

    private static readonly Type[] FencedTypes =
    {
        typeof(CommitConstraintStripBinder),
        typeof(CommitRefusalBarkBinder),
    };

    private static readonly string[] AllowedParameterNamespaces =
    {
        "System",
        "System.Collections.Generic",
        "ProjectAegis.Delegation.Projection",
        "ProjectAegis.Delegation.UnityAdapter.Presentation",
    };

    private static readonly Type[] AllowedForeignParameterTypes =
    {
        typeof(ProjectAegis.Sim.Engage.EngageContext),
        typeof(Decision.OrderLogEntry),
    };

    [TestCaseSource(nameof(FencedSources))]
    public void Strip_and_bark_sources_contain_no_sim_or_session_query_tokens(string fileName)
    {
        var source = UiIaSourceReader.ReadUnder(
            "src",
            "ProjectAegis.Delegation.UnityAdapter",
            "Presentation",
            fileName);

        foreach (var token in ForbiddenTokens)
        {
            Assert.That(source, Does.Not.Contain(token), $"{fileName} must not reference {token} (ADR-010 fence).");
        }
    }

    [TestCaseSource(nameof(FencedTypes))]
    public void Public_bind_surface_accepts_projection_dtos_only(Type binder)
    {
        var methods = binder.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.That(methods, Is.Not.Empty);

        foreach (var method in methods)
        {
            foreach (var parameter in method.GetParameters())
            {
                var type = parameter.ParameterType.IsByRef
                    ? parameter.ParameterType.GetElementType()!
                    : parameter.ParameterType;
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                }

                var allowed = AllowedForeignParameterTypes.Contains(type)
                    || AllowedParameterNamespaces.Contains(type.Namespace);
                Assert.That(
                    allowed,
                    Is.True,
                    $"{binder.Name}.{method.Name}({parameter.Name}: {type.FullName}) reaches outside projection DTOs.");
            }
        }
    }

    [TestCaseSource(nameof(FencedTypes))]
    public void Binders_are_stateless_static_classes(Type binder)
    {
        Assert.That(binder.IsAbstract && binder.IsSealed, Is.True, $"{binder.Name} must be a static class.");
        var mutableStatics = binder
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => !f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.Name)
            .ToArray();
        Assert.That(mutableStatics, Is.Empty, $"{binder.Name} must hold no mutable static state.");
    }

    [Test]
    public void Bound_states_are_never_fire_orders()
    {
        Assert.That(CommitConstraintStripState.Empty.IsFireOrder, Is.False);
        Assert.That(
            CommitRefusalBarkBinder.Refused("u1", "fire-single", "NO_AMMO", simTime: 0).IsFireOrder,
            Is.False);
    }
}
