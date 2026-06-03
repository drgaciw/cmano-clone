-- ADR-008: platform positions and combat radius for mission-editor validation (deterministic ORDER BY platform_id).
CREATE TABLE IF NOT EXISTS catalog_snapshot (
    snapshot_id TEXT PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS platform (
    platform_id TEXT NOT NULL,
    snapshot_id TEXT NOT NULL,
    lat_deg REAL NOT NULL,
    lon_deg REAL NOT NULL,
    combat_radius_nm REAL NOT NULL CHECK (combat_radius_nm > 0),
    PRIMARY KEY (platform_id, snapshot_id)
);

INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ('baltic_patrol');

INSERT OR IGNORE INTO platform (platform_id, snapshot_id, lat_deg, lon_deg, combat_radius_nm) VALUES
    ('u1', 'baltic_patrol', 57.0, 20.0, 400.0),
    ('hostile-1', 'baltic_patrol', 58.5, 21.0, 200.0),
    ('hostile-far', 'baltic_patrol', 65.0, 35.0, 200.0);