# Vpet 1.12.2

- Add a light, high tone when a Brick Battle ball hits either paddle and a distinct lower tone when it hits a brick. Explosive hits play one impact sound while destroying neighboring bricks.
- Play an ascending two-tone chime when either paddle collects a power-up, including replacements and repeated paddle-growth pickups.
- Prepare gently faded sound effects before play and allow overlapping hits without cutting off other cues. Stop sounds when stopping the game, returning to the lobby, switching games, or closing the arcade.

## Vpet 1.12.1

- Keep the ball, triangle, coin, card, D20, plate, pudding, and joystick above the pet sprite, including both visible portions during movement between displays. Keep the Toy Chest and fences beneath the pet. Continue respecting Window Location and keeping Arcade Window above desktop assets. Hovering or clicking an overlapping toy targets the toy instead of pausing or shaking the pet underneath.
- Play the bundled Blue Dragon's default idle and walking animations at twice their previous speed. Apply the faster speed to new profiles, Restore Default, and exported default sprite packages. Preserve custom sprites' saved animation speeds.
- Center the Brick Battle NPC directly over the right paddle and draw the pet in front of its bar. Keep it aligned while moving, resizing, growing the paddle, and shaking after an explosive hit.

## Vpet 1.12.0

- Add Brick Battle to the third arcade cabinet. Control the left paddle with the mouse and click to serve against the Vpet on the right, with Easy, Normal, and Hard difficulty choices.
- Use a full-width field with a center wall three bricks wide and twelve rows tall. Balls bounce off walls, paddles, and bricks; broken bricks disappear. Initial serves launch both sides together and start the two-minute round clock.
- Award a point when a ball exits the opponent's side, with five dots per side and a two-second flash for each new point. Win rounds by reaching five points or leading when time runs out. Finish a match at 2–0 after two rounds, or after round three or later when one side leads; tied rounds and matches continue without awarding a draw point.
- Add optional power-ups, enabled by default: blue three-ball launches, yellow paddle growth, green sticky catches, and red explosive balls that destroy neighboring bricks or briefly freeze a paddle. Broken bricks have a 25% drop chance; power-ups move toward the paddle that last touched the ball.
- Show the pet's configured name on its score card, or Vpet when unnamed. Display a flashing WINNER! on the winning half, with Proud when the pet wins and Sad when it loses. Keep Start/Stop, resizing, saved power-up preferences, and existing Dance Time and Simon Says controls available.

## Vpet 1.11.7

- Increase Sprite Maker's limit from five to ten frames for each movement and optional reaction animation. Include all ten slots in selection, previews, Magic Tweak, undo, saving, and sprite export.
- Use the supplied Blue Dragon PNG as Vpet's default sprite, with eight idle frames and ten walking frames in all eight directions. Preserve its visible pixels and transparency, and use the new artwork for Restore Default and the default template.
- Keep existing custom pets selected during updates. Continue loading older sprite packages and projects; older projects gain empty frame slots 6–10 while retaining their existing selections, dimensions, facing, offsets, and speeds.
- Save new Sprite Maker projects and exports in package version 6 for ten-frame support. These files require Vpet 1.11.7 or later; previously installed legacy PNG pets remain supported.

## Vpet 1.11.6

- Enlarge the pet and all four surrounding directional squares in Dance Time and Simon Says to use the arcade lobby's on-screen proportions as the window resizes. Scale the square labels and moving targets with them. Keep the key-mapping button at its existing size beside the upper square.
- Make Dance Time's Misses label larger and bold, with room for its counters and Practice total.
- Remove the dark outer band around Dance Time's fixed squares. Use a thicker light outline inside each square and the same outline thickness and dimensions for its moving targets.
- Keep visual target overlap aligned with the scoring model while scaling the game artwork. Retain existing hit timing, scoring, music, and game controls.

## Vpet 1.11.5

