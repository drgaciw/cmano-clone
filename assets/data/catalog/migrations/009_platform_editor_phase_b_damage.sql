-- Req-21 Platform Editor Phase B: platform damage model (HP + withdraw threshold + critical flags).
-- Additive only. Deterministic reads via ORDER BY platform_id. Provenance mirrors Phase B fitting tables.
-- Idempotency guarded by SqliteCatalogReader.ShouldSkipMigration ("009" + platform_damage exists).
-- Workbook: damage columns land on Platforms sheet (S25-03); no new sheet.

CREATE TABLE IF NOT EXISTS platform_damage (
    platform_id TEXT PRIMARY KEY,
    max_hp REAL NOT NULL DEFAULT 100 CHECK (max_hp > 0),
    withdraw_threshold_pct REAL NOT NULL DEFAULT 0 CHECK (withdraw_threshold_pct >= 0),
    critical_flags INTEGER NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS catalog_staging_damage (
    batch_id TEXT NOT NULL,
    platform_id TEXT NOT NULL,
    max_hp REAL NOT NULL DEFAULT 100 CHECK (max_hp > 0),
    withdraw_threshold_pct REAL NOT NULL DEFAULT 0 CHECK (withdraw_threshold_pct >= 0),
    critical_flags INTEGER NOT NULL DEFAULT 0,
    review_state TEXT NOT NULL DEFAULT 'provisional',
    trl_level INTEGER NOT NULL DEFAULT 9,
    value_tier TEXT NOT NULL DEFAULT 'gameplay_abstraction',
    citation_ref TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (batch_id, platform_id),
    FOREIGN KEY (batch_id) REFERENCES catalog_staging_batch(batch_id)
);

CREATE INDEX IF NOT EXISTS idx_staging_damage_batch
    ON catalog_staging_damage (batch_id ASC, platform_id ASC);