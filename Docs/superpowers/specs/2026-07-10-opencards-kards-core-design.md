# OpenCards KARDS-style Core Gameplay Design

Date: 2026-07-10

Status: Approved

## 1. Objective

Replace the current card-browser-only Godot application with a complete offline, single-player tactical card game inspired by the core battle rules of KARDS.

The finished application must support deck construction, mulligan, a complete match against AI, all three card categories, shared-frontline combat, data-driven abilities, victory and defeat, replayable match logs, and restarting or returning to deck construction.

The project uses the term **Credit** everywhere. KARDS documentation may use “Kredit,” but OpenCards UI, JSON, code, tests, and documentation use `Credit`, `credit`, `credit_slots`, and `MAX_CREDITS`.

## 2. Product Boundary

### In scope

- Offline single-player matches against AI.
- A deck builder with validation and local deck persistence.
- Two original nations: United States and Soviet Union.
- Main-nation and ally-nation deck construction.
- Mulligan with different first-player and second-player hand sizes.
- Unit deployment, movement, shared-frontline control, attacks, counterattacks, Orders, Countermeasures, triggers, modifiers, and destruction.
- Three AI difficulty levels that use the same legal action interface as the player.
- Match result, rematch, return to deck builder, action history, and deterministic replay data.
- Original card definitions and 12 original generated illustrations.

### Out of scope

- Online multiplayer, matchmaking, accounts, cloud saves, leaderboards, and social systems.
- Store, packs, currencies outside a match, collection progression, crafting, and monetization.
- Campaigns, draft mode, seasonal rules, and live-service balance rotation.
- Copying KARDS names, card text, card artwork, logos, audio, or visual trade dress.

## 3. Rules Reference and Terminology

KARDS is a behavioral reference for battlefield structure and battle flow, not a content source.

Primary references:

