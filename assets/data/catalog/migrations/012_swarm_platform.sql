-- SWARM-01 / SWARM-02 / SWARM-21 (Phase A schema): drone swarm platform catalog rows.
-- Distinct from SwarmTier (req-09 near-future entity-count tiers) and SwarmSalvoDeconfliction (salvo slots).
-- Additive only. Deterministic reads via ORDER BY platform_id.
-- Idempotency guarded by SqliteCatalogReader.ShouldSkipMigration ("012" + platform_swarm exists).
-- PE/PDA chrome for these columns is Phase B (documented gap under production/qa/).

CREATE TABLE IF NOT EXISTS platform_swarm (
    platform_id TEXT PRIMARY KEY,
    is_swarm INTEGER NOT NULL DEFAULT 1 CHECK (is_swarm IN (0, 1)),
    max_drones INTEGER NOT NULL CHECK (max_drones > 0),
    armor_class TEXT NOT NULL DEFAULT 'light-air',
    default_sensor_id TEXT NOT NULL DEFAULT '',
    default_weapon_id TEXT NOT NULL DEFAULT '',
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_platform_swarm_id
    ON platform_swarm (platform_id ASC);
