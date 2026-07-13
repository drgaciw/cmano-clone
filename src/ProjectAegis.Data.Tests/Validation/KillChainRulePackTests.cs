using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using Xunit;

namespace ProjectAegis.Data.Tests.Validation;

/// <summary>S33-03 / DBI-3.5 kill-chain impossibility rule pack.</summary>
public sealed class KillChainRulePackTests
{
    [Fact]
    public void KillChain_R1_orphan_edge_flags_missing_platform()
    {
        var reader = WithEdges(
            InMemoryCatalogReader.BalticPatrolFixture(),
            [new CatalogDependencyEdge("ghost-platform", "mount-a", "weapon-a", "")]);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f =>
            f.Code == KillChainRules.OrphanEdgeCode &&
            f.Severity == "error" &&
            f.Message.Contains("ghost-platform", StringComparison.Ordinal));
    }

    [Fact]
    public void KillChain_R1_orphan_edge_flags_missing_mount()
    {
        var inner = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainShortRange);
        var edges = inner.GetSortedDependencyEdges()
            .Append(new CatalogDependencyEdge("u1", "missing-mount", CatalogWeaponIds.KillChainShortRange, ""))
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.MountId, StringComparer.Ordinal)
            .ThenBy(e => e.WeaponId, StringComparer.Ordinal)
            .ThenBy(e => e.SensorId, StringComparer.Ordinal)
            .ToArray();
        var reader = WithEdges(inner, edges);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f =>
            f.Code == KillChainRules.OrphanEdgeCode &&
            f.Message.Contains("missing-mount", StringComparison.Ordinal));
    }

    [Fact]
    public void KillChain_R1_orphan_edge_flags_missing_weapon()
    {
        var reader = WithEdges(
            BuildWeaponChainReader(
                combatRadiusNm: 400,
                maxSpeedKnots: 32,
                weaponId: CatalogWeaponIds.KillChainShortRange),
            [new CatalogDependencyEdge("u1", "vls-fwd", "unknown-weapon", "")]);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f =>
            f.Code == KillChainRules.OrphanEdgeCode &&
            f.Message.Contains("unknown-weapon", StringComparison.Ordinal));
    }

    [Fact]
    public void KillChain_R1_orphan_edge_flags_missing_sensor()
    {
        var reader = WithEdges(
            InMemoryCatalogReader.BalticPatrolFixture(),
            [new CatalogDependencyEdge("u1", "", "", "missing-sensor")]);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f =>
            f.Code == KillChainRules.OrphanEdgeCode &&
            f.Message.Contains("missing-sensor", StringComparison.Ordinal));
    }

    [Fact]
    public void KillChain_R2_range_exceeds_sensor_when_weapon_outranges_envelope()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainLongRange,
            basePd: 0.1);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f => f.Code == KillChainRules.RangeExceedsSensorCode && f.Severity == "error");
    }

    [Fact]
    public void KillChain_R2_boundary_short_range_weapon_passes_sensor_envelope()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainShortRange,
            basePd: 0.75);

        var findings = KillChainRules.Evaluate(reader);

        Assert.DoesNotContain(findings, f => f.Code == KillChainRules.RangeExceedsSensorCode);
    }

    [Fact]
    public void KillChain_R3_speed_mismatch_when_hypersonic_weapon_exceeds_platform_speed()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            basePd: 1.0);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f =>
            f.Code == KillChainRules.SpeedMismatchCode &&
            f.Severity == "error" &&
            f.Message.Contains("weapon_min_speed_kts", StringComparison.Ordinal));
    }

    [Fact]
    public void KillChain_R3_missing_mobility_emits_warning_not_error()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            includeMobility: false);

        var findings = KillChainRules.Evaluate(reader);

        var speedFinding = Assert.Single(findings, f => f.Code == KillChainRules.SpeedMismatchCode);
        Assert.Equal("warning", speedFinding.Severity);
        Assert.Contains("mobility missing", speedFinding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void KillChain_R4_weapon_exceeds_platform_reach_when_range_beyond_combat_radius()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 50,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainLongRange,
            basePd: 1.0);

        var findings = KillChainRules.Evaluate(reader);

        Assert.Contains(findings, f => f.Code == KillChainRules.WeaponExceedsReachCode && f.Severity == "error");
    }

    [Fact]
    public void KillChain_R4_boundary_weapon_within_platform_reach_passes()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 400,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainShortRange,
            basePd: 1.0);

        var findings = KillChainRules.Evaluate(reader);

        Assert.DoesNotContain(findings, f => f.Code == KillChainRules.WeaponExceedsReachCode);
    }

    [Fact]
    public void KillChain_Baltic_patrol_golden_hash_stable_on_clean_fixture()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var findings = KillChainRules.Evaluate(reader);

        Assert.Empty(findings);
        Assert.Equal(KillChainGoldenHashes.BalticPatrolClean, KillChainRules.ComputeFindingsHash(findings));
        Assert.Equal(
            KillChainRules.ComputeFindingsHash(findings),
            KillChainRules.ComputeFindingsHash(KillChainRules.Evaluate(reader)));
    }

    [Fact]
    public void KillChain_findings_are_deterministically_sorted()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 50,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            basePd: 0.1);

        var first = KillChainRules.Evaluate(reader);
        var second = KillChainRules.Evaluate(reader);

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }

        Assert.True(first
            .Zip(first.Skip(1))
            .All(pair => string.Compare(pair.First.Code, pair.Second.Code, StringComparison.Ordinal) <= 0));
    }

    [Fact]
    public void KillChain_quarantined_sensor_edges_excluded_from_checks()
    {
        var reader = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved),
                new CatalogSensorBinding("u1", "radar-quarantine", 0.05, ReviewState: CatalogReviewStates.Provisional),
            ],
            "kill-chain-quarantine",
            [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)],
            mounts: [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            magazines:
            [
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.KillChainLongRange, 4),
            ]);

        var edges = reader.GetSortedDependencyEdges();
        Assert.DoesNotContain(edges, e => e.SensorId == "radar-quarantine");

        var findings = KillChainRules.Evaluate(reader);
        Assert.DoesNotContain(findings, f => f.Message.Contains("radar-quarantine", StringComparison.Ordinal));
    }

    [Fact]
    public void CatalogRulesValidationAgent_appends_kill_chain_findings_after_rule_gate()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 50,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            basePd: 0.1);

        var report = new CatalogRulesValidationAgent().Run(new DatabaseAgentContext(reader));

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, f => f.Code == "KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH");
        Assert.Contains(report.Findings, f => f.Code.StartsWith("KILL_CHAIN_", StringComparison.Ordinal));
    }

    [Fact]
    public void CrossSystem_orchestrator_Baltic_default_has_no_kill_chain_codes_on_clean_catalog()
    {
        var result = DatabaseIntelligenceOrchestrator.RunBalticDefault();
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");

        Assert.DoesNotContain(rules.Findings, f => f.Code.StartsWith("KILL_CHAIN_", StringComparison.Ordinal));
    }

    [Fact]
    public void CrossSystem_orchestrator_surfaces_kill_chain_codes_on_curated_fixture()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 50,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            basePd: 0.1);

        var result = new DatabaseIntelligenceOrchestrator().Run(reader);
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");
        var killChainCodes = rules.Findings
            .Where(f => f.Code.StartsWith("KILL_CHAIN_", StringComparison.Ordinal))
            .Select(f => f.Code)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(3, killChainCodes.Length);
        Assert.Contains(killChainCodes, c => c == KillChainRules.RangeExceedsSensorCode);
        Assert.Contains(killChainCodes, c => c == KillChainRules.SpeedMismatchCode);
        Assert.Contains(killChainCodes, c => c == KillChainRules.WeaponExceedsReachCode);
    }

    [Fact]
    public void CatalogRules_detect_only_does_not_mutate_catalog_on_report_path()
    {
        var reader = BuildWeaponChainReader(
            combatRadiusNm: 50,
            maxSpeedKnots: 32,
            weaponId: CatalogWeaponIds.KillChainHypersonic,
            basePd: 0.1);

        var mountsBefore = reader.GetSortedMounts().Count;
        var magazinesBefore = reader.GetSortedMagazines().Count;
        var edgesBefore = reader.GetSortedDependencyEdges().Count;

        _ = new CatalogRulesValidationAgent().Run(new DatabaseAgentContext(reader));

        Assert.Equal(mountsBefore, reader.GetSortedMounts().Count);
        Assert.Equal(magazinesBefore, reader.GetSortedMagazines().Count);
        Assert.Equal(edgesBefore, reader.GetSortedDependencyEdges().Count);
    }

    private static InMemoryCatalogReader BuildWeaponChainReader(
        double combatRadiusNm,
        double maxSpeedKnots,
        string weaponId,
        string mountId = "vls-fwd",
        double basePd = 0.75,
        bool includeMobility = true)
    {
        CatalogMobility[] mobility = includeMobility
            ? [new CatalogMobility("u1", MaxSpeedKnots: maxSpeedKnots, RangeNm: 4200)]
            : [];

        return new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", basePd, ReviewState: CatalogReviewStates.Approved)],
            "kill-chain-fixture",
            [new CatalogPlatformEntry("u1", 57.0, 20.0, combatRadiusNm)],
            mobility: mobility,
            mounts: [new CatalogMount("u1", mountId, ReviewState: CatalogReviewStates.Approved)],
            magazines:
            [
                new CatalogMagazineEntry("u1", "asuw-default", mountId, weaponId, 4),
            ]);
    }

    private static ICatalogReader WithEdges(
        InMemoryCatalogReader inner,
        IReadOnlyList<CatalogDependencyEdge> edges) =>
        new FixedDependencyEdgesReader(inner, edges);

    private sealed class FixedDependencyEdgesReader(InMemoryCatalogReader inner, IReadOnlyList<CatalogDependencyEdge> edges)
        : ICatalogReader
    {
        public string LayerVersion => inner.LayerVersion;

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => inner.GetSortedSensorBindings();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd) =>
            inner.TryGetBasePd(platformId, sensorId, out basePd);

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId) =>
            inner.TryResolveDbRef(dbRef, out resolvedSnapshotId);

        public bool TryGetSnapshotBranch(string snapshotId, out string branch) =>
            inner.TryGetSnapshotBranch(snapshotId, out branch);

        public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef) =>
            inner.TryResolveSnapshotForTlBranch(tlBranch, out snapshotId, out dbRef);

        public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm) =>
            inner.TryGetCombatRadiusNm(platformId, out combatRadiusNm);

        public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg) =>
            inner.TryGetPlatformPosition(platformId, out latDeg, out lonDeg);

        public bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope) =>
            inner.TryGetWeaponEnvelope(weaponId, out envelope);

        public IReadOnlyList<CatalogMobility> GetSortedMobility() => inner.GetSortedMobility();

        public IReadOnlyList<CatalogSignature> GetSortedSignatures() => inner.GetSortedSignatures();

        public IReadOnlyList<CatalogEmcon> GetSortedEmcon() => inner.GetSortedEmcon();

        public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => inner.GetSortedPlatformDamage();

        public IReadOnlyList<CatalogMount> GetSortedMounts() => inner.GetSortedMounts();

        public IReadOnlyList<CatalogLoadout> GetSortedLoadouts() => inner.GetSortedLoadouts();

        public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines() => inner.GetSortedMagazines();

        public IReadOnlyList<CatalogDependencyEdge> GetSortedDependencyEdges() => edges;

        public bool TryGetMobility(string platformId, out CatalogMobility mobility) =>
            inner.TryGetMobility(platformId, out mobility);

        public bool TryGetSignature(string platformId, out CatalogSignature signature) =>
            inner.TryGetSignature(platformId, out signature);

        public bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon) =>
            inner.TryGetEmcon(platformId, condition, emitterId, out emcon);

        public bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage) =>
            inner.TryGetPlatformDamage(platformId, out damage);
    }
}