- [How to Play](https://www.kards.com/en/how-to-play)
- [The Frontline](https://support.kards.com/hc/en-us/articles/360026464712-Battlefield-elements-The-Frontline)
- [The Support Line](https://support.kards.com/hc/en-us/articles/360026757171-Battlefield-elements-The-Support-Line)
- [Hands and Mulligan](https://support.kards.com/hc/en-us/articles/360026500332-Battlefield-elements-Hands)
- [Cards and Copy Limits](https://support.kards.com/hc/en-us/articles/360026768151-Cards)
- [Nations and Allies](https://support.kards.com/hc/en-us/articles/360026461872-Nations)
- [Orders](https://support.kards.com/hc/en-us/articles/360026754851-Orders)
- [Countermeasures](https://support.kards.com/hc/en-us/articles/360026404872-Countermeasure)

OpenCards intentionally exposes rules through its own names and original content.

## 4. Core Rules Contract

### 4.1 Deck construction

- A valid deck contains exactly 40 cards: one Headquarters and 39 playable cards.
- The Headquarters determines the main nation.
- The main nation contributes at least 28 cards including Headquarters.
- The optional ally nation contributes at most 12 cards.
- Standard, Limited, Special, and Elite cards allow at most 4, 3, 2, and 1 copies respectively.
- Only United States and Soviet Union content exists in the initial card pool, but nation validation is data-driven.
- Two valid starter decks ship with the game so a match can begin without editing a deck.

### 4.2 Match setup and mulligan

- The starting player is selected by the match’s seeded random number generator.
- The starting player receives four cards and the other player receives five.
- Each side may select any number of starting cards to replace once.
- Replaced cards return to the deck before replacements are drawn; the deck is shuffled deterministically.
- Both players begin with a 20-defense Headquarters.

### 4.3 Battlefield

- Each player owns one Support Line with a Headquarters and up to four units.
- Both players share one Frontline containing up to five units.
- The Frontline may be controlled by only one player at a time.
- A player may move a unit into the Frontline only when the opponent does not control it and capacity remains.
- Units do not normally move from the Frontline back to the Support Line; an explicit Retreat effect is required.
- Unit type rules determine legal targets and operation patterns for Infantry, Tank, Artillery, Fighter, and Bomber units.
- Ground units require Frontline access to attack the enemy Support Line or Headquarters. Long-range unit types use their type-specific targeting rules.

### 4.4 Turn and Credit economy

- At the start of each player’s turn, their Credit slot count increases by one, up to 12 unless an effect changes that limit.
- Available Credit refills to the current Credit slot count.
- The active player draws one card and start-of-turn triggers resolve.
- During the action phase, the player may deploy units, issue Orders, activate or deactivate Countermeasures, move, attack, and activate manual abilities in any legal order.
- Deployment pays `deployment_cost`.
- A unit operation pays `operation_cost` and consumes the appropriate operation allowance.
- End-of-turn triggers resolve, temporary modifiers expire, unused Countermeasures are updated, and control passes to the opponent.

### 4.5 Hand, deck, and fatigue

- Maximum hand size is nine.
- A draw into a full hand moves the drawn card directly to discard.
- Drawing from an empty deck damages the drawing player’s Headquarters.
- Fatigue begins at one damage and increases by one for each later failed draw by that player.

### 4.6 Combat

- All attacks and movements are represented as operations and validated by `CombatRules`.
- Newly deployed units cannot operate unless a keyword or effect permits it.
- Attack and counterattack damage resolve through events so triggers observe a stable sequence.
- A unit with zero defense is destroyed and moved to discard after pending damage events reach their destruction checkpoint.
- Headquarters at zero defense ends the match immediately.
- Guard, Smokescreen, Pinned, Heavy Armor, Ambush, Blitz, Fury, and Retreat alter validation or resolution through named rule handlers rather than UI checks.

### 4.7 Orders and Countermeasures

- An Order pays its Credit cost before Countermeasure checks.
- Targeted Orders validate and lock targets before entering resolution.
- A Countermeasure is activated while remaining hidden in its owner’s hand.
- An activated Countermeasure automatically tests its trigger during the next enemy turn.
- A triggered Countermeasure resolves, reveals itself, and moves to discard.
- An activated Countermeasure that does not trigger becomes inactive and stays in hand.
- Deactivating a Countermeasure refunds its activation cost according to the reference behavior.
- The timeline reveals only information the opposing player is entitled to know.

## 5. Architecture

The rules core is pure GDScript and has no dependency on Godot scenes or Controls. UI and AI consume state snapshots and submit the same action objects.

```text
Godot Scenes / UI                AI Player
        |                            |
        +------ GameAction ----------+
                     |
              MatchController
                     |
       +-------------+-------------+
       |             |             |
  CombatRules   EffectEngine   TurnManager
       |             |             |
       +--------- MatchState -------+
                     |
         Card / Ability / Deck Data
```

### 5.1 State layer

- `MatchState` owns both players, active player, turn number, Frontline, winner, seeded RNG state, and event sequence number.
- `PlayerState` owns Headquarters, deck, hand, Support Line, discard, active Countermeasures, Credit slots, available Credit, and fatigue.
- `CardInstance` owns runtime identity, owner, zone, slot, current attack and defense, operation state, visibility, modifiers, and statuses.
- Static card and ability definitions remain separate from runtime instances.
- UI receives copied view snapshots and cannot mutate live state.

### 5.2 Action and event contracts

`GameAction` contains:

- action type;
- actor ID;
- source card instance ID when applicable;
- ordered target instance IDs or zone positions;
- action-specific payload;
- expected event sequence number to reject stale UI input.

`ActionResult` contains:

- accepted flag;
- stable reason code and display message for rejection;
- emitted ordered events;
- resulting public snapshot.

The initial action set includes mulligan, deploy unit, play Order, toggle Countermeasure, move unit, attack unit, attack Headquarters, activate ability, end turn, concede, and restart.

### 5.3 MatchController

`MatchController` is the only state mutation gateway. It validates actor, phase, ownership, zone, Credit, capacity, source state, targets, and stale sequence numbers before applying an action.

The controller emits semantic events such as card drawn, card deployed, Credit spent, unit moved, attack started, damage dealt, card destroyed, Frontline changed, Countermeasure triggered, turn changed, and match ended.

### 5.4 EffectEngine

Abilities are represented as data-driven trigger, condition, target-selector, and effect lists.

Supported trigger families:

- deploy, draw, discard, move, attack, defend, damage dealt, damage taken, destroyed;
- turn start, turn end;
- Order played, Countermeasure triggered;
- Frontline gained and Frontline lost.

Supported effects:

- damage, repair, buff, debuff, add or remove status;
- draw, discard, create, copy, destroy, return, move, and retreat;
- add, spend, or change Credit and Credit slots;
- suppress or replace an event;
- select random valid targets through the match RNG.

Effects enter an ordered queue. Each action may resolve at most 64 generated effect events. Exceeding the limit rejects the unresolved tail, records a diagnostic event, and ends the match as invalid rather than hanging.

### 5.5 Determinism

- Each match stores one seed and uses one owned `RandomNumberGenerator`.
- Rule code does not call global random functions.
- The action log stores all accepted actions and resulting event sequence numbers.
- Seed plus deck definitions plus accepted actions must reproduce the same public match state.

## 6. Content and Data

The initial pool contains 34 original definitions:

- 16 United States playable cards and one United States Headquarters;
- 16 Soviet Union playable cards and one Soviet Union Headquarters.

The pool includes Units, Orders, and Countermeasures across Infantry, Tank, Artillery, Fighter, and Bomber types. It exercises every initial trigger family and all named core keywords.

### 6.1 Files

```text
data/cards.json
data/abilities.json
data/decks.json
data/rules.json
```

`cards.json` stores identity, nation, category, unit type, rarity, costs, base stats, keywords, ability IDs, rules text, and artwork path.

`abilities.json` stores trigger, conditions, targets, and ordered effects. Complex one-off behavior is allowed only through a registered handler ID with a focused test.

`decks.json` stores only the two shipped starter decks. User-created decks are persisted under `user://decks.json`; shipped definitions remain read-only.

`rules.json` stores content-facing constants and copy limits. Structural invariants remain code constants where changing them would invalidate state shape.

### 6.2 Validation

Startup validation checks:

- unique IDs and valid references;
- valid enums, ranges, and required fields;
- valid artwork paths;
- valid target and effect combinations;
- starter-deck size, nation, Headquarters, and rarity-copy constraints;
- enough cards in each deck to initialize a match.

Invalid shipped content blocks match startup and displays a concise diagnostic panel. Invalid user decks remain editable but cannot be selected for play.

## 7. AI

AI receives a perspective-filtered snapshot containing public state, its own hidden state, and only legitimately revealed player cards. It cannot inspect the player’s hidden hand or deck order.

All difficulties enumerate legal actions through the rules core:

- Easy selects among legal actions with simple affordability and survival weighting.
- Standard evaluates bounded action sequences using Headquarters safety, board value, Frontline control, hand value, and Credit efficiency.
- Hard uses a wider beam and deeper sequence evaluation under a fixed time and node budget.

AI ends its turn when no positive-scoring sequence remains or its search budget expires. A fallback always chooses a legal end-turn action. Given the same state, seed, and difficulty, AI produces the same actions.

## 8. User Experience

### 8.1 Screen flow

```text
Deck Builder -> Deck Selection -> Mulligan -> Match -> Result
      ^                                             |
      +---------------- Return ---------------------+
```

- The application starts in Deck Builder or deck selection, never in the old standalone card list.
- The current collection browser becomes the searchable card catalog inside Deck Builder.
- A valid starter deck is preselected for first launch.
- Result offers rematch with the same decks and seed policy, or return to Deck Builder.

### 8.2 Match layout

The board uses the approved **Tactical Command Table** direction:

- quiet military green surfaces, brass accents, restrained borders, and clear tactical symbols;
- AI Support Line at the top, shared Frontline in the center, player Support Line and hand at the bottom;
- Headquarters and current defense remain visible in both Support Lines;
- available and maximum Credit sit next to the End Turn command;
- a compact event timeline sits at the side;
- legal targets highlight after source selection;
- click-to-select and drag-and-drop both issue the same action;
- rejected actions show a brief reason and leave visual state unchanged.

The primary target is 1280x720. Responsive constraints preserve all commands and prevent text or card overlap in narrow desktop windows. Cards may scale down, but battlefield capacity and action controls keep stable dimensions.

## 9. Artwork

The implementation generates exactly 12 original card illustrations with imagegen, six per nation. Artwork contains no text, logos, frames, numbers, or watermarks. Godot renders all card frames and information.

Illustrations use a coherent painterly historical concept-art style compatible with the Tactical Command Table UI. Multiple cards of the same unit family may reuse artwork with deterministic crop or color treatment. Artwork is saved under project-owned paths and referenced by validated data.

Existing copied placeholder images may remain during implementation but are not required in the final starter decks once generated assets are connected.

## 10. Error Handling and Diagnostics

- Every rejected action returns a stable reason code and user-facing message without modifying state.
- Multi-step mutations are prepared and validated before commit; effects never leave a partially moved card.
- Missing card or ability definitions fail validation before a match starts.
- Missing optional artwork uses a neutral in-project fallback image and records a warning.
- Effect recursion and AI search have hard budgets.
- Match logs include seed, decks, accepted actions, events, and terminal result.
- Unexpected internal invariants end the current match as invalid and preserve the diagnostic log rather than guessing a repair.

## 11. Testing and Acceptance

### 11.1 Rule tests

Headless tests cover:

- deck validation and mulligan;
- draw, overdraw, empty-deck fatigue, discard, and zone ownership;
- Credit growth, refill, spending, refunds, and slot changes;
- Support Line and Frontline capacity and ownership;
- operation eligibility and all unit-type target rules;
- attack, counterattack, armor, destruction, and Headquarters victory;
- Orders, Countermeasure activation, automatic trigger, expiry, and refund;
- every trigger family, effect type, and named keyword;
- invalid actions proving state remains unchanged;
- deterministic replay from seed and action log.

Each of the 34 card definitions has at least one focused behavior test. Shared mechanics use table-driven tests rather than duplicating test logic.

### 11.2 Integration tests

- Fixed-seed AI-versus-AI matches complete without illegal actions, deadlocks, or effect overflow.
- All three AI difficulties respect hidden information and terminate within their budgets.
- A scripted player flow completes deck selection, mulligan, deployment, movement, combat, Order play, Countermeasure resolution, victory, and rematch.
- Startup validation reports intentionally malformed fixture data with stable error codes.

### 11.3 Visual and runtime verification

- Godot starts without parser or runtime errors.
- The complete flow is manually verified at 1280x720 and a narrow desktop viewport.
- Card text, costs, stats, targets, timeline, and action controls do not overlap.
- Generated artwork imports and renders on cards.
- A release resource pack export succeeds; platform application export depends on installed Godot export templates.

## 12. Migration Strategy

- Keep current JSON and artwork available while introducing the new validated schema.
- Build and test the rules core before connecting scene input.
- Replace the monolithic `scripts/main.gd` with screen orchestration and focused UI components.
- Reuse the current search and detail behavior inside Deck Builder.
- Use the Unity branch only as a semantic reference for zones, abilities, and AI; do not port Unity scene coupling or its separate per-player Frontline model.
- The deliverable is complete only when the whole in-scope flow is playable. Internal implementation may use checkpoints, but no confirmed in-scope subsystem is deferred from the final result.

## 13. Completion Criteria

The migration is complete when a player can launch OpenCards, choose or build a valid deck, complete mulligan, play a full rules-valid match against any AI difficulty using Units, Orders, Countermeasures, abilities, and shared-Frontline combat, reach a deterministic victory or defeat, inspect the action timeline, and rematch or return to deck construction without errors.
