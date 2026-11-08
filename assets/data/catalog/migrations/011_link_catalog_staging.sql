-- S34-02: write-gate staging for link_catalog (additive; link_catalog DDL from 007).
-- Idempotency guarded by SqliteCatalogReader.ShouldSkipMigration ("011" + catalog_staging_link exists).

CREATE TABLE IF NOT EXISTS catalog_staging_link (
    batch_id TEXT NOT NULL,
    link_id TEXT NOT NULL,
    display_name TEXT NOT NULL DEFAULT '',
    link_type TEXT NOT NULL DEFAULT 'tactical',
    latency_ms_nominal INTEGER NOT NULL DEFAULT 0 CHECK (latency_ms_nominal >= 0),
    PRIMARY KEY (batch_id, link_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE INDEX IF NOT EXISTS idx_staging_link_batch ON catalog_staging_link (batch_id ASC, link_id ASC);