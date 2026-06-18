-- Req-21 Platform Editor Phase B: signatures, mobility/propulsion, EMCON (export-only spike; import deferred).
-- Additive only. Deterministic reads via ORDER BY canonical keys. Provenance columns mirror Phase A fitting tables.
-- Idempotency guarded by SqliteCatalogReader.ShouldSkipMigration ("008" + platform_mobility exists).

-- Platform mobility / propulsion (one row per platform class).
CREATE TABLE IF NOT EXISTS platform_mobility (
    platform_id TEXT PRIMARY KEY,
    max_speed_knots REAL NOT NULL DEFAULT 0 CHECK (max_speed_knots >= 0),
    cruise_speed_knots REAL NOT NULL DEFAULT 0 CHECK (cruise_speed_knots >= 0),
    max_altitude_ft REAL NOT NULL DEFAULT 0 CHECK (max_altitude_ft >= 0),
    max_depth_m REAL NOT NULL DEFAULT 0 CHECK (max_depth_m >= 0),
    fuel_capacity REAL NOT NULL DEFAULT 0 CHECK (fuel_capacity >= 0),
    range_nm REAL NOT NULL DEFAULT 0 CHECK (range_nm >= 0),
    endurance_hr REAL NOT NULL DEFAULT 0 CHECK (endurance_hr >= 0),
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT ''
);

-- Platform signatures (one row per platform class).
CREATE TABLE IF NOT EXISTS platform_signature (
    platform_id TEXT PRIMARY KEY,
    rcs_band_dbsm REAL NOT NULL DEFAULT 0,
    ir_signature REAL NOT NULL DEFAULT 0,
    acoustic_signature_db REAL NOT NULL DEFAULT 0,
    magnetic_signature REAL NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT ''
);

-- Platform EMCON profiles (per platform, condition, emitter).
CREATE TABLE IF NOT EXISTS platform_emcon (
    platform_id TEXT NOT NULL,
    condition TEXT NOT NULL DEFAULT 'silent',       -- silent | restricted | free
    emitter_id TEXT NOT NULL DEFAULT '',
    posture TEXT NOT NULL DEFAULT 'off',            -- off | standby | active
    review_state TEXT NOT NULL DEFAULT 'provisional',
    PRIMARY KEY (platform_id, condition, emitter_id)
);

CREATE INDEX IF NOT EXISTS idx_platform_emcon_platform
    ON platform_emcon (platform_id ASC, condition ASC, emitter_id ASC);

-- S23-05: write-gate staging tables (additive; commit path deferred to Sprint 24).
CREATE TABLE IF NOT EXISTS catalog_staging_mobility (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    max_speed_knots REAL NOT NULL DEFAULT 0 CHECK (max_speed_knots >= 0),
    cruise_speed_knots REAL NOT NULL DEFAULT 0 CHECK (cruise_speed_knots >= 0),
    max_altitude_ft REAL NOT NULL DEFAULT 0 CHECK (max_altitude_ft >= 0),
    max_depth_m REAL NOT NULL DEFAULT 0 CHECK (max_depth_m >= 0),
    fuel_capacity REAL NOT NULL DEFAULT 0 CHECK (fuel_capacity >= 0),
    range_nm REAL NOT NULL DEFAULT 0 CHECK (range_nm >= 0),
    endurance_hr REAL NOT NULL DEFAULT 0 CHECK (endurance_hr >= 0),
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_signature (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    rcs_band_dbsm REAL NOT NULL DEFAULT 0,
    ir_signature REAL NOT NULL DEFAULT 0,
    acoustic_signature_db REAL NOT NULL DEFAULT 0,
    magnetic_signature REAL NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_emcon (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    condition TEXT NOT NULL DEFAULT 'silent',
    emitter_id TEXT NOT NULL DEFAULT '',
    posture TEXT NOT NULL DEFAULT 'off',
    review_state TEXT NOT NULL DEFAULT 'provisional',
    PRIMARY KEY (batch_id, platform_id, condition, emitter_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE INDEX IF NOT EXISTS idx_staging_mobility_batch ON catalog_staging_mobility (batch_id ASC, platform_id ASC);
CREATE INDEX IF NOT EXISTS idx_staging_signature_batch ON catalog_staging_signature (batch_id ASC, platform_id ASC);
CREATE INDEX IF NOT EXISTS idx_staging_emcon_batch ON catalog_staging_emcon (batch_id ASC);