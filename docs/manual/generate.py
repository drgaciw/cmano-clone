#!/usr/bin/env python3
"""
CMO Manual HTML Generator
Renders each PDF page as an image and wraps them in a navigable HTML site.
Run from the project root: python docs/manual/generate.py
"""

import os
import subprocess
import json
import shutil
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
PROJECT_ROOT = Path(__file__).parent.parent.parent
PDF_PATH     = PROJECT_ROOT / "CMO_manual_EBOOK.pdf"
OUT_DIR      = Path(__file__).parent          # docs/manual/
PAGES_DIR    = OUT_DIR / "pages"
ASSETS_DIR   = OUT_DIR / "assets"
IMAGES_DIR   = ASSETS_DIR / "images"

# ---------------------------------------------------------------------------
# TOC structure
# Each tuple: (slug, number, title, parent_slug, start_page)
# end_page is computed from the flat list order.
# ---------------------------------------------------------------------------
TOC_RAW = [
    ("preface",            "",       "Preface",                                            None,         9),
    ("historical-intro",   "",       "Historical Introduction",                            None,         9),
    ("what-is-command",    "",       "What is Command?",                                   None,        13),

    # Chapter 1
    ("ch01",               "1",      "Installation",                                       None,        14),
    ("ch01-01",            "1.1",    "System Requirements",                                "ch01",      14),
    ("ch01-02",            "1.2",    "Support",                                            "ch01",      15),
    ("ch01-03",            "1.3",    "Notes for Multitaskers and Returning Players",       "ch01",      16),

    # Chapter 2
    ("ch02",               "2",      "Introduction to COMMAND",                            None,        16),
    ("ch02-01",            "2.1",    "Important Terms",                                    "ch02",      19),
    ("ch02-02",            "2.2",    "Fundamentals",                                       "ch02",      22),
    ("ch02-02-01",         "2.2.1",  "Starting COMMAND",                                   "ch02-02",   22),

    # Chapter 3
    ("ch03",               "3",      "User Interface",                                     None,        27),
    ("ch03-01",            "3.1",    "The Globe Display",                                  "ch03",      27),
    ("ch03-02",            "3.2",    "Mouse Functions",                                    "ch03",      33),
    ("ch03-03",            "3.3",    "Buttons and Windows",                                "ch03",      35),
    ("ch03-03-01",         "3.3.1",  "Engage Target(s) - Auto",                            "ch03-03",   35),
    ("ch03-03-02",         "3.3.2",  "Engage Target(s) - Manual",                          "ch03-03",   35),
    ("ch03-03-03",         "3.3.3",  "Plot Course",                                        "ch03-03",   38),
    ("ch03-03-04",         "3.3.4",  "Throttle and Altitude",                              "ch03-03",   38),
    ("ch03-03-05",         "3.3.5",  "Formation Editor",                                   "ch03-03",   40),
    ("ch03-03-06",         "3.3.6",  "Magazines",                                          "ch03-03",   41),
    ("ch03-03-07",         "3.3.7",  "Air Operations",                                     "ch03-03",   42),
    ("ch03-03-08",         "3.3.8",  "Boat Operations",                                    "ch03-03",   45),
    ("ch03-03-09",         "3.3.9",  "Mounts and Weapons",                                 "ch03-03",   47),
    ("ch03-03-10",         "3.3.10", "Sensors",                                            "ch03-03",   48),
    ("ch03-03-11",         "3.3.11", "Systems and Damage",                                 "ch03-03",   49),
    ("ch03-03-12",         "3.3.12", "Doctrine",                                           "ch03-03",   50),
    ("ch03-03-13",         "3.3.13", "General / Strategic",                                "ch03-03",   51),
    ("ch03-03-14",         "3.3.14", "EMCON Tab",                                          "ch03-03",   59),
    ("ch03-03-15",         "3.3.15", "WRA Tab",                                            "ch03-03",   61),
    ("ch03-03-16",         "3.3.16", "Withdraw/Redeploy Tab",                              "ch03-03",   64),
    ("ch03-03-17",         "3.3.17", "Mission Editor",                                     "ch03-03",   65),

    # Chapter 4
    ("ch04",               "4",      "Menus and Dialogs",                                  None,        66),
    ("ch04-01",            "4.1",    "Right Click on Unit / Context Dialog",               "ch04",      66),
    ("ch04-01-01",         "4.1.1",  "Attack Options",                                     "ch04-01",   66),
    ("ch04-01-02",         "4.1.2",  "ASW-specific Actions",                               "ch04-01",   68),
    ("ch04-01-03",         "4.1.3",  "Context Menu, Cont.",                                "ch04-01",   69),
    ("ch04-01-04",         "4.1.4",  "Group Operations",                                   "ch04-01",   70),
    ("ch04-01-05",         "4.1.5",  "Scenario Editor",                                    "ch04-01",   71),
    ("ch04-02",            "4.2",    "Control Right Click on Map Dialog",                  "ch04",      72),
    ("ch04-03",            "4.3",    "Units, Groups and Weapons Symbols",                  "ch04",      72),
    ("ch04-04",            "4.4",    "Group Mode and Unit View Mode",                      "ch04",      74),
    ("ch04-05",            "4.5",    "Right Side Information Panel",                       "ch04",      75),
    ("ch04-05-01",         "4.5.1",  "Unit Status Dialog",                                 "ch04-05",   75),
    ("ch04-05-02",         "4.5.2",  "Sensors Button",                                     "ch04-05",   79),
    ("ch04-05-03",         "4.5.3",  "Weapon Buttons",                                     "ch04-05",   80),
    ("ch04-05-04",         "4.5.4",  "Unit Fuel",                                          "ch04-05",   80),
    ("ch04-05-05",         "4.5.5",  "Unit Alt/Speed",                                     "ch04-05",   80),
    ("ch04-05-06",         "4.5.6",  "Unit EMCON",                                         "ch04-05",   81),
    ("ch04-05-07",         "4.5.7",  "Doctrine",                                           "ch04-05",   81),
    ("ch04-05-08",         "4.5.8",  "Doctrines, Postures, WRA, and ROE",                  "ch04-05",   81),

    # Chapter 5
    ("ch05",               "5",      "ScenEdit",                                           None,        82),
    ("ch05-01",            "5.1",    "Getting Started",                                    "ch05",      82),
    ("ch05-02",            "5.2",    "Scenario Intro Walkthrough",                         "ch05",      82),
    ("ch05-02-01",         "5.2.1",  "Full Scenario Walkthrough",                          "ch05-02",   83),
    ("ch05-02-02",         "5.2.2",  "Final Checklist for Polishing",                      "ch05-02",   84),
    ("ch05-02-03",         "5.2.3",  "Detailed Single Scenario Walkthrough",               "ch05-02",   86),
    ("ch05-03",            "5.3",    "Lua Scripting",                                      "ch05",      99),
    ("ch05-04",            "5.4",    "Editor Drop-Down Menu",                              "ch05",     105),
    ("ch05-04-01",         "5.4.1",  "Scenario Times + Duration",                          "ch05-04",  105),
    ("ch05-04-02",         "5.4.2",  "Database",                                           "ch05-04",  108),
    ("ch05-04-03",         "5.4.3",  "Campaigns",                                          "ch05-04",  108),
    ("ch05-04-04",         "5.4.4",  "Add/Edit Sides",                                     "ch05-04",  109),
    ("ch05-04-05",         "5.4.5",  "Edit Briefing",                                      "ch05-04",  113),
    ("ch05-04-06",         "5.4.6",  "Edit Scoring Dialog",                                "ch05-04",  114),
    ("ch05-04-07",         "5.4.7",  "God's Eye View",                                     "ch05-04",  116),
    ("ch05-04-08",         "5.4.8",  "Weather",                                            "ch05-04",  117),
    ("ch05-04-09",         "5.4.9",  "Scenario Features + Settings",                       "ch05-04",  118),
    ("ch05-04-10",         "5.4.10", "Merge Scenario",                                     "ch05-04",  120),
    ("ch05-05",            "5.5",    "Events",                                             "ch05",     121),
    ("ch05-05-01",         "5.5.1",  "Actions",                                            "ch05-05",  123),
    ("ch05-05-02",         "5.5.2",  "Special Actions",                                    "ch05-05",  127),
    ("ch05-05-03",         "5.5.3",  "Triggers",                                           "ch05-05",  127),
    ("ch05-05-04",         "5.5.4",  "Setting up Triggers",                                "ch05-05",  130),
    ("ch05-05-05",         "5.5.5",  "Constructing an Event",                              "ch05-05",  134),
    ("ch05-06",            "5.6",    "Scenario Batch-Rebuilder (SBR)",                     "ch05",     135),
    ("ch05-07",            "5.7",    "Unit Actions",                                       "ch05",     135),
    ("ch05-07-01",         "5.7.1",  "Add a Unit",                                         "ch05-07",  135),
    ("ch05-07-02",         "5.7.2",  "Add Satellites",                                     "ch05-07",  137),
    ("ch05-08",            "5.8",    "Import/Export Units",                                "ch05",     145),

    # Chapter 6
    ("ch06",               "6",      "Drop-Down Menus",                                    None,       148),
    ("ch06-01",            "6.1",    "File",                                               "ch06",     149),
    ("ch06-02",            "6.2",    "View",                                               "ch06",     149),
    ("ch06-03",            "6.3",    "Game",                                               "ch06",     152),
    ("ch06-03-01",         "6.3.1",  "Start/Stop",                                         "ch06-03",  152),
    ("ch06-03-02",         "6.3.2",  "Time Compression",                                   "ch06-03",  153),
    ("ch06-03-03",         "6.3.3",  "Order of Battle",                                    "ch06-03",  153),
    ("ch06-03-04",         "6.3.4",  "Database Viewer",                                    "ch06-03",  155),
    ("ch06-03-05",         "6.3.5",  "Browse Scenario Platforms",                          "ch06-03",  158),
    ("ch06-03-06",         "6.3.6",  "Scenario Description",                               "ch06-03",  158),
    ("ch06-03-07",         "6.3.7",  "Side Briefing",                                      "ch06-03",  158),
    ("ch06-03-08",         "6.3.8",  "Side Doctrine/ROE/WRA/EMCON",                        "ch06-03",  159),
    ("ch06-03-09",         "6.3.9",  "EMCON Tab",                                          "ch06-03",  159),
    ("ch06-03-10",         "6.3.10", "Special Actions",                                    "ch06-03",  159),
    ("ch06-03-11",         "6.3.11", "Recorder",                                           "ch06-03",  161),
    ("ch06-03-12",         "6.3.12", "Message Log",                                        "ch06-03",  162),
    ("ch06-03-13",         "6.3.13", "Losses and Expenditures",                            "ch06-03",  162),
    ("ch06-03-14",         "6.3.14", "Scoring",                                            "ch06-03",  163),
    ("ch06-04",            "6.4",    "Game Options Window",                                "ch06",     165),
    ("ch06-04-01",         "6.4.1",  "General",                                            "ch06-04",  165),
    ("ch06-04-02",         "6.4.2",  "Map Display",                                        "ch06-04",  166),
    ("ch06-04-03",         "6.4.3",  "Message Log",                                        "ch06-04",  168),
    ("ch06-04-04",         "6.4.4",  "Sounds and Music",                                   "ch06-04",  168),
    ("ch06-04-05",         "6.4.5",  "Game Speed",                                         "ch06-04",  168),
    ("ch06-04-06",         "6.4.6",  "Tacview",                                            "ch06-04",  168),
    ("ch06-04-07",         "6.4.7",  "Hover Info",                                         "ch06-04",  168),
    ("ch06-05",            "6.5",    "Map Settings Drop-Down Menu",                        "ch06",     169),
    ("ch06-06",            "6.6",    "Quick Jump",                                         "ch06",     174),
    ("ch06-07",            "6.7",    "Unit Orders Drop-Down Menu",                         "ch06",     175),
    ("ch06-08",            "6.8",    "Contacts Drop Down Menu",                            "ch06",     175),
    ("ch06-09",            "6.9",    "Missions + Reference Points Drop-Down Menu",         "ch06",     176),
    ("ch06-10",            "6.10",   "Help Dropdown Menu",                                 "ch06",     180),

    # Chapter 7
    ("ch07",               "7",      "Missions and Reference Points",                      None,       180),
    ("ch07-01",            "7.1",    "Mission Editor",                                     "ch07",     180),
    ("ch07-01-01",         "7.1.1",  "Add New Mission",                                    "ch07-01",  185),
    ("ch07-01-02",         "7.1.2",  "Mission Parameter Tabs",                             "ch07-01",  186),
    ("ch07-02",            "7.2",    "Missions",                                           "ch07",     187),
    ("ch07-02-01",         "7.2.1",  "Ferry Mission",                                      "ch07-02",  187),
    ("ch07-02-02",         "7.2.2",  "Support Mission",                                    "ch07-02",  188),
    ("ch07-02-03",         "7.2.3",  "Patrol Mission",                                     "ch07-02",  191),
    ("ch07-02-04",         "7.2.4",  "Strike Mission",                                     "ch07-02",  195),
    ("ch07-02-05",         "7.2.5",  "Mining Mission",                                     "ch07-02",  200),
    ("ch07-02-06",         "7.2.6",  "Mine-Clearing Mission",                              "ch07-02",  201),
    ("ch07-02-07",         "7.2.7",  "Cargo Mission",                                      "ch07-02",  203),
    ("ch07-03",            "7.3",    "Reference Points",                                   "ch07",     205),
    ("ch07-03-01",         "7.3.1",  "Changing Reference Point Properties",                "ch07-03",  206),

    # Chapter 8
    ("ch08",               "8",      "Databases and Templates",                            None,       207),
    ("ch08-01",            "8.1",    "Scenarios vs. Databases",                            "ch08",     207),
    ("ch08-02",            "8.2",    "Scenario Maintenance",                               "ch08",     208),
    ("ch08-03",            "8.3",    "Rebuild Single Scenario",                            "ch08",     210),
    ("ch08-04",            "8.4",    "Rebuild Multiple Scenarios",                         "ch08",     210),
    ("ch08-05",            "8.5",    "Shallow Rebuild vs. Deep Rebuild",                   "ch08",     211),
    ("ch08-06",            "8.6",    "Log Files",                                          "ch08",     212),
    ("ch08-07",            "8.7",    "Scenario Config Files (INI file)",                   "ch08",     212),
    ("ch08-08",            "8.8",    "Scenario Config File Templates and Delta Templates", "ch08",     214),
    ("ch08-09",            "8.9",    "Editing Scenario Config Files",                      "ch08",     216),
    ("ch08-10",            "8.10",   "Scenario Config File Overview",                      "ch08",     218),

    # Chapter 9
    ("ch09",               "9",      "Combat",                                             None,       222),
    ("ch09-01",            "9.1",    "Sensors and Weapons",                                "ch09",     223),
    ("ch09-01-01",         "9.1.1",  "Sensors",                                            "ch09-01",  223),
    ("ch09-01-02",         "9.1.2",  "Weapons",                                            "ch09-01",  225),
    ("ch09-02",            "9.2",    "Battle",                                             "ch09",     230),
    ("ch09-02-01",         "9.2.1",  "Air Combat",                                         "ch09-02",  230),
    ("ch09-02-02",         "9.2.2",  "Naval Combat",                                       "ch09-02",  232),
    ("ch09-02-03",         "9.2.3",  "Submarine Combat",                                   "ch09-02",  233),
    ("ch09-02-04",         "9.2.4",  "Mine Warfare",                                       "ch09-02",  239),
    ("ch09-02-05",         "9.2.5",  "Land Combat",                                        "ch09-02",  241),
    ("ch09-02-06",         "9.2.6",  "Electronic Warfare",                                 "ch09-02",  243),
    ("ch09-02-07",         "9.2.7",  "Damage and Repairs",                                 "ch09-02",  244),
    ("ch09-02-08",         "9.2.8",  "My Weapon Won't Fire!!!",                            "ch09-02",  244),
    ("ch09-02-09",         "9.2.9",  "DLZ And Why It Matters",                             "ch09-02",  254),
    ("ch09-02-10",         "9.2.10", "A Note On Losses",                                   "ch09-02",  257),
    ("ch09-03",            "9.3",    "Construction and Destruction",                       "ch09",     258),
    ("ch09-03-01",         "9.3.1",  "Building and Destroying Air Bases",                  "ch09-03",  258),
    ("ch09-03-02",         "9.3.2",  "Building and Destroying Naval Bases",                "ch09-03",  264),
    ("ch09-03-03",         "9.3.3",  "Building and Destroying Air Defenses",               "ch09-03",  264),

    # Chapter 10
    ("ch10",               "10",     "Appendices",                                         None,       266),
    ("ch10-01",            "10.1",   "Keyboard Commands",                                  "ch10",     266),
    ("ch10-02",            "10.2",   "Custom Overlays 101",                                "ch10",     269),
    ("ch10-03",            "10.3",   "Database Edit Capability in the Scenario Editor",    "ch10",     271),
    ("ch10-04",            "10.4",   "Common Air Units",                                   "ch10",     275),
    ("ch10-04-01",         "10.4.1", "Early/Mid Cold War",                                 "ch10-04",  275),
    ("ch10-04-02",         "10.4.2", "Late Cold War/Modern",                               "ch10-04",  275),
    ("ch10-04-03",         "10.4.3", "Aircraft Organization",                              "ch10-04",  277),
    ("ch10-05",            "10.5",   "Common Sea Units",                                   "ch10",     277),
    ("ch10-06",            "10.6",   "Common Naval Tactics",                               "ch10",     278),
    ("ch10-07",            "10.7",   "Comms Disruption & Cyber Attacks",                   "ch10",     280),
    ("ch10-08",            "10.8",   "Tacview",                                            "ch10",     288),

    # Chapter 11
    ("ch11",               "11",     "Glossary",                                           None,       290),

    # Chapter 12
    ("ch12",               "12",     "COMMAND Update History",                             None,       291),

    # Chapter 13
    ("ch13",               "13",     "Credits",                                            None,       351),
]

