-- SWARM-21 PE chrome: staging table for Swarms workbook sheet write-gate round-trip.
-- Idempotency: ShouldSkipMigration("014" + catalog_staging_swarm exists).

CREATE TABLE IF NOT EXISTS catalog_staging_swarm (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    is_swarm INTEGER NOT NULL DEFAULT 1 CHECK (is_swarm IN (0, 1)),
    max_drones INTEGER NOT NULL CHECK (max_drones > 0),
    armor_class TEXT NOT NULL DEFAULT 'light-air',
    default_sensor_id TEXT NOT NULL DEFAULT '',
    default_weapon_id TEXT NOT NULL DEFAULT '',
    default_mode TEXT NOT NULL DEFAULT 'Hold',
    requires_host INTEGER NOT NULL DEFAULT 0 CHECK (requires_host IN (0, 1)),
    allowed_host_classes TEXT NOT NULL DEFAULT '',
    cec_capable INTEGER NOT NULL DEFAULT 0 CHECK (cec_capable IN (0, 1)),
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE INDEX IF NOT EXISTS idx_staging_swarm_batch
    ON catalog_staging_swarm (batch_id ASC, platform_id ASC);
