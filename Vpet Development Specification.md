# Virtual Pet Development Specification

Status: original prototype design with a current release addendum. The 1.6.0 rules below supersede earlier baseline details where they differ. See README.md and SPRITE-MAKER.md for the complete current application and editor instructions.

## Version 1.6.0 additions and revised rules

- Restricted movement uses a fixed rectangular fence with a draggable plus control and resizable edges/corners. Remove radius controls. The full pet stays inside; excluding it by editing the fence moves it to the center. Fences stay below the pet and remain active while hidden.
- Add Advanced > Sync Play Zone with Restricted Area, default true. Restricted movement uses the play-zone fence, including when the chest is hidden, and Display restricted area controls the same visibility as Display Play Zone. False restores the independent saved fence. Existing radius data initializes its rectangular dimensions.
- Triangle > Sound Setting opens a 0–100% volume slider with Test sound, Save, and Cancel. Testing does not commit the change or add a remembered tap; saved volume applies to user and pet notes.
- Tweak and Complete offers per-animation speed from 0.25× to 3×, default 1×, with immediate live preview. Speeds persist in version 4 projects and exported sprites. Older formats retain their original playback speed.
- Random reactions: Often 15–30 seconds, Sometimes 30–60 seconds, Rarely 90–120 seconds. Spontaneous toy play remains a separate 60–120-second schedule.
- Settings wrap text and controls, resize to the work area, and scroll when necessary. Sprite settings accept custom .vpetsprite packages; PNG sheets go through Sprite Maker. Remove the Legacy 5 x 10 option/text from that page. Previously installed legacy pets still load.
- Coin: visit, pause ¼ second, shake ½ second, flip in the air, then announce Heads or Tails using the supplied PNGs.
- Card: announce one of 52 cards on arrival and retain its bubble until choosing High (top half) or Low (bottom half). Flip another exact card, excluding the announced one. Compare ranks 1–10, J, Q, K: correct means Love, incorrect means Sad, equal means Question. Reset all 52 cards each round. Autonomous play chooses High/Low randomly, then resumes normal behavior after its outcome.
- D20: pull and release to spin/roll and ricochet inside the zone. The pet stops and faces it without chasing, then announces 1–20 when it stops. Coin, Card, and D20 join the randomized toy choices.
- Card values, Heads, Tails, and die numbers are special announcements, excluded from random and customizable reactions like the pause indicator. Pause appears only while Vpet settings are open.

## Purpose and launch

A desktop companion that wanders around connected displays, reacts to interaction, and supports custom pet artwork and emotes. Launch from a desktop shortcut. Start with the default pet unless a valid imported sprite sheet has been saved. Close Vpet exits the application and all its related windows.

The prototype targets Windows using native Windows Forms and layered windows.

## Rendering and window behavior

- Render the pet on a transparent surface. Transparent space around the pet passes clicks to applications underneath; visible pet pixels accept hover, left-button dragging, and right-click interaction.
- Wandering and reactions never activate the pet window or steal keyboard focus. Explicitly opened menus and settings can receive input normally.
- Over Everything (default): keep the pet above ordinary application windows.
- Under All: lock the pet and its reaction bubble beneath all application windows while remaining visible on uncovered desktop space. Clicking, dragging, and reactions must not raise the pet. Reapply the ordering as other windows open or change position in the stack. No Explorer desktop parenting is required.
- Dynamic: use ordinary window stacking behavior, including normal activation behavior, rather than determining stacking solely from dragging.
- Keep speech bubbles within usable display space, repositioning them around the pet if necessary.

## Movement and interaction

Choose a random reachable destination, walk toward it, then idle for a newly randomized 10–30 seconds before choosing another destination. Use the configured cruising speed except during arrival slowdown.

### Movement modes

