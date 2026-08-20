-- BUG-catalog-emcon-tables-empty: conservative EMCON posture seed for Baltic Patrol platforms.
-- Additive data only (platform_emcon / catalog_staging_emcon already exist from 008).
-- Idempotent INSERT OR IGNORE. Emitter IDs are existing sensor.sensor_id values —
-- no invented emitter performance. Posture is off for silent/restricted/free;
-- review_state is explicit provisional. Staging batch is actor_type=migration,
-- approval_state=proposed, proposed_utc_ticks=0 (deterministic; not a write-gate approve).
-- CMO sensor IDs are bound as emitters with posture=off. A gameplay alias emitter_id='radar-1'
-- is also inserted so the default sim resolver triple (condition='free', emitter_id='radar-1')
-- finds a CatalogEmcon row: free/active preserves legacy Active fallback (not classified
-- performance); silent/restricted stay off. u1 is denylisted so fixture radar-1 is untouched.
-- Fixture denylist (u1, swarm, v3 OOB) keeps SeedBalticPatrol Phase B Emcon count=2.
-- Do NOT skip this file in ShouldSkipMigration based on table existence (008 already created it).
-- Skip only when Visby published+staging sentinels exist, the free/radar-1 alias exists,
-- AND no non-fixture platform with sensors remains unseeded.
--
-- Rollback:
--   DELETE FROM catalog_staging_emcon WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017';
--   DELETE FROM catalog_staging_batch WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017';
--   DELETE FROM platform_emcon
--   WHERE review_state = 'provisional'
--     AND platform_id NOT IN (
--       'u1','hostile-1','hostile-far','uas-swarm-generic','usn-uas-swarm-cec',
--       'cmo-sensor-catalog','ucav-blue','ucav-red','usub-blue','usub-red'
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
WHERE p.platform_id NOT IN (
    'u1',
    'hostile-1',
    'hostile-far',
    'uas-swarm-generic',
    'usn-uas-swarm-cec',
    'cmo-sensor-catalog',
    'ucav-blue',
    'ucav-red',
    'usub-blue',
    'usub-red'
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
    'Conservative unsourced EMCON seed for Baltic Patrol (gauntlet T2 + remaining non-fixture platforms); review_state=provisional; posture=off'
FROM platform p
INNER JOIN sensor s ON s.platform_id = p.platform_id
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
WHERE p.platform_id NOT IN (
    'u1',
    'hostile-1',
    'hostile-far',
    'uas-swarm-generic',
    'usn-uas-swarm-cec',
    'cmo-sensor-catalog',
    'ucav-blue',
    'ucav-red',
    'usub-blue',
    'usub-red'
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
WHERE p.platform_id NOT IN (
    'u1',
    'hostile-1',
    'hostile-far',
    'uas-swarm-generic',
    'usn-uas-swarm-cec',
    'cmo-sensor-catalog',
    'ucav-blue',
    'ucav-red',
    'usub-blue',
    'usub-red'
)
ORDER BY p.platform_id ASC, c.condition ASC, s.sensor_id ASC;

INSERT OR IGNORE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
SELECT p.platform_id,
       c.condition,
       'radar-1',
       CASE WHEN c.condition = 'free' THEN 'active' ELSE 'off' END,
       'provisional'
FROM (
    SELECT DISTINCT platform_id
    FROM platform
    WHERE platform_id NOT IN (
        'u1',
        'hostile-1',
        'hostile-far',
        'uas-swarm-generic',
        'usn-uas-swarm-cec',
        'cmo-sensor-catalog',
        'ucav-blue',
        'ucav-red',
        'usub-blue',
        'usub-red'
    )
      AND EXISTS (SELECT 1 FROM sensor s WHERE s.platform_id = platform.platform_id)
) AS p
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
ORDER BY p.platform_id ASC, c.condition ASC;

INSERT OR IGNORE INTO catalog_staging_emcon (
    batch_id, platform_id, condition, emitter_id, posture, review_state)
SELECT
    'batch-emcon-gauntlet-t2-seed-017',
    p.platform_id,
    c.condition,
    'radar-1',
    CASE WHEN c.condition = 'free' THEN 'active' ELSE 'off' END,
    'provisional'
FROM (
    SELECT DISTINCT platform_id
    FROM platform
    WHERE platform_id NOT IN (
        'u1',
        'hostile-1',
        'hostile-far',
        'uas-swarm-generic',
        'usn-uas-swarm-cec',
        'cmo-sensor-catalog',
        'ucav-blue',
        'ucav-red',
        'usub-blue',
        'usub-red'
    )
      AND EXISTS (SELECT 1 FROM sensor s WHERE s.platform_id = platform.platform_id)
) AS p
INNER JOIN (
    SELECT 'silent' AS condition
    UNION ALL SELECT 'restricted'
    UNION ALL SELECT 'free'
) AS c
ORDER BY p.platform_id ASC, c.condition ASC;

UPDATE catalog_staging_batch
SET record_count = (
    SELECT COUNT(*) FROM catalog_staging_emcon
    WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017')
WHERE batch_id = 'batch-emcon-gauntlet-t2-seed-017';
