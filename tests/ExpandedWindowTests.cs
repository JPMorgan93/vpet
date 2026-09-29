using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static IEnumerable<Control> Descendants(Control control)
        {foreach(Control child in control.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
        static ToolStripItem Item(ContextMenuStrip menu,string text){return menu.Items.Cast<ToolStripItem>().Single(i=>i.Text==text);}
        static void CaptureForm(Form form,string name)
        {using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(artifacts,name+".png"));}}
        static void ExpandedWindows()
        {
            float saved=.7f,tested=-1;
            using(var sound=new ToySoundWindow(saved,v=>tested=v,v=>saved=v))
            {
                sound.Show();Application.DoEvents();sound.Volume.Value=35;FindButton(sound,"Test sound").PerformClick();
                Check(tested==.35f&&saved==.7f,"Testing volume previews without saving it");
                CaptureForm(sound,"triangle-volume");FindButton(sound,"Cancel").PerformClick();Check(saved==.7f,"Cancel keeps the previous instrument volume");
            }
            using(var sound=new ToySoundWindow(saved,v=>tested=v,v=>saved=v))
            {sound.Show();Application.DoEvents();sound.Volume.Value=0;FindButton(sound,"Save").PerformClick();Check(saved==0,"Save commits a muted instrument volume");}
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();var speed=MakerField<TrackBar>(tweak,"animationSpeed");Check(speed.Value==100,"Tweak starts the selected animation at 1x");
                    FindButton(tweak,"Tweak").PerformClick();speed.Value=150;
                    Check(maker.Project.Speed(0)==1.5f&&!MakerField<bool>(tweak,"tweaking")&&maker.Dirty,"Speed slider previews playback immediately and marks the project unsaved");
                    FindButton(tweak,"Walk Left/Right").PerformClick();Check(speed.Value==100,"Each animation starts with its own independent speed");speed.Value=50;
                    FindButton(tweak,"Idle Up").PerformClick();Check(speed.Value==150&&maker.Project.Speed(7)==.5f,"Switching animation restores its saved speed without changing another type");
                    CaptureForm(tweak,"animation-speed");tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
            Point original=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();
            try
            {
                string root=AppDomain.CurrentDomain.BaseDirectory;
                using(var pet=new PetWindow(Path.Combine(artifacts,"expanded-ui-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"expanded-smoke")))
                {
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();pet.Model.Settings.Movement=MovementMode.Static;
                    using(var marker=new Bitmap(20,20)){using(var graphics=Graphics.FromImage(marker))graphics.Clear(Color.Purple);pet.Present(marker,new Point(pet.Model.Current.Work.Left+5,pet.Model.Current.Work.Top+5));}
                    var windows=pet.Toys;var toys=windows.Model;windows.SetVisible(true);
                    foreach(string toy in new[]{"Coin","Card","D20","Triangle"})Item(windows.Menu,toy).PerformClick();windows.Update();Application.DoEvents();
                    Check(windows.Coin.Visible&&windows.Card.Visible&&windows.Die.Visible,"Chest toggles show all three new native toy windows");
                    Check(windows.Die.Width==(int)Math.Ceiling(46*toys.Scale)&&windows.Die.Height==(int)Math.Ceiling(46*toys.Scale),"Native D20 window returns to its original width and height");
                    Check(windows.Menu.Items[windows.Menu.Items.Count-2].Text=="Help Messages"&&windows.Menu.Items[windows.Menu.Items.Count-1].Text=="Close Toy Chest","New toys retain the help and close footer");
                    Item(windows.TriangleMenu,"Sound Setting").PerformClick();var sound=MakerField<ToySoundWindow>(windows,"soundWindow");
                    Check(sound.Visible&&windows.Busy&&!pet.ShowPause,"Sound Setting opens the volume dialog without the settings pause emote");
                    sound.Volume.Value=42;FindButton(sound,"Test sound").PerformClick();Check(toys.RememberedNotes==0&&toys.Settings.Volume==1,"Test sound does not create a remembered tune or change saved volume");
                    FindButton(sound,"Save").PerformClick();Check(toys.Settings.Volume==.42f&&Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Toys.Volume==.42f,"Saved instrument volume persists through the actual menu");
                    var point=Point.Round(toys.Coin);ToyMouse(windows.Coin,0x201,point);ToyMouse(windows.Coin,0x202,point);Check(toys.Target==PlayTarget.Coin&&pet.Model.Playing,"Native coin click starts a visit");
                    double now=0;Until(toys,pet.Model,ref now,()=>toys.Fetch==FetchPhase.Pausing);
                    typeof(PetWindow).GetMethod("Render",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pet,null);windows.Update();Application.DoEvents();
                    Check(!pet.Bounds.IntersectsWith(windows.Coin.Bounds),"Rendered pet stands beside the coin without covering it");
                    point=Point.Round(toys.Card);ToyMouse(windows.Card,0x201,point);ToyMouse(windows.Card,0x202,point);Until(toys,pet.Model,ref now,()=>toys.WaitingForCardChoice);
                    typeof(PetWindow).GetMethod("Render",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pet,null);windows.Update();Application.DoEvents();
                    Check(Native.WindowFromPoint(new Native.POINT(point.X,point.Y-12))==windows.Card.Handle&&Native.WindowFromPoint(new Native.POINT(point.X,point.Y+12))==windows.Card.Handle,"Both card choices remain directly clickable while the pet stands beside it");
                    var high=new Point(point.X,point.Y-12);ToyMouse(windows.Card,0x201,high);ToyMouse(windows.Card,0x202,high);Check(toys.ChoiceHigh==true&&toys.Fetch==FetchPhase.Flipping,"Top half of the native card chooses High");
                    toys.PressCard(now);Until(toys,pet.Model,ref now,()=>toys.WaitingForCardChoice);var low=new Point(point.X,point.Y+12);ToyMouse(windows.Card,0x201,low);ToyMouse(windows.Card,0x202,low);Check(toys.ChoiceHigh==false,"Bottom half of the native card chooses Low");
                    point=Point.Round(toys.Die);ToyMouse(windows.Die,0x204,point);ToyMouse(windows.Die,0x200,new Point(point.X+50,point.Y-25));Check(windows.Arrow.Visible,"Right-pulling the D20 shows the red launch arrow");ToyMouse(windows.Die,0x205,Cursor.Position);
                    Check(toys.DieVelocity.X<0&&toys.DieVelocity.Y>0&&toys.Fetch==FetchPhase.Watching&&!pet.Model.Destination.HasValue,"Native die release launches opposite the pull without a chase");
                    toys.CancelFetchForPetDrag(now);windows.SetVisible(false);pet.Model.ChangeMode(MovementMode.Restricted,now);windows.Update();Application.DoEvents();
                    Check(windows.Fence.Visible&&!windows.Chest.Visible,"Synced restricted fence works with the toy chest hidden");
                    var oldZone=toys.Zone;point=Point.Round(toys.Center);ToyMouse(windows.Fence,0x201,point);ToyMouse(windows.Fence,0x200,new Point(point.X-25,point.Y-20));ToyMouse(windows.Fence,0x202,Cursor.Position);
                    Check(toys.Zone!=oldZone&&pet.Model.RestrictedArea==toys.Zone,"Hidden-chest shared fence remains movable and drives pet restriction");
                    using(var settings=new SettingsWindow(pet))
                    {
                        settings.Show();Application.DoEvents();var tabs=MakerField<TabControl>(settings,"tabs");Check(tabs.TabCount==4&&tabs.TabPages[3].Text=="Advanced","Settings includes the Advanced tab");
                        foreach(int width in new[]{550,850})
                        {
                            settings.ClientSize=new Size(width,650);
                            for(int tab=0;tab<4;tab++)
                            {
                                settings.SelectTab(tab);Application.DoEvents();var page=tabs.TabPages[tab];
                                Check(!page.HorizontalScroll.Visible,"Settings tab fits horizontally at width "+width+": "+page.Text);
                                if(width==550)CaptureForm(settings,"settings-responsive-"+tab);
                                foreach(var label in Descendants(page).OfType<Label>().Where(l=>l.Visible&&!string.IsNullOrEmpty(l.Text)))
                                {
                                    var needed=TextRenderer.MeasureText(label.Text,label.Font,new Size(Math.Max(1,label.ClientSize.Width),int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);
                                    Check(label.Height+5>=needed.Height,"Settings label has room for all text: "+label.Text.Substring(0,Math.Min(35,label.Text.Length))+" ("+label.Size+" vs "+needed+")");
                                }
                            }
                        }
                        settings.SelectTab(0);Check(Descendants(tabs.TabPages[0]).OfType<TrackBar>().Count()==1,"Movement settings retains walking speed and removes radius controls");
                        Check(!Descendants(tabs.TabPages[2]).Any(c=>c.Text.Contains("Legacy")),"Sprite settings removes legacy sheet instructions");
                        settings.SelectTab(3);var sync=Descendants(settings).OfType<CheckBox>().Single(c=>c.Name=="SyncPlayZone");Check(sync.Checked,"Advanced sync defaults on");sync.Checked=false;
                        Check(!pet.Model.Settings.SyncPlayZone&&pet.Model.RestrictedArea==pet.Model.OwnRestrictedArea,"Advanced switch restores the independent fence");settings.Close();
                    }
                    var overlay=MakerField<RestrictedAreaOverlay>(pet,"restrictedOverlay");overlay.Update();Application.DoEvents();
                    var independent=MakerField<LayeredWindow>(overlay,"fence");var before=pet.Model.OwnRestrictedArea;
                    point=Point.Round(pet.Model.Anchor);ToyMouse(independent,0x201,point);ToyMouse(independent,0x200,new Point(point.X-20,point.Y-15));ToyMouse(independent,0x202,Cursor.Position);
                    Check(pet.Model.OwnRestrictedArea!=before&&toys.Zone!=pet.Model.OwnRestrictedArea,"Independent fence center moves separately from the play zone");
                    before=pet.Model.OwnRestrictedArea;point=new Point((int)before.Right-2,(int)before.Bottom-2);
                    ToyMouse(independent,0x201,point);ToyMouse(independent,0x200,new Point(point.X-40,point.Y-30));ToyMouse(independent,0x202,Cursor.Position);
                    Check(pet.Model.OwnRestrictedArea.Width<before.Width&&pet.Model.OwnRestrictedArea.Height<before.Height&&pet.Model.InsideRestriction(pet.Model.Position),"Independent fence corner resizes both dimensions and keeps the pet inside");
                    ToyUpdateWindowChecks(pet);
                    pet.Close();
                }
            }
            finally{Cursor.Position=original;if(foreground!=IntPtr.Zero)Native.SetForegroundWindow(foreground);}
        }
    }
}
