-- SWARM-21 Phase B + SWARM-31 catalog gate (CEC capable flag).
-- Additive columns on platform_swarm. Idempotency: ShouldSkipMigration("013" + cec_capable column).
-- Generic preset remains cec_capable=0; US/NATO exemplars may set 1.

ALTER TABLE platform_swarm ADD COLUMN default_mode TEXT NOT NULL DEFAULT 'Hold';
ALTER TABLE platform_swarm ADD COLUMN requires_host INTEGER NOT NULL DEFAULT 0;
ALTER TABLE platform_swarm ADD COLUMN allowed_host_classes TEXT NOT NULL DEFAULT '';
ALTER TABLE platform_swarm ADD COLUMN cec_capable INTEGER NOT NULL DEFAULT 0;
