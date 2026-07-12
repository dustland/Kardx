# OpenCards Content and AI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace demo data with a validated 34-card original pool, two legal starter decks, and deterministic non-cheating AI at three difficulties.

**Architecture:** JSON definitions are loaded into the rules core through one validated catalog. AI sees a perspective-filtered snapshot, asks the controller for legal actions, simulates bounded sequences on cloned state, and submits the chosen real action through `MatchController`.

**Tech Stack:** Godot 4.7, typed GDScript, JSON, rules core from Plan 01, custom headless test runner.

## Global Constraints

- Complete Plan 01 with all rules tests passing before this plan.
- Use `Credit`, never `Kredit`, in OpenCards content and code.
- Ship exactly 34 definitions: 16 playable cards plus one Headquarters for each of two nations.
- Ship two valid 40-card starter decks.
- Every card definition must have a focused behavior assertion.
- AI must not inspect unrevealed enemy hand cards or deck order.
- AI output must be deterministic for the same seed, state, and difficulty.
- No KARDS names, text, art, logos, or balance data may be copied.

---

## File Map

```text
data/cards.json                         34 original card definitions
data/abilities.json                     Trigger/condition/target/effect definitions
data/decks.json                         Two starter deck lists
data/rules.json                         Content-facing limits and display labels
scripts/content/content_catalog.gd      Parse and index shipped JSON
scripts/content/content_validator.gd    Structured validation diagnostics
scripts/ai/action_generator.gd           Enumerate legal actions
scripts/ai/board_evaluator.gd            Perspective-safe state scoring
scripts/ai/ai_player.gd                  Difficulty budgets and deterministic search
tests/content/test_catalog.gd            Schema and reference tests
tests/content/test_card_behaviors.gd     Focused behavior checks for all definitions
tests/ai/test_ai.gd                      Legality, hidden information, determinism
tests/ai/test_ai_matches.gd              Full fixed-seed match simulations
```

## Locked Card Matrix

Costs are `deployment/operation`; Orders and Countermeasures use `0` operation. Stats are `attack/defense`.

