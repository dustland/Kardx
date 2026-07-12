# OpenCards Artwork and Release Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate and integrate 12 original card illustrations, complete visual QA, and verify the finished Godot project and resource export.

**Architecture:** Imagegen produces text-free portrait illustrations only; Godot owns frames and information. A deterministic art mapping connects 34 definitions to 12 shared illustrations. Automated import, data, rules, AI, and UI tests precede manual full-flow verification and export.

**Tech Stack:** Built-in imagegen, Godot 4.7, PNG assets, typed GDScript validation, Git, Godot export presets.

## Global Constraints

- Complete Plans 01–03 before this plan.
- Generate exactly 12 original illustrations, six United States and six Soviet Union.
- Use built-in imagegen, one call per distinct illustration.
- Images contain no text, letters, numbers, logos, UI, card frames, or watermarks.
- Save final project-bound images under `game_assets/generated_cards/`; do not leave referenced files only under `$CODEX_HOME`.
- Preserve the Tactical Command Table UI and render all card information in Godot.
- Do not overwrite unrelated user changes in the dirty worktree.
- A PCK export must succeed; native application export is required only when matching Godot export templates are installed.

---

## File Map

```text
game_assets/generated_cards/us-infantry.png
game_assets/generated_cards/us-support.png
game_assets/generated_cards/us-armor.png
game_assets/generated_cards/us-artillery.png
game_assets/generated_cards/us-fighter.png
game_assets/generated_cards/us-bomber.png
game_assets/generated_cards/su-infantry.png
game_assets/generated_cards/su-support.png
game_assets/generated_cards/su-armor.png
game_assets/generated_cards/su-artillery.png
game_assets/generated_cards/su-fighter.png
game_assets/generated_cards/su-bomber.png
data/cards.json
tests/content/test_art_assets.gd
tests/capture_ui.gd
export_presets.cfg
.gitignore
README.md
```

## Exact Image Prompts

Use this shared suffix on every prompt:

```text
Use case: historical-scene
Asset type: portrait card illustration for an original World War II tactical card game
Style/medium: painterly historical concept art, grounded equipment, restrained realism, visible brush texture
Composition/framing: vertical 2:3 composition, primary subject fully visible in the central 80 percent, clear silhouette, useful edge padding for card crop
Lighting/mood: overcast battlefield light with restrained warm highlights, tense but readable
Color palette: muted olive, steel, smoke gray, weathered earth, small warm brass accents
Constraints: historically plausible 1940s equipment; no identifiable real person; no copied game artwork; no text, letters, numbers, insignia, logos, UI, border, frame, watermark, gore, or modern equipment
```

Prepend one subject line for each file:

| File | Subject line |
|---|---|
| `us-infantry.png` | `Primary request: an original United States infantry platoon advancing cautiously through a hedgerow-lined field` |
| `us-support.png` | `Primary request: an original United States field support scene with combat engineers unloading medical and radio supplies beside a canvas command post` |
| `us-armor.png` | `Primary request: an original United States medium tank column moving along a muddy European road, lead tank dominant` |
| `us-artillery.png` | `Primary request: an original United States 105 mm field artillery crew preparing a gun position with forward observers nearby` |
| `us-fighter.png` | `Primary request: an original United States single-engine fighter aircraft banking above broken cloud over farmland` |
| `us-bomber.png` | `Primary request: an original United States twin-engine medium bomber formation crossing a smoky horizon` |
| `su-infantry.png` | `Primary request: an original Soviet guards rifle section advancing through a snow-dusted ruined village` |
| `su-support.png` | `Primary request: an original Soviet support scene with combat sappers, medics, and rail supplies near a field command shelter` |
| `su-armor.png` | `Primary request: an original Soviet medium tank spearhead crossing a churned steppe track, lead tank dominant` |
| `su-artillery.png` | `Primary request: an original Soviet truck-mounted rocket artillery battery preparing to fire from a concealed field position` |
| `su-fighter.png` | `Primary request: an original Soviet single-engine fighter aircraft climbing through cold dawn cloud` |
| `su-bomber.png` | `Primary request: an original Soviet twin-engine dive bomber approaching a distant battlefield through flak smoke` |

### Task 1: Generate, Inspect, and Save 12 Illustrations

**Files:**
- Create: the 12 PNG files listed in the File Map.

**Interfaces:**
- Produces: portrait PNGs at least `1024x1536`, readable when center-cropped to `2:3`.
- Consumes: the exact prompt matrix above.

- [ ] **Step 1: Generate the six United States images**

For each US subject, call built-in imagegen separately with the subject line plus the shared suffix. After each call, inspect the result for subject, period plausibility, crop safety, and forbidden text. Copy the selected result into the exact workspace filename.

Expected: six distinct project files under `game_assets/generated_cards/`.

- [ ] **Step 2: Generate the six Soviet Union images**