PDF_TOTAL_PAGES = 353


# ---------------------------------------------------------------------------
# Build entry dicts with computed end_page
# ---------------------------------------------------------------------------
def build_entries():
    entries = []
    for i, (slug, num, title, parent, start) in enumerate(TOC_RAW):
        # end_page = start of the next entry - 1, or total pages
        if i + 1 < len(TOC_RAW):
            next_start = TOC_RAW[i + 1][4]
            end = max(start, next_start - 1)
        else:
            end = PDF_TOTAL_PAGES
        entries.append({
            "slug":   slug,
            "num":    num,
            "title":  title,
            "parent": parent,
            "start":  start,
            "end":    end,
        })
    return entries


# ---------------------------------------------------------------------------
# Convert PDF pages to JPEG images
# ---------------------------------------------------------------------------
def convert_pages(entries, dpi=130, quality=82):
    print("Converting PDF pages to images …")
    needed_pages = set()
    for e in entries:
        for p in range(e["start"], e["end"] + 1):
            needed_pages.add(p)

    total = len(needed_pages)
    for idx, page in enumerate(sorted(needed_pages), 1):
        img_path = IMAGES_DIR / f"page-{page:03d}.jpg"
        if img_path.exists():
            continue
        if idx % 20 == 0 or idx == 1:
            print(f"  Rendering page {page} ({idx}/{total}) …")
        subprocess.run(
            [
                "pdftoppm",
                "-jpeg", f"-jpegopt", f"quality={quality}",
                "-r", str(dpi),
                "-f", str(page),
                "-l", str(page),
                "-singlefile",
                str(PDF_PATH),
                str(IMAGES_DIR / f"page-{page:03d}"),
            ],
            check=True, capture_output=True,
        )
    print("  Done.")