| ID | Display name | Nation/type | Rarity | Cost | Stats | Behavior under test |
|---|---|---|---|---:|---:|---|
| `us-hq` | US Command Post | US/HQ | Standard | 0/0 | 0/20 | Match ends at zero defense |
| `us-rifle-platoon` | Rifle Platoon | US/Infantry | Standard | 1/1 | 1/2 | Normal deployment sickness |
| `us-combat-engineers` | Combat Engineers | US/Infantry | Standard | 2/1 | 2/3 | Repairs friendly HQ for 1 on deploy |
| `us-field-hospital` | Field Hospital | US/Infantry | Standard | 2/1 | 1/4 | Repairs a friendly unit for 2 on deploy |
| `us-supply-column` | Supply Column | US/Infantry | Standard | 2/1 | 1/3 | Adds 1 available Credit on deploy |
| `us-forward-observers` | Forward Observers | US/Infantry | Standard | 3/1 | 2/3 | Draws one card when Frontline is gained |
| `us-p40-patrol` | P-40 Patrol | US/Fighter | Standard | 3/1 | 3/2 | Smokescreen is lost after operation |
| `us-rapid-resupply` | Rapid Resupply | US/Order | Standard | 2/0 | 0/0 | Draws two cards |
| `us-signal-watch` | Signal Watch | US/Countermeasure | Standard | 2/0 | 0/0 | Cancels first enemy Order targeting a friendly unit |
| `us-ranger-company` | Ranger Company | US/Infantry | Limited | 3/1 | 3/3 | Blitz allows deployment-turn operation |
| `us-armored-group` | Armored Group | US/Tank | Limited | 4/2 | 4/5 | Fury permits two operations with two payments |
| `us-field-battery` | 105mm Field Battery | US/Artillery | Limited | 4/2 | 4/3 | Long-range attack has no counterattack |
| `us-emergency-repairs` | Emergency Repairs | US/Countermeasure | Limited | 2/0 | 0/0 | Prevents lethal HQ damage and repairs 3 |
| `us-tank-hunters` | Tank Hunters | US/Artillery | Special | 5/2 | 6/3 | Deals +2 damage to Tank targets |
| `us-b25-strike-group` | B-25 Strike Group | US/Bomber | Special | 6/2 | 5/4 | Ignores Guard when selecting a unit target |
| `us-air-superiority` | Air Superiority | US/Order | Elite | 5/0 | 0/0 | Deals 3 to all enemy air units |
| `us-combined-arms` | Combined Arms | US/Order | Elite | 6/0 | 0/0 | Friendly units gain +1/+1 this turn and draw one |
| `su-hq` | Soviet Command Post | Soviet/HQ | Standard | 0/0 | 0/20 | Match ends at zero defense |
| `su-guards-rifle` | Guards Rifle Section | Soviet/Infantry | Standard | 1/1 | 1/3 | Guard protects adjacent unit |
| `su-siberian-volunteers` | Siberian Volunteers | Soviet/Infantry | Standard | 2/1 | 2/3 | Gains +1 attack while damaged |
| `su-combat-sappers` | Combat Sappers | Soviet/Infantry | Standard | 2/1 | 2/2 | Deals 1 damage to target enemy on deploy |
| `su-medical-battalion` | Medical Battalion | Soviet/Infantry | Standard | 2/1 | 1/4 | Repairs all friendly units for 1 on turn end |
| `su-rail-convoy` | Rail Supply Convoy | Soviet/Infantry | Standard | 3/1 | 1/4 | Adds one Credit slot on deploy, capped by effect rules |
| `su-partisan-scouts` | Partisan Scouts | Soviet/Infantry | Standard | 2/1 | 2/2 | Intel reveals one random enemy hand card |
| `su-massed-assault` | Massed Assault | Soviet/Order | Standard | 3/0 | 0/0 | Friendly Infantry gain +1 attack this turn |
| `su-maskirovka` | Maskirovka | Soviet/Countermeasure | Standard | 2/0 | 0/0 | Gives attacked friendly unit Ambush for that combat |
| `su-t34-spearhead` | T-34 Spearhead | Soviet/Tank | Limited | 4/1 | 4/4 | Blitz permits immediate operation |
| `su-heavy-breakthrough` | Heavy Breakthrough Regiment | Soviet/Tank | Limited | 6/2 | 6/7 | Heavy Armor reduces incoming damage by 1 |
| `su-katyusha-battery` | Katyusha Battery | Soviet/Artillery | Limited | 4/2 | 3/3 | On attack, deals 1 to adjacent enemy units |
| `su-hold-the-line` | Hold the Line | Soviet/Countermeasure | Limited | 3/0 | 0/0 | When Frontline would be lost, repairs its units for 2 |
| `su-yak-patrol` | Yak Patrol | Soviet/Fighter | Special | 5/1 | 4/4 | Fury allows two operations |
| `su-pe2-bomber-wing` | Pe-2 Bomber Wing | Soviet/Bomber | Special | 6/2 | 5/4 | Deployment deals 2 to enemy HQ |
| `su-deep-battle` | Deep Battle | Soviet/Order | Elite | 5/0 | 0/0 | Retreats target enemy Frontline unit, then draws one |
| `su-artillery-preparation` | Artillery Preparation | Soviet/Order | Elite | 6/0 | 0/0 | Deals 2 to every enemy unit |

Starter deck recipe for each nation: first Standard in matrix order x4; remaining seven Standard x3; all four Limited x2; both Special x2; both Elite x1; Headquarters x1. This yields 39 playable cards plus Headquarters and stays within copy limits.

### Task 1: Catalog Loader and Structured Validation

**Files:**
- Create: `scripts/content/content_catalog.gd`
- Create: `scripts/content/content_validator.gd`
- Create: `data/rules.json`
- Create: `tests/content/test_catalog.gd`
- Modify: `tests/test_suite.gd`