Repeat the same built-in flow for each Soviet subject and exact filename.

Expected: 12 total project PNGs, none referenced from `$CODEX_HOME`.

- [ ] **Step 3: Add a failing asset validation test before accepting outputs**

```gdscript
# tests/content/test_art_assets.gd
extends RefCounted

const EXPECTED := [
    "us-infantry", "us-support", "us-armor", "us-artillery", "us-fighter", "us-bomber",
    "su-infantry", "su-support", "su-armor", "su-artillery", "su-fighter", "su-bomber",
]

static func run(t) -> void:
    for slug in EXPECTED:
        var path := "res://game_assets/generated_cards/%s.png" % slug
        t.assert_true(FileAccess.file_exists(path), "%s exists" % path)
        var image := Image.load_from_file(path)
        t.assert_true(not image.is_empty(), "%s decodes" % path)
        t.assert_true(image.get_width() >= 1024 and image.get_height() >= 1536, "%s has portrait resolution" % path)
        var ratio := float(image.get_width()) / float(image.get_height())
        t.assert_true(absf(ratio - (2.0 / 3.0)) < 0.08, "%s is near 2:3" % path)
```

Register the suite in `tests/test_suite.gd`.

- [ ] **Step 4: Run asset validation and inspect imports**

Run: `godot --headless --path . --import`

Expected: all 12 assets import with no decode errors.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: asset suite passes.

- [ ] **Step 5: Commit generated source assets and validation**

```bash
git add game_assets/generated_cards tests/content/test_art_assets.gd tests/test_suite.gd
git commit -m "art: add original US and Soviet card illustrations"
```

### Task 2: Map Every Card to Generated Artwork

**Files:**
- Modify: `data/cards.json`
- Modify: `tests/content/test_art_assets.gd`

**Interfaces:**
- Consumes: the 12 exact asset paths.
- Produces: every card definition references one generated asset; no starter card uses the old temporary fallback.

- [ ] **Step 1: Extend the test to reject legacy paths**

```gdscript
for card in catalog.cards:
    var image_path: String = card.get("image_path", "")
    t.assert_true(image_path.begins_with("res://game_assets/generated_cards/"), "%s uses generated art" % card.get("id", "unknown"))
    t.assert_true(FileAccess.file_exists(image_path), "%s art exists" % card.get("id", "unknown"))
```

- [ ] **Step 2: Run and verify temporary mappings fail**

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: failures naming cards still mapped to `game_assets/cards/`.

- [ ] **Step 3: Apply the locked mapping**

Map by nation and role:

- US rifle/ranger/combined-arms cards → `us-infantry.png`.
- US engineers/hospital/supply/resupply/signal/repairs cards → `us-support.png`.
- US armored group → `us-armor.png`.
- US observers/field battery/tank hunters → `us-artillery.png`.
- US P-40/air superiority → `us-fighter.png`.
- US B-25 → `us-bomber.png`.
- Soviet guards/volunteers/massed assault → `su-infantry.png`.
- Soviet sappers/medical/rail/partisan/maskirovka/hold cards → `su-support.png`.
- Soviet T-34/heavy breakthrough/deep battle → `su-armor.png`.
- Soviet Katyusha/artillery preparation → `su-artillery.png`.
- Soviet Yak → `su-fighter.png`.
- Soviet Pe-2 → `su-bomber.png`.
- Headquarters use their nation’s support image.

- [ ] **Step 4: Run all content and asset tests**

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: content validates with no missing paths.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: every card uses generated artwork.

- [ ] **Step 5: Commit mappings**

```bash
git add data/cards.json tests/content/test_art_assets.gd
git commit -m "feat: connect generated artwork to starter cards"
```

### Task 3: Capture and Review Complete UI States

**Files:**
- Create: `tests/capture_ui.gd`
- Modify: `scripts/ui/card_view.gd`
- Modify: `scripts/ui/deck_builder_view.gd`
- Modify: `scripts/ui/mulligan_view.gd`
- Modify: `scripts/ui/match_view.gd`
- Modify: `scripts/ui/result_view.gd`
- Modify: `scripts/ui/theme_factory.gd`
- Modify: `scenes/ui/card_view.tscn`
- Modify: `scenes/ui/deck_builder_view.tscn`
- Modify: `scenes/ui/mulligan_view.tscn`
- Modify: `scenes/ui/match_view.tscn`
- Modify: `scenes/ui/result_view.tscn`

**Interfaces:**
- Produces captures under ignored `builds/qa/`: `deck-builder.png`, `mulligan.png`, `match-1280.png`, `match-960.png`, and `result.png`.
- Consumes fixed test deck IDs and seed `90210`.

- [ ] **Step 1: Add deterministic capture-state setup**