# ---------------------------------------------------------------------------
# Build TOC tree for sidebar rendering
# ---------------------------------------------------------------------------
def build_tree(entries):
    """Return top-level entries list; each entry has .children list."""
    by_slug = {e["slug"]: dict(e, children=[]) for e in entries}
    roots = []
    for e in entries:
        node = by_slug[e["slug"]]
        if e["parent"] and e["parent"] in by_slug:
            by_slug[e["parent"]]["children"].append(node)
        else:
            roots.append(node)
    return roots, by_slug


# ---------------------------------------------------------------------------
# HTML helpers
# ---------------------------------------------------------------------------
SITE_TITLE = "COMMAND: Modern Operations — Manual"

def page_img_tag(page_num, prefix="../"):
    return (
        f'<img src="{prefix}assets/images/page-{page_num:03d}.jpg" '
        f'alt="Page {page_num}" class="manual-page-img" loading="lazy">'
    )

def render_sidebar_node(node, active_slug, prefix="../", depth=0):
    slug      = node["slug"]
    title     = node["title"]
    num       = node["num"]
    children  = node["children"]
    label     = f"{num}&nbsp;&nbsp;{title}" if num else title
    href      = f"{prefix}pages/{slug}.html"
    is_active = slug == active_slug
    has_kids  = bool(children)

    li_cls = "active" if is_active else ""
    details_open = "open" if is_active or any_descendant_active(node, active_slug) else ""

    if has_kids:
        child_html = "".join(
            render_sidebar_node(c, active_slug, prefix, depth + 1)
            for c in children
        )
        return (
            f'<li class="{li_cls}">'
            f'<details {details_open}>'
            f'<summary><a href="{href}">{label}</a></summary>'
            f'<ul>{child_html}</ul>'
            f'</details></li>'
        )
    else:
        return f'<li class="{li_cls}"><a href="{href}">{label}</a></li>'


