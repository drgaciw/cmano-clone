#!/usr/bin/env python3
"""Compare public corpus H3 counts vs live catalog rows (enterprise >= 99% gate).

Platform-family coverage uses hardened metrics (not naive domain tallies):

- ship/platform: surface domain, excluding enterprise placeholders and water-(surface)
  facility classes
- submarine: collision-disambiguated ID overlap with submarine.md (same pattern as
  ground-unit); avoids ASW surface auxiliaries mis-tagged subsurface via the
  "submarine" substring in platform_class
- facility: land domain plus water-(surface) facility classes
- ground-unit: collision-disambiguated ID overlap with ground-units.md

When a domain-raw count diverges from markdown-overlap, a print-only diagnostic
line is emitted; the gate still uses the hardened metric above.
"""
from __future__ import annotations

import argparse
import re
import sqlite3
import sys
from collections.abc import Callable
from pathlib import Path

ENTERPRISE_EXCLUDE_PLATFORM_IDS = frozenset(
    {"cmo-sensor-catalog", "u1", "hostile-1", "hostile-far"}
)

# Subsurface class cues aligned with InferDomain intent (CmoMarkdownImporter.InferDomain).
# Used only for diagnostics / optional class-path counting — not the submarine gate.
_SUBSURFACE_CLASS_RE = re.compile(
    r"submarine|\bssk\b|\bssn\b|\bssbn\b|\bssgn\b|\bssg\b|\bssb\b|\bssm\b|\bagss\b|"
    r"hunter-killer|plarb|plark|\brov\b|\buuv\b|\bsdv\b|underwater|\bPLA-\d",
    re.IGNORECASE,
)
# Surface auxiliaries that mention "submarine" but are not subsurface combatants.
_SUBSURFACE_FALSE_POSITIVE_RE = re.compile(
    r"\b(rescue ship|tender|chaser)\b",
    re.IGNORECASE,
)
_SURFACE_HULL_OVERRIDE_RE = re.compile(
    r"\b(carrier|cruiser|destroyer)\b",
    re.IGNORECASE,
)


def slug_platform_id(title: str) -> str:
    head = title.split(",")[0].strip()
    slug = re.sub(r"[^\w]+", "-", head.lower()).strip("-")
    return slug[:64] if slug else "unknown-platform"


def h3_count(markdown_path: Path) -> int:
    if not markdown_path.exists():
        return 0
    text = markdown_path.read_text(encoding="utf-8", errors="ignore")
    return len(re.findall(r"(?m)^### ", text))


def h3_slugs(markdown_path: Path) -> set[str]:
    if not markdown_path.exists():
        return set()
    text = markdown_path.read_text(encoding="utf-8", errors="ignore")
    slugs: set[str] = set()
    for match in re.finditer(r"(?m)^### (.+)$", text):
        slugs.add(slug_platform_id(match.group(1).strip()))
    return slugs


def load_platform_ids(db: Path) -> set[str]:
    con = sqlite3.connect(db)
    try:
        rows = con.execute("SELECT platform_id FROM platform").fetchall()
        return {row[0] for row in rows}
    finally:
        con.close()


def count_table(db: Path, table: str, where: str = "", params: tuple = ()) -> int:
    con = sqlite3.connect(db)
    try:
        sql = f"SELECT COUNT(*) FROM {table}"
        if where:
            sql += f" WHERE {where}"
        return int(con.execute(sql, params).fetchone()[0])
    finally:
        con.close()


def looks_like_submarine_class(platform_class: str) -> bool:
    """True when platform_class matches InferDomain subsurface intent (not ASW ships)."""
    pc = platform_class or ""
    if _SURFACE_HULL_OVERRIDE_RE.search(pc):
        return False
    if _SUBSURFACE_FALSE_POSITIVE_RE.search(pc):
        return False
    # InferDomain matches "anti-submarine" / "asw" as air before subsurface; exclude those.
    if re.search(r"anti-submarine|\basw\b", pc, re.IGNORECASE):
        return False
    return bool(_SUBSURFACE_CLASS_RE.search(pc))


def count_ship_platforms(db: Path) -> int:
    con = sqlite3.connect(db)
    try:
        sql = """
            SELECT COUNT(*) FROM platform
            WHERE domain = 'surface'
              AND platform_id NOT IN ({exclude})
              AND lower(platform_class) NOT LIKE '%water (surface)%'
        """.format(
            exclude=",".join("?" for _ in ENTERPRISE_EXCLUDE_PLATFORM_IDS)
        )
        return int(
            con.execute(sql, tuple(ENTERPRISE_EXCLUDE_PLATFORM_IDS)).fetchone()[0]
        )
    finally:
        con.close()


def count_facility_platforms(db: Path) -> int:
    con = sqlite3.connect(db)
    try:
        return int(
            con.execute(
                """
                SELECT COUNT(*) FROM platform
                WHERE domain = 'land'
                   OR (domain = 'surface'
                       AND lower(platform_class) LIKE '%water (surface)%')
                """
            ).fetchone()[0]
        )
    finally:
        con.close()


def count_submarine_class_heuristic(db: Path) -> int:
    """Count live platforms whose class clearly indicates a submarine / UUV / ROV."""
    con = sqlite3.connect(db)
    try:
        rows = con.execute("SELECT platform_class FROM platform").fetchall()
        return sum(1 for (pc,) in rows if looks_like_submarine_class(pc or ""))
    finally:
        con.close()


