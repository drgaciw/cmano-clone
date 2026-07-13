# Platform inventory vs exponential QA scenario complexity

**Date:** 2026-07-09  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Purpose:** Size how many ships / aircraft / submarines the catalog needs so QA scenario **variations** grow **exponentially** (product of independent axes), not just linearly with platform count.

## 1. What “exponential” means here

Scenario variety is modeled as a **product of independent discrete axes**, not as a raw count of platforms:

\[
N_{\text{scenarios}} \approx
\underbrace{M}_{\text{mission types}}
\times
\underbrace{D(p)}_{\text{domain mixes enabled by inventory}}
\times
\underbrace{R}_{\text{ROE pairs}}
\times
\underbrace{E}_{\text{EMCON postures}}
\times
\underbrace{S}_{\text{seeds}}
\times
\underbrace{C(p)}_{\text{platform composition choices}}
\]

- Linear growth: only \(C(p)\) grows as \(O(p)\) if you always pick one unit per side.
- **Super-linear / exponential in practical QA terms**: when inventory unlocks **new domain mixes** \(D(p)\) *and* multi-unit compositions \(C(p)\), the product grows much faster than \(p\).

This matches the gauntlet ladder (mission × platform mix × victory × events × ROE × EMCON) where **platform mix is the bottleneck** once missions/ROE/EMCON templates exist.

## 2. Domain-mix function \(D(p)\)

Let \(n_s, n_a, n_u\) = counts of usable **surface / air / subsurface** platforms (excluding pure fixtures if desired).

Enabled mix classes (binary presence thresholds):

| Mix class | Requirement | Gauntlet tiers unlocked |
|---|---|---|
| Surface-only duel | \(n_s \ge 2\) | T1–T2 |
| Surface + air | \(n_s \ge 2, n_a \ge 1\) | T2–T3 air-assisted |
| Surface + sub | \(n_s \ge 2, n_u \ge 1\) | T3 ASW-lite |
| Air + air | \(n_a \ge 2\) | AAW / CAP |
| Sub + surface ASW | \(n_u \ge 1, n_s \ge 1\) | T4 ASW |
| **Joint 3-domain** | \(n_s \ge 2, n_a \ge 2, n_u \ge 2\) | T4–T5 multi-domain |
| **Asymmetric joint** | each domain \(\ge 3\) *per side pool* | T5 theater asymmetry |

Number of non-empty domain-mix *labels* scales as \(2^3-1 = 7\) once all domains are non-empty; with **cardinality** (how many units per domain) mixes become:

\[
D_{\text{card}} \approx \prod_{d \in \{s,a,u\}} (k_d + 1) - 1
\]

where \(k_d = \min(n_d, k_{\max})\) is units drawn per domain (cap \(k_{\max}=2\) or \(3\) for QA budgets).

Example with \(k_{\max}=2\):

| \(n_s,n_a,n_u\) | \(D_{\text{card}}\) |
|---|---|
| (2,0,0) | 2 |
| (2,1,0) | 5 |
| (2,2,1) | 17 |
| (4,3,3) | \(5\times4\times4-1=79\) |
| (8,6,6) | \(3\times3\times3-1=26\) if \(k_{\max}=2\); \(9\times7\times7-1=440\) if \(k_{\max}=3\) |

So once each domain has **≥3–4 distinct platforms**, mix cardinality alone is tens to hundreds of *structures* before ROE/EMCON/seeds.

## 3. Composition choices \(C(p)\)

For a fixed mix structure (e.g. 1 surface + 1 air vs 1 surface + 1 sub):

\[
C \approx \prod_{\text{slots}} n_{d(\text{slot})}
\]

- 1v1 surface: \(C = n_s(n_s-1)\)  
- Blue surface+air vs Red surface+sub: \(C = n_s \cdot n_a \cdot n_s \cdot n_u\)

With \(n_s=n_a=n_u=6\): \(C = 6\cdot6\cdot6\cdot6 = 1296\) for that one structure alone.

## 4. Fixed QA axes (templates already in gauntlet)