```gdscript
# tests/capture_ui.gd
extends SceneTree

func _initialize() -> void:
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://builds/qa"))
    var app = preload("res://scenes/main.tscn").instantiate()
    root.add_child(app)
    await process_frame
    await process_frame
    await _capture_screen(app, "deck_builder", Vector2i(1280, 720), "deck-builder.png")
    await _capture_screen(app, "mulligan", Vector2i(1280, 720), "mulligan.png")
    await _capture_screen(app, "match", Vector2i(1280, 720), "match-1280.png")
    await _capture_screen(app, "match", Vector2i(960, 640), "match-960.png")
    await _capture_screen(app, "result", Vector2i(1280, 720), "result.png")
    quit(0)
```

`_capture_screen` must initialize each screen with seed `90210`, wait two frames after rendering, call `root.get_texture().get_image().save_png(path)`, and assert the image contains nontransparent pixels.

- [ ] **Step 2: Run capture and inspect every image**

Run in a rendering-capable Godot session: `godot --path . --script tests/capture_ui.gd`

Expected: five nonblank PNGs under `builds/qa/`.

Inspect each with the local image viewer. Reject the set for overlapping text, clipped commands, blank artwork, unreadable selected states, missing target highlights, invisible Credit, or layout movement caused by dynamic content.

- [ ] **Step 3: Apply only evidence-based visual corrections**

Use container minimum sizes, clipping, wrapping, or card display modes to fix each observed issue. Keep cards at 8px corner radius or less, preserve the approved green/brass palette, and do not add explanatory on-screen copy.

- [ ] **Step 4: Re-run capture and smoke tests**

Run: `godot --path . --script tests/capture_ui.gd`

Expected: all five captures are nonblank and visually accepted.

Run: `godot --headless --path . --script tests/ui_smoke.gd`

Expected: both tested viewports remain within bounds.

- [ ] **Step 5: Commit visual corrections and capture tooling**

```bash
git add tests/capture_ui.gd scripts/ui scenes/ui
git commit -m "test: verify complete game UI states"
```

### Task 4: Final Runtime, Export, and Documentation Verification

**Files:**
- Modify: `.gitignore`
- Modify: `export_presets.cfg`
- Modify: `README.md`
- Modify: `project.godot`

**Interfaces:**
- Produces ignored exports under `builds/`.
- Preserves named `PCK` export preset with output `builds/pck/OpenCards.pck`.

- [ ] **Step 1: Update housekeeping and run clean checks**

`.gitignore` must include `.godot/`, `.superpowers/`, and `builds/`, while `export_presets.cfg` remains tracked. README must describe Deck Builder, Mulligan, Match, AI difficulties, generated original art, and exact validation commands.

Run: `git diff --check`

Expected: no whitespace errors.

- [ ] **Step 2: Run the complete automated verification set**

Run: `godot --headless --path . --import`

Expected: import completes without errors.

Run: `godot --headless --path . --script tests/data_validation.gd`

Expected: 34 cards, two decks, all abilities, and all artwork validate.

Run: `godot --headless --path . --script tests/test_suite.gd`

Expected: all rules, content, art, AI, and UI contracts pass.

Run: `godot --headless --path . --script tests/ui_smoke.gd`

Expected: responsive smoke passes.

Run: `godot --headless --path . --quit-after 2`

Expected: application boots with no parser or runtime errors.

- [ ] **Step 3: Complete one manual player match flow**

Launch Godot normally. Build or select `us-starter`, select Standard AI, mulligan at least one card, deploy a unit, move into the Frontline, attack a unit, play an Order, activate a Countermeasure, allow it to trigger or expire, finish by victory or concede, rematch, and return to Deck Builder.

Expected: every action updates Credit, zones, timeline, and legal highlights consistently; no action gets stuck and no hidden AI card becomes visible.

- [ ] **Step 4: Export the resource pack and conditionally native app**

Run: `godot --headless --path . --export-pack PCK builds/pck/OpenCards.pck`

Expected: exit `0` and a nonempty `builds/pck/OpenCards.pck`.

If `/Users/ticos/Library/Application Support/Godot/export_templates/4.7.stable/macos.zip` exists, run: `godot --headless --path . --export-release macOS builds/macos/OpenCards.zip`.

Expected when templates exist: exit `0` and a nonempty archive. When templates do not exist, record that native export is blocked only by the missing official template; do not weaken or remove the preset.

- [ ] **Step 5: Commit release configuration and docs**

```bash
git add .gitignore export_presets.cfg project.godot README.md
git commit -m "docs: finalize OpenCards run and export workflow"
```

## Plan Completion Check

Re-run every command in Task 4 Step 2 and the PCK export from Step 4 after the final commit. Confirm `git status --short` contains no generated `builds/`, `.godot/`, or `.superpowers/` entries.

The project is complete only after the full manual flow succeeds with generated artwork and all automated suites remain green.