- Free Roam (default): choose destinations across connected displays.
- Restricted: create a circle around the pet the first time this mode is enabled, then keep its center and size fixed until explicitly edited. Dragging the pet never recenters it. Directly below the movement type, show a **Display restricted area** checkbox and radius slider. Check the box whenever entering Restricted mode. When checked, draw an on-screen outer fence and a draggable center handle; the ring and empty interior pass clicks through. The slider changes the radius in desktop pixels. Moving or shrinking the area so the pet is outside immediately moves the pet to the center. Releasing a pet outside the fence also returns it to the center. Hiding the fence leaves the restriction active. Keep the area when switching modes or restarting; recover its location only if a display change makes it unreachable.
- Static: remain where placed in idle animation; dragging, reactions, and the half-second click shake still work.
- Restricted radius is limited to 30–1,000 desktop pixels. Clamp larger saved values to 1,000. Keep the entire fence UI, including its center handle, below the pet in every layer mode, including while a handle is clicked or the pet crosses displays.
- The description below the movement type and any restricted controls changes to explain the selected mode. Restricted explains the checkbox, center handle, and radius slider; Static explains remaining where placed; Free Roam explains travel between displays.

### Speed and animation

- Speed slider: 0–100, with 50 at the exact midpoint of the tick marks. Default 50 corresponds to approximately 100 reference pixels per second; 0 stops autonomous movement.
- Required animations: directional idle and walk cycles for all eight directions (up, up-right, right, down-right, down, down-left, left, and up-left). Use the approved base sprite reference and the animation contract below. Shake uses a positional effect rather than a separate sprite row and lasts exactly half a second on a simple left click. There is no shake-duration setting and no shake on drag release.
- Select the walking animation from the travel angle using eight equal 45-degree sectors. Diagonal travel uses diagonal artwork rather than a cardinal animation. Use a small angular tolerance to prevent rapid switching at sector boundaries.
- Retain the last facing direction on ordinary arrival. Hovering, pressing the left mouse button, dragging, and the click shake all use down-facing idle. On a fresh launch without a saved facing direction, face down. Idle is an animated cycle, including in Static mode.
- Reduce speed to half cruising speed near the destination, then quarter cruising speed closer to arrival. Use an arrival tolerance and clamp the final movement step so the pet stops reliably without overshooting.

### Hover and drag sequence

1. On pointer entry, stop movement and show down-facing idle animation. Show the personality's hover emote once, subject to a cooldown.
2. When the pointer leaves, resume an interrupted destination if movement is otherwise allowed. Do not replace that destination simply because of hovering.
3. Left-button interaction immediately switches to down-facing idle. Hold and move to drag the pet. When dragging begins, show the personality's pickup reaction: Sweet uses Love, Sassy uses Fear, and Bashful uses Sad. Starting a drag cancels an active click shake. A simple click is recognized separately on release and continues to show Love for every personality.
4. On release after an actual drag, cancel the previous destination and rest in down-facing idle for five seconds. Do not shake. A simple click without dragging triggers a half-second shake and Love on release, preserving any interrupted destination.
5. Keep the pet stationary during a click shake. Hovering may extend the idle pause but does not extend or cancel the half-second effect. Ignore the former saved shake-duration setting and omit it from future saves.
6. After that pause, choose a new destination only if the pointer has left and the selected movement mode and speed permit movement. Restricted mode continues to use the same fixed area.

A new drag can interrupt the click shake or pause. Drag release starts a new five-second rest without shaking. Shake is a visual offset around the resting position; keep its visible bounds inside usable display space.

## Multiple displays and taskbars

- Never walk, idle, shake, or remain placed over a taskbar. Use each display's usable work area, with the full rendered sprite footprint accounted for.
- Keep the entire sprite visible at screen edges except during a transition between displays.
- Permit travel between all connected displays, including arrangements whose edges do not align or touch. Do not treat the bounding rectangle of all displays as entirely usable screen space.
- Cross display boundaries using synchronized sprite fragments clipped to the two work areas. For adjoining edges, both fragments share a continuous screen position and recombine into one sprite without missing or duplicated pixels. Prefer a shared crossing coordinate where usable edges overlap. For separated or offset work areas, use paired edge positions and matching crossing progress rather than an instantaneous exit/entry jump. Route through intermediate neighboring displays when necessary. Do not render any part over taskbars.
- Clamp a released drag to a valid position. If no display contains the full sprite at release, place it at the nearest valid position on a connected display.
- On display disconnection, invalidate routes and destinations involving that display. If the pet was on it, immediately relocate the pet to a valid connected display. Recenter any restricted movement area as needed and exclude disconnected displays from future movement.
- When resolution, scaling, or work areas change, revalidate the pet's position, destination, and restricted area.
- Use the display where the app launched as the session's reference for apparent sprite size and movement speed. Adapt rendering scale and movement units on other displays to preserve that appearance and speed as closely as available display-scaling information allows. Exact physical matching is not guaranteed by display metadata.