def any_descendant_active(node, active_slug):
    for c in node["children"]:
        if c["slug"] == active_slug or any_descendant_active(c, active_slug):
            return True
    return False


def render_sidebar(roots, active_slug, prefix="../"):
    items = "".join(render_sidebar_node(n, active_slug, prefix) for n in roots)
    return (
        f'<nav id="sidebar">'
        f'<a class="sidebar-home" href="{prefix}index.html">📖 Manual Index</a>'
        f'<ul id="toc-tree">{items}</ul>'
        f'</nav>'
    )


SHARED_CSS = """\
/* ------------------------------------------------------------------ */
/* CMO Manual — shared stylesheet                                      */
/* ------------------------------------------------------------------ */
:root {
  --bg:        #1a1c22;
  --sidebar-bg:#13151a;
  --sidebar-w: 280px;
  --accent:    #4d9de0;
  --text:      #dde1ea;
  --muted:     #7a8099;
  --border:    #2e3240;
  --page-max:  860px;
}

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

html { font-size: 15px; }
body {
  display: flex;
  min-height: 100vh;
  background: var(--bg);
  color: var(--text);
  font-family: 'Segoe UI', system-ui, sans-serif;
  line-height: 1.55;
}

/* ---- Sidebar ---- */
#sidebar {
  position: fixed;
  top: 0; left: 0;
  width: var(--sidebar-w);
  height: 100vh;
  background: var(--sidebar-bg);
  border-right: 1px solid var(--border);
  overflow-y: auto;
  padding: 0 0 2rem;
  z-index: 100;
  display: flex;
  flex-direction: column;
}

.sidebar-home {
  display: block;
  padding: .9rem 1rem;
  font-weight: 700;
  font-size: .85rem;
  letter-spacing: .04em;
  text-transform: uppercase;
  color: var(--accent);
  text-decoration: none;
  border-bottom: 1px solid var(--border);
  background: rgba(77,157,224,.07);
}
.sidebar-home:hover { background: rgba(77,157,224,.15); }

#toc-tree, #toc-tree ul {
  list-style: none;
}
#toc-tree { margin-top: .25rem; }

#toc-tree li a {
  display: block;
  padding: .35rem 1rem .35rem 1.1rem;
  color: var(--text);
  text-decoration: none;
  font-size: .83rem;
  border-left: 3px solid transparent;
  transition: background .15s, border-color .15s;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
#toc-tree li a:hover  { background: rgba(255,255,255,.05); border-left-color: var(--muted); }
#toc-tree li.active > a,
#toc-tree li.active > details > summary a {
  color: var(--accent);
  border-left-color: var(--accent);
  font-weight: 600;
}

/* nested indentation */
#toc-tree ul      { padding-left: 1rem; }
#toc-tree ul ul   { padding-left: .75rem; }

/* collapsible details */
details > summary {
  list-style: none;
  cursor: pointer;
}
details > summary::before {
  content: '▶ ';
  font-size: .65rem;
  color: var(--muted);
  display: inline-block;
  margin-right: .2rem;
  transition: transform .2s;
}
details[open] > summary::before { transform: rotate(90deg); }

/* ---- Main content ---- */
#main {
  margin-left: var(--sidebar-w);
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

header.page-header {
  padding: 1.5rem 2rem 1rem;
  border-bottom: 1px solid var(--border);
}
header.page-header .section-num {
  font-size: .75rem;
  text-transform: uppercase;
  letter-spacing: .06em;
  color: var(--muted);
  margin-bottom: .3rem;
}
header.page-header h1 {
  font-size: 1.55rem;
  color: var(--accent);
  font-weight: 700;
}

.page-images {
  padding: 1.5rem 2rem;
  max-width: var(--page-max);
  display: flex;
  flex-direction: column;
  gap: .5rem;
}

.manual-page-img {
  width: 100%;
  border: 1px solid var(--border);
  border-radius: 4px;
  box-shadow: 0 4px 16px rgba(0,0,0,.4);
  background: #fff;
}

/* ---- Footer nav ---- */
.page-nav {
  margin: 1rem 2rem 2.5rem;
  display: flex;
  gap: 1rem;
}
.page-nav a {
  display: inline-flex;
  align-items: center;
  gap: .4rem;
  padding: .55rem 1.1rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  color: var(--accent);
  text-decoration: none;
  font-size: .85rem;
  transition: background .15s;
}
.page-nav a:hover { background: rgba(77,157,224,.1); }

/* ---- Index page ---- */
.index-body { margin-left: 0 !important; }
#index-main {
  max-width: 900px;
  margin: 0 auto;
  padding: 3rem 2rem 4rem;
}
#index-main h1 {
  font-size: 2rem;
  color: var(--accent);
  margin-bottom: .4rem;
}
#index-main .subtitle {
  color: var(--muted);
  margin-bottom: 2.5rem;
  font-size: .95rem;
}

.toc-section { margin-bottom: 1.8rem; }
.toc-section h2 {
  font-size: 1rem;
  text-transform: uppercase;
  letter-spacing: .07em;
  color: var(--muted);
  border-bottom: 1px solid var(--border);
  padding-bottom: .35rem;
  margin-bottom: .6rem;
}
.toc-chapter {
  margin-bottom: 1rem;
}
.toc-chapter > a.chapter-link {
  display: block;
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--accent);
  text-decoration: none;
  padding: .3rem 0;
}
.toc-chapter > a.chapter-link:hover { text-decoration: underline; }
.toc-chapter .toc-children { padding-left: 1.2rem; }
.toc-chapter .toc-children .toc-children { padding-left: 1.1rem; }

.toc-section-link {
  display: flex;
  align-items: baseline;
  gap: .5rem;
  padding: .15rem 0;
  color: var(--text);
  text-decoration: none;
  font-size: .9rem;
}
.toc-section-link .sec-num {
  color: var(--muted);
  font-size: .8rem;
  min-width: 3rem;
  flex-shrink: 0;
}
.toc-section-link:hover { color: var(--accent); }

/* mobile toggle */
#sidebar-toggle {
  display: none;
  position: fixed;
  top: .7rem; left: .7rem;
  z-index: 200;
  background: var(--accent);
  color: #fff;
  border: none;
  border-radius: 4px;
  padding: .4rem .7rem;
  font-size: 1.1rem;
  cursor: pointer;
}

@media (max-width: 720px) {
  :root { --sidebar-w: 260px; }
  #sidebar { transform: translateX(-100%); transition: transform .25s; }
  #sidebar.open { transform: translateX(0); }
  #main { margin-left: 0; }
  #sidebar-toggle { display: block; }
  header.page-header h1 { font-size: 1.2rem; }
}
"""

