-- S29-02: TL export metadata — branch tier on catalog_snapshot (Phases 1–2, export-only).
-- Values TL-0 … TL-5; existing rows default to TL-0 baseline.

ALTER TABLE catalog_snapshot ADD COLUMN branch TEXT NOT NULL DEFAULT 'TL-0';