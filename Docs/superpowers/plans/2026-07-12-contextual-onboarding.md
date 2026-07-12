# Contextual Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing Deck Builder and normal match teach the player how to start, deploy, move, attack, and recognize when ending the turn is the only legal action.

**Architecture:** A pure `MatchCoachModel` derives guidance and visual states from the public snapshot, legal `GameAction` candidates, and current selection. `OnboardingStore` persists completed milestones without blocking play. Existing views render these outputs while `Main` records milestones only after accepted actions.

**Tech Stack:** Godot 4.7 Controls, typed GDScript, JSON persistence, existing headless UI contract and capture harnesses.

## Global Constraints

- Keep Deck Builder as the first screen.
- Do not change rules, card data, AI, replay semantics, or `GameAction` contracts.
- Guidance must derive from public snapshots and legal actions only.
- Click and drag must continue to resolve through the same action candidates.
- Use `Credit` everywhere.
- Preserve 1280x720 and true 1024x720 layouts.
- Guidance is concise command text, not modal documentation.

---

## File Map

```text
scripts/ui/onboarding_store.gd       Atomic local onboarding persistence
scripts/ui/match_coach_model.gd      Pure objective and availability derivation
scripts/ui/deck_builder_view.gd      Starter readiness and Start Battle state
scripts/ui/match_view.gd             Coach bar and semantic card/slot states
scripts/ui/card_view.gd              Legal, unavailable, and selected visuals
scripts/ui/zone_view.gd              Strong target-slot highlights
scripts/main.gd                      Accepted-action milestone recording
scenes/ui/deck_builder_view.tscn     Starter readiness strip
scenes/ui/match_view.tscn            Fixed-height objective strip
tests/ui/test_onboarding.gd          Pure and integrated onboarding contracts
tests/ui/run_onboarding.gd           Fast async test runner
tests/ui/run_onboarding.sh           Strict runtime/log gate
tests/capture_ui.gd                  Deterministic onboarding captures
README.md                            Player-facing controls and onboarding behavior
```

### Task 1: Pure Coach Model and Persistence

**Files:**
- Create: `scripts/ui/match_coach_model.gd`
- Create: `scripts/ui/onboarding_store.gd`
- Create: `tests/ui/test_onboarding.gd`
- Create: `tests/ui/run_onboarding.gd`
- Create: `tests/ui/run_onboarding.sh`

**Interfaces:**
- Produces: `MatchCoachModel.derive(snapshot, legal_actions, selection, onboarding) -> Dictionary`.
- Produces result keys: `objective`, `legal_source_ids`, `source_reasons`, `end_turn_only`, and `next_kind`.
- Produces: `OnboardingStore.load() -> Dictionary`, `complete(milestone) -> bool`, and `dismiss_deck_hint() -> bool`.

- [ ] **Step 1: Write failing coach-priority tests**

Cover opponent turn, pending slot/target selection, deploy, move, attack, Order, Countermeasure, and sole-End-Turn states. Assert exact objective copy and source IDs. Include a first-turn fixture with `credit=1`, a playable one-cost unit, and an unaffordable three-cost unit.

- [ ] **Step 2: Run the focused runner and confirm RED**

Run: `HOME=/tmp/opencards-onboarding godot --headless --path . --script tests/ui/run_onboarding.gd`

Expected: non-zero parser/load failure naming `MatchCoachModel` or `OnboardingStore`.

- [ ] **Step 3: Implement pure derivation**

Group legal actions by `source_id`; never infer availability from card cost alone. Use selection fields `selected_source_id`, `selected_targets`, `selected_zone`, and `selected_slot` to identify the next required dimension. `end_turn_only` is true only when the complete legal action list contains exactly one `end_turn` action.

- [ ] **Step 4: Add reason derivation tests and implementation**

For hand cards without legal source actions, derive one of `Not enough Credit`, `Support Line is full`, `Wait for your turn`, `No legal target`, or `Already active`. Inputs include card category/cost/countermeasure state and public player credit/support occupancy; unknown cases return `No legal action for this card`.

- [ ] **Step 5: Add persistence RED/GREEN coverage**

Use an injected `user://test-onboarding.json` path. Verify safe defaults, atomic save/reload, dismissal, all three milestones, corrupt JSON fallback, and an unwritable-path failure that preserves in-memory state.

- [ ] **Step 6: Run strict focused tests and commit**

Run: `sh tests/ui/run_onboarding.sh`

Expected: `PASS onboarding model and persistence`, no script/runtime/leak output except the exact macOS CA diagnostic.

Commit: `feat: add contextual onboarding model`

### Task 2: Deck Builder Starter Readiness

**Files:**
- Modify: `scenes/ui/deck_builder_view.tscn`
- Modify: `scripts/ui/deck_builder_view.gd`
- Modify: `tests/ui/test_onboarding.gd`