SHARED_JS = """\
document.addEventListener('DOMContentLoaded', () => {
  const btn = document.getElementById('sidebar-toggle');
  const sidebar = document.getElementById('sidebar');
  if (btn && sidebar) {
    btn.addEventListener('click', () => sidebar.classList.toggle('open'));
  }
  // lazy-load images as they scroll into view
  if ('IntersectionObserver' in window) {
    const imgs = document.querySelectorAll('img[loading="lazy"]');
    const obs = new IntersectionObserver((entries) => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.src = e.target.src; } });
    });
    imgs.forEach(img => obs.observe(img));
  }
});
"""


def html_shell(title, body_content, extra_head="", body_class=""):
    cls_attr = f' class="{body_class}"' if body_class else ""
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{title} — {SITE_TITLE}</title>
  <link rel="stylesheet" href="../assets/style.css">
  {extra_head}
</head>
<body{cls_attr}>
  <button id="sidebar-toggle" aria-label="Toggle menu">☰</button>
{body_content}
  <script src="../assets/manual.js"></script>
</body>
</html>"""


def index_html_shell(title, body_content):
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{SITE_TITLE}</title>
  <link rel="stylesheet" href="assets/style.css">
</head>
<body>
{body_content}
  <script src="assets/manual.js"></script>
</body>
</html>"""


