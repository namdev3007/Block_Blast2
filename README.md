🧱 Blockblast — Unity Puzzle

A tactile, combo-chasing block puzzler with juicy UI, smart piece spawning, and arcade-style scoring.
✨ Highlights

Crisp grid UX with hover/line previews, place-flash, intro wave and clear-pop animations (DOTween).
<img width="330" height="590" alt="image" src="https://github.com/user-attachments/assets/b1189006-62b8-44e0-8f53-a08b0d944459" />

<img width="342" height="593" alt="image" src="https://github.com/user-attachments/assets/80370d24-cc33-446f-b91f-bf4a9f4fd755" />

Smart spawn system (Triplet Bag, hole-filler, placeable guarantee, line-chaser bias) → fewer soft-locks, more clever turns.

Arcade scoring & combo with popup layering:

Combo badge → delayed +Points → optional Praise (“Good!”, “Great!”, …).

Unified popups (PopupManager): Combo, Points (fly-to total), Praise.

Modular UI: UIManager (HUD/pause), UISettingManager (toggles), UISwitcher (save to PlayerPrefs).
<img width="331" height="588" alt="image" src="https://github.com/user-attachments/assets/ee26e8dd-0a8c-4dad-8e3c-6e9d47612809" />


Audio pipeline (AudioManager): BGM crossfade, pooled SFX, click sounds, mixer support, persistent toggles.

Firebase-ready (Analytics/Crashlytics/Remote Config optional).

🎮 How to Play

Place shapes on the board to complete rows/columns. Clear multiple lines to stack combos and rack up points. The longer you keep the chain alive, the bigger the multiplier—and the juicier the praise.

🧠 Spawn System (fun but fair)

Goals: always “one interesting move”, encourage clears, keep end-game solvable without feeling rigged.

Triplet Bag (optional): build many 3-piece candidates and pick one bag that:

can be placed sequentially on a board snapshot,

optional: at least one piece leads to a line clear,

avoids duplicates.

Placeable Guarantee: each refill tries to ensure at least requiredPlaceableSlots piece(s) can be placed immediately.

Hole-Filler (optional): small chance to inject a shape that perfectly fits a tiny cavity (≤ holeFillerMaxCells).

Line-Chaser Bias: per-slot chance to prefer shapes more likely to clear lines.

High-Line Spikes (rare & exciting):

~7% chance: pieces that can set up 5-line clears,

~3% chance: pieces that can set up 6-line clears.

No “ramp harder over time” (removed by design).

All knobs are on ShapeSpawnConfig (ScriptableObject) so you can A/B test safely.

🧮 Scoring & Combo

Clear points: increase with simultaneous lines; multiplied by combo.

Block placement points: separate (optionally not multiplied, per spec).

Combo multiplier f(c) (c = current combo level):

c = 1 → 1.0×

2 ≤ c ≤ 4 → c + 1 (e.g. 3→4×)

5 ≤ c ≤ 9 → 9 + 1.5·(c − 5) (9×, 10.5×, …, 15×)

c ≥ 10 → 22 + 2·(c − 10) (22×, 24×, 26×, …)

Popups flow:

show Combo (0.8s) → 2) show +Points (clear only), flying to the total score anchor

if linesCleared ≥ 2 also show Praise (“Good!”, “Great!”, “Excellent!”, “Fantastic!”, “Legendary!”).
Color uses a per-instance randomized gradient + outline + underlay for a playful but readable look.

🧱 Grid & Drag

Snap assist: when releasing between two cells, the piece snaps to the nearest cell center (ghost never disappears mid-drag).

Previews: shape footprint + potential line completion; occupied tiles hidden when line preview overlays.

Juice: glow flash on place, scale pop on clear, intro wave overlay.

🔊 Audio

Two-track BGM with crossfade (bgmA/bgmB), pooled SFX, click effect helper.

Mixer support: optional exposed params (BGMVol, SFXVol).

Toggles: music/SFX wired to UISwitcher and saved with PlayerPrefs (am_music_on, am_sfx_on).

🧩 UI

UIManager: HUD/Pause, reacts to GameManager state.

UISettingManager: resume/restart/audio toggles (+ click sfx).

UISwitcher: animated toggle (DOTween), persists state via prefsKey and fires onValueChanged(bool) for binding.

🛠️ Getting Started
Requirements

Unity 6.

Packages: TextMeshPro, DOTween (Pro or free), optionally Firebase Unity SDK.

Setup

Clone:

git clone https://github.com/yourname/blockblast.git


Open in Unity (2021.3+).

Install DOTween → Tools → Demigiant → DOTween Utility Panel → Setup DOTween…

Open the sample scene (e.g. Scenes/Main.unity).

Press Play.

Optional: Firebase

Drop google-services.json (Android) to Assets/StreamingAssets/.

Drop GoogleService-Info.plist (iOS) into Assets/.

Add a small initializer (see FirebaseInit example) and you’re ready for Analytics/Crashlytics/Remote Config.

🧪 Designer Knobs (ScriptableObject)

Open ShapeSpawnConfig:

requiredPlaceableSlots: guarantee ≥ N placeable pieces.

useTripletBag, bagCandidateCount, bagRequireSequentialPlaceability…

enableHoleFiller, holeFillerChance, holeFillerMaxCells…

forceLineClearChance: per-slot bias for line-clear shapes.

highLine5Chance / highLine6Chance (if you expose them): the rare “wow” spawns.

🗺️ Roadmap

Daily goals & missions

Themes & skins pack (Grid/Tile sprites)

Cloud save (Firebase Auth + Firestore)

Leaderboards (GPGS / Game Center)