**Interfaces:**
- Produces: `ContentCatalog.load_from_paths(cards_path, abilities_path, decks_path, rules_path) -> ContentCatalog`.
- Produces indexes: `cards_by_id`, `abilities_by_id`, `decks_by_id`, and `rules`.
- Produces: `ContentValidator.validate(catalog) -> Array[Dictionary]`, each diagnostic containing `code`, `path`, and `message`.

- [ ] **Step 1: Write failing catalog diagnostics tests**

```gdscript
static func run(t) -> void:
    var catalog := ContentCatalog.new()
    catalog.cards = [{"id": "duplicate"}, {"id": "duplicate"}]
    catalog.abilities = []
    catalog.decks = []
    catalog.rebuild_indexes()
    var errors := ContentValidator.validate(catalog)
    t.assert_true(errors.any(func(e): return e.code == "duplicate_card_id"), "duplicate IDs reported")
    t.assert_true(errors.all(func(e): return e.has("path") and e.has("message")), "diagnostics structured")
```

- [ ] **Step 2: Run and verify missing catalog failure**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: non-zero exit naming `ContentCatalog`.

- [ ] **Step 3: Implement parsing and validator rules**

```json
{
  "deck_size": 40,
  "playable_deck_size": 39,
  "max_hand_size": 9,
  "max_credits": 12,
  "copy_limits": {"Standard": 4, "Limited": 3, "Special": 2, "Elite": 1},
  "display_terms": {"credit": "Credit", "frontline": "Frontline", "support_line": "Support Line"}
}
```

The validator must report duplicate IDs, missing fields, invalid categories/types/rarities, negative costs or stats, unknown ability IDs, impossible target/effect pairs, missing artwork paths, malformed deck entries, and disagreement between structural constants and `rules.json`.

- [ ] **Step 4: Run catalog tests**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: catalog suite passes and diagnostics are sorted by `path`, then `code`.

- [ ] **Step 5: Commit**

```bash
git add scripts/content/content_catalog.gd scripts/content/content_validator.gd data/rules.json tests/content/test_catalog.gd tests/test_suite.gd
git commit -m "feat: validate data-driven game content"
```

### Task 2: Original Card, Ability, and Starter Deck Data

**Files:**
- Replace: `data/cards.json`
- Replace: `data/abilities.json`
- Create: `data/decks.json`
- Replace: `tests/data_validation.gd`
- Create: `tests/content/test_card_behaviors.gd`
- Modify: `tests/test_suite.gd`

**Interfaces:**
- Consumes: the exact card matrix above and Plan 01 effect schema.
- Produces: valid `us-starter` and `su-starter` deck IDs.
- Produces artwork references under `res://game_assets/generated_cards/`; until Plan 04, all entries use `res://game_assets/cards/BasicGermanB1.png` as a valid temporary fallback.

- [ ] **Step 1: Replace validation expectations before data**

```gdscript
# tests/data_validation.gd core assertions
assert(catalog.cards.size() == 34, "expected exactly 34 original definitions")
assert(catalog.decks.size() == 2, "expected two starter decks")
assert(diagnostics.is_empty(), "content diagnostics: %s" % diagnostics)
for deck_id in ["us-starter", "su-starter"]:
    assert(DeckValidator.validate(catalog.decks_by_id[deck_id].get("cards", []), catalog.cards_by_id).valid)
```

In `test_card_behaviors.gd`, create one named case per matrix row. Headquarters cases test defeat; vanilla unit cases test costs or operation eligibility; every effect card asserts its specific matrix behavior.

- [ ] **Step 2: Run and verify old demo data fails**

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: failure because the current catalog has 14 cards and no `decks.json`.

- [ ] **Step 3: Write complete JSON data**

Each card uses this exact shape:

