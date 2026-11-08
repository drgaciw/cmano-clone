namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Immutable text section for the command-review view; contains no authority handles.</summary>
public sealed record CommandReviewSection(string Id, string Title, IReadOnlyList<string> Lines);