# ---------------------------------------------------------------------------
# Generate index.html
# ---------------------------------------------------------------------------
def generate_index(roots):
    print("Generating index.html …")

    def render_toc_node(node, level=0):
        slug    = node["slug"]
        num     = node["num"]
        title   = node["title"]
        kids    = node["children"]
        label   = f"{num} {title}" if num else title
        href    = f"pages/{slug}.html"

        if level == 0:
            # Top-level chapter heading
            kid_html = ""
            if kids:
                kid_items = "".join(render_toc_node(c, level + 1) for c in kids)
                kid_html = f'<div class="toc-children">{kid_items}</div>'
            return (
                f'<div class="toc-chapter">'
                f'<a class="chapter-link" href="{href}">{label}</a>'
                f'{kid_html}'
                f'</div>'
            )
        else:
            kid_html = ""
            if kids:
                kid_items = "".join(render_toc_node(c, level + 1) for c in kids)
                kid_html = f'<div class="toc-children">{kid_items}</div>'
            num_span = f'<span class="sec-num">{num}</span>' if num else ""
            return (
                f'<a class="toc-section-link" href="{href}">{num_span}{title}</a>'
                f'{kid_html}'
            )

    front_roots   = [n for n in roots if not n["num"]]
    chapter_roots = [n for n in roots if n["num"]]

    front_html   = "".join(render_toc_node(n) for n in front_roots)
    chapter_html = "".join(render_toc_node(n) for n in chapter_roots)

    body = f"""
<div id="index-main">
  <h1>COMMAND: Modern Operations</h1>
  <p class="subtitle">Game Manual — HTML Edition</p>

  <div class="toc-section">
    <h2>Front Matter</h2>
    {front_html}
  </div>

  <div class="toc-section">
    <h2>Contents</h2>
    {chapter_html}
  </div>
</div>
"""
    html = index_html_shell(SITE_TITLE, body)
    (OUT_DIR / "index.html").write_text(html, encoding="utf-8")
    print("  → index.html")


