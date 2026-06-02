-- Rejected / gated-out catalog rows (human review queue).
CREATE TABLE IF NOT EXISTS sensor_quarantine (
    platform_id TEXT NOT NULL,
    sensor_id TEXT NOT NULL,
    base_pd REAL NOT NULL,
    source_fact_id TEXT NOT NULL DEFAULT 'fixture',
    confidence REAL NOT NULL DEFAULT 1.0,
    import_batch_id TEXT NOT NULL DEFAULT '',
    source_file TEXT NOT NULL DEFAULT '',
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 1,
    rejection_reason TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (platform_id, sensor_id)
);