- Make Dance Time and Simon Says game spaces and floor backgrounds square at every window size, and remove their titles from the game space. Keep the game names in the window title.
- Set the default Dance Time music volume to 25%, retaining saved volume choices and existing playback gain behavior.
- Move Practice mode to the right of Difficulty. Move the song timer into the header, to the left of Score Card; keep it visible in Practice mode.
- Use one dark neutral color for both games' Off backgrounds, Dance Time's padded misses block, the backing around its four fixed directional squares, and the entire padded volume column with its title, slider, and percentage.
- Brighten Dance Time's moving targets and borders for better visibility over the floor. Give all four target lanes equal entrance distances in the square scene without changing song timing or scoring.
- Change Dynamic Dance Floor rotation to an instant clockwise 90-degree turn every one second, with a four-second full cycle.

## Vpet 1.11.4

- Reduce the default Dance Time volume from 53% to 27%, approximately half the previous slider setting. Preserve saved volume choices, normal device volume at 100%, and mute at 0%.
- Prepare all three Dance Time songs in the background and decode complete PCM audio before enabling Start. Play queued audio from memory at its native sample rate, synchronize targets with the sample clock, and reuse prepared songs on replay and difficulty changes.
- Add Dance Time Background choices below Practice mode: Dynamic (default), Static, and Off. Dynamic instantly rotates the supplied Dance Floor PNG clockwise 90 degrees every two seconds; Static holds the original orientation; Off restores the blank gradient.
- Add Simon Says Background choices below Difficulty: On (default) for the supplied Simon Floor PNG, or Off for the blank gradient. Save both games' background choices independently.
- Smooth Simon tones with cosine attack/release fades and prepared overlapping playback, so rapid identical or different presses do not cut off another tone.
- Start Simon Says with one button on Easy, three on Normal, and five on Hard. Continue adding one button per completed round, with the full one-second pause retained.

## Vpet 1.11.3

- Fix Dance Time music volume with explicit playback attenuation instead of scaled MCI volume requests. Full slider volume uses the normal Windows/device level; lower values provide useful quiet levels, and zero is mute. Retain saved volume choices and show the selected percentage below the slider.
- Move Dance Time's flashing Streak Combo indicator to the top right.
- Add Practice mode below Difficulty. Replace miss-limit circles with a numeric miss total, play the whole song regardless of misses, and disable scoring and streak bonuses. Practice rounds do not change high scores; the choice is saved and defaults to off.
- Extend Simon Says' down-facing pause after the final input highlight to one full second before the next combination.

## Vpet 1.11.2

- Use normal window stacking for Arcade Window in every Window Location mode while keeping Vpet toys and desktop assets beneath it. Restore their configured stacking when the arcade closes.
- Halve Dance Time's base music playback volume for all songs and slider levels, including previously saved settings. Retain the volume slider and saved choices.
- Start Excellent streaks at three consecutive Excellents. Track active streak points separately; when a streak ends, apply its multiplier only to those points and add the result to the calculated score. Show calculated points, streak points, and the multiplier separately.
- Keep Simon Says' last correct input highlighted, then face the pet down for a full half-second pause before the next sequence. Play the same directional tones for player inputs as for the pet's demonstration.

## Vpet 1.11.1

- Use the supplied Joystick.png for the desktop joystick, retaining transparent corners and its existing size. Bundle the image in the installer.
- Keep Arcade Window above all other Vpet desktop assets in every window-location mode. Dynamic still allows other applications above the arcade. Display all four directional arrows on the Dance Time cabinet.
- Reduce the default music volume by one quarter, from 70% to 53% after rounding. Preserve saved volume choices. Keep Dance Time's volume slider visible before, during, and after a round.
- Replace Start with Stop during countdowns and active rounds in both games. Stop immediately cancels the round, playback, effects, and pending scoring without changing high scores; Start returns for replay.
- Display a bold, flashing Streak Combo indicator at the top left during Dance Time streaks. Good or Great now ends the streak and resets the multiplier to 1.0x while retaining pending points. Display calculated-score multipliers as 1.0x rather than x1.0.
- Pulse successfully hit Dance Time targets briefly before removing them, without allowing duplicate scoring or counting them as misses.
- Play a distinct short tone for each direction highlighted by the pet in Simon Says. Wait half a second after a correct reply before beginning the next, longer sequence.

