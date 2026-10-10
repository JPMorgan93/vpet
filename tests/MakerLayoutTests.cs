using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static Rectangle OnForm(Form form,Control control){return new Rectangle(form.PointToClient(control.PointToScreen(Point.Empty)),control.Size);}
        static void AnimationRows(Form window,Button[] cycles,string context)
        {
            var idle=cycles.Take(5).Where(button=>button!=null&&button.Visible).ToArray();var walk=cycles.Skip(5).Take(5).Where(button=>button!=null&&button.Visible).ToArray();
            Check(idle.Select(button=>OnForm(window,button).Top).Distinct().Count()==1&&walk.Select(button=>OnForm(window,button).Top).Distinct().Count()==1,context+": Idle and Walk each fit on one row");
            Check(walk.Min(button=>OnForm(window,button).Top)>idle.Max(button=>OnForm(window,button).Bottom),context+": Walk begins below all Idle buttons");
            foreach(var button in idle.Concat(walk))Check(button.Parent.ClientRectangle.Contains(button.Bounds),context+": button fits its animation row: "+button.Text);
            var title=Descendants(window).OfType<Label>().Single(label=>label.Name=="AnimationTypesTitle");var description=Descendants(window).OfType<Label>().Single(label=>label.Name=="AnimationTypesDescription");
            Check(title.Font.Bold&&OnForm(window,title).Bottom<=OnForm(window,description).Top&&OnForm(window,description).Bottom<idle.Min(button=>OnForm(window,button).Top),context+": title and description appear above the animation rows");
        }
        static void MakerLayoutWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(TenFrameFixture(),null);maker.Show();Application.DoEvents();
                var cycles=MakerField<Button[]>(maker,"cycles");var slots=MakerField<Button[]>(maker,"slots");var scroll=MakerField<Panel>(maker,"controlScroll");
                foreach(bool diagonals in new[]{true,false})foreach(Size size in new[]{new Size(1000,800),new Size(800,650)})
                {
                    MakerField<CheckBox>(maker,"diagonal").Checked=diagonals;maker.ClientSize=size;Application.DoEvents();
                    AnimationRows(maker,cycles,"Maker "+size+" diagonals="+diagonals);
                    Check(slots.Take(5).Select(button=>OnForm(maker,button).Top).Distinct().Count()==1&&slots.Skip(5).Select(button=>OnForm(maker,button).Top).Distinct().Count()==1&&OnForm(maker,slots[5]).Top>OnForm(maker,slots[4]).Bottom,"Frames 1-5 and 6-10 have separate complete rows at "+size);
                    var set=FindButton(maker,"Set");var clear=FindButton(maker,"Clear");
                    Check(OnForm(maker,set).Top>slots.Max(button=>OnForm(maker,button).Bottom)&&OnForm(maker,clear).Top==OnForm(maker,set).Top,"Set and Clear share a row below all ten frame buttons");
                    var instructions=Descendants(maker).OfType<Label>().Single(label=>label.Name=="AnimationFramesDescription");
                    Check(OnForm(maker,instructions).Bottom<OnForm(maker,slots[0]).Top&&instructions.Text.Contains("playback order"),"Frame instructions precede all numbered slots");
                    scroll.ScrollControlIntoView(set);Application.DoEvents();Check(scroll.RectangleToScreen(scroll.ClientRectangle).Contains(set.RectangleToScreen(set.ClientRectangle))&&scroll.RectangleToScreen(scroll.ClientRectangle).Contains(clear.RectangleToScreen(clear.ClientRectangle)),"Frame actions remain reachable at "+size);
                    Check(MakerField<SpriteSheetViewport>(maker,"viewport").Height>=80,"Grouping buttons retains a usable resizable sheet preview");
                }
                MakerField<CheckBox>(maker,"diagonal").Checked=true;maker.ClientSize=new Size(1000,800);scroll.AutoScrollPosition=Point.Empty;Application.DoEvents();CaptureForm(maker,"maker-grouped-buttons");
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();
                    foreach(Size size in new[]{new Size(880,750),new Size(740,640),new Size(1200,900)})
                    {
                        tweak.Size=size==new Size(740,640)?tweak.MinimumSize:size;Application.DoEvents();AnimationRows(tweak,MakerField<Button[]>(tweak,"cycles"),"Tweak "+size);
                        var viewport=MakerField<SpriteSheetViewport>(tweak,"viewport");
                        var choiceScroll=Descendants(tweak).OfType<Panel>().Single(panel=>panel.Name=="AnimationChoicesScroll");
                        foreach(var button in MakerField<Button[]>(tweak,"cycles").Take(10))Check(choiceScroll.RectangleToScreen(choiceScroll.ClientRectangle).Contains(button.RectangleToScreen(button.ClientRectangle)),"Idle/Walk buttons are fully visible above the preview: "+button.Text);
                        Check(viewport.Height>=80&&tweak.ClientRectangle.Contains(OnForm(tweak,viewport)),"Tweak keeps at least 80 pixels of visible preview space at "+size+" (height "+viewport.Height+")");
                        foreach(string caption in new[]{"Tweak","Magic Tweak","Complete","How to Guide"})Check(tweak.ClientRectangle.Contains(OnForm(tweak,FindButton(tweak,caption))),"Tweak action remains within resized window: "+caption);
                        CaptureForm(tweak,size==new Size(740,640)?"tweak-grouped-minimum":"tweak-grouped-buttons");
                    }
                    tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
        }
        static void UpdatedPetMenu(PetWindow pet)
        {
            var menu=pet.ContextMenuStrip;var movement=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Movement Controls");
            Check(movement.DropDownItems.Cast<ToolStripItem>().Select(item=>item.Text).SequenceEqual(new[]{"Type","Location","Display restricted area"}),"Movement menu places Location immediately below Type and removes Movement settings");
            var personality=menu.Items.OfType<ToolStripMenuItem>().Single(item=>item.Text=="Personality");
            Check(!personality.DropDownItems.Cast<ToolStripItem>().Any(item=>item.Text.StartsWith("Emote frequency")),"Personality submenu removes Emote Frequency");
            Check(!menu.Items.Cast<ToolStripItem>().Any(item=>item.Text.StartsWith("Upload Vpet")),"Pet menu removes Upload Vpet while Sprite settings remain available");
            var location=(ToolStripMenuItem)movement.DropDownItems[1];
            Check(location.DropDownItems.OfType<ToolStripMenuItem>().Select(item=>(LayerMode)item.Tag).SequenceEqual(Enum.GetValues(typeof(LayerMode)).Cast<LayerMode>()),"Moved Location submenu retains all saved window-layer choices");
        }
    }
}
