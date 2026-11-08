namespace ProjectAegis.Data.Import;

/// <summary>Quarantine row for orphan mount/weapon FK during CMO platform fitting import (S27-03 / PLE-2.3).</summary>
public sealed record CmoMarkdownFittingQuarantineEntry(
    string PlatformId,
    string LoadoutId,
    string MountId,
    string WeaponRef,
    string Reason,
    string SourceFile);