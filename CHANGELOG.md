# Vpet 1.1.6

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
