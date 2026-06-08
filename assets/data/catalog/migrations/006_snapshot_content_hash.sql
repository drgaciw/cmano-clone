-- P2-3: catalog snapshot content hash for release-train binding (S19-03).

ALTER TABLE catalog_snapshot ADD COLUMN content_hash_sha256 TEXT NOT NULL DEFAULT '';