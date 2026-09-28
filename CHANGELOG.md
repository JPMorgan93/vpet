# Vpet 1.5.0

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
