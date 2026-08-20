-- BUG-catalog-emcon-tables-empty: conservative EMCON posture seed for gauntlet T2 platforms.
-- Additive data only (platform_emcon / catalog_staging_emcon already exist from 008).
-- Idempotent INSERT OR IGNORE. Emitter IDs are existing sensor.sensor_id values —
-- no invented emitter performance. Posture is off for silent/restricted/free;
-- review_state is explicit provisional. Staging batch is actor_type=migration,
-- approval_state=proposed, proposed_utc_ticks=0 (deterministic; not a write-gate approve).
-- Default sim resolver queries (condition='free', emitter_id='radar-1') do not match CMO
-- sensor IDs, so legacy Active fallback is unchanged unless a caller uses the real emitter.
-- Do NOT skip this file in ShouldSkipMigration based on table existence (008 already created it).
-- Skip only when BOTH the Visby silent published sentinel AND the staging batch sentinel exist.
--
-- Rollback:
--   DELETE FROM catalog_staging_emcon WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017';
--   DELETE FROM catalog_staging_batch WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017';
--   DELETE FROM platform_emcon
--   WHERE review_state = 'provisional'
--     AND posture = 'off'
--     AND platform_id IN (
--       'k-22-gavle-ex-goteborg-class',
--       'k-21-goteborg',
--       'k-11-stockholm-spica-iii-1986',
--       'jas-39e-gripen-ng-2021',
--       'mrk-shkval-pr-22800-karakurt',
--       'skr-admiral-grigorovich-pr-1135-6m',
--       'skr-admiral-sergey-gorshkov-pr-2235-0',
--       'ka-27m-helix-a',
--       'k-31-visby-2009',
--       'em-sovremenny-i-pr-956-sarych'
--     );

INSERT OR IGNORE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
SELECT p.platform_id, c.condition, s.sensor_id, 'off', 'provisional'
FROM platform p
INNER JOIN sensor s ON s.platform_id = p.platform_id
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
WHERE p.platform_id IN (
    'k-22-gavle-ex-goteborg-class',
    'k-21-goteborg',
    'k-11-stockholm-spica-iii-1986',
    'jas-39e-gripen-ng-2021',
    'mrk-shkval-pr-22800-karakurt',
    'skr-admiral-grigorovich-pr-1135-6m',
    'skr-admiral-sergey-gorshkov-pr-2235-0',
    'ka-27m-helix-a',
    'k-31-visby-2009',
    'em-sovremenny-i-pr-956-sarych'
)
ORDER BY p.platform_id ASC, c.condition ASC, s.sensor_id ASC;

INSERT OR IGNORE INTO catalog_staging_batch (
    batch_id, actor_type, actor_id, proposed_utc_ticks, approval_state, record_count, rationale)
SELECT
    'batch-emcon-gauntlet-t2-seed-017',
    'migration',
    '017_platform_emcon_gauntlet_t2_seed',
    0,
    'proposed',
    COUNT(*),
    'Conservative unsourced EMCON seed for gauntlet T2; review_state=provisional; posture=off'
FROM platform p
INNER JOIN sensor s ON s.platform_id = p.platform_id
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
WHERE p.platform_id IN (
    'k-22-gavle-ex-goteborg-class',
    'k-21-goteborg',
    'k-11-stockholm-spica-iii-1986',
    'jas-39e-gripen-ng-2021',
    'mrk-shkval-pr-22800-karakurt',
    'skr-admiral-grigorovich-pr-1135-6m',
    'skr-admiral-sergey-gorshkov-pr-2235-0',
    'ka-27m-helix-a',
    'k-31-visby-2009',
    'em-sovremenny-i-pr-956-sarych'
)
HAVING COUNT(*) > 0;

INSERT OR IGNORE INTO catalog_staging_emcon (
    batch_id, platform_id, condition, emitter_id, posture, review_state)
SELECT
    'batch-emcon-gauntlet-t2-seed-017',
    p.platform_id,
    c.condition,
    s.sensor_id,
    'off',
    'provisional'
FROM platform p
INNER JOIN sensor s ON s.platform_id = p.platform_id
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
WHERE p.platform_id IN (
    'k-22-gavle-ex-goteborg-class',
    'k-21-goteborg',
    'k-11-stockholm-spica-iii-1986',
    'jas-39e-gripen-ng-2021',
    'mrk-shkval-pr-22800-karakurt',
    'skr-admiral-grigorovich-pr-1135-6m',
    'skr-admiral-sergey-gorshkov-pr-2235-0',
    'ka-27m-helix-a',
    'k-31-visby-2009',
    'em-sovremenny-i-pr-956-sarych'
)
ORDER BY p.platform_id ASC, c.condition ASC, s.sensor_id ASC;
