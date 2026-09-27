# Sprite Maker review and implementation outline

Reviewed: 2026-09-27. Source: `C:\Users\Chase\Documents\Vpet Sprite Maker.docx`.

Historical design review. Sprite Maker is implemented in version 1.2.0 with the user-approved bottom-center alignment for Magic Tweak. See [the user guide](SPRITE-MAKER.md) for current behavior. The outline below records the original implementation plan.

## Intended workflow

1. **Sprite settings:** replace Choose sprite sheet with **Upload Custom Sprite** and **Open Sprite Maker**. Keep the existing preview and explicit Use this pet step so importing does not unexpectedly replace the active pet.
2. **Sprite Maker:** upload a transparent PNG, view it on a checkerboard with zoom/pan, choose an animation and frame, position a red selection box, resize from its corners, then Set or Clear that frame.
3. **Save / Load:** save a portable editing project containing the original sheet, every selected rectangle, diagonal support, frame dimensions, and all alignment offsets. Incomplete projects can be saved and resumed.
4. **Tweak and Complete:** preview an animation; Tweak pauses it and reveals a frame slider plus alignment controls. Save preserves edits; Complete saves the project and exports a separate file suitable for Upload Custom Sprite.

## Animation rules

The document's button list mentions idle cycles, but its diagonal toggle and preview example also require walking cycles. Include both:

| Diagonals | Required cycles | Frames per cycle |
| --- | --- | --- |
| Off | Idle Up, Idle Down, Idle Left/Right, Walk Up, Walk Down, Walk Left/Right | 1–5 |
| On | All six above, plus Idle Diag Up, Idle Diag Down, Walk Diag Up, Walk Diag Down | 1–5 |

- Author left-facing side/diagonal frames; mirror them for the corresponding right-facing direction, consistent with the existing pet.
- Set adds a green check to that numbered frame and advances to the next number. Setting frame 5 leaves it selected. Clear removes only the selected frame.
- At least one nonempty frame must be set for every enabled cycle. Unused frame slots are valid, not errors. Play populated slots in numeric order, skipping gaps.
- List missing cycles in red. Show a green saved status for a cycle with one or more valid frames. Block Tweak and Complete/export until every required cycle is usable, but allow project Save at any time.
- Turning diagonals off should retain their selections in the project so toggling back on does not destroy work. Disabled cycles do not block export.
- Without diagonal artwork, use the nearest cardinal direction, with a small angle hysteresis to prevent flicker around the 45-degree boundaries. Retain the last direction for idle; hover/pickup still uses down idle. This selects a frame sequence rather than visually blending incompatible poses.

## Selection and alignment

- Use one shared frame width/height across the project, retaining the current **100 × 150 px maximum per frame** unless a later change explicitly raises it. Show numeric dimensions alongside corner resizing for accurate pixel selection.
- Clicking the sheet places the selection's top-left corner. Resizing changes all selected rectangles' dimensions while keeping their origins. Flag out-of-bounds selections; never silently crop or discard them.
- Require some transparency in the sheet and at least one visible pixel in every selected frame. Store original image coordinates independently of display zoom.
- Tweak's slider spans **0 to N−1**, where N is the number of populated frames in the selected cycle. For a single frame it stays at 0.
- Dragging the preview or using arrow keys adjusts a per-frame integer offset. Keep the source pixels untouched. Warn about clipping and block export until the result fits the frame.
- **Magic Tweak:** detect each frame's visible-pixel bounds, then align their bottom centers within the selected cycle. This is the recommended default for a walking pet because it stabilizes the feet. Do not stretch differently sized poses. Provide Undo/reset for automatic alignment.
- The document describes matching centers when sizes differ; pure center alignment can make feet bounce. The user approved bottom-center alignment, which is implemented in version 1.2.0.

## File formats and compatibility

The current runtime assumes a five-column, ten-row PNG with **four idle frames and five walking frames**, and all diagonal rows present. The proposed editor cannot be implemented fully by changing the upload button alone.

Recommended formats:

- **`.vpetproject`:** a versioned ZIP containing a JSON editing manifest and the original PNG. Store original selections and offsets so users can keep editing. Include the source image inside the project so moving or sharing it does not break paths.
- **`.vpetsprite`:** a versioned ZIP containing a PNG atlas and a JSON runtime manifest with cell dimensions, enabled directions, ordered frames per cycle, and frame counts. Export baked alignment offsets so the runtime only needs to draw frames.
- Keep **PNG-only image assets** inside both formats. Existing valid five-by-ten PNG sheets remain importable through Upload Custom Sprite, with their existing four/five-frame behavior.
- Do not run code from imported files. Read known archive entries directly, reject duplicate/unknown required fields and unsupported versions, and bound decoded image dimensions and uncompressed archive size before allocation.
- Suggested initial sheet limit: **4096 × 4096 pixels**, with a **64 MiB total uncompressed project limit**. This supports irregular sheets while bounding memory use; "any sprite sheet" should mean arbitrary layout, not unlimited file size.

## Implementation stages

1. Add the versioned project/runtime data model, import/export validation, variable frame counts, and diagonal fallback while preserving legacy PNG imports.
2. Build Sprite Maker's sheet view, frame selection and global resizing, cycle validation, and project Save/Load.
3. Build Tweak and Complete's animation preview, frame offsets, Magic Tweak, undo, and final export.
4. Integrate both Sprite settings buttons and persist custom sprite packages across app restarts and updates.

## Acceptance checks

- Save and reload an incomplete project without losing selections, source artwork, disabled diagonal cycles, or tweaks.
- Preview/export/reload 1–5-frame animations, including nonconsecutive populated slots and five-frame idle cycles.
- Use diagonal sequences when available; select stable cardinal animations when absent.
- Keep sprite, hover, drag, click reactions, names, movement limits, and cross-display rendering working with custom cells.
- Verify shared resizing, zoom coordinates, transparent/empty crops, clipping detection, and Magic Tweak undo.
- Reject corrupt/oversized packages safely; a failed import must leave the active pet unchanged.

Sprite Maker was not included in the 1.1.6 drag-fix release; it is included in 1.2.0.