## Vpet 1.11.0

- Add Joystick to Display Items. Drag it independently of movement fences; click it to send the pet behind it, even in Static mode or at zero walking speed. On arrival, Arcade Window opens and the desktop pet hides. Closing Arcade restores the pet behind the joystick and resumes its normal movement rules.
- Add an arcade lobby with the active Dance Time and Simon Says cabinets, three grey reserved cabinets, and your current pet walking in front of them. Close Game returns to the lobby; closing the window returns to the desktop.
- Add Dance Time with the supplied Easy, Normal, and Hard songs, inward-moving targets, 1x/1.5x/2x target speeds, and three/two/one miss counters. Score Good, Great, or Excellent for at least 1%, 50%, or 90% target overlap. Five consecutive Excellents start a streak multiplier; additional Excellents increase it by 0.1. Misses bank pending points and reset the streak; the final miss discards the entire game score. Completing the song banks the remaining points and can set a new high score.
- Add Simon Says with red/up, blue/down, green/left, and yellow/right squares. Repeat increasingly long sequences within five seconds, with faster demonstrations on Normal and Hard. Each completed round earns 50 points; a wrong key or timeout ends the game and banks the score.
- Both games offer Easy, Normal, and Hard difficulty, a three-second countdown, WASD or arrow-key controls, and Start to replay after a result. Click the score box to view the selected game's difficulty high score. Save separate high scores for each game and difficulty, the selected key controls, and Dance Time's music volume.
- Bundle all three arcade songs for offline play. Joystick visibility and position are saved, and disconnected-display recovery keeps it on a connected screen.

## Vpet 1.10.0

- Add Find My Vpet under Advanced settings, off by default. Choose ALT or CTRL, click the key box to clear its old mapping, then press a key to assign it with confirmation. The default ALT + ALT shortcut uses a double tap; CTRL + CTRL also uses two taps.
- Find My Vpet circles the pet and dims the surrounding displays, follows the pet as it moves, and fades away over one second. It can reveal the pet beneath other windows without changing Window Location, taking keyboard focus, or blocking mouse clicks.
- Hovering over the D20 now pauses the pet's current action, turns it toward the die, and displays the die's current value. Moving away resumes the paused action.
- Add a one-eighth-second pause after each eating shake, before one third of the food disappears.
- Move Load Vpet on PC startup and Auto-update on app startup to the top of Advanced settings, retaining their saved values.
- Change reminder alerts to a simple three-tone chime.
- Add Remove Plate to the plate's right-click menu to hide the plate, clear its food, and end feeding.

## Vpet 1.9.1

- Reduce the plate and every pudding portion to 75% of their previous width and height. Adjust the pet's eating position and screen-edge limits to match the smaller artwork.
- Add a separator between Reminders and Check for Updates in the pet's right-click menu.

## Vpet 1.9.0

- Add Display Items as the first pet-menu entry, with Toy Chest and the new Plate toggle. Move Reminders below the separator after Settings, directly above Check for Updates.
- Add an independent white plate that follows Window Location and can be dragged around the desktop outside either fence. Its visibility, position, and default food are saved; it starts empty when Vpet restarts.
- Right-click the plate and choose Pudding to select and serve it, or left-click an empty plate to serve the default. Pudding includes caramel, whipped cream, and a strawberry, with whole, two-thirds, and one-third portions.
- Serving food interrupts the pet's action and temporarily overrides Static, zero speed, and movement fences. The pet walks just above the plate, behind the food, then takes three bites. Each bite shakes for one eighth of a second and removes one third, with a brief pause between bites. Normal movement resumes after eating, including returning inside the restricted fence.
- Add Hunger (U+1F37D) to default reactions, image replacement and preview controls, and optional Sprite Maker animations. Existing projects keep all frame mappings, dimensions, offsets, and speeds; Hunger starts empty. New projects and exports with reaction animations require Vpet 1.9.0 or later.
- Keep an airborne coin flipping while other toys are dragged, including when the pointer passes over the pet.

