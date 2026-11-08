# Platform workbook contract (exporter schema `010`)

> **S125-06 / AUTH-08.** This table is the documented contract for the Platform Editor workbook
> (Req-21 / ADR-011, Excel-primary). It is enforced in CI by
> `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookContractTests.cs`, which fails when the code
> contract (`PlatformWorkbookContract`), the exporter (`PlatformWorkbookExporter`), the importer's
> stageable-change classification (`PlatformWorkbookImporter.Plan`) or this table drift apart.
> Doc 21's sheet table is illustrative; this page is the exact, shipped column list.

Column order matters: the exporter writes these headers in this order and the diff compares headers
by sequence. The `_Meta` sheet (`Key` / `Value` binding rows) is exempt and never imported.

<!-- contract:start -->
| Sheet | Columns (ordered) | Stageable columns |
|-------|-------------------|-------------------|
| `Platforms` | `PlatformId`, `LatDeg`, `LonDeg`, `CombatRadiusNm`, `MaxHp`, `WithdrawThresholdPct`, `CriticalFlags` | `MaxHp`, `WithdrawThresholdPct`, `CriticalFlags` |
| `Sensors` | `PlatformId`, `SensorId`, `BasePd`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` | `PlatformId`, `SensorId`, `BasePd`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` |
| `Mounts` | `PlatformId`, `MountId`, `MountType`, `ArcDeg`, `Capacity`, `ReviewState` | `PlatformId`, `MountId`, `MountType`, `ArcDeg`, `Capacity`, `ReviewState` |
| `Loadouts` | `PlatformId`, `LoadoutId`, `LoadoutName`, `Role`, `IsDefault` | `PlatformId`, `LoadoutId`, `LoadoutName`, `Role`, `IsDefault` |
| `Magazines` | `PlatformId`, `LoadoutId`, `MountId`, `WeaponId`, `Quantity`, `ReloadTimeSec`, `Depth` | `PlatformId`, `LoadoutId`, `MountId`, `WeaponId`, `Quantity`, `ReloadTimeSec`, `Depth` |
| `Comms` | `PlatformId`, `LinkId`, `Role`, `SatcomCapable`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` | `PlatformId`, `LinkId`, `Role`, `SatcomCapable`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` |
| `LinkCatalog` | `LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal` | `LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal` |
| `Mobility` | `PlatformId`, `MaxSpeedKnots`, `CruiseSpeedKnots`, `MaxAltitudeFt`, `MaxDepthM`, `FuelCapacity`, `RangeNm`, `EnduranceHr` | `PlatformId`, `MaxSpeedKnots`, `CruiseSpeedKnots`, `MaxAltitudeFt`, `MaxDepthM`, `FuelCapacity`, `RangeNm`, `EnduranceHr` |
| `Signatures` | `PlatformId`, `RcsBandDbsm`, `IrSignature`, `AcousticSignatureDb`, `MagneticSignature` | `PlatformId`, `RcsBandDbsm`, `IrSignature`, `AcousticSignatureDb`, `MagneticSignature` |
| `Emcon` | `PlatformId`, `Condition`, `EmitterId`, `Posture` | `PlatformId`, `Condition`, `EmitterId`, `Posture` |
| `Swarms` | `PlatformId`, `IsSwarm`, `MaxDrones`, `ArmorClass`, `DefaultSensorId`, `DefaultWeaponId`, `DefaultMode`, `RequiresHost`, `AllowedHostClasses`, `CecCapable`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` | `PlatformId`, `IsSwarm`, `MaxDrones`, `ArmorClass`, `DefaultSensorId`, `DefaultWeaponId`, `DefaultMode`, `RequiresHost`, `AllowedHostClasses`, `CecCapable`, `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` |
<!-- contract:end -->

`Platforms.LatDeg` / `LonDeg` are scenario placement (doc 11) and `CombatRadiusNm` has no write-gate
path yet; edits to them are reported, never staged. Invalid lat/lon cells raise the non-blocking
`PLE-PLT-LATLON` warning with the accepted range and decimal-degree format.

## Silent-drop counts

Every import plan carries `PlatformImportPlan.DropCounts` (`PlatformImportDropCounts`), and staging
appends its `Summary` to the import notes (and the `platform_import_xlsx` JSON `dropCounts` field)
whenever anything is ignored:

| Bucket | Counted when |
|--------|--------------|
| `UnknownSheetRows` / `UnknownSheets` | a sheet is not in the contract (e.g. `SensorCatalog`) |
| `MissingSheets` | a documented sheet is absent |
| `HeaderDriftRows` | a sheet's header differs from the contract, so its cell edits cannot be evaluated |
| `RemovedRowsNotStaged` | a row was deleted (no write-gate delete path) |
| `QuarantinedRows` | FK / orphan / TRL quarantine (see `PlatformImportQuarantineEntry`) |
| `UnknownColumnFields` | non-empty cells under a column that is not in the contract |
| `OverflowFields` | non-empty cells beyond the header width |
| `UnstageableEdits` | cell edits / row adds on non-stageable columns (Platforms core) |