There is no drawable screen space in a physical or configured monitor gap. Paired edge fragments preserve visual continuity there; adjoining work areas use exact continuous coordinates.

## Emotes and personalities

Display emotes in a speech bubble, normally above the pet. Each bubble lasts three seconds. Allow only one bubble at a time. Interaction reactions take priority over random reactions; a pickup reaction replaces an active hover reaction. Do not queue a backlog of random reactions.

Draw the speech bubble body and tail as one closed path. Fill it, clip the emoji/image to the interior, then stroke the complete outline last. Preserve the border on both tail edges and rounded corners for either tail direction and at all display scales.

| Emote | Windows emoji | Unicode |
| --- | --- | --- |
| Music | 🎵 | U+1F3B5 |
| Love | ♥️ | U+2665 |
| Question | ❓ | U+2753 |
| Anger | 💢 | U+1F4A2 |
| Sad | 💧 | U+1F4A7 |
| Fear | ❗ | U+2757 |
| Disgust | 🌀 | U+1F300 |
| Proud | 🏆 | U+1F3C6 |

Render these characters using the PC's installed Segoe UI Emoji font and native color-font support. Append U+FE0F to Love to request emoji presentation. Do not impose the previous custom symbol colors or shapes. User-supplied image replacements still take precedence; Restore original restores the matching Windows emoji.

| Personality | Hover reaction | Pickup reaction |
| --- | --- | --- |
| Sweet (default) | Music | Love |
| Sassy | Question | Fear |
| Bashful | Love | Sad |

### Random selection priorities

Priority 1 has weight 6, priority 2 has weight 3, and priority 3 has weight 1. An emote's probability is its weight divided by the sum of eligible emote weights.

| Emote | Sweet | Sassy | Bashful |
| --- | --- | --- | --- |
| Music | 1 | 3 | 2 |
| Love | 1 | 3 | 2 |
| Question | 1 | 2 | 1 |
| Anger | 3 | 1 | 3 |
| Sad | 2 | 2 | 1 |
| Fear | 3 | 1 | 2 |
| Disgust | 2 | 2 | 1 |
| Proud | 2 | 1 | 3 |

Each custom emote has priority 2. Prototype default: custom emotes participate individually without a collective probability cap, so adding many increases their combined selection probability.

### Frequency

- Rarely: 180–300 seconds.
- Sometimes (default): 60–120 seconds.
- Often: 30–60 seconds.
- Off: disable random emotes; interaction emotes remain enabled.

Sample a new interval each time. Prototype default: measure the interval from the end of the preceding bubble, and restart the random timer after interaction reactions. Suppress random reactions during dragging, shaking, and open pet menus/settings.

## Custom emote artwork

- In Personality settings, provide a selector for the eight default emotes, an image preview, **Choose image…**, and **Restore original**. Replacing an image preserves that emote's personality interaction triggers and random weight. Save each validated replacement independently, use it for both interaction and random reactions, and preserve the current image if an import fails. The additional random-emote folder remains a separate feature.

- PNG only; reject an image if either dimension exceeds 512 pixels. Smaller images are allowed.
- Display custom images centered on a white background inside the speech bubble. Preserve aspect ratio, fit within the available content area, and do not crop.
- Composite any transparent areas against white.
- Custom opens a persistent user-data folder for adding emote images.
- Validate newly added images and report unsupported or corrupt files without disrupting the running pet. Prototype default: refresh while running when files are added, changed, or removed.

The 512 × 512 limit applies to each custom emote image, not the complete pet sprite sheet.

## Pet sprite sheets

- PNG with transparency.
- Every frame must use the same dimensions: at most 100 pixels wide and 150 pixels tall. The complete sheet may be larger because it contains multiple frames.
- Upload Vpet opens a window containing an import button, the default sprite sheet as a format example, and an animation preview.
- Validate the format and preview animations before replacing the active pet. Invalid imports preserve the current pet and show an actionable error.
- Copy accepted artwork into persistent user data so reopening the app does not depend on the original file's location.
- Reload the saved pet on subsequent launches. Add Restore Default Pet. If a saved asset becomes missing or invalid, fall back to the default pet.
- Source frame dimensions and displayed size are separate: display scaling can change rendered pixel dimensions to preserve apparent size across monitors.