## Vpet 1.8.1

- Keep the reminder speech bubble beside the pet and move it with the pet during walking, dragging, and travel between displays. Use the actual rendered sprite and emote positions instead of WinForms' outdated window bounds, which could leave reminders near the original window location.
- Position reminders in the same rendering pass as the emote bubble, using the active display's working area so they remain visible at screen edges and after crossing to another display. Preserve window-location settings, message contents, and Dismiss behavior.

## Vpet 1.8.0

- Add Reminders directly above Settings in the pet menu. The Reminder Window provides Add Reminder and an inline editor above a read-only list, with an Edit button beside each saved entry.
- Support One-Time reminders with a date and time, and Recurring reminders with multiple Sunday-through-Saturday days. Each day has its own Every, Every other, First, Second, Third, or Fourth weekday frequency. All times use explicit AM/PM controls and the PC's local time.
- Save messages up to 200 characters with clickable web links. New reminders start active; Save, Delete, and Cancel manage entries. Every-other schedules begin with the week in which that day was selected.
- Play a chime when a reminder is due and show a separate speech bubble beside the pet with Dismiss. Notifications preserve keyboard focus, follow Window Location, and remain visible until dismissed. Hovering holds the pet still for reading. One-time reminders become Completed; recurring reminders remain scheduled.
- Preserve reminder schedules, pending messages, and firing history across restarts. Vpet must be running to notify; reopening or waking shows the latest missed occurrence of each reminder. Multiple reminders queue, and undismissed recurring occurrences are combined. Saves retain a backup and do not overwrite unreadable reminder files.
- Grey out Clean Up Toys when no toys are present, and enable it when any toy is added.
- Keep the pet moving while toys or the chest are dragged, or while the chest menu, triangle menu, or instrument sound settings are open. Moving a toy the pet is approaching updates its destination without pausing that visit.
- Make Sprite Maker's display/edit area resizable within the window. Drag the divider above Preview zoom to resize it; upper controls scroll independently. A second divider adjusts the validation area. Zoom and frame mappings are preserved, and guide/completion buttons remain visible at the bottom.

## Vpet 1.7.0

- Hover over a stopped D20 to show its current value in the pet's speech bubble. Moving away restores the usual reaction or game announcement; Settings retains its pause indicator.
- Left-drag the ball or D20 to move it inside the play zone. Right-drag to aim and release to launch along the red arrow. Simple left-clicks still bounce the ball or roll the die.
- Label fences above their top-left corners: Restricted Area for the separate red fence and Play Zone for the blue shared/toy fence. Labels follow movement, resizing, visibility, and window location.
- Add Clean Up Toys directly below Display Play Zone in the chest menu. It removes every toy and cancels active play while leaving the chest and fence available.
- Clicking a face-up card now only flips it face down. Click again to call the pet for another game. During spontaneous play, the pet approaches first, turns a revealed card face down, then starts its round.
- Give user-started card games a 30-second decision window after the pet announces its card. If no choice is made, clear the announcement and have the pet move away before resuming its saved movement rules. Settings and other movement pauses suspend this timer.
- Always flip the coin upward, even when its animation crosses the fence or screen edge.
- Move Check for Updates below the final separator, immediately above Close Vpet.

## Vpet 1.6.3

- Display A instead of 1 for all four Ace cards, both on card faces and in card-value speech bubbles. Ace remains the lowest rank in High/Low.
- During spontaneous D20 play, have the pet walk to a clear position beside the die before launching it. It then watches the roll, announces the result, and resumes its normal movement rules. Moving the play zone updates the visit, and removing the die or picking up the pet cancels it.
- Keep user-triggered D20 rolls immediate: clicking or pulling the die makes the pet watch from its current position.

