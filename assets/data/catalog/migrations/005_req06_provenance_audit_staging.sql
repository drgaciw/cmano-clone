-- Req-06 P0: field provenance, append-only audit, staging batches, release train metadata.
-- Deterministic reads unchanged (ORDER BY platform_id, sensor_id).

ALTER TABLE sensor ADD COLUMN value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction';
ALTER TABLE sensor ADD COLUMN reviewer_id TEXT NOT NULL DEFAULT '';
ALTER TABLE sensor ADD COLUMN revised_utc_ticks INTEGER NOT NULL DEFAULT 0;
ALTER TABLE sensor ADD COLUMN citation_ref TEXT NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS catalog_change_log (
    change_id INTEGER PRIMARY KEY AUTOINCREMENT,
    batch_id TEXT NOT NULL,
    table_name TEXT NOT NULL,
    entity_key TEXT NOT NULL,
    field_name TEXT NOT NULL,
    previous_value TEXT NOT NULL DEFAULT '',
    new_value TEXT NOT NULL DEFAULT '',
    actor_type TEXT NOT NULL,
    actor_id TEXT NOT NULL,
    rationale TEXT NOT NULL DEFAULT '',
    approval_state TEXT NOT NULL DEFAULT 'approved',
    revised_utc_ticks INTEGER NOT NULL,
    release_version TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_catalog_change_log_batch ON catalog_change_log (batch_id ASC, change_id ASC);

CREATE TABLE IF NOT EXISTS catalog_staging_batch (
    batch_id TEXT PRIMARY KEY,
    actor_type TEXT NOT NULL,
    actor_id TEXT NOT NULL,
    proposed_utc_ticks INTEGER NOT NULL,
    approval_state TEXT NOT NULL DEFAULT 'proposed',
    record_count INTEGER NOT NULL DEFAULT 0,
    rationale TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS catalog_staging_sensor (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    sensor_id TEXT NOT NULL,
    base_pd REAL NOT NULL,
    source_fact_id TEXT NOT NULL DEFAULT 'fixture',
    confidence REAL NOT NULL DEFAULT 1.0,
    import_batch_id TEXT NOT NULL DEFAULT '',
    source_file TEXT NOT NULL DEFAULT '',
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 1,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    reviewer_id TEXT NOT NULL DEFAULT '',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id, sensor_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE TABLE IF NOT EXISTS db_release (
    release_version TEXT PRIMARY KEY,
    snapshot_id TEXT NOT NULL,
    schema_version TEXT NOT NULL DEFAULT '005',
    created_utc_ticks INTEGER NOT NULL,
    notes TEXT NOT NULL DEFAULT ''
);

INSERT OR IGNORE INTO db_release (release_version, snapshot_id, schema_version, created_utc_ticks, notes)
VALUES ('catalog-p0-2026-06-04', 'baltic_patrol', '005', 0, 'Req-06 P0 Baltic catalog drop');