```json
{
  "id": "us-rifle-platoon",
  "title": "Rifle Platoon",
  "description": "Reliable infantry for holding ground.",
  "nation": "UnitedStates",
  "category": "Unit",
  "unit_type": "Infantry",
  "rarity": "Standard",
  "deployment_cost": 1,
  "operation_cost": 1,
  "attack": 1,
  "defense": 2,
  "keywords": [],
  "ability_ids": [],
  "image_path": "res://game_assets/cards/BasicGermanB1.png"
}
```

Ability IDs follow `<card-id>--<trigger-or-effect>`. Encode every behavior from the matrix with generic effects unless a registered handler is necessary. Deck entries are arrays of card IDs with repeated IDs representing copies.

- [ ] **Step 4: Run content and behavior tests**

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: `Validated 34 cards, 2 decks, and <N> abilities` with exit `0`.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: all 34 named behavior cases pass.

- [ ] **Step 5: Commit**

```bash
git add data/cards.json data/abilities.json data/decks.json tests/data_validation.gd tests/content/test_card_behaviors.gd tests/test_suite.gd
git commit -m "feat: add original US and Soviet starter cards"
```

### Task 3: Legal Action Generator and Board Evaluation

**Files:**
- Create: `scripts/ai/action_generator.gd`
- Create: `scripts/ai/board_evaluator.gd`
- Create: `tests/ai/test_ai.gd`
- Modify: `scripts/core/match_controller.gd`
- Modify: `tests/test_suite.gd`

**Interfaces:**
- Produces: `MatchController.legal_actions(actor_id) -> Array[GameAction]`.
- Produces: `ActionGenerator.generate(controller, actor_id) -> Array[GameAction]`.
- Produces: `BoardEvaluator.score(snapshot, actor_id) -> float`.

- [ ] **Step 1: Write legality and information-boundary tests**

```gdscript
static func run(t) -> void:
    var controller = AiFixtures.midgame_controller(700)
    var actions := ActionGenerator.generate(controller, "opponent")
    t.assert_true(not actions.is_empty(), "AI has legal actions")
    for action in actions:
        var clone = controller.clone_for_simulation("opponent")
        t.assert_true(clone.submit_action(action).accepted, "generated action is legal: %s" % action.type)
    var hidden_snapshot := controller.state.snapshot_for("opponent")
    t.assert_true(hidden_snapshot.players.player.hand.all(func(c): return c.hidden), "player hand remains hidden")
```

- [ ] **Step 2: Run and verify missing generator failure**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: non-zero exit naming `ActionGenerator`.

- [ ] **Step 3: Implement generation from rule queries**

Generate deploy targets, Order targets, Countermeasure toggles, movement slots, attack targets, abilities, and end turn. Sort by a stable key composed of type, source ID, target IDs, and canonical payload JSON.

```gdscript
func score(snapshot: Dictionary, actor_id: String) -> float:
    var me: Dictionary = snapshot.players[actor_id]
    var them: Dictionary = snapshot.players[_other_id(snapshot, actor_id)]
    return (me.hq_defense - them.hq_defense) * 8.0 \
        + (_unit_value(me) - _unit_value(them)) \
        + (me.hand.size() - them.hand.size()) * 1.5 \
        + (10.0 if snapshot.frontline_controller_id == actor_id else 0.0) \
        + me.credit * 0.25
```

The evaluator receives only the perspective snapshot and must not accept a raw `MatchState`.

- [ ] **Step 4: Run AI legality tests**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: all generated actions are accepted on simulation clones and repeated generation order is identical.

- [ ] **Step 5: Commit**

```bash
git add scripts/ai/action_generator.gd scripts/ai/board_evaluator.gd scripts/core/match_controller.gd tests/ai/test_ai.gd tests/test_suite.gd
git commit -m "feat: generate and score legal AI actions"
```

### Task 4: Three Deterministic AI Difficulties

**Files:**
- Create: `scripts/ai/ai_player.gd`
- Modify: `tests/ai/test_ai.gd`
- Modify: `tests/test_suite.gd`

