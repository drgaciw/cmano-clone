-- S111-02 / DRG-10: catalog-extend sensor modality (Radar default; IR / Visual fixtures).
-- Additive only. Deterministic reads ORDER BY platform_id, sensor_id unchanged.
-- Idempotency: ShouldSkipMigration("016" + sensor.modality column).

ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar';
