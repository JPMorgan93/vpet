# Create a custom Vpet

Open **Settings > Sprite > Open Sprite Maker**. **Tweak and Complete** sits at the bottom right, with **How to Guide** immediately to its left. The Tweak and Complete window uses the same footer arrangement with **Complete** and **How to Guide**.

Resize the display/edit area inside Sprite Maker by dragging the horizontal divider above **Preview zoom**. The controls above it scroll independently when space is limited. Drag the second divider beneath the sheet to change the space reserved for validation messages. Both dividers preserve zoom, frame selections, and saved mappings; the guide and completion buttons stay at the bottom.

## Step 1 — Upload the sprite sheet

Choose **Upload Sprite Sheet** and select a transparent PNG up to 4096 × 4096 pixels and 64 MiB. Sheets can use any arrangement. **Download current sprite sheet** in Sprite settings exports the active pet's PNG for editing; **Save default template** exports the original companion.

Set **Sheet faces** to **Left** or **Right**, matching the side-facing and diagonal artwork. Export normalizes the directional rows, then the app mirrors them for the opposite travel direction. Up/down and reaction artwork are kept as drawn. Downloaded runtime sheets face left.

To revise artwork in an existing project, open it and choose **Update Sprite Sheet**. Select the new transparent PNG. All existing frame coordinates, dimensions, alignment offsets, facing, diagonal settings, and optional emote selections stay intact. The current animation, slot, and preview zoom are retained. **Upload Sprite Sheet** continues to start a new project.

Keep the same sheet layout for the existing mappings to line up. Coordinates are kept in pixels, without automatic scaling or detection of moved sprites. A larger or smaller replacement is allowed; empty or out-of-bounds enabled frames are flagged and must be repaired before Tweak and Complete. You can still save an incomplete project. Invalid PNG files leave current artwork and mappings unchanged.

Use **Save Project** to embed the revised sheet and retained mappings, then **Tweak and Complete** to adjust and export the updated pet. The new sheet is an unsaved project edit until you save it.

## Step 2 — Set frames for all animations

Choose an animation type and set its frame width and height, up to 100 × 150 pixels. Every frame of that type shares its dimensions; other types can have different sizes.

Choose a numbered slot. Click the sheet to place the red frame, drag its border to move it, or drag a corner to resize all frames of this animation type. Choose **Set** to save the selection and advance to the next slot. The scroll position stays fixed. **Clear** removes a slot. Each required animation needs 1–5 nonempty selections, played in numbered order. Gaps in slot numbering are allowed.

Turn **Diagonal animations** off if your sheet has no diagonal poses. Saved diagonal selections remain in the project and can be enabled again.

Turn **Emote Animations (optional)** on to add Music, Love, Question, Anger, Sad, Fear, Disgust, Proud, and Hunger buttons. Create any reactions you want; empty rows never block export and use normal pet behavior. Selected frames must still be valid. Turning this option off preserves its selections in the project but excludes them from export. At runtime, matching default reactions (including replaced reaction images) use these animations at six frames per second at 1×, including greetings, clicks, pickup, feeding, and card outcomes. Each row's saved speed multiplies that rate. Extra custom emotes, special game announcements, and the pause indicator use normal behavior. Hover and pickup use down-idle when no matching reaction animation exists.

## Step 3 — Review your selections

Check each slot and resolve any missing-frame or invalid-selection messages. Use **Preview zoom**, **+ / −**, **100%**, **Fit**, or **Ctrl + mouse wheel** to inspect the image. Scrollbars reach enlarged areas. Zoom changes only the preview, keeping source coordinates and offsets unchanged. Zoom supports 1–1600%, reduced for very large canvases to respect Windows limits.

## Step 4 — Save the project

**Save Project** stores a `.vpetproject` with the original PNG, all selections, dimensions, sheet facing, enabled options and alignment offsets. Incomplete projects can be saved. **Save Project As** makes another copy. **Load Project** resumes editing without needing the original PNG on disk. **Load Last Project**, beside it, reopens the most recently opened or saved project, even after restarting Vpet. It starts disabled until you open or save a project. The last file path updates after successful loads, saves, and Save Project As. Save your edits before closing to resume them later; this button loads the saved file. A missing or unreadable file leaves current work intact and shows a message; use Load Project to find a moved file again.

## Step 5 — Tweak, complete, and use your pet

When required animations are ready, choose **Tweak and Complete**. Its own **How to Guide** covers the controls:

- Choose an animation to watch it play. Only populated optional reaction rows appear here.
- **Animation speed** changes only the selected type, from **0.25× to 3×**. **1×** keeps its original speed; **Reset to 1x** restores it. Moving the slider resumes playback immediately, so you can compare speeds. Speed affects animation playback, independently of the pet's walking speed. Save Tweaks or Complete saves it with the project and export.
- **Tweak** pauses playback and enables the frame slider. Select a frame to adjust it. A one-frame animation stays at slider position 0.
- Drag the artwork, or click the preview and use arrow keys to move one pixel. Hold Shift for five pixels.
- **Magic Tweak** aligns every pose's lowest nontransparent pixel row to the green ground line, centering its midpoint. A single bottom pixel is the anchor; midpoints between pixels use the left pixel. Faint alpha counts. It never stretches artwork or changes the source.
- The purple rectangle is the output frame. Artwork outside it is clipped from the preview and export, even if a whole frame is moved outside. Move it back to recover clipped source pixels.
- **Undo** reverses alignment edits, **Reset Cycle** removes that cycle's offsets, and **Save Tweaks** saves the editable project.
- **Resume Preview** plays the adjusted animation. Close the Tweak and Complete window with its **X** to return to Sprite Maker and change selections or sizes; your tweaks stay in the open project. Both previews have independent zoom controls.

**Complete** saves the project and exports a `.vpetsprite` with the PNG atlas and animation frame counts. Keep the project for later editing. Close Sprite Maker to preview the export in settings, then choose **Use this pet**. Later, **Upload Custom Sprite** can load the exported file. Exporting alone does not change the active pet.

Each animation is clipped before export. Smaller frames receive transparent padding to the largest populated, enabled frame size, aligned at bottom-center without stretching. Padding cannot restore clipped pixels.

## File compatibility

The Hunger update loads all existing version 1–4 projects and sprites. Opening an older project preserves every frame mapping, size, offset, and animation speed, and adds empty optional Hunger slots at 1× speed. New project saves and exports containing reaction animations use package version 5 and require the Hunger update or later. Movement-only exports still use version 2 without speed metadata, or version 4 with saved speeds. Older files without speed metadata continue at 1×.

Previously installed legacy PNG pets remain supported. New PNG sheets go through Sprite Maker before use. New runtime atlases with emotes have nine additional rows in the reaction order above; older eight-reaction atlases still load. Downloaded PNGs carry artwork only: use Sprite Maker to select populated cells and restore frame counts and animation speeds before exporting a usable `.vpetsprite`.