## Vpet 1.6.2

- Have the pet stand beside the coin using the same spacing as the card. Keep the full sprite clear of the toy and inside the screen, including custom sprites, screen edges, and repositioned coins.
- Return the D20 to its original size, with matching fence clearance, rolling rotation, and launch arrow. Make its numbers smaller and fit them inside the central face so double-digit values do not overlap the die's lines.

## Vpet 1.6.1

- Give the coin a plain gold face with no Heads/Tails artwork or values. Flip it around the horizontal X axis, compressing its height as it turns. The pet still announces Heads or Tails after landing.
- Have the pet stand beside the card to play High/Low, leaving both halves clickable. Choose a clear adjacent position that fits the full sprite near display edges, including for custom sprites and relocated cards.
- Double the D20's width and height. Update its fence clearance, minimum play-zone size, rolling rotation, and aiming arrow to match the larger die, including when moving the zone between displays with different scaling.

## Vpet 1.6.0

- Add Coin, Card, and D20 toys to the chest. Coin visits include a quarter-second pause, half-second shake, airborne flip, and Heads/Tails result using the supplied artwork. Pull and release the D20 to roll and ricochet; the pet watches without chasing and announces its final 1–20 result.
- Play High/Low with the card: the pet announces a card, then choose High on the top half or Low on the bottom. Correct guesses show Love, incorrect guesses show Sad, and equal ranks show Question. Each round resets all 52 cards and excludes the announced exact card from the flip. Pets can also choose and play these games on their own.
- Keep card, coin, and die announcements separate from the eight customizable reactions. Called cards remain in the speech bubble until a choice is made. Picking up the pet or closing/removing the toy cancels its game.
- Add Sound Setting at the top of the triangle menu, with a volume slider, Test sound, Save, and Cancel. The saved volume applies to your taps and the pet's replies; testing does not save or add a remembered tap.
- Add independent Animation speed controls in Tweak and Complete, from 0.25x to 3x, with 1x as the default. Changes play immediately and survive project saving, export, and use by the pet. Older projects retain their original speed.
- Change random reaction intervals to Often: 15–30 seconds, Sometimes: 30–60 seconds, and Rarely: 90–120 seconds.
- Replace the restricted circle and radius settings with a rectangular fence. Drag its plus control to move it and its edges or corners to resize it. It stays below the pet and remains active while hidden.
- Add an Advanced tab with Sync Play Zone with Restricted Area enabled by default. Restricted movement uses the play-zone fence, and both display toggles control that shared fence, even with the chest closed. Disable sync to use separate saved fences.
- Make settings resizable with wrapping text, controls, and vertical scrolling. Remove the Legacy 5 x 10 option from Sprite settings; use Sprite Maker to turn PNG sheets into custom sprite packages.

## Vpet 1.5.4

- Move Tweak and Complete to the bottom right of Sprite Maker, with How to Guide immediately to its left. Remove Back to Sprite Maker from the Tweak and Complete footer; close that window to return to the editor.
- Make the hollow center of the triangle toy clickable, including right-clicking to open its sound menu. Simplify the sound names to Chime, Honk, and Drum; Chime remains the default.
- Manual Check for updates opens a window showing the installed version and either the newest available version with an Update button, or confirmation that Vpet is current. Optional automatic updates at startup still install automatically.
- After a successful update, show the changes from every release since the last version run, grouped by version. The installed app includes the full change history for offline viewing, and the installer supports updates from older Vpet versions.

## Vpet 1.5.3

- Right-click the triangle to choose Chime (Default), Honk, or Drum (Snare). The saved sound is used for both your taps and the pet's replies, while preserving tap counts and rhythm.
- Add Update Sprite Sheet to Sprite Maker. Replace an open project's PNG while retaining all frame coordinates, frame sizes, alignment tweaks, facing, diagonal settings, and optional emote mappings.
- Updated artwork appears immediately in the existing selections and is embedded when saving the project. Keep the sheet layout consistent; mappings outside a smaller sheet or over empty cells are listed for repair before completion. Invalid replacement files leave the existing sheet intact.