**Interfaces:**
- Produces: `AIPlayer.create(difficulty, seed) -> AIPlayer`.
- Produces: `AIPlayer.choose_action(controller, actor_id) -> GameAction`.
- Difficulty budgets: Easy `32` nodes/depth `1`; Standard `256` nodes/depth `4` actions; Hard `1200` nodes/depth `8` actions and beam width `16`.

- [ ] **Step 1: Write deterministic and tactical-choice tests**

```gdscript
static func run(t) -> void:
    var lethal = AiFixtures.lethal_controller(811)
    for difficulty in ["easy", "standard", "hard"]:
        var a := AIPlayer.create(difficulty, 99).choose_action(lethal, "opponent")
        var b := AIPlayer.create(difficulty, 99).choose_action(lethal, "opponent")
        t.assert_eq(a.to_dict(), b.to_dict(), "%s deterministic" % difficulty)
    var hard := AIPlayer.create("hard", 99).choose_action(lethal, "opponent")
    t.assert_eq(hard.type, "attack_hq", "hard takes immediate lethal")
```

- [ ] **Step 2: Run and verify missing AIPlayer failure**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: non-zero exit naming `AIPlayer`.

- [ ] **Step 3: Implement bounded search**

Easy samples stable legal actions using its owned RNG and prefers affordable deployment, favorable attacks, and end turn last. Standard and Hard use a beam of cloned controllers, accumulate evaluator score after each action, stop at end turn or terminal state, and never exceed node budgets.

```gdscript
const BUDGETS := {
    "easy": {"nodes": 32, "depth": 1, "beam": 4},
    "standard": {"nodes": 256, "depth": 4, "beam": 8},
    "hard": {"nodes": 1200, "depth": 8, "beam": 16},
}
```

Tie-break with the stable action key, then the AI RNG. Always return a legal `end_turn` when search finds no improving action.

- [ ] **Step 4: Run AI tests and check budgets**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: deterministic choices pass; each test records `visited_nodes <= configured nodes`.

- [ ] **Step 5: Commit**

```bash
git add scripts/ai/ai_player.gd tests/ai/test_ai.gd tests/test_suite.gd
git commit -m "feat: add three deterministic AI difficulties"
```

### Task 5: Full AI Match Simulation

**Files:**
- Create: `tests/ai/test_ai_matches.gd`
- Create: `tests/run_ai_match.gd`
- Modify: `tests/test_suite.gd`

**Interfaces:**
- Consumes: shipped catalog, starter decks, `MatchController`, and `AIPlayer`.
- Produces: command-line match simulation with optional seed and difficulty constants at the top of `tests/run_ai_match.gd`.

- [ ] **Step 1: Write fixed-seed completion tests**

```gdscript
static func run(t) -> void:
    for seed in [1, 7, 42, 90210]:
        var result := AiMatchRunner.run_match(seed, "standard", "standard", 300)
        t.assert_true(result.completed, "seed %d completes" % seed)
        t.assert_true(result.turns <= 300, "seed %d has no deadlock" % seed)
        t.assert_eq(result.illegal_actions, 0, "AI submits only legal actions")
```

- [ ] **Step 2: Run and verify missing runner failure**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: non-zero exit naming `AiMatchRunner`.

- [ ] **Step 3: Implement simulation with diagnostics**

The runner auto-confirms seeded mulligans, alternates AI actions until terminal state, records action count, turns, winner, state hash, illegal action count, maximum effect queue, and visited AI nodes. If 300 turns are reached, return `completed: false` with the replay log path in diagnostics.

- [ ] **Step 4: Run content, AI, and full regression suites**

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: 34 cards and two decks validated.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: all rules, content, card behavior, and AI match suites pass.

- [ ] **Step 5: Commit**

```bash
git add tests/ai/test_ai_matches.gd tests/run_ai_match.gd tests/test_suite.gd
git commit -m "test: verify complete deterministic AI matches"
```

## Plan Completion Check

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: `Validated 34 cards, 2 decks, and <N> abilities`.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: exit `0`; all 34 behavior cases and all fixed-seed AI matches pass.
