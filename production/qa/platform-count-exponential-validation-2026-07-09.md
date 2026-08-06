# Validation: platform count vs exponential scenario complexity

**Date:** 2026-07-09  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Method:** Parallel agents (inventory / combat-readiness / complexity math) + live SQLite confirmation  
**Related model:** `platform-count-complexity-analysis-2026-07-09.md`

## Verdict: **PASS**

Current combat-ready inventory meets the recommended floor (**≥8 platforms per domain**) and supports **orders-of-magnitude** growth in discrete scenario space relative to seed-only (~10³ → ~10⁵–10⁶ under averaged \(C\); ≫10⁶ for a single multi-domain composition class).

---

## Live inventory (confirmed)

| Metric | Value |
|--------|------:|
| Total platforms | **43** |
| Surface (raw) | **19** |
| Air (raw) | **14** |
| Subsurface (raw) | **10** |
| Seeds (`u1`, `hostile-1`, `hostile-far`) | **3** |
| Imported multi-domain | **40** |
| Weapons with `max_range_meters > 0` | **172 / 172** |

### Combat-ready pools

**Definition:** ≥1 `sensor` row **and** ≥1 `platform_magazine` → `weapon_catalog` with `max_range_meters > 0`.

| Domain | Total | Combat-ready | Chrome / incomplete |
|--------|------:|-------------:|--------------------:|
| Surface | 19 | **17** | 2 (`hostile-1`, `hostile-far`) |
| Air | 14 | **13** | 1 (`p-3c-orion-update-iii-2023` — sensors, no weapons) |
| Subsurface | 10 | **10** | 0 |
| **All** | **43** | **40** | **3** |

Threshold check (≥8 combat-ready per domain): **surface 17 ✓ · air 13 ✓ · sub 10 ✓**

---

## Complexity model

\[
N \approx M \times D_{\mathrm{card}} \times R \times E \times S \times \bar{C}
\]

| Axis | Value |
|------|------:|
| Mission \(M\) | 5 |
| ROE \(R\) | 4 |
| EMCON \(E\) | 3 |
| Seeds \(S\) | 3 |
| **Fixed product** | **180** |

\[
D_{\mathrm{card}} = \prod_{d}(k_d+1)-1,\quad k_d=\min(n_d,k_{\max})
\]

With combat-ready \((n_s,n_a,n_u)=(17,13,10)\):

| \(k_{\max}\) | \(D_{\mathrm{card}}\) |
|-------------:|----------------------:|
| 2 | **26** |
| 3 | **63** |

Representative multi-domain composition (blue surface+air vs red surface+sub):

\[
C = n_s^2 \cdot n_a \cdot n_u = 17^2 \times 13 \times 10 = \mathbf{37\,570}
\]

| Estimate path | \(N\) order |
|---------------|------------|
| Seed-only (\(\sim 180\times2\times2\)) | **~10³** (720) |
| Current, \(k_{\max}=2\), \(\bar{C}=50\)–100 | **~2–5×10⁵** |
| Current, \(k_{\max}=3\), \(\bar{C}=100\) | **~1.1×10⁶** |
| Current, one multi-domain structure only (\(180\times D\times C\)) | **~10⁸** |

Fold vs seed-only: **~5×10² – 1.5×10³×** (product sense, not linear in platform count).

---

## Domain-mix unlocks (gauntlet)

| Mix class | Requirement | Status |
|-----------|-------------|--------|
| Surface duel | \(n_s\ge2\) | ✓ |
| Surface + air | \(n_s\ge2,n_a\ge1\) | ✓ |
| Surface + sub / ASW | \(n_u\ge1,n_s\ge1\) | ✓ |
| Air + air | \(n_a\ge2\) | ✓ |
| Joint 3-domain | each ≥2 | ✓ |
| Asymmetric joint | each ≥3 | ✓ |

---

## Caveats (do not over-claim)

1. **\(k_{\max}\) caps \(D_{\mathrm{card}}\)** — once each domain has ≥3 units under \(k_{\max}=2\)–3, extra platforms grow **composition \(C\)**, not mix-structure count.
2. **Near-duplicates** (Gotland year variants, Gripen batches) inflate \(n_d\) more than doctrinal diversity.
3. **Chrome platforms** do not increase combat \(C(p)\); use combat-ready counts (17/13/10), not raw 19/14/10.
4. **CI must sample**, not enumerate \(N\); gauntlet policy remains rotating picks per tier.
5. Optional quality gap: arm the P-3C (or exclude MPA from combat pool) if maritime-patrol scenarios matter.

---

## Parallel validation streams

| Stream | Result |
|--------|--------|
| Inventory agent | 43 total; 19/14/10 by domain; 3 seeds + 40 imports |
| Combat-readiness agent | 40 combat-ready; 3 chrome fails; 172/172 weapons ranged |
| Math agent | PASS ≥8/domain; \(N\sim10^5\)–\(10^6+\) vs seed \(10^3\) |

---

## Policy recommendation

- **Stop bulk catalog expansion** for complexity purposes: exponential-jump floor is met.
- Further growth is optional stretch (theater nationality pools, MPA weapons, land/facility domains) — not required for product-space jump.
- Gauntlet: keep sampling ≤4 scenarios/tier × seeds with rotating domain picks.
