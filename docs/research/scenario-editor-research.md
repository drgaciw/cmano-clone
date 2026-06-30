Research Notes: Improving Scenario Editor Requirements for an Agentic CMANO-Style System
Overview
Publicly available material on Command: Modern Air/Naval Operations and Command: Modern Operations shows that the scenario editor is a central authoring tool rather than a simple map-placement utility.[cite:41][cite:45][cite:49] It supports scenario creation, side setup, unit placement, aircraft editing through parent hosts, database selection, scenario feature changes, and scenario database upgrades.[cite:41][cite:42][cite:45][cite:49]
Professional-edition material adds an even stronger signal: an umpire can edit a scenario before and after server-side turn execution, including adding and removing units during adjudication.[cite:24] That implies the editor is not only a design tool but also a live control and exercise-management surface.[cite:24][cite:27]
For an agentic clean-room successor, the scenario editor should therefore be treated as a full scenario authoring, validation, debugging, simulation-control, and adjudication environment.[cite:24][cite:41]
Public product signals
Community tutorials describe the editor as fast and capable, and they highlight practical workflows such as creating a blank scenario, defining sides and postures, selecting a database, placing units, and editing aircraft via their host platforms.[cite:41] The official addendum pages also indicate dedicated scenario-editor interface and functionality changes over time, which suggests the editor is a major product surface receiving ongoing refinement.[cite:45]
A Steam discussion confirms that scenario authors can upgrade a scenario to a newer database build from editor mode.[cite:42] That matters because it shows a hard coupling between scenario content and database versions, which creates migration, validation, and compatibility requirements for any successor tool.[cite:42]
The professional-edition manual snippet further indicates server-side turn execution with umpire edits before and after execution.[cite:24] That makes adjudication, auditability, and role-sensitive editing first-class requirements rather than optional extras.[cite:24]
Requirement areas
Scenario model integrity
The editor should maintain an explicit structured model for sides, posture relationships, unit instances, parent-host relationships, missions, doctrine, events, triggers, objectives, and scenario features.[cite:41][cite:45][cite:49] Public tutorials already show that some objects have hidden constraints, such as aircraft depending on compatible host units and other platforms requiring valid map placement.[cite:41][cite:46]
A stronger requirement set should move these from expert knowledge into system behavior. The editor should validate incompatible host relationships, invalid terrain placement, broken references, and impossible setup states continuously during authoring instead of only reporting them late on save or run.[cite:41][cite:46]
Database binding and migration
Because Command scenarios can be upgraded to a later DB version from editor mode, a replacement editor should include a dedicated migration subsystem with transparency and reversibility.[cite:42] A one-click migration is not enough for professional authoring.
The migration workflow should include:
DB diff preview before applying changes.[cite:42]
Mapping of obsolete platform IDs to successor IDs.[cite:42]
Detection of broken mounts, sensors, loadouts, and doctrine references after migration.[cite:42]
Reversible migration with snapshot/rollback.
Behavior comparison support for pre-upgrade versus post-upgrade scenario baselines.
This area becomes even more important when combined with the public CMO database request process, which shows that data evolves continuously through a formal issue pipeline.[cite:29] A scenario editor for a living database must assume ongoing schema and content drift.[cite:29]
Authoring workflow and usability
Public guides suggest that the current workflow is powerful but menu-heavy and dependent on knowing where specialized actions live.[cite:41][cite:46][cite:49] That implies the strongest successor requirement is not only more functionality but lower authoring friction.
Recommended workflow requirements:
Global command palette for editor actions.
Context-aware inspectors for the selected unit, mission, event, or side.
Multi-select and bulk editing for large orders of battle.
Undo/redo across all authoring operations.
Staged edit transactions so authors can preview and commit changes in batches.
Split-pane authoring with map view, order-of-battle tree, event graph, and validation panel.
Event logic and debugging
Scenario quality depends heavily on event logic, triggers, conditions, and phased objectives rather than just unit placement.[cite:43] That means the editor should treat scenario logic as a core design layer.
Important requirements include:
Visual trigger-condition-action graph editor.
Text scripting for advanced edge cases.
Static analysis for dead triggers, unreachable states, contradictory conditions, and circular dependencies.
Event trace tools explaining why a trigger fired or did not fire.
Isolated test harnesses for event logic without rerunning the entire scenario manually.[cite:43]
Umpire and adjudication mode
The professional-edition material is the clearest signal that the scenario editor also serves live exercise control.[cite:24] A clean-room successor should formalize this into a separate adjudication workspace rather than treating it as ordinary editing.
Requirements should include:
Turn-boundary snapshots.[cite:24]
Before/after diffs for umpire interventions.[cite:24]
Role-based permissions for player, scenario author, reviewer, and umpire.
Audit logging with reasons attached to all live edits.
Freeze, step, inject, and resume controls for adjudicated play.[cite:24]
Testability and experiment handoff
Professional-edition descriptions emphasize Monte Carlo analysis and broader analytical use cases.[cite:24][cite:27] That implies the editor should connect directly to a test and experimentation pipeline rather than ending at manual playtesting.
Recommended requirements:
Scenario smoke tests before publish.
Regression baselines for authored scenarios after rules or DB changes.
Batch-run handoff to experiment runners.
Sensitivity analysis presets for reinforcement timing, loadouts, doctrine packages, and weather assumptions.
Result packaging for after-action review and scenario balancing.[cite:24][cite:27]
AI-assisted authoring
An agentic editor should help with structure and validation, not only text generation. The safest design is an assistant that proposes and explains changes, while a human remains the decision-maker.
Key requirements:
Natural-language brief to draft scenario scaffold, including map bounds, sides, initial forces, and objective skeleton.
Constraint-aware placement assistant that refuses invalid unit-host or map placement combinations.[cite:41][cite:46]
Red-team planning assistant to generate adversary doctrine variants.
Automated smoke-test agent that searches for trivial wins, impossible objectives, orphaned units, and silent failure paths.
Explainability pane that ties every AI suggestion to scenario state evidence and rule checks.
Provenance tags on AI-created or AI-edited content.
Publishing and provenance
The public ecosystem around Command scenarios and database updates suggests that publication should be treated as a governed workflow rather than simple file export.[cite:29][cite:42] The editor should produce structured scenario packages with explicit metadata and validation state.
Recommended requirements:
Scenario manifest with title, synopsis, assumptions, recommended DB version, and change log.
Semantic versioning for scenario releases.
Validation report embedded at publish time.
Provenance fields for imported orders of battle, media, and AI-assisted content.
Review gates for official, community, classroom, and private exercise scenarios.
Proposed requirement statements
Highest-value improvements
The highest-value improvements suggested by the public material are live validation, transparent database migration, visual logic debugging, transaction-based bulk editing, and a dedicated umpire workspace.[cite:24][cite:41][cite:42] These features address the visible weaknesses of an expert-heavy workflow in which some constraints are implicit, database evolution can disrupt authored content, and the editor is used both for design and live exercise control.[cite:24][cite:41][cite:46]
Design implications for a fork
A fork should treat the scenario editor as the orchestration layer above the simulation kernel and database rather than as a secondary utility.[cite:24][cite:41] In practical terms, that means investing early in model validation, event debugging, auditability, migration tooling, and AI review surfaces instead of postponing them until after the base map editor is complete.[cite:24][cite:42]
This approach aligns better with the public Command product signals, especially the combination of database-coupled scenarios, iterative feature expansion, and professional adjudication workflows.[cite:24][cite:42][cite:45]