| Axis | Count | Notes |
|---|---|---|
| Mission types \(M\) | 5 | patrol, strike, escort, multi-mission, theater |
| ROE pairs \(R\) | 4 | free/free, free/tight, tight/free, asymmetric |
| EMCON \(E\) | 3 | unrestricted, passive-one-side, phased |
| Seeds \(S\) | 3 | e.g. 42, 7, 123 |

Product of fixed axes: \(M \times R \times E \times S = 5 \times 4 \times 3 \times 3 = \mathbf{180}\).

Total scenario ballpark:

\[
N \approx 180 \times D_{\text{card}} \times \bar{C}
\]

where \(\bar{C}\) is average compositions per mix (often \(\ge 10\) once \(n_d \ge 4\)).

| Inventory (usable non-seed) | \(D_{\text{card}}\) (\(k_{\max}=2\)) | \(\bar{C}\) (order) | \(N\) order |
|---|---|---|---|
| Seed only (3 surface) | ~2 | ~2 | ~\(10^3\) |
| Prior multi-domain (~16 new: 6s/6a/4u) | ~35 | ~20 | ~\(10^5\) |
| **Target expansion** (per domain **8+**) | ~80 | ~50–100 | ~\(10^6\) |
| Full corpus (100s per domain) | capped by \(k_{\max}\) | huge | wasteful for CI |

**Interpretation:** Moving from seed-only to **≥8 platforms per domain** multiplies scenario space by **orders of magnitude** (\(10^3 \rightarrow 10^6\))—**exponential in the product sense**—without needing the entire cmo-db.com corpus.

## 5. Recommended minimum inventory

| Domain | Minimum for exponential jump | Stretch (theater QA) |
|---|---|---|
| Surface (ships) | **8** distinct combatants (2+ per major nationality pool) | 12–16 |
| Air (aircraft) | **8** (fighters + 1–2 support/MP) | 12 |
| Subsurface | **6** (SSK diversity + 1 opposing class) | 8–10 |
| **Total platforms** | **~22+ multi-domain** (excluding 3 Baltic seeds) | **~35–40** |

**Plus related data (hard requirements for non-stub use):**
- ≥1 sensor row per combat platform (or shared class sensor)
- ≥1 weapon with **max_range_meters > 0** linked via magazine/mount for showcase units
- Capacity-consistent magazines (PLE-MAG-CAPACITY)

## 6. Comparison to catalog (this goal — post expansion)

| Metric | Pre expansion | Recommended min | **Post expansion** |
|---|---|---|---|
| Total platforms | 19 | ~35–40 stretch / 22+ multi-domain | **43** |
| Surface | 9 | ≥ 15 | **19** |
| Air | 6 | ≥ 12 | **14** |
| Subsurface | 4 | ≥ 8 | **10** |
| Weapons max_range>0 | — | all combat | **172 / 172** (0 zero-range) |

**Verdict:** Catalog now meets/exceeds the **recommended minimum for exponential jump** (each domain ≥8 multi-domain combat inventory when counting non-seed: surface 16, air 14, sub 10). Scenario product \(N pprox 180 	imes D_{card} 	imes ar C\) moves into **~10⁶** order under \(k_{max}=2–3\).

Prior import unlocked joint 3-domain mixes; **this expansion** grows \(C(p)\) and asymmetric nationality pools (Sweden/Finland/Denmark/Germany/Russia/Poland/…).

## 7. Practical QA policy

1. **CI / gauntlet:** sample \(\le 4\) scenarios/tier × 3 seeds using **rotating** platform picks from each domain pool (do not enumerate \(10^6\)).
2. **Catalog depth:** stop expanding when each domain has **≥8** distinct *combat* platforms with weapons; further growth yields diminishing returns under \(k_{\max}\).
3. **Quality over raw count:** a platform without sensors/weapons does **not** increase \(C(p)\) for combat scenarios—only ORBAT chrome.

## 8. Legal

Harvest is internal design reference via community viewer cmano-db.com; TDM rights reserved by the site. Do not ship the CMO proprietary product database wholesale.
