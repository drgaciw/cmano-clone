-- DATA-2: sensor basePd with provenance (ADR-006). Deterministic reads ORDER BY platform_id, sensor_id.
CREATE TABLE IF NOT EXISTS sensor (
    platform_id TEXT NOT NULL,
    sensor_id TEXT NOT NULL,
    base_pd REAL NOT NULL CHECK (base_pd >= 0 AND base_pd <= 1),
    source_fact_id TEXT NOT NULL DEFAULT 'fixture',
    confidence REAL NOT NULL DEFAULT 1.0,
    import_batch_id TEXT NOT NULL DEFAULT '',
    source_file TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (platform_id, sensor_id)
);