### Base artwork and animation contract

Use [Base Vpet Sprite Sheet.png](assets/reference/Base%20Vpet%20Sprite%20Sheet.png) as the visual base for all frame generation. Preserve its character design, proportions, purple/blue palette, outlines, shading style, and directional poses. Read each cycle from left to right.

The supplied reference is 154 × 704 pixels, contains red row labels, and has an opaque white background (RGB without an alpha channel). It is an annotated reference rather than a runtime-ready transparent grid. Keep the original reference unchanged. Runtime assets must omit labels, spacing used for captions, and the opaque background.

The sheet establishes ten source cycles and 45 illustrated frames:

| Row | Reference label | Source cycle | Frames | Runtime directions |
| --- | --- | --- | --- | --- |
| 1 | Idle Up | idle_up | 4 | Up |
| 2 | Idle Down | idle_down | 4 | Down |
| 3 | Idle Left/Right | idle_left | 4 | Left; mirror horizontally for right |
| 4 | Idle UpD | idle_up_left | 4 | Up-left; mirror horizontally for up-right |
| 5 | Idle DownD | idle_down_left | 4 | Down-left; mirror horizontally for down-right |
| 6 | Walk Up | walk_up | 5 | Up |
| 7 | Walk Down | walk_down | 5 | Down |
| 8 | Walk Left/Right | walk_left | 5 | Left; mirror horizontally for right |
| 9 | Walk UpD | walk_up_left | 5 | Up-left; mirror horizontally for up-right |
| 10 | Walk DownD | walk_down_left | 5 | Down-left; mirror horizontally for down-right |

Interpretation: UpD and DownD denote upward and downward diagonal poses. The supplied lateral and diagonal poses are treated as left-facing source art; opposite-facing cycles are horizontal mirrors. This produces sixteen directional runtime cycles from ten source rows. If a future character has intentional left/right asymmetry, its format will need explicit opposite-facing art.

### Runtime sheet layout

Implementation default based on the reference:

- Use a transparent PNG arranged as five columns and ten rows, in the table's order, without labels, outer margins, or gutters.
- Use a uniform frame cell width W and height H across the whole sheet. W must be at most 100 pixels and H at most 150 pixels, with both positive integers. These limits apply to cells, not the entire sheet.
- Infer W from total sheet width divided by five and H from total height divided by ten. Reject sheets that are not exactly divisible into that grid or exceed the per-frame limits.
- Idle rows use columns 1–4; column 5 stays fully transparent and is never played. Walk rows use all five columns. Do not create a fifth idle frame merely to fill the grid.
- Align all frames to a consistent bottom-center ground anchor, with enough transparent padding for the complete character, including ears and tail. Preserve intentional animation motion relative to that anchor; avoid accidental crop-induced jitter.
- Apply horizontal mirroring about the same frame anchor. Mirror only the character artwork, not speech bubbles or their contents.
- Preserve crisp pixel art with nearest-neighbor scaling and prefer integer enlargement when compatible with the display-size requirement.
- Publish a transparent template matching this contract and show the annotated reference separately as an explanatory example in the upload screen.

### Playback and frame generation

- Prototype playback defaults: idle at four frames per second (one-second loop), walk at eight frames per second (0.625-second loop). Validate loop continuity in a preview and tune timing if needed.
- Play frames in the illustrated order and repeat; do not automatically use a ping-pong sequence.
- Keep walking animation time-based and independent of rendering frame rate. Prototype default: scale walking playback by actual speed relative to the default 100 reference pixels per second, including arrival slowdown. Idle timing remains fixed.
- When changing direction within the same activity, retain cycle phase. When switching between walking and idle, begin the new activity's cycle at its first frame.
- Select facing from normalized travel direction; diagonal movement must have the same overall travel speed as cardinal movement.
- Prototype angular tolerance: retain the current direction until the heading crosses five degrees beyond its nominal sector boundary.
- For the half-second click shake, use down-facing idle artwork and apply a bounded positional shake around the ground anchor. Do not require generated shake frames.
- Frame generation must use the supplied image as its visual reference, preserve the four-frame idle and five-frame walk structure, and maintain consistent scale and palette between directions. Do not replace diagonal cycles with rotated cardinal art.
- Review generated frames both individually and as looping animations before accepting them as the default pet.

