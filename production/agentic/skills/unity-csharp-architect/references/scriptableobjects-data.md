# ScriptableObject data ownership

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §5.3 (ScriptableObjects)  
**Program:** UCA-M2 · **Audience:** agents authoring Unity assets, panel settings, designer config  
**Implements:** DRG-130 (UCA-06)

> **Law (one line):** ScriptableObjects hold **designer/config data** (and Unity engine settings) — never live authoritative sim / order-log / catalog truth. Runtime authority stays in headless .NET.

**Related:** [`presentation-boundary.md`](presentation-boundary.md) · [`headless-command-ui.md`](headless-command-ui.md) · [`asmdefs-and-layers.md`](asmdefs-and-layers.md) · ADR-006 (no SQLite from UI)

---

## 1. Ownership matrix

| Kind of data | Owner | Not on SO / scene |
| --- | --- | --- |
| Platform catalog, mounts, sensors | `ProjectAegis.Data` + SQLite / packages | MB fields, SO “live catalog” |
| Scenario package, ORBAT, missions | Data / scenario services / CLI-MCP | Inspector-only truth |
| Orders, DecisionLog, alive/contacts | Sim + Delegation | SO mutable fields |
| Selection, camera, panel layout prefs | Presentation (`C2PresentationController`, hosts) | Order log / replay hash |
| USS theme tokens, PanelSettings, addressable keys | Unity assets / SO **config** | Sim fingerprint |
| Designer constants (icon atlas id, default zoom) | SO or Addressables | Per-entity magazines |

**Aegis today:** production SO usage is thin (e.g. `ScriptableObject.CreateInstance<PanelSettings>()` for UI Toolkit bootstrap). Catalog and scenario truth are **not** SO-driven — do not invent a parallel SO world model.

---

## 2. Prefer / avoid

| Prefer | Avoid |
| --- | --- |
| SO for **immutable-ish designer config** and engine settings | SO as second sim: HP, fuel, contact lists, ROE live state |
| Load catalog via Data / approved readers (`ICatalogReader`) | `Resources.Load` / SO that opens SQLite (ADR-006) |
| Scene refs + injected services at composition root | Public mutable SO fields mutated every tick as authority |
| Addressables / explicit asset refs | Hidden `Resources` bags for production content |
| Validate config at load / import gate | Trusting inspector-only values for export/play authority |
| Headless fixtures for scenario data | Play Mode–only SO that cannot be asserted under `dotnet test` |

---

## 3. Patterns

### 3.1 Good — config shell

```csharp
// GOOD: SO describes presentation chrome defaults, not world truth
[CreateAssetMenu(menuName = "Aegis/UI/MapChromeConfig")]
public sealed class MapChromeConfig : ScriptableObject
{
    [SerializeField] private float _defaultOrthoSize = 12f;
    [SerializeField] private string _friendlyUssClass = "map-symbol--friendly";

    public float DefaultOrthoSize => _defaultOrthoSize;
    public string FriendlyUssClass => _friendlyUssClass;
}

// Host binds projection rows; SO only supplies chrome defaults
sealed class MapPanelHost : MonoBehaviour
{
    [SerializeField] private MapChromeConfig _chrome;
    public void Bind(IReadOnlyList<MapSymbolEntry> symbols) { /* USS from _chrome + rows */ }
}
```

### 3.2 Bad — SO as authority

```csharp
// BAD: SO mutated as live world — breaks determinism, CLI/MCP parity, ADR-010
[CreateAssetMenu]
public sealed class LiveBattleState : ScriptableObject
{
    public List<string> AliveUnitIds = new();
    public void Kill(string id) => AliveUnitIds.Remove(id); // BAD authority
}
```

### 3.3 Catalog / scenario — never SO truth

| Surface | Correct path |
| --- | --- |
| Catalog rows | Data layer / import / write-gate — not SO duplicates |
| Scenario load | Scenario package / CLI verbs / approved loaders |
| UI graph highlights | Read-only catalog projections via `C2PresentationController` |
| Magazines / fuel labels | Projection from snapshot / log → binder |

---

## 4. Runtime vs authoring

| Context | SO role |
| --- | --- |
| **Play Mode / player** | Read config; bind projections; enqueue commands |
| **Editor tools** | May **author** assets that become Data/scenario inputs after validation |
| **Import / Excel** | Data.Excel + gates — SO is not the import authority |
| **Tests** | Prefer plain C# fixtures; SO only when testing Unity serialization seams |

Editor authoring that writes authoritative scenario content must still land in **Data contracts** exportable without the Editor (ADR-010 parity).

---

## 5. c-sharp-engineer cross-cut

| Concern | Expectation |
| --- | --- |
| **Immutability** | Treat runtime-bound SO config as read-mostly; do not share mutable SO as global game state |
| **DI** | Inject or serialize config refs; no service-locator SO singletons for sim |
| **Testing** | Config that affects logic should be expressible as plain DTOs under headless tests |
| **SRP** | One SO type = one config surface (chrome, audio mix, panel defaults) — not “GameManagerSO” |

---

## 6. Agent checklist (before Done)

- [ ] SO fields are **config / presentation**, not sim authority
- [ ] No SO writing DecisionLog, catalogs, or live entity state
- [ ] No production `Resources.Load` as default content path
- [ ] Catalog/scenario still flow through Data / approved readers
- [ ] Presentation still binds `IReadOnly*` projections (ADR-007/010)
- [ ] If SO is new shared asset, document load path + who mutates it (ideally: nobody at runtime)
- [ ] PR cites this skill §5.3 + ADR-010/006 as applicable

---

## 7. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) §5.3 | Parent SO rules |
| [`presentation-boundary.md`](presentation-boundary.md) | What presentation may hold |
| [`headless-command-ui.md`](headless-command-ui.md) | Intent → command (not SO mutation) |
| [`mono-anti-patterns.md`](mono-anti-patterns.md) | God-MB + Find/Resources |
| `docs/architecture/adr-006-*.md` (catalog / no SQLite from UI) | Presentation never opens SQLite |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI is a client |
| `src/ProjectAegis.Data/` | Authoritative catalog/scenario ownership |

**UCA-M2 note:** Structure pack doctrine for data ownership. Do not re-host catalog schemas here.
