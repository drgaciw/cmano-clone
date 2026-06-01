namespace ProjectAegis.Delegation.Traits;

public sealed record PersonalityPreset(
    string Name,
    TraitVector Traits,
    double AttentionBudgetMultiplier = 1.0);

public static class PersonalityCatalog
{
    public const double DefaultAttentionBudget = 20.0;

    public static IReadOnlyList<PersonalityPreset> All { get; } =
    [
        new PersonalityPreset("Aggressive", new TraitVector(0.9, 0.8, 0.2, 0.15, 0.6, 0.8)),
        new PersonalityPreset("Defensive", new TraitVector(0.2, 0.3, 0.3, 0.08, 0.7, 0.5)),
        new PersonalityPreset("Cautious", new TraitVector(0.3, 0.2, 0.5, 0.05, 0.8, 0.3)),
        new PersonalityPreset("Opportunistic", new TraitVector(0.6, 0.6, 0.25, 0.12, 0.65, 0.7)),
        new PersonalityPreset("SwarmCoordinator", new TraitVector(0.5, 0.5, 0.15, 0.1, 0.75, 0.85), 1.25),
        new PersonalityPreset("EwSpecialist", new TraitVector(0.4, 0.4, 0.2, 0.07, 0.85, 0.6), 0.9),
    ];

    public static double ResolveAttentionBudget(PersonalityPreset preset) =>
        DefaultAttentionBudget * preset.AttentionBudgetMultiplier;
}
