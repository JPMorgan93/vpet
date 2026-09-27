# Create a custom Vpet

Open **Settings → Sprite → Open Sprite Maker**.

Both the sheet selector and **Tweak and Complete** have a **Preview zoom** toolbar directly above the image. Type a percentage, use **+ / −**, choose **100%** for actual pixel size, or choose **Fit** to see the whole sheet/frame. **Ctrl + mouse wheel** zooms around the pointer; scrollbars let you reach enlarged areas. Toolbar changes zoom around the center of the visible area. Zoom changes only the preview, keeping source pixels, selected rectangles, and saved offsets unchanged.

Zoom supports 1–1600%. The maximum is reduced for very large sheets to stay within Windows' canvas size limits. The animation preview initially fits the frame to the window; **Fit** can be used again after resizing the window.

## Select animation frames

1. Choose **Upload Sprite Sheet** and select a PNG with a transparent background. Sheets can have any arrangement, up to 4096 × 4096 pixels and 64 MiB. Use zoom and the scrollbars to inspect larger sheets.
2. Set the shared frame **width** and **height** (up to 100 × 150 pixels). These dimensions apply to every animation frame.
3. Enable **Diagonal animations** if your sheet has diagonal poses. With it off, the pet chooses the closest cardinal animation as it moves. Existing diagonal selections remain saved if you turn this option off and on.
4. Choose an animation, then a numbered frame. Click the sheet to place the red box's top-left corner. Drag its red border to move the selection. Drag a red corner to resize every frame's dimensions, or type the dimensions above the sheet.
5. Choose **Set** to save that selection. A green check appears, and the next numbered frame is selected. The sheet keeps its scroll position. **Clear** removes the selected frame.

Each enabled animation needs **one to five nonempty frames**. Unused slots are fine; playback follows populated slots in numbered order. Both idle and walking animations support five frames. Select left-facing side/diagonal artwork; right-facing poses are mirrored automatically.

Alerts identify missing animations, empty selections, or boxes extending outside the sheet. You can save incomplete work using **Save Project** or **Save Project As…**. The `.vpetproject` file includes the original PNG, all selections, dimensions, and alignment offsets. **Load Project** resumes editing without needing the original PNG on disk.

## Preview and align

When every required cycle has at least one valid frame, choose **Tweak and Complete**.

- Choose an animation to watch it play.
- **Tweak** pauses playback and enables the slider numbered **0 through N−1**, where N is the number of saved frames. A one-frame animation stays at 0. Controls keep their space so the preview's ground line stays still when you resume playback.
- Drag the preview to move that frame's artwork, or click the preview and use arrow keys. Hold Shift to move five pixels at a time.
- **Magic Tweak** puts the **lowest nontransparent pixel row** of each pose on the same green ground line. It aligns the midpoint of that row horizontally, so changing tails or arms above the feet do not shift the ground point. For a single bottom pixel, that pixel is the anchor. Midpoints between pixels use the left pixel consistently. All nonzero alpha counts, including faint shadows.
- The shared ground point is centered as closely as the artwork allows without clipping. If the poses cannot share a ground point in the current width, the editor asks you to widen the frame and check the selections. It leaves your offsets unchanged. Magic Tweak never stretches artwork or changes source pixels; manual nudging afterward can change the alignment.
- **Undo** reverses alignment edits. **Reset Cycle** removes offsets from the selected animation. **Save Tweaks** saves the project, including all current offsets.
- Red warnings identify artwork that would be clipped. Adjust it inside the frame or use Magic Tweak before exporting.

Use **Back to Sprite Maker** to change selections. Tweaks remain in the project. Closing Sprite Maker asks whether to save any unsaved changes.

## Export and use your pet

Choose **Complete** to save the project and export a separate **`.vpetsprite`** file. This package includes a PNG atlas, animation frame counts, and the diagonal setting. Keep the `.vpetproject` for later editing and share the `.vpetsprite` for use in Vpet.

Close Sprite Maker after exporting. Sprite settings will preview the new sprite; choose **Use this pet** to apply it. You can also choose **Upload Custom Sprite** and select any exported `.vpetsprite` file. Loading or exporting a sprite does not replace the active pet until you choose **Use this pet**.

The original five-column, ten-row PNG format remains supported. Those legacy sheets keep four idle frames and five walking frames per cycle. The default and blank template buttons still export templates for that legacy format.
