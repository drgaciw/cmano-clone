namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class TargetRegistryTests
{
    // BUG regression (qa-r2-08-unity-adapter): TargetRegistry.Register only guarded against
    // duplicate EntityKey registration. Registering two different sim entities under the same
    // string targetKey silently overwrote the TargetId -> binding mapping in `_byTarget` and
    // appended a second, duplicate TargetId into the `_memberIds` list. That list is exactly
    // what OobTreeBridge/MapPictureBridge/UnitDetailBridge feed into `registry.CollectMemberIds()`
    // -> OobTreeProjection.Project (no de-dup) -> the C2 OOB tree / map picture, so the presentation
    // layer would silently render the *same* unit id twice for what are actually two distinct
    // Unity/ECS entities, and TryGetBinding(TargetId) would resolve every future order for that
    // target id to whichever entity registered last, silently dropping orders for the other one.
    [Test]
    public void RegisterUnit_with_duplicate_target_key_throws_instead_of_corrupting_registry()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var registry = bridge.Registry;

        registry.RegisterUnit(new EntityKey(1), "u1");

        Assert.Throws<InvalidOperationException>((Action)(() =>
            registry.RegisterUnit(new EntityKey(2), "u1")));
    }

    [Test]
    public void RegisterGroup_with_duplicate_target_key_throws_instead_of_corrupting_registry()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var registry = bridge.Registry;

        registry.RegisterGroup(new EntityKey(1), "g1");

        Assert.Throws<InvalidOperationException>((Action)(() =>
            registry.RegisterGroup(new EntityKey(2), "g1")));
    }

    [Test]
    public void CollectMemberIds_never_contains_duplicate_target_ids_after_failed_duplicate_registration()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var registry = bridge.Registry;

        registry.RegisterUnit(new EntityKey(1), "u1");
        Assert.Throws<InvalidOperationException>((Action)(() =>
            registry.RegisterUnit(new EntityKey(2), "u1")));

        var memberIds = registry.CollectMemberIds();
        Assert.That(memberIds, Has.Count.EqualTo(1),
            "a rejected duplicate registration must not leave a phantom duplicate entry in the member-id list " +
            "that OobTreeProjection/MapPictureBridge render into the OOB tree / map picture");
    }
}