## Vpet 1.5.2

- Add Load Last Project beside Load Project in Sprite Maker. Reopen the most recently opened or saved .vpetproject, including after restarting Vpet.
- Remember successful project opens and saves, including Save Project As. The button becomes available after opening or saving a project. Switching projects retains the unsaved-changes prompt; missing or unreadable files leave current work intact and show recovery guidance.

## Vpet 1.5.1

- Keep Help Messages at the bottom of the toy chest menu, directly above Close Toy Chest and below all toy options.
- Show the pause emote only while Vpet settings are open. Toy interactions, the toy chest menu, and the pet's right-click menu no longer trigger it. Try It Out previews still temporarily replace the settings pause symbol.

## Vpet 1.5.0

- Add Close Toy Chest to the chest menu, putting away the chest, fence, ball, and triangle together.
- Add a triangle instrument. Tap it for a short chime; after a brief pause, your pet walks over and repeats the number and rhythm of your taps. Drag the triangle to reposition it inside the play zone.
- Pets occasionally visit available toys on a random 60–120-second schedule. Settings, menus, hovering, and dragging pause their actions. Toy visits preserve movement settings and return restricted pets to their circle.
- Add a pause-symbol speech bubble while controls pause movement, including when settings are open. Explicit reaction previews temporarily replace it. Pause is not a customizable reaction.
- Add How to Guide in Sprite Maker and Tweak and Complete, covering sheet upload, frame selection, review, project saving, alignment, clipping, export, and applying the finished pet.
- Add a Left/Right sheet-facing choice so either source direction exports with correct travel animations.
- Add optional Emote Animations for Music, Love, Question, Anger, Sad, Fear, Disgust, and Proud. Missing rows use normal pet behavior. Existing projects and sprites remain supported.
- Replace Download blank sprite sheet with Download current sprite sheet, beneath Save default template. It exports the active pet's transparent PNG for editing and reuse in Sprite Maker.

## Vpet 1.4.0

- Add Auto-update on app startup beneath the startup setting in Sprite. Choose Yes to install the newest public release when Vpet opens; No remains the default.
- Check for updates installs available releases directly, without a continue/install prompt. Release notes appear only after a successful update; up-to-date checks show a simple status message.
- Give each Sprite Maker animation type its own frame width and height. All frames within that type share its dimensions; other animations keep their own sizes.
- Allow clipping in Tweak and Complete. Artwork outside the frame is cut off in the preview and exported sprite, including fully clipped blank frames. Existing projects and sprites still load.
- Use a plus symbol for the play-zone center control and a solid red ball launch arrow.
- Make Ball a toggle that adds or removes the ball. Replace the old menu help text with a Help Messages toggle; hints appear above the chest only while hovering over the fence controls or ball.
- Clicking the ball produces three bounces, each half the height of the last. Pet return shots use a wider range of random launch strengths.

## Vpet 1.3.0

- Add Display Toy Chest to the pet's right-click menu. The chest and toys follow the pet's window location setting.
- Move the rectangular play zone by its center control, or resize it by dragging its edges and corners. Drag the chest anywhere inside; moving the fence away recenters it.
- Right-click the chest to hide or show the play-zone fence, or bring out a red ball. The invisible fence still keeps toys inside.
- Click the ball to bounce it. Pull back to aim, then release to launch in the arrow's direction. The ball ricochets off the fence and gradually stops.
- Your pet walks to the ball's resting spot, waits a quarter second, shakes for half a second, and sends it in a random direction. It does not chase its own return.
- Fetching temporarily overrides Static mode, zero walking speed, and restricted roaming. Afterwards the pet resumes its saved settings, walking back into its circle when needed. Dragging the pet cancels fetching.
- Save chest visibility, play-zone visibility, rectangle size and position, and chest position. Recover toys onto a connected display if their monitor disconnects.

