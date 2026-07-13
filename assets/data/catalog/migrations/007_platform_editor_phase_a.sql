-- Req-21 Platform Editor Phase A: editable fitting schema (mounts, loadouts, magazines, comms, link/weapon catalogs).
-- Additive only. Deterministic reads via ORDER BY canonical keys. Provenance columns mirror `sensor` (req-06 §6).
-- Idempotency guarded by SqliteCatalogReader.ShouldSkipMigration ("007" + platform_mount exists).

-- Platform class metadata (extends thin P0 platform row; additive ALTERs with defaults).
ALTER TABLE platform ADD COLUMN display_name TEXT NOT NULL DEFAULT '';
ALTER TABLE platform ADD COLUMN domain TEXT NOT NULL DEFAULT 'surface';        -- surface | subsurface | air | land | facility
ALTER TABLE platform ADD COLUMN platform_class TEXT NOT NULL DEFAULT '';
ALTER TABLE platform ADD COLUMN nationality TEXT NOT NULL DEFAULT '';
ALTER TABLE platform ADD COLUMN game_technology_level INTEGER NOT NULL DEFAULT 0;

-- Datalink / net type catalog (req-19).
CREATE TABLE IF NOT EXISTS link_catalog (
    link_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL DEFAULT '',
    link_type TEXT NOT NULL DEFAULT 'tactical',   -- strategic | tactical | voice | satcom
    latency_ms_nominal INTEGER NOT NULL DEFAULT 0
);

-- Weapon record catalog (extends WeaponEnvelopeDto: min/max range in meters).
CREATE TABLE IF NOT EXISTS weapon_catalog (
    weapon_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL DEFAULT '',
    min_range_meters REAL NOT NULL DEFAULT 0 CHECK (min_range_meters >= 0),
    max_range_meters REAL NOT NULL DEFAULT 0 CHECK (max_range_meters >= 0),
    weapon_type TEXT NOT NULL DEFAULT '',
    guidance TEXT NOT NULL DEFAULT ''
);

-- Platform comms / datalink fitting.
CREATE TABLE IF NOT EXISTS platform_comms (
    platform_id TEXT NOT NULL,
    link_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT 'txrx',            -- tx | rx | txrx | relay
    satcom_capable INTEGER NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (platform_id, link_id)
);

-- Mounts: hosts that carry weapons (VLS cells, rails, guns, tubes, pylons).
CREATE TABLE IF NOT EXISTS platform_mount (
    platform_id TEXT NOT NULL,
    mount_id TEXT NOT NULL,
    mount_type TEXT NOT NULL DEFAULT 'rail',      -- vls | rail | gun | tube | pylon
    arc_deg REAL NOT NULL DEFAULT 360 CHECK (arc_deg >= 0 AND arc_deg <= 360),
    capacity INTEGER NOT NULL DEFAULT 1 CHECK (capacity >= 0),
    review_state TEXT NOT NULL DEFAULT 'provisional',
    PRIMARY KEY (platform_id, mount_id)
);

-- Named loadout presets per platform.
CREATE TABLE IF NOT EXISTS platform_loadout (
    platform_id TEXT NOT NULL,
    loadout_id TEXT NOT NULL,
    loadout_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT '',                -- asuw | asw | aaw | strike | recon
    is_default INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (platform_id, loadout_id)
);

-- Magazine stores: weapon quantities loaded into a mount under a loadout.
CREATE TABLE IF NOT EXISTS platform_magazine (
    platform_id TEXT NOT NULL,
    loadout_id TEXT NOT NULL,
    mount_id TEXT NOT NULL,
    weapon_id TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    reload_time_sec INTEGER NOT NULL DEFAULT 0 CHECK (reload_time_sec >= 0),
    depth INTEGER NOT NULL DEFAULT 0 CHECK (depth >= 0),
    PRIMARY KEY (platform_id, loadout_id, mount_id, weapon_id)
);

CREATE INDEX IF NOT EXISTS idx_platform_magazine_loadout
    ON platform_magazine (platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC);

-- S22-04: write-gate staging tables (additive; DBI-1.4 orphan guard via DeleteStagingRows).
-- All new Propose* use explicit ORDER BY canonical keys for determinism (DBI-1.1/7.3).

CREATE TABLE IF NOT EXISTS catalog_staging_platform (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    display_name TEXT NOT NULL DEFAULT '',
    domain TEXT NOT NULL DEFAULT 'surface',
    platform_class TEXT NOT NULL DEFAULT '',
    nationality TEXT NOT NULL DEFAULT '',
    game_technology_level INTEGER NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_weapon (
    batch_id TEXT NOT NULL,
    weapon_id TEXT NOT NULL,
    display_name TEXT NOT NULL DEFAULT '',
    min_range_meters REAL NOT NULL DEFAULT 0 CHECK (min_range_meters >= 0),
    max_range_meters REAL NOT NULL DEFAULT 0 CHECK (max_range_meters >= 0),
    weapon_type TEXT NOT NULL DEFAULT '',
    guidance TEXT NOT NULL DEFAULT '',
    review_state TEXT NOT NULL DEFAULT 'provisional',
    PRIMARY KEY (batch_id, weapon_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_mount (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    mount_id TEXT NOT NULL,
    mount_type TEXT NOT NULL DEFAULT 'rail',
    arc_deg REAL NOT NULL DEFAULT 360 CHECK (arc_deg >= 0 AND arc_deg <= 360),
    capacity INTEGER NOT NULL DEFAULT 1 CHECK (capacity >= 0),
    review_state TEXT NOT NULL DEFAULT 'provisional',
    PRIMARY KEY (batch_id, platform_id, mount_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_loadout (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    loadout_id TEXT NOT NULL,
    loadout_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT '',
    is_default INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (batch_id, platform_id, loadout_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_magazine (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    loadout_id TEXT NOT NULL,
    mount_id TEXT NOT NULL,
    weapon_id TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    reload_time_sec INTEGER NOT NULL DEFAULT 0 CHECK (reload_time_sec >= 0),
    depth INTEGER NOT NULL DEFAULT 0 CHECK (depth >= 0),
    PRIMARY KEY (batch_id, platform_id, loadout_id, mount_id, weapon_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS catalog_staging_comms (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    link_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT 'txrx',
    satcom_capable INTEGER NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id, link_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE INDEX IF NOT EXISTS idx_staging_platform_batch ON catalog_staging_platform (batch_id ASC, platform_id ASC);
CREATE INDEX IF NOT EXISTS idx_staging_mount_batch ON catalog_staging_mount (batch_id ASC);
