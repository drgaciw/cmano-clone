-- DRG-416 / EW-01 follow-up: persist DRG-386 radar ECCM attributes in SQLite.
-- Additive only. Defaults mean "unspecified" (no jam adjustment). Deterministic reads unchanged.
-- Idempotency: ShouldSkipMigration("018" + sensor.radar_scan_type column).

ALTER TABLE sensor ADD COLUMN radar_scan_type TEXT NOT NULL DEFAULT '';
ALTER TABLE sensor ADD COLUMN frequency_agile INTEGER NOT NULL DEFAULT 0;
ALTER TABLE sensor ADD COLUMN radar_tech_generation INTEGER NOT NULL DEFAULT 0;
