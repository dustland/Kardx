# OpenCards Contextual Onboarding Design

## Goal

Make the existing Deck Builder and normal match teach themselves without adding a separate scripted tutorial. A first-time player should understand how to start a valid preset deck, deploy a unit, move toward the Frontline, attack, and recognize when ending the turn is the only legal action.

## Scope

- Keep Deck Builder as the first screen.
- Keep all rules, AI behavior, card data, and action contracts unchanged.
- Add concise contextual guidance to Deck Builder and Match only.
- Store dismissed or completed onboarding steps locally.
- Do not add modal rule pages, forced tutorial matches, or long instructional paragraphs.

## Deck Builder

Add a compact status band under the header:

- For an untouched shipped deck: `Starter deck ready - 40 valid cards.`
- Supporting text: `Choose a difficulty and start battle, or click cards to edit.`
- Primary command text becomes `Start Battle` instead of `Play`.
- The valid preset state visually emphasizes `Start Battle` and keeps editing controls available.
- Once the deck is edited, the band switches to ordinary validation feedback and explains the first concrete error, such as card count or copy limit.

This band remains present because it carries live deck state, but the explanatory sentence may be dismissed and remembered in `user://onboarding.json`.

## Match Coach

Add one compact objective bar above the player hand. It derives its message exclusively from the current public snapshot and legal actions.

Priority order:

1. Opponent turn: `Opponent is acting.`
2. Pending selection: state the exact next input, such as `Choose a highlighted Support Line slot.`
3. Deploy available: `Select a highlighted card to deploy. You have N Credit.`
4. Move available: `Select a ready unit, then choose a highlighted Frontline slot.`
5. Attack available: `Select a ready unit, then choose a highlighted target.`
6. Order or Countermeasure available: identify the playable card class.
7. Only End Turn remains: `No other actions are available. End the turn to gain another Credit slot.`

Messages are commands, not explanations of internal systems. They contain no keyboard-shortcut prose.

## Interaction Feedback

- Hand cards with at least one legal source action receive a clear active outline.
- Hand cards with no legal source action are visually subdued but remain inspectable.
- Pressing an unavailable card shows a specific reason derived without submitting an illegal action:
  - insufficient Credit,
  - wrong phase or opponent turn,
  - Support Line full,
  - no legal target,
  - card is already active as a Countermeasure.
- Selecting a legal source visibly marks the card itself and strengthens existing target/slot highlights.
- The coach updates after every selection, cancellation, accepted action, rejection, and AI action.
- End Turn receives emphasis only when it is the sole legal action.

Click and drag continue to use the same `GameAction` candidates and `ActionBuilder` paths. The coach never creates or mutates actions.

## First-Time Milestones

Track three local booleans:

- `deployed_unit`
- `moved_to_frontline`
- `completed_attack`

Before each milestone is complete, the relevant coach message may include one extra short clause. Completing the accepted action records the milestone atomically. Resetting decks or starting a rematch does not reset onboarding. A corrupt onboarding file falls back to all milestones incomplete and shows a non-blocking diagnostic only in logs.

## Architecture

- `MatchCoachModel` is a pure view model. Input: player snapshot, legal actions, selection state, onboarding state. Output: objective text, legal source IDs, source unavailability reasons, and whether End Turn is the sole action.
- `OnboardingStore` owns `user://onboarding.json`, uses temporary-file plus rename persistence, and exposes safe defaults.
- `MatchView` renders coach output and semantic states. It does not inspect controller internals.
- `Main` records milestones only after an accepted player action and refreshes the view.
- `DeckBuilderView` renders preset readiness using existing deck validation plus onboarding dismissal state.

## Visual Treatment

- Preserve the Tactical Command Table palette.
- The objective bar is an unframed command strip, not another card.
- Legal sources use the existing brass action color with a thicker border.
- Selected sources use a distinct pale command highlight.
- Unavailable cards use reduced saturation and opacity while retaining readable artwork and tooltip text.
- Target slots use a stronger inset highlight without resizing or shifting layout.
- At 1024x720, the coach occupies one fixed-height line and must not reduce hand or board controls below their tested bounds.

## Error Handling

- Guidance is derived from legal actions first; it never claims an action is possible unless a matching legal candidate exists.
- If legal actions are unexpectedly empty on the player's action turn, show `No legal action is available.` and keep diagnostics visible instead of silently enabling End Turn.
- Persistence failure does not block play; onboarding remains in memory for the session.
- Existing controller rejection messages take precedence over coach text until the player changes selection or the state advances.

## Testing

- Pure tests cover coach priority, legal source IDs, every unavailable reason, and sole-End-Turn detection.
- Rules integration fixtures verify first own turn has 1 Credit and that the coach recognizes a playable 1-cost card when present.
- Scene tests verify legal/subdued/selected styles, stronger slot highlights, and 1280x720 plus 1024x720 containment.
- Flow smoke covers starter deck readiness, first deployment, Frontline move, attack milestone, persistence reload, and a hand where End Turn is genuinely the only action.
- Capture QA adds Deck Builder and Match onboarding states and checks that coach text and controls do not overlap.

## Success Criteria

- A valid shipped deck presents an obvious `Start Battle` path without requiring deck edits.
- On every player turn, the interface names the next useful action or clearly explains why only End Turn remains.
- Legal cards and targets are visually discoverable before clicking.
- An unavailable card produces a concrete reason.
- Deployment, movement, and attack prompts disappear permanently after successful completion.
- Existing deterministic replay, hidden-information, AI, and responsive tests remain green.
