-- ADR-006 TL/review gate columns (applied after 001).
ALTER TABLE sensor ADD COLUMN review_state TEXT NOT NULL DEFAULT 'approved';
ALTER TABLE sensor ADD COLUMN trl_level INTEGER NOT NULL DEFAULT 9 CHECK (trl_level >= 1 AND trl_level <= 9);