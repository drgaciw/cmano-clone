namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// DRG-73: platform_design_propose — draft + stage a peer-relative platform via CatalogWriteGate.
/// No auto-approve. Actor defaults to agent / platform-design-assistant.
/// </summary>
public static class PlatformDesignProposeCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string[] args, TextWriter output)
    {
        string? db = null;
        string id = "new-platform";
        string name = "New Platform";
        string domain = "surface";
        string role = "standard";
        string concept = "";
        var whatIf = true;
        var peers = new List<string>();
        string actorType = "agent";
        string actorId = "platform-design-assistant";
        long clockTicks = 0;
        var draftOnly = false;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--db" when i + 1 < args.Length:
                    db = args[++i];
                    break;
                case "--id" when i + 1 < args.Length:
                    id = args[++i];
                    break;
                case "--name" when i + 1 < args.Length:
                    name = args[++i];
                    break;
                case "--domain" when i + 1 < args.Length:
                    domain = args[++i];
                    break;
                case "--role" when i + 1 < args.Length:
                    role = args[++i];
                    break;
                case "--concept" when i + 1 < args.Length:
                    concept = args[++i];
                    break;
                case "--peer" when i + 1 < args.Length:
                    peers.Add(args[++i]);
                    break;
                case "--actor-type" when i + 1 < args.Length:
                    actorType = args[++i];
                    break;
                case "--actor-id" when i + 1 < args.Length:
                    actorId = args[++i];
                    break;
                case "--clock" when i + 1 < args.Length:
                    long.TryParse(args[++i], out clockTicks);
                    break;
                case "--what-if":
                    whatIf = true;
                    break;
                case "--no-what-if":
                    whatIf = false;
                    break;
                case "--draft-only":
                    draftOnly = true;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(db))
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                ok = false,
                verb = "platform_design_propose",
                error = "--db <catalog.db> required",
            }, JsonOptions));
            return 1;
        }

        if (!File.Exists(db))
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                ok = false,
                verb = "platform_design_propose",
                error = $"catalog db not found: {db}",
            }, JsonOptions));
            return 1;
        }

        try
        {
            using var catalog = new SqliteCatalogReader(db, "platform-design-propose");
            var brief = new PlatformDesignBrief(
                PlatformId: id,
                DisplayName: name,
                Domain: domain,
                RoleWeight: role,
                Concept: concept,
                WhatIf: whatIf,
                PeerPlatformIds: peers.Count > 0 ? peers : null);

            var assistant = new PlatformDesignAssistant();
            if (draftOnly)
            {
                var draft = assistant.Draft(catalog, brief);
                output.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = true,
                    verb = "platform_design_propose",
                    mode = "draft",
                    platformId = draft.Binding.PlatformId,
                    displayName = draft.Binding.DisplayName,
                    whatIf = draft.WhatIf,
                    combatRadiusNm = draft.CombatRadiusNm,
                    maxHp = draft.Damage.MaxHp,
                    maxSpeedKnots = draft.Mobility.MaxSpeedKnots,
                    peers = draft.Peers.Select(p => new { p.PlatformId, p.Score }),
                    basis = draft.Basis.Select(b => new { b.Field, b.Value, b.Method }),
                    outliers = draft.Outliers,
                    skillsApplied = draft.SkillsApplied,
                    summary = draft.Summary,
                }, JsonOptions));
                return 0;
            }

            var result = assistant.Propose(
                db,
                catalog,
                brief,
                new FixedCatalogClock(clockTicks),
                actorType,
                actorId);

            output.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                verb = "platform_design_propose",
                mode = "propose",
                platformId = result.Proposal.Binding.PlatformId,
                displayName = result.Proposal.Binding.DisplayName,
                whatIf = result.Proposal.WhatIf,
                platformBatchId = result.PlatformBatchId,
                damageBatchId = result.DamageBatchId,
                mobilityBatchId = result.MobilityBatchId,
                batchIds = result.BatchIds,
                peers = result.Proposal.Peers.Select(p => new { p.PlatformId, p.Score }),
                skillsApplied = result.Proposal.SkillsApplied,
                notes = result.Notes,
                summary = result.Proposal.Summary,
            }, JsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                ok = false,
                verb = "platform_design_propose",
                error = ex.Message,
            }, JsonOptions));
            return 2;
        }
    }
}
