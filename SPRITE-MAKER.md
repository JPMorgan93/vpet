# Create a custom Vpet

Open **Settings > Sprite > Open Sprite Maker**. **How to Guide** is available in both Sprite Maker windows.

## Step 1 — Upload the sprite sheet

Choose **Upload Sprite Sheet** and select a transparent PNG up to 4096 × 4096 pixels and 64 MiB. Sheets can use any arrangement. **Download current sprite sheet** in Sprite settings exports the active pet's PNG for editing; **Save default template** exports the original companion.

Set **Sheet faces** to **Left** or **Right**, matching the side-facing and diagonal artwork. Export normalizes the directional rows, then the app mirrors them for the opposite travel direction. Up/down and reaction artwork are kept as drawn. Downloaded runtime sheets face left.

## Step 2 — Set frames for all animations

Choose an animation type and set its frame width and height, up to 100 × 150 pixels. Every frame of that type shares its dimensions; other types can have different sizes.

Choose a numbered slot. Click the sheet to place the red frame, drag its border to move it, or drag a corner to resize all frames of this animation type. Choose **Set** to save the selection and advance to the next slot. The scroll position stays fixed. **Clear** removes a slot. Each required animation needs 1–5 nonempty selections, played in numbered order. Gaps in slot numbering are allowed.

Turn **Diagonal animations** off if your sheet has no diagonal poses. Saved diagonal selections remain in the project and can be enabled again.

Turn **Emote Animations (optional)** on to add Music, Love, Question, Anger, Sad, Fear, Disgust, and Proud buttons. Create any reactions you want; empty rows never block export and use normal pet behavior. Selected frames must still be valid. Turning this option off preserves its selections in the project but excludes them from export. At runtime, matching default reactions (including replaced reaction images) use these animations at six frames per second; extra custom emotes and the pause indicator use normal behavior. Hover and pickup keep the pet's down-idle animation.

## Step 3 — Review your selections

Check each slot and resolve any missing-frame or invalid-selection messages. Use **Preview zoom**, **+ / −**, **100%**, **Fit**, or **Ctrl + mouse wheel** to inspect the image. Scrollbars reach enlarged areas. Zoom changes only the preview, keeping source coordinates and offsets unchanged. Zoom supports 1–1600%, reduced for very large canvases to respect Windows limits.

## Step 4 — Save the project

**Save Project** stores a `.vpetproject` with the original PNG, all selections, dimensions, sheet facing, enabled options and alignment offsets. Incomplete projects can be saved. **Save Project As** makes another copy. **Load Project** resumes editing without needing the original PNG on disk.

## Step 5 — Tweak, complete, and use your pet

When required animations are ready, choose **Tweak and Complete**. Its own **How to Guide** covers the controls:

- Choose an animation to watch it play. Only populated optional reaction rows appear here.
- **Tweak** pauses playback and enables the frame slider. Select a frame to adjust it. A one-frame animation stays at slider position 0.
- Drag the artwork, or click the preview and use arrow keys to move one pixel. Hold Shift for five pixels.
- **Magic Tweak** aligns every pose's lowest nontransparent pixel row to the green ground line, centering its midpoint. A single bottom pixel is the anchor; midpoints between pixels use the left pixel. Faint alpha counts. It never stretches artwork or changes the source.
- The purple rectangle is the output frame. Artwork outside it is clipped from the preview and export, even if a whole frame is moved outside. Move it back to recover clipped source pixels.
- **Undo** reverses alignment edits, **Reset Cycle** removes that cycle's offsets, and **Save Tweaks** saves the editable project.
- **Resume Preview** plays the adjusted animation. **Back to Sprite Maker** lets you change selections and sizes. Both previews have independent zoom controls.

**Complete** saves the project and exports a `.vpetsprite` with the PNG atlas and animation frame counts. Keep the project for later editing. Close Sprite Maker to preview the export in settings, then choose **Use this pet**. Later, **Upload Custom Sprite** can load the exported file. Exporting alone does not change the active pet.

Each animation is clipped before export. Smaller frames receive transparent padding to the largest populated, enabled frame size, aligned at bottom-center without stretching. Padding cannot restore clipped pixels.

## File compatibility

Vpet 1.5.0 saves version 3 projects and continues loading version 1 and 2 projects. Older dimensions, crops and offsets are preserved, with empty optional reaction rows added. Runtime exports without emote animations use version 2 (Vpet 1.4.0 or later). Exports containing emote animations use version 3 and require Vpet 1.5.0 or later.

The original five-column, ten-row PNG format still works with four idle and five walking frames. Runtime atlases with emotes have eight additional rows in the reaction order above. Downloaded PNGs carry artwork only: use Sprite Maker to select populated cells and restore their animation metadata before exporting a usable `.vpetsprite`.