# ---------------------------------------------------------------------------
# Generate one section page
# ---------------------------------------------------------------------------
def generate_page(entry, flat_entries, roots):
    slug   = entry["slug"]
    num    = entry["num"]
    title  = entry["title"]
    start  = entry["start"]
    end    = entry["end"]

    # Find prev / next in flat list
    flat_slugs = [e["slug"] for e in flat_entries]
    idx        = flat_slugs.index(slug)
    prev_entry = flat_entries[idx - 1] if idx > 0                      else None
    next_entry = flat_entries[idx + 1] if idx < len(flat_entries) - 1  else None

    num_str = f"{num} — " if num else ""
    heading = f"{num_str}{title}"

    # Page images (prefix one level up from pages/ dir)
    imgs = "".join(page_img_tag(p) for p in range(start, end + 1))

    # Prev/next nav
    nav_parts = []
    if prev_entry:
        prev_label = f"{prev_entry['num']} {prev_entry['title']}" if prev_entry["num"] else prev_entry["title"]
        nav_parts.append(f'<a href="{prev_entry["slug"]}.html">← {prev_label}</a>')
    if next_entry:
        next_label = f"{next_entry['num']} {next_entry['title']}" if next_entry["num"] else next_entry["title"]
        nav_parts.append(f'<a href="{next_entry["slug"]}.html">{next_label} →</a>')
    nav_html = f'<div class="page-nav">{"".join(nav_parts)}</div>' if nav_parts else ""

    sidebar = render_sidebar(roots, slug)

    body = f"""{sidebar}
<div id="main">
  <header class="page-header">
    {'<div class="section-num">Section ' + num + '</div>' if num else ''}
    <h1>{title}</h1>
  </header>
  <div class="page-images">
    {imgs}
  </div>
  {nav_html}
</div>"""

    html = html_shell(heading, body)
    out_path = PAGES_DIR / f"{slug}.html"
    out_path.write_text(html, encoding="utf-8")


# ---------------------------------------------------------------------------
# Write shared assets
# ---------------------------------------------------------------------------
def write_assets():
    (ASSETS_DIR / "style.css").write_text(SHARED_CSS, encoding="utf-8")
    (ASSETS_DIR / "manual.js").write_text(SHARED_JS, encoding="utf-8")
    print("  → assets/style.css, assets/manual.js")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main():
    print(f"Output dir: {OUT_DIR}")
    PAGES_DIR.mkdir(parents=True, exist_ok=True)
    IMAGES_DIR.mkdir(parents=True, exist_ok=True)

    flat_entries = build_entries()
    roots, by_slug = build_tree(flat_entries)

    # 1. Convert pages
    convert_pages(flat_entries, dpi=130, quality=82)

    # 2. Write CSS / JS
    write_assets()

    # 3. Index page
    generate_index(roots)

    # 4. Section pages
    print(f"Generating {len(flat_entries)} section pages …")
    for entry in flat_entries:
        generate_page(entry, flat_entries, roots)
    print(f"  Done — {len(flat_entries)} pages written.")

    print("\n✓ All done. Open docs/manual/index.html in a browser.")


if __name__ == "__main__":
    main()