def count_submarine_platforms(db: Path, corpus: Path) -> int:
    """Prefer submarine.md ID overlap; fall back to class heuristic if markdown missing."""
    md_path = corpus / "submarine.md"
    if md_path.exists():
        return count_platform_markdown_overlap(
            db, corpus, "submarine.md", default_domain="subsurface"
        )
    return count_submarine_class_heuristic(db)


PLATFORM_PATH = re.compile(r"/(ship|aircraft|submarine|facility)/(\d+)/")
TYPE_ROW = re.compile(r"^\|\s*Type\s*\|\s*(.+?)\s*\|", re.I)
SECTION_HEADING = re.compile(r"^### (.+)$")


def read_platform_ids_from_markdown(markdown_path: Path, default_domain: str = "surface") -> set[str]:
    """Mirror CmoMarkdownImporter.ReadPlatformBindings collision disambiguation."""
    _ = default_domain  # Kept for call-site parity; IDs come from title + numeric path.
    if not markdown_path.exists():
        return set()

    text = markdown_path.read_text(encoding="utf-8", errors="ignore")
    bindings: list[tuple[str, int]] = []
    title: str | None = None
    platform_numeric_id: int | None = None

    def flush() -> None:
        nonlocal title, platform_numeric_id
        if title is None or platform_numeric_id is None:
            return
        bindings.append((slug_platform_id(title), platform_numeric_id))
        title = None
        platform_numeric_id = None

    for raw_line in text.split("\n"):
        line = raw_line.rstrip("\r")
        heading = SECTION_HEADING.match(line)
        if heading:
            flush()
            title = heading.group(1).strip()
            continue
        if title is None:
            continue
        path_match = PLATFORM_PATH.search(line)
        if path_match and platform_numeric_id is None:
            platform_numeric_id = int(path_match.group(2))

    flush()

    collision_counts: dict[str, int] = {}
    for slug, _ in bindings:
        collision_counts[slug] = collision_counts.get(slug, 0) + 1

    platform_ids: set[str] = set()
    for slug, numeric_id in bindings:
        platform_id = slug
        if collision_counts.get(slug, 0) > 1:
            platform_id = f"{slug}-{numeric_id}"
        platform_ids.add(platform_id)
    return platform_ids


def count_platform_markdown_overlap(
    db: Path, corpus: Path, markdown_name: str, default_domain: str = "surface"
) -> int:
    expected_ids = read_platform_ids_from_markdown(corpus / markdown_name, default_domain)
    live = load_platform_ids(db)
    return len(expected_ids & live)


def count_slug_overlap(db: Path, corpus: Path, markdown_name: str) -> int:
    slugs = h3_slugs(corpus / markdown_name)
    live = load_platform_ids(db)
    return len(slugs & live)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--db", required=True)
    parser.add_argument("--min-coverage", type=float, default=99.0)
    parser.add_argument("--corpus-root", default="")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent
    db = Path(args.db)
    corpus = Path(args.corpus_root) if args.corpus_root else repo_root / "docs/reference/cmano-db"
    min_pct = args.min_coverage

    if not db.is_file():
        print(f"Catalog DB not found: {db}", file=sys.stderr)
        return 1

    # (name, md_name, live_fn, domain_raw_fn|None)
    # live_fn = hardened gate metric; domain_raw_fn is naive domain COUNT for diagnostics.
    checks: list[tuple[str, str, Callable[[], int], Callable[[], int] | None]] = [
        ("sensor", "sensor.md", lambda: count_table(db, "sensor"), None),
        ("weapon", "weapon.md", lambda: count_table(db, "weapon_catalog"), None),
        (
            "platform",
            "ship.md",
            lambda: count_ship_platforms(db),
            lambda: count_table(db, "platform", "domain = ?", ("surface",)),
        ),
        (
            "aircraft",
            "aircraft.md",
            lambda: count_table(db, "platform", "domain = ?", ("air",)),
            lambda: count_table(db, "platform", "domain = ?", ("air",)),
        ),
        (
            "submarine",
            "submarine.md",
            lambda: count_submarine_platforms(db, corpus),
            lambda: count_table(db, "platform", "domain = ?", ("subsurface",)),
        ),
        (
            "facility",
            "facility.md",
            lambda: count_facility_platforms(db),
            lambda: count_table(db, "platform", "domain = ?", ("land",)),
        ),
        (
            "ground-unit",
            "ground-units.md",
            lambda: count_platform_markdown_overlap(
                db, corpus, "ground-units.md", default_domain="land"
            ),
            lambda: count_table(db, "platform", "domain = ?", ("land",)),
        ),
    ]

    failed = False
    print("=== verify-corpus-coverage ===")
    print(f"DB: {db}")
    print(f"Min coverage: {min_pct}%")

    for name, md_name, live_fn, domain_raw_fn in checks:
        md_path = corpus / md_name
        expected = h3_count(md_path)
        live = live_fn()
        pct = (live / expected * 100) if expected else 0.0
        ok = pct >= min_pct
        print(
            f"{name}: live={live} expected~={expected} coverage={pct:.1f}% "
            f"{'PASS' if ok else 'FAIL'}"
        )
        if domain_raw_fn is not None:
            domain_raw = domain_raw_fn()
            md_overlap = count_platform_markdown_overlap(
                db, corpus, md_name, default_domain="surface"
            )
            if domain_raw != md_overlap:
                print(
                    f"  diagnostic: domain-raw={domain_raw} "
                    f"markdown-overlap={md_overlap}"
                )
        if not ok:
            failed = True

    if failed:
        print("=== FAIL ===")
        return 1

    print("=== PASS ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
