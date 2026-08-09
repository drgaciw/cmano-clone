-- DRG-73 PDA follow-up: preserve scaled Lat/Lon/CombatRadius through propose → approve.
-- Additive columns on catalog_staging_platform.
-- Idempotency: ShouldSkipMigration("015" + apply_core_position column).

ALTER TABLE catalog_staging_platform ADD COLUMN lat_deg REAL NOT NULL DEFAULT 0;
ALTER TABLE catalog_staging_platform ADD COLUMN lon_deg REAL NOT NULL DEFAULT 0;
ALTER TABLE catalog_staging_platform ADD COLUMN combat_radius_nm REAL NOT NULL DEFAULT 0;
ALTER TABLE catalog_staging_platform ADD COLUMN apply_core_position INTEGER NOT NULL DEFAULT 0 CHECK (apply_core_position IN (0, 1));