**Interfaces:**
- Consumes: `OnboardingStore`.
- Produces nodes: `%StarterStatus`, `%StarterHint`, `%DismissHint`, and existing `%PlayButton` renamed in display only to `Start Battle`.

- [ ] **Step 1: Write failing scene and state tests**

Instantiate a shipped valid 40-card deck and assert status `Starter deck ready - 40 valid cards.`, visible hint `Choose a difficulty and start battle, or click cards to edit.`, and button text `Start Battle`. Assert the hint hides after dismissal but the live readiness status remains.

- [ ] **Step 2: Add edited/invalid deck tests**

After adding or removing a card, assert the preset readiness copy disappears, the first concrete validator error is shown, and `Start Battle` is disabled. Returning to an untouched shipped deck restores readiness.

- [ ] **Step 3: Implement the fixed-height status strip**

Place it below `Header`, keep it unframed, and limit it to two compact lines at 1280 and 1024 widths. Dismiss is an icon-style close button with tooltip `Hide starter hint`; it affects only the explanatory line.

- [ ] **Step 4: Run responsive tests and commit**

Run: `sh tests/ui/run_onboarding.sh` and `sh tests/ui/run_task6.sh`.

Expected: all onboarding and existing strict UI contracts pass.

Commit: `feat: clarify starter deck launch path`

### Task 3: Match Coach, Visual Feedback, and Milestones

**Files:**
- Modify: `scenes/ui/match_view.tscn`
- Modify: `scripts/ui/match_view.gd`
- Modify: `scripts/ui/card_view.gd`
- Modify: `scripts/ui/zone_view.gd`
- Modify: `scripts/main.gd`
- Modify: `tests/ui/test_onboarding.gd`
- Modify: `tests/capture_ui.gd`
- Modify: `README.md`

**Interfaces:**
- Consumes: `MatchCoachModel.derive(...)` and `OnboardingStore`.
- Produces: `MatchView.set_onboarding_state(state)` and card semantic method `set_action_state(state, reason = "")`, where state is `normal`, `legal`, `selected`, or `unavailable`.
- Records milestones only for accepted `deploy_unit`, accepted `move_unit` into `frontline`, and accepted `attack_unit` or `attack_hq`.

- [ ] **Step 1: Write failing first-turn integration test**

Build a real controller through start/mulligan confirmation. Assert the active first player has `credit=1`, and when a one-cost legal unit is present the coach lists it as legal and says `Select a highlighted card to deploy. You have 1 Credit.` If no affordable action exists, assert sole-End-Turn guidance only when the controller exposes no other legal action.

- [ ] **Step 2: Write failing interaction-state tests**

Assert legal hand cards use `legal`, selected source uses `selected`, unavailable cards use `unavailable` with a concrete tooltip/reason, and clicking an unavailable card updates `%StatusLabel` without submitting an action. Assert slot/target highlights become stronger without changing control rectangles.

- [ ] **Step 3: Implement objective strip and semantic states**

Add `%CoachObjective` in a stable one-line strip above `%HandScroll`. Refresh after snapshot, legal actions, selection, cancellation, rejection, lock changes, and AI results. Rejection copy takes precedence until selection/state changes.

- [ ] **Step 4: Implement accepted-action milestone recording**

In `Main.submit_player_action`, inspect the accepted action before refreshing the view. Record the matching milestone through `OnboardingStore`; never record rejected or AI actions. Pass the current onboarding dictionary to every Match render.

- [ ] **Step 5: Add complete-flow and persistence tests**

Exercise a real deploy, move, and attack action. Assert each prompt appears before completion, disappears after accepted completion, survives store reload, and does not reset on rematch. Include opponent-turn and only-End-Turn fixtures.

- [ ] **Step 6: Add capture QA and responsive assertions**

Capture Deck Builder starter readiness, first deploy prompt, selected slot prompt, and sole-End-Turn state. Assert coach/control bounds and non-overlap at 1280x720 and 1024x720; inspect generated PNGs for legible text and obvious highlights.

- [ ] **Step 7: Run regression verification**

Run:

```bash
sh tests/ui/run_onboarding.sh
sh tests/ui/run_task6.sh
sh tests/run_task5.sh
godot --headless --path . --script tests/data_validation.gd
godot --headless --path . --script tests/capture_ui.gd
```

Expected: all commands exit zero; generated captures are nonblank; deterministic gameplay and hidden-information tests remain green.

- [ ] **Step 8: Update README and commit**

Document `Start Battle`, legal card highlighting, the objective bar, Credit growth, deployment to Support Line, movement to Frontline, attack targeting, and End Turn guidance.

Commit: `feat: teach core gameplay contextually`
