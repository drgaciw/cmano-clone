UNITED STATES MARINE CORPS
PROJECT AEGIS SIMULATION TEAM
AEGIS RESEARCH AND DEVELOPMENT
PSC 20096
CAMP LEJEUNE, NORTH CAROLINA 28542-0096

IN REPLY REFER TO:
3504
20 JUN 2026

From: Project Aegis Simulation Team
To: Marine Corps Center for Lessons Learned (MCCLL)
Via: (1) Aegis Program Lead
Subj: PROJECT AEGIS BALTIC THEATER SIMULATION EXERCISE AFTER ACTION REPORT
Ref: (a) Project Aegis RC1 Development Plan
     (b) Baltic Patrol Scenario Policies
Encl: (1) Game Play Session Logs (attached separately)

1. Situation

During the period 20 June 2026, the Project Aegis Simulation Team conducted a series of simulated Baltic patrol and mission operations to evaluate core game play mechanics in the Aegis simulation system. Multiple playthroughs were executed using varied random seeds and scenario configurations to assess AI agent decision-making, sensor detection, engagement outcomes, communications effects, and mission scripting under realistic contested conditions. Sessions included standard patrol, comms-challenged operations, mission execution with contact windows, and jammed sensor environments. All sessions utilized Weapons Free rules of engagement and focused on a single primary hostile target (hostile-1) for consistency in analysis.

The exercise provided direct observation of game play results including contact detection, autonomous agent decisions, weapon launches, outcome resolution (kill/miss), resource management (magazine), and external factors (comms/jamming). Three primary play sessions plus supporting runs were analyzed for this report.

2. Topic/Discussion/Recommendations (TDR)

a. Topic 1: Persistent Re-Engagement of Neutralized Targets

   Topic Area: S-3 Operations / Fires

   Impact: Moderate negative on enterprise level (simulates wasted ordnance and potential real-world inefficiency in force application)

   DOTMLPF-P: Materiel, Training

   Warfighting Function: Fires, Maneuver, Logistics

   Discussion: Across multiple play sessions (notably Session 1 with seed 123 and Session 3 with seed 789), after achieving an initial confirmed kill on hostile-1, the AI agent (a1 controlling unit u1) continued to select Engage actions on the lost contact for the remainder of the scenario. This resulted in 10+ additional engagement attempts logged as TARGET_DESTROYED with no additional tactical effect. In Session 1, this occurred over 15 ticks with scores ranging from 0.16 to 3.48. Similar behavior was noted in other runs. While persistent pursuit can be doctrinally sound in some contexts, the lack of target status assessment led to inefficient ammunition expenditure and unrealistic follow-on actions on a neutralized threat. Comms-challenged and jammed scenarios showed different patterns due to external constraints.

   Recommendation: Update the AI decision policy (PatrolCandidateEngagePolicy and related candidate generation) to incorporate a check for target status using order log outcomes or sensor data before assigning high-priority Engage on previously engaged contacts. Add logic to deprioritize or abort re-engagement on confirmed destroyed targets. Incorporate this into training data for the agent and test via additional simulation runs. This will improve realism in force application and resource management.

b. Topic 2: Communications Degradation as a Decisive Game Play Factor

   Topic Area: S-6 Communications / Information

   Impact: Positive to moderate (enhances realism and introduces meaningful friction without breaking core loop)

   DOTMLPF-P: Materiel, Doctrine

   Warfighting Function: Command and Control, Information, Force Protection

   Discussion: In the comms-challenged patrol session (seed 456, baltic-patrol-comms scenario), initial engagement achieved a kill on hostile-1 (PkDraw 0.59 after an early miss). However, progressive degradation of brigade-net from Nominal to Degraded (due to jamming at tick 8) and then to Denied (link-down at tick 12+) completely prevented further launches. Multiple subsequent Engage decisions resulted in PolicyDenial (CommsDenied). This turned an otherwise successful patrol into a mission where the commander lost the ability to act mid-execution. The jammed scenario (seed 999, baltic-patrol-jammed) showed all 6 attempts failing with NO_FIRE_CONTROL_TRACK due to sensor issues, preventing any engagement. These mechanics added significant depth and "player" consequence to the game play.

   Recommendation: Retain and expand the comms and jamming model as a core game play element. Develop additional scenarios that force players to plan around comms windows or use alternative C2 methods (e.g., pre-planned fires, delegation adjustments). Update doctrine references in game briefings to reflect electronic warfare impacts on engagement. Consider adding player tools for comms mitigation in future iterations.

c. Topic 3: Initial Misses and Persistence Leading to Eventual Success

   Topic Area: S-2 Intelligence / Fires

   Impact: Positive (demonstrates value of persistence and realistic Pk variance)

   DOTMLPF-P: Training, Materiel

   Warfighting Function: Fires, Intelligence

   Discussion: In the mission execution session (seed 789), the contact window event triggered detection, but the first two engagements resulted in misses (PkDraw 0.91 and 0.95). The third attempt achieved a kill (PkDraw 0.58). Similar variance was seen in other sessions (e.g., early miss then kill in comms session). The AI agent persisted with Engage decisions despite initial failures, eventually succeeding. This mirrors real combat friction but highlighted that pure high-bias Engage policy can lead to resource drain before success.

   Recommendation: Enhance sensor and Pk modeling to provide more player feedback on why misses occur (e.g., environmental factors, target aspect). Introduce limited "re-target" or "adjust fire" options for the AI/player to improve success rate without endless re-attempts. Add training scenarios focused on patience and assessment before follow-on shots.

d. Topic 4: Value of Structured Mission Events in Enhancing Game Play Narrative

   Topic Area: S-3 Operations

   Impact: Positive (adds depth and player agency to otherwise reactive patrols)

   DOTMLPF-P: Doctrine, Training

   Warfighting Function: Command and Control, Maneuver

   Discussion: The inclusion of MissionTransition and EventFired (contact-window open) in the mission scenario created a clear narrative arc: planning to execution, opportunity window for detection, then combat. This structured the play beyond simple "detect and engage" and forced timing considerations. In contrast, pure patrol scenarios felt more open-ended but less purposeful. All sessions benefited from the contact detection leading to focused action.

   Recommendation: Expand the use of mission scripting and event triggers across more scenarios. Develop a library of "play" packages with varying objectives (e.g., escort, SEAD, BDA follow-up). Incorporate these into player briefings and after-action tools to increase replayability and training value.

3. Directed Questions

N/A for this development-focused AAR. If applicable to future MCCRE-style evaluations of the simulation as a training tool, insert specific questions per template.

4. Conclusion

The Baltic theater simulation exercise successfully exercised the core game play loop of detection, decision, engagement, and outcome in varied conditions. The sessions provided valuable insights into AI persistence, the decisive role of communications and sensors, and the benefits of structured mission elements. While core mechanics are sound and produce engaging, realistic friction, refinements to target assessment logic and player feedback will improve both fidelity and user experience. The exercise met its objectives of validating RC1 game play and identifying actionable improvements. Overall assessment: effective with minor adjustments required.

5. Point of Contact

Project Aegis Simulation Lead
Aegis R&D
DSN: XXX-XXXX
Commercial: (XXX) XXX-XXXX
Email: aegis-sim@usmc.mil

/s/
[Signature Block]

