# Player animation backlog

Updated 27 September 2026. **All current player animations are accepted as good for now.** End the dedicated animation pass and continue game development. New art is incremental work attached to gameplay milestones, not a prerequisite for connecting Tutorial.

The current set includes original walking/carrying and idle breathing, four-direction sprint/dash/opening strike/stationary aim/throw/catch, and fast detached axe spin. There are 148 registered body cel slots (including reuse and empty-hand variants), eight spin cels and the separate original 16-cel walk sheet. The articulated rig is retired.

## Priority and when to work on it

| Priority | Future animation | Trigger / starting budget per required view |
| --- | --- | --- |
| 1 | Hurt, strong stagger, defeat and return to control after respawn | Add alongside the first real enemy and death loop. About 3 / 5 / 6 cels; use existing feedback until then. |
| 2 | Connecting combo strike, finisher, charge stance and charged cleave | Improve when combat encounters and balance expose the need. About 6 / 8 / 3 / 8–10 cels. Preserve working mechanics and current fallback art. |
| 3 | Deliberate ground pickup, standing use, rest and Recall awakening | Add only when the corresponding Tutorial interaction ships. About 5 / 4 / 4 cels; awakening scope follows its short sequence. Automatic small pickups should not interrupt movement. |
| 4 | Dedicated moving aim and moving catches | Revisit once the complete route is playable. Prove one gait before expanding forward/backward/strafe variants; about 6 cels per gait. Existing walking/carrying fallback remains. |
| 5 | Dedicated diagonals, expanded idle/walk and secondary motion | Consider during broader gameplay review. Eight-direction art is a future scope option; diagonal travel and continuous aiming already work with cardinal drawings. Idle/walk budgets are 3 / 6, not required replacements. |
| 6 | Cosmetic continuity touch-ups to accepted art | Only fix distracting issues found in normal gameplay. North catch, east/north/south throws, south forehand and the rest of the current set are accepted. Preserve the approved exact east/west sprint mirror. |

Final audio, restrained trails/spin streaks/impact accents and recovery/cancel tuning accompany game presentation polish. Early swing aim correction and late-recovery dodge buffering remain proposed feel work; current swings commit direction at their start and reject dashes during attacks.

**Next main milestone:** connect Tutorial's ordinary gameplay loop—target lessons, easy enemy, first shard, bonfire and checkpoint—then the stone awakening and contextual guide. See the [development plan and deferred register](VerticalSlice.md). No further animation-only review cycle is scheduled.

## Foundation to preserve

- The [original character](../TheLostShrine/Assets/Art/Sprites/Player/PlayerSheet.png) is the style authority: compact proportions/boots, muted colors, soft charcoal outlines, patterned red/cream cap band, simple blob hands and the established hatchet scale. Draw hands and wrists naturally; do not paste anatomy from references.
- Use full-body authored poses. Weight comes from planted feet, torso, shoulder and whole-arm movement with readable anticipation and recovery. Keep the haft straight and blade perspective coherent; no articulated rig or rotating a held prop against a stationary fist.
- Draw body and axe together, export synchronized full-canvas layers, and preserve hidden surfaces/masks. Registered cels use 640×640, 128 PPU and foot origin (320,96) from the bottom. Point filtering; no compression, mipmaps or automatic fitting of individual silhouettes.
- Start most actions with six to eight drawings and unequal exposures. Judge normal-speed motion at game zoom before individual pixels. Effects support approved motion.
- Keep the weapon hand/satchel arrangement across new views. The explicitly approved east-to-west sprint mirror is the exception.
- Add only views and interactions the current milestone needs; preserve approved sources and one useful current preview rather than retaining every draft.

## Timing and ownership

One gameplay owner accepts input, governs commitment/cancellation and drives the action clock. The presenter samples body/weapon tracks; it does not grant possession, deal damage, spend stamina or decide movement.

- Tap E quick-throws; hold E aims and release throws. Existing launch timing is 120 ms after quick-throw key-down or 40 ms after prepared key-up. Commit direction and the 25 stamina cost once on valid release.
- Moving aim stays world-relative at 55% walk speed, suppresses sprint and allows stamina recovery. RMB or an accepted dash cancels uncommitted aim. A rejected dash leaves aim intact.
- Latch throw/Recall intent at key-down. Clear pending actions on control/focus loss, pause, stagger, death and scene changes; key-up after a catch must not trigger another throw.
- Physical release, collision and possession belong to the weapon. Keep the safe player-centered collision origin until a swept offset policy is validated. Visual hand height is not a physics offset.
- Contact, upgrades, hit pause, target deduplication and combo buffering retain existing gameplay rules. A miss has no impact pause; animation must not extend dash immunity.
- Keep one complete visible axe through flight and Recall. Catch follows confirmed arrival, hands off possession once, and does not add an action lock. Moving catches use the current fallback until compatible whole-body art is ready.
- Sprint follows actual travel (3.2 units/cycle); walls stop foot motion and walk/sprint/turn handoffs retain phase.

[Accepted previews](Art/Player/README.md) · [Sources and rebuild workflow](../ArtSource/Player/README.md) · [Verification index](../Tools/Verification/README.md)

Timing references retained from the original research: [walk cycles](https://www.slynyrd.com/blog/2024/5/24/pixelblog-50-human-walk-cycle), [animation timing](https://www.slynyrd.com/blog/2018/8/19/pixelblog-8-intro-to-animation) and [melee studies](https://www.slynyrd.com/blog/2018/9/8/pixelblog-9-melee-attacks).