The prototype now extracts the 45 illustrated frames from the preserved reference at runtime, removes exterior white background pixels, and aligns them in 32 × 36 cells. The resulting transparent runtime sheet can be exported from the artwork settings as a template. The original reference remains unchanged; no replacement AI artwork has been generated.

## Menu and persistence

The right-click menu exposes:

- Movement Controls: Type, Display restricted area and radius directly beneath Type when Restricted, Speed, and Location. Do not show a shake-duration control.
- Personality: Type, Emote Frequency, default-emote replacement and restoration, and the additional custom-emote folder.
- Upload Vpet, including Restore Default Pet in its window.
- Close Vpet.

Persist movement type, speed, location mode, personality, emote frequency, restricted radius, fence visibility, the fixed restricted anchor, default-emote replacements, and accepted custom assets. Restore the last valid pet position, validating it against currently connected displays on launch. Drop the obsolete shake-duration preference. The Bring pet here action is removed.

## Prototype tuning defaults

These values can be adjusted after observing the prototype:

- Speed mapping: slider value multiplied by two reference pixels per second.
- Slowdown thresholds: half speed within 40 reference pixels; quarter speed within 15.
- Arrival tolerance: one reference pixel.
- Hover reaction cooldown: five seconds between eligible pointer entries.
- Use a monotonic clock for movement, shake duration, and reaction scheduling.
- While a pet menu or settings window is open, pause autonomous movement. Resume when it closes if no other pause condition applies.

## Development stages

1. Core prototype: default pet, transparent surface, animation, wandering, hover, dragging, click shake, closing, and early validation of multiple displays and all three stacking modes.
2. Complete default experience: movement controls, personalities, emotes, settings, and persistence.
3. Customization: published sprite format/template, validated imports, animation previews, custom emote folder, and restoration of defaults.

## Acceptance criteria

- Hover interrupts walking; pointer exit resumes the interrupted destination when permitted.
- Simple left clicks shake for exactly half a second and show Love for every personality. Dragging and drag release never shake; release still rests for five seconds.
- Static mode never starts autonomous travel, including after dragging.
- Dragging the pet never recenters a restricted area. Its center handle moves the fence explicitly; moving or shrinking it past the pet relocates the pet to the center. The checkbox controls visibility without removing the restriction.
- Speed 50 sits at the center of a 0–100 slider, and the movement description follows the selected type.
- The pet avoids taskbars and stays fully visible except while crossing between displays.
- The pet can move between nonaligned displays without becoming stranded in invisible desktop gaps.
- Disconnecting its display relocates the pet visibly onto a connected display and invalidates stale travel targets.
- Display scaling changes preserve apparent pet size and speed as closely as practical.
- Autonomous activity never steals keyboard focus; transparent empty areas pass clicks through.
- Dynamic follows ordinary window stacking.
- Under All keeps both pet and reaction windows below application windows and blocks elevation until another location mode is selected.
- Sometimes generates intervals within 60–120 seconds.
- Interaction emotes replace random emotes; only one bubble is visible.
- Custom emotes accept only valid PNG files no larger than 512 × 512 and appear against white inside the bubble.
- Each default emote can be replaced and restored in the UI; replacements retain their triggers and weights, survive restart, and reject invalid images without losing the current image.
- Imported pet sheets use transparent PNG frames no larger than 100 × 150.
- Runtime sprite sheets use the specified five-column, ten-row grid; idle rows play four frames and walking rows play five.
- All eight movement directions use the correct facing, including mirrored diagonal cycles, without diagonal speed increases.
- Hover, left-button press, dragging, and click shaking use down-facing idle. Ordinary arrival retains its last facing direction. Frame changes and mirroring do not cause unintended ground-anchor jumps.
- The annotated reference remains unchanged; runtime frames contain no captions or opaque reference background.
- Invalid imports preserve the current pet; successful imports survive a restart; Restore Default Pet works.
