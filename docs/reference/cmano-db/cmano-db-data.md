# cmano-db.com — Command Modern Database (Reference Export)

Offline markdown reference of the Command: Modern Air/Naval Operations modern database, harvested from the community viewer **[cmano-db.com](https://cmano-db.com/)**.

- **Database version:** 511
- **Captured:** 2026-05-29
- **Scope:** Modern database, all platform/equipment categories, full specifications.

## Categories

| Category | Records | Groups | File |
|---|---:|---:|---|
| Aircraft | 7387 | 150 | [aircraft.md](aircraft.md) |
| Ships | 4844 | 133 | [ship.md](ship.md) |
| Submarines | 732 | 63 | [submarine.md](submarine.md) |
| Facilities | 4511 | 119 | [facility.md](facility.md) |
| Sensors | 7208 | 37 | [sensor.md](sensor.md) |
| Weapons | 4403 | 42 | [weapon.md](weapon.md) |
| **Total (source corpora)** | **29085** | | |

### Derived views

Filtered subsets of a source corpus above — not independently scraped. Their records are **already counted** in the totals above, so do not add them to the total.

| View | Records | Groups | Derived from | File |
|---|---:|---:|---|---|
| Ground Units | 3289 | 112 | Facilities with a mobile `Type` — Mobile Vehicle(s), Mobile Vehicle(s) - Wheeled/Tracked, Mobile Personnel | [ground-units.md](ground-units.md) |

Ground Units is the CMO markdown importer's 7th `--entity` category (`--entity ground-unit`); `facility.md` remains the complete superset. See [`docs/engineering/cmo-markdown-import.md`](../../engineering/cmo-markdown-import.md).

## Source & terms

Data originates from the CMANO game database and is presented by cmano-db.com. The site's robots.txt asserts content-signal reservations (AI-train / AI-input / search) under EU Directive 2019/790 Article 4. This export is for **personal/internal game-design reference only** — not redistribution, AI training, or search indexing. Crawled politely (rate-limited, single-host, no re-fetching).