## Vpet 1.2.2

- Add visible zoom controls to both Sprite Maker previews: editable percentage, zoom in/out, 100%, Fit, and Ctrl + mouse wheel.
- Keep the image under the pointer while zooming, with scrollbars for larger previews. Zoom preserves frame selections, artwork, and alignment offsets.
- Replace the application, installer, and shortcut icon with the supplied Vpet Pixel icon.

## Vpet 1.2.1

- Keep the sprite sheet's scroll position when selecting and setting animation frames.
- Drag the red selection border to move a frame; drag its corners to resize it.
- Magic Tweak aligns the lowest visible pixels to a shared ground point, so changing tails or arms do not shift the feet sideways. Preview controls keep the ground line steady.
- Preserve exact PNG pixels during sprite import, project loading, and export, regardless of the image's stored DPI.
- Show update descriptions before installing and show what changed after a successful update. Check for updates also lets you review the current version's changes.

## Vpet 1.2.0

- Add Sprite Maker with PNG sheet upload, zoom, frame selection, shared dimensions, corner resizing, and project save/load.
- Support one to five frames in every animation, optional diagonal poses, and automatic cardinal-direction fallback.
- Add Tweak and Complete with animated previews, frame scrubbing, mouse/keyboard nudging, Undo, and Reset Cycle.
- Magic Tweak aligns the visible bottom-center of every frame in the selected animation without stretching artwork.
- Export portable .vpetsprite files and resume editing with .vpetproject files. Existing PNG sprite sheets remain supported.
- Sprite settings now provide Upload Custom Sprite and Open Sprite Maker. The active pet changes only after Use this pet.

## Vpet 1.1.6

- Dragging uses progressive display fragments instead of snapping the entire pet between monitor bounds.
- Preserve mouse capture throughout a cross-display drag, including pickup during a walking transition.
- Releasing midway across a display edge smoothly settles the pet onto a connected display.
- Add a review and implementation outline for Sprite Maker; the editor is not included in this release.

## Vpet 1.1.5

- Accept custom emote PNGs up to 512 × 512 pixels and resize smoothly while preserving centering and proportions.
- Reduce the gap above the uploaded-emote list.
- Updates show download/install progress and a completion message, preserve shortcut choices, and reopen the pet without the setup wizard.
- Existing installations launched through older updaters also use the progress-only installation flow.

## Vpet 1.1.4

- Custom emotes are centered horizontally and vertically by their visible artwork, ignoring transparent PNG padding.
- Keeps the existing speech-bubble size and preserves image proportions for both extra emotes and default replacements.

## Vpet 1.1.3

- Personality settings list loaded custom emotes by name, with a Try It Out button next to each.
- Includes added emotes and custom replacements for default reactions.
- The list refreshes as images are added, replaced, or removed; previews use the pet's speech bubble.

## Vpet 1.1.2

- Added Load Vpet on PC startup under Sprite, with No (Default) and Yes options.
- Startup launches at Windows sign-in for the current user; choosing No removes it.
- Uninstall removes this installation's startup entry while preserving personal settings.

## Vpet 1.1.1

- Pet names appear below the pet, with a white outline around the letters and no background box.
- Clicking outside the pet's right-click menu closes it while allowing the click to reach the underlying application.
- Submenus remain interactive, and names stay clear of speech bubbles and taskbars.

## Vpet 1.1.0

- Optional pet name at the top of Personality settings.
- Hide the name, show it on hover, or always display it.
- Names sit between the pet and its speech bubble without overlapping.
- Automatic public-release checks at startup and every six hours.
- Update notifications and a manual check in the right-click menu.
- User-approved installer download with SHA-256 verification; personal settings are preserved.
- GitHub test/public release workflow and VS Code development tasks.

Users on 1.0.0 must install 1.1.0 once to enable future update notifications.
