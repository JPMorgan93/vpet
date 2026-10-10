using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal static class MakerGuide
    {
        internal const string SheetGuide=
            "Step 1 — Upload the sprite sheet\r\n"+
            "Choose Upload Sprite Sheet and select a transparent PNG (up to 4096 × 4096 pixels). You can download the current pet's sheet from Settings > Sprite. Set Sheet faces to Left or Right to match the side-facing artwork in your sheet; use the same direction for diagonal artwork. Export automatically handles both travel directions.\r\n\r\n"+
            "Updating existing artwork? Open your project, then choose Update Sprite Sheet to replace its PNG while keeping all frame positions, sizes, tweaks, and animation options. Keep sprites in the same positions on the revised sheet for the existing mappings to line up. Smaller sheets or moved artwork may need selections adjusted; any invalid enabled frames are listed below the preview. Save Project to keep the new sheet, then use Tweak and Complete to review and export.\r\n\r\n"+
            "Step 2 — Set frames for all animations\r\n"+
            "Under Animation types, choose an Idle animation from the first row or a Walk animation from the next. Under Animation frames, choose a numbered slot: 1–5 are on the first row and 6–10 on the second. Set and Clear are below these rows. Scroll the upper controls if needed. Click the sheet to place the red frame. Drag its border to move it; drag a corner or enter width and height to resize it. Choose Set to save that selection. Repeat in playback order, using 1–10 slots per animation. Width and height are shared within an animation type, but each type can have its own size (up to 100 × 150 pixels). Clear removes a slot.\r\n"+
            "Turn off Diagonal animations if the sheet has none. Emote Animations adds optional Music, Love, Question, Anger, Sad, Fear, Disgust, Proud, and Hunger rows. Fill any you want; leave the others empty for normal pet behavior. Turning this option off keeps your selections in the project but excludes them from export.\r\n\r\n"+
            "Step 3 — Review your selected frames\r\n"+
            "Drag the horizontal divider above Preview zoom to give the display/edit area more or less room inside Sprite Maker. The controls above it scroll independently. A second divider below the sheet changes the space reserved for validation messages. Resizing the panes does not change zoom or frame mappings.\r\n\r\n"+
            "Check each saved slot and resolve the missing-frame or invalid-selection messages below the preview. Use the zoom controls, Fit, or Ctrl + mouse wheel for accuracy. Scroll around larger sheets. Select only the desired sprite and leave the background transparent.\r\n\r\n"+
            "Step 4 — Save the project\r\n"+
            "Save Project creates a .vpetproject containing the original sheet, selected frames, sizes, facing, and tweaks. Save Project As makes another copy. Load Project lets you return and edit later. Load Last Project reopens the most recently opened or saved project, even after restarting Vpet. It becomes available after you first open or save a project. Incomplete projects can also be saved; save your changes to resume them later.\r\n\r\n"+
            "Step 5 — Tweak, complete, and use your pet\r\n"+
            "Choose Tweak and Complete at the bottom right once the required animations are ready. How to Guide is immediately to its left. Preview each cycle, adjust its frames, and use Magic Tweak for bottom-center alignment. Anything moved outside a frame is cut off in the exported animation. Complete saves the project and exports a .vpetsprite file. Close Sprite Maker, review the result in Sprite settings, and choose Use this pet. You can also load that .vpetsprite later with Upload Custom Sprite.\r\n";
        internal const string TweakGuide=
            "1 — Preview each animation\r\n"+
            "Under Animation types, choose an Idle animation from the first row or a Walk animation from the next to play its saved frames. Scroll this area to reach optional emote animations; only those with frames appear here. Animation speed changes just this animation, from 0.25x to 3x; 1x is the original speed. Moving the speed slider resumes playback immediately. Reset to 1x restores this animation's default. Save Tweaks or Complete saves the speed in the project and exported sprite. Use Fit, 100%, the zoom controls, or Ctrl + mouse wheel to inspect the pixels. Zoom changes the view, not the saved artwork.\r\n\r\n"+
            "2 — Select a frame to adjust\r\n"+
            "Choose Tweak to pause playback. Use the slider to pick a frame; its number and offset appear above the controls. Click the preview, then drag the sprite or use arrow keys to nudge it one pixel at a time. Hold Shift with an arrow key for five pixels.\r\n\r\n"+
            "3 — Align the animation\r\n"+
            "Magic Tweak aligns the lowest visible pixel row of every frame in the selected animation to the green ground line, centered on that row's midpoint. This helps feet stay in place. You can then nudge individual frames to refine the result. The purple border is the output frame: anything outside it is clipped from both preview and final sprite. Use Undo to reverse an edit, or Reset Cycle to remove all offsets for this animation. To change the crop size or selection, close this window with its X to return to Sprite Maker. Your tweaks stay in the open project.\r\n\r\n"+
            "4 — Check and save your tweaks\r\n"+
            "Choose Resume Preview to watch the adjusted cycle, then check the other animations. Save Tweaks stores your work in the editable .vpetproject. The source sheet stays intact, so clipped pixels can be recovered by moving the frame's artwork back inside.\r\n\r\n"+
            "5 — Complete and use the sprite\r\n"+
            "Complete saves the project and asks where to export the .vpetsprite. Keep the project for future editing. Close Sprite Maker to preview the exported pet in Settings > Sprite, then choose Use this pet. Left/right travel direction follows the Sheet faces choice from Sprite Maker. Any missing optional reaction animation uses normal pet behavior.\r\n";
        internal static Form Create(bool tweak)
        {
            var window=new Form{Text=tweak?"Tweak and Complete — How to Guide":"Sprite Maker — How to Guide",ClientSize=new Size(700,640),MinimumSize=new Size(480,400),StartPosition=FormStartPosition.CenterParent,AutoScaleMode=AutoScaleMode.Dpi};
            var close=MakerUi.Button("Close",delegate{window.Close();});close.Dock=DockStyle.Bottom;
            window.Controls.Add(new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(248,247,252),Font=new Font("Segoe UI",11),Text=tweak?TweakGuide:SheetGuide,DetectUrls=false,ScrollBars=RichTextBoxScrollBars.Vertical});
            window.Controls.Add(close);window.CancelButton=close;return window;
        }
        internal static void Show(Form owner,bool tweak){using(var window=Create(tweak)){window.Icon=owner.Icon;window.ShowDialog(owner);}}
    }
}
