using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ReminderWindows()
        {
            var now=new DateTime(2026,9,29,8,0,0);string file=Path.Combine(artifacts,"reminder-ui-"+Guid.NewGuid().ToString("N"),"reminders.json");var store=new ReminderStore(file);
            using(var window=new ReminderWindow(store))
            {
                window.LocalNow=()=>now;window.Show();Application.DoEvents();FindButton(window,"Add Reminder").PerformClick();Application.DoEvents();
                Check(window.Text=="Reminder Window"&&MakerField<GroupBox>(window,"editor").Visible,"Add Reminder opens inline editor in Reminder Window");
                Check(MakerField<CheckBox>(window,"active").Checked,"New reminders default active");
                Check(MakerField<ComboBox>(window,"period").Items.Cast<string>().SequenceEqual(new[]{"AM","PM"}),"Time always has explicit AM and PM options");
                var text=MakerField<RichTextBox>(window,"message");text.Text="A reminder with https://example.com and up to 200 characters.";
                MakerField<NumericUpDown>(window,"hour").Value=12;MakerField<NumericUpDown>(window,"minute").Value=0;MakerField<ComboBox>(window,"period").SelectedIndex=1;
                FindButton(window,"Save").PerformClick();Application.DoEvents();Check(store.Items.Single().MinuteOfDay==720&&!MakerField<GroupBox>(window,"editor").Visible,"12 PM saves as noon and collapses the editor");
                Check(MakerField<TableLayoutPanel>(window,"list").Controls.Count==1&&Descendants(MakerField<TableLayoutPanel>(window,"list")).OfType<RichTextBox>().All(t=>t.ReadOnly&&t.DetectUrls),"Saved reminder list is locked and detects hyperlinks");
                FindButton(window,"Edit").PerformClick();Application.DoEvents();Check(text.Text==store.Items.Single().Message,"Edit restores the saved reminder above the list");
                var kind=MakerField<ComboBox>(window,"kind");kind.SelectedIndex=1;var days=MakerField<CheckBox[]>(window,"days");days[0].Checked=true;days[2].Checked=true;days[4].Checked=true;Application.DoEvents();
                var frequencies=MakerField<ComboBox[]>(window,"frequencies");Check(frequencies[0].Visible&&frequencies[2].Visible&&frequencies[4].Visible&&frequencies[1].Parent==null,"Each selected day has its own visible frequency picker");
                Check(days.Select(d=>d.Text).SequenceEqual(new[]{"Sun","Mon","Tue","Wed","Thu","Fri","Sat"})&&frequencies[2].SelectedIndex==0,"Days start Sunday and selected days default to Every");
                frequencies[0].SelectedIndex=1;frequencies[2].SelectedIndex=2;frequencies[4].SelectedIndex=5;MakerField<NumericUpDown>(window,"hour").Value=12;MakerField<ComboBox>(window,"period").SelectedIndex=0;
                text.Text=new string('a',199)+"b";Check(text.MaxLength==200&&MakerField<Label>(window,"count").Text.StartsWith("200"),"Editor exposes 200-character limit and count");
                CaptureForm(window,"reminder-recurring-editor");
                foreach(int width in new[]{500,850})
                {
                    window.ClientSize=new Size(width,620);Application.DoEvents();
                    Check(!MakerField<Panel>(window,"scroll").HorizontalScroll.Visible,"Reminder editor fits without horizontal scrolling at "+width);
                    foreach(var control in Descendants(window).Where(c=>c.Visible&&(c is ComboBox||c is NumericUpDown||c is Button)))
                    {
                        var bounds=new Rectangle(window.PointToClient(control.PointToScreen(Point.Empty)),control.Size);
                        Check(bounds.Left>=0&&bounds.Right<=window.ClientSize.Width,"Reminder control fits window width: "+control.Text);
                    }
                }
                FindButton(window,"Save").PerformClick();Check(store.Items.Count()==1&&store.Items.Single().MinuteOfDay==0&&store.Items.Single().Days.Count==3,"Recurring edit replaces the existing reminder and 12 AM saves as midnight");
                FindButton(window,"Edit").PerformClick();Check(frequencies[4].SelectedIndex==5,"Edit restores each day's independent frequency");FindButton(window,"Cancel").PerformClick();
                FindButton(window,"Add Reminder").PerformClick();text.Text="";FindButton(window,"Save").PerformClick();Check(store.Items.Count()==1&&MakerField<Label>(window,"error").Text.Length>0,"Invalid reminders keep their draft and show an inline error");FindButton(window,"Delete").PerformClick();Check(store.Items.Count()==1,"Deleting an unsaved draft does not delete a saved reminder");
                FindButton(window,"Edit").PerformClick();FindButton(window,"Delete").PerformClick();Check(!store.Items.Any(),"Delete permanently removes the edited reminder");window.Close();
            }
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();var split=MakerField<SplitContainer>(maker,"editorSplit");var viewport=MakerField<SpriteSheetViewport>(maker,"viewport");var sheet=MakerField<SpriteSheetView>(maker,"sheet");
                float zoom=sheet.Zoom;var draft=sheet.Draft;int height=viewport.Height;split.SplitterDistance-=30;Application.DoEvents();
                Check(viewport.Height<height&&sheet.Zoom==zoom&&sheet.Draft==draft,"Internal splitter resizes preview without changing zoom or selection");
                var workspace=MakerField<SplitContainer>(maker,"workspaceSplit");height=viewport.Height;workspace.SplitterDistance-=60;Application.DoEvents();Check(viewport.Height>height&&sheet.Zoom==zoom,"Upper divider gives the edit canvas more room without changing zoom");
                maker.ClientSize=new Size(800,650);Application.DoEvents();Check(viewport.Height>=split.Panel1MinSize,"Preview retains usable minimum at smallest maker size");
                foreach(string caption in new[]{"How to Guide","Tweak and Complete"}){var button=FindButton(maker,caption);Check(maker.ClientRectangle.Contains(new Rectangle(maker.PointToClient(button.PointToScreen(Point.Empty)),button.Size)),"Maker footer stays fully visible: "+caption);}
                CaptureForm(maker,"maker-resizable-preview");maker.Dirty=false;maker.Close();
            }
            Point original=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();
            try
            {
                string root=AppDomain.CurrentDomain.BaseDirectory;
                using(var pet=new PetWindow(Path.Combine(artifacts,"reminder-pet-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"reminder-smoke")))
                {
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();typeof(PetWindow).GetField("smokeStep",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,100);
                    var work=pet.Model.Current.Work;pet.Model.Place(new PointF(work.Left+100,work.Top+200));pet.Model.Settings.Movement=MovementMode.FreeRoam;pet.Model.Settings.Speed=50;
                    var toys=pet.Toys;toys.Model.MoveZone(new PointF(work.Left+work.Width/2,work.Top+work.Height/2));toys.SetVisible(true);Application.DoEvents();
                    var cleanup=Item(toys.Menu,"Clean Up Toys");Check(!cleanup.Enabled,"Cleanup starts greyed out with no toys");
                    foreach(string name in new[]{"Ball","Triangle","Coin","Card","D20"})
                    {Item(toys.Menu,name).PerformClick();Check(cleanup.Enabled,"Each toy enables Cleanup: "+name);cleanup.PerformClick();Check(!cleanup.Enabled,"Cleanup greys itself out again: "+name);}
                    var menu=MakerField<ContextMenuStrip>(pet,"menu");int remindersIndex=menu.Items.IndexOf(Item(menu,"Reminders"));Check(menu.Items[remindersIndex-1] is ToolStripSeparator&&menu.Items[remindersIndex-2].Text=="Settings…"&&menu.Items[remindersIndex+1] is ToolStripSeparator&&menu.Items[remindersIndex+2].Text=="Check for Updates…","Reminders is separated from both Settings and the update check");
                    Item(menu,"Reminders").PerformClick();Application.DoEvents();Check(MakerField<ReminderWindow>(pet,"reminderWindow").Visible&&!pet.ShowPause,"Menu opens Reminder Window without settings pause symbol");MakerField<ReminderWindow>(pet,"reminderWindow").Close();
                    foreach(string name in new[]{"Ball","Triangle","Coin","Card","D20"}){var item=(ToolStripMenuItem)Item(toys.Menu,name);item.Checked=false;item.PerformClick();}
                    foreach(var window in new[]{toys.Chest,toys.Ball,toys.Triangle,toys.Coin,toys.Card,toys.Die})
                    {
                        var point=new Point(window.Left+window.Width/2,window.Top+window.Height/2);ToyMouse(window,0x201,point);ToyMouse(window,0x200,new Point(point.X+7,point.Y+3));AssertPetWalks(pet,"dragging "+window.Text);ToyMouse(window,0x202,Cursor.Position);
                    }
                    toys.Menu.Show(toys.Chest,new Point(3,3));Application.DoEvents();AssertPetWalks(pet,"toy chest menu");toys.Menu.Close();
                    toys.TriangleMenu.Show(toys.Triangle,new Point(3,3));Application.DoEvents();AssertPetWalks(pet,"triangle menu");toys.TriangleMenu.Close();
                    Item(toys.TriangleMenu,"Sound Setting").PerformClick();Application.DoEvents();AssertPetWalks(pet,"triangle sound settings");MakerField<ToySoundWindow>(toys,"soundWindow").Close();Application.DoEvents();
                    pet.ReminderNow=()=>now.AddMinutes(5);var r=Reminder.New(now);r.Message="A reminder with a link: https://example.com. Dismiss when finished.";pet.Reminders.Save(r,now);
                    using(var focus=new Form{Text="Reminder focus test",Size=new Size(220,120),StartPosition=FormStartPosition.Manual,Location=new Point(work.Right-230,work.Bottom-130)})
                    {
                    focus.Show();focus.Activate();Application.DoEvents();IntPtr before=Native.GetForegroundWindow();pet.CheckReminders();typeof(PetWindow).GetMethod("PositionReminder",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);Application.DoEvents();
                    var bubble=MakerField<ReminderBubble>(pet,"reminderBubble");Check(bubble.Visible&&bubble.Controls.OfType<RichTextBox>().Single().Text==r.Message,"Due reminder displays in its own clickable speech bubble");
                    Check(Native.GetForegroundWindow()==before,"Reminder popup does not steal keyboard focus");
                    Check(!bubble.Bounds.IntersectsWith(OnScreen(pet))&&pet.Model.Current.Work.Contains(bubble.Bounds),"Reminder sits beside pet within the screen");CaptureForm(bubble,"reminder-bubble");
                    pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();typeof(PetWindow).GetMethod("PositionReminder",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);Application.DoEvents();
                    using(var capture=new Bitmap(bubble.Width,bubble.Height)){using(var g=Graphics.FromImage(capture))g.CopyFromScreen(bubble.Location,Point.Empty,bubble.Size);capture.Save(Path.Combine(artifacts,"reminder-bubble-screen.png"));}
                    foreach(float scale in new[]{1f,1.5f,2f})
                    {
                        var longReminder=r.Copy();longReminder.Message=new string('W',200);bubble.Display(longReminder);bubble.Place(pet.Bounds,Rectangle.Empty,work,LayerMode.OverEverything,scale,pet);Application.DoEvents();
                        var body=bubble.Controls.OfType<RichTextBox>().Single();var end=body.GetPositionFromCharIndex(body.TextLength-1);
                        Check(end.Y+body.Font.Height<=body.ClientSize.Height,"All 200 message characters fit the reminder bubble at scale "+scale);
                    }
                    FindButton(bubble,"Dismiss").PerformClick();Check(!bubble.Visible&&pet.Reminders.Pending==null,"Dismiss closes and acknowledges only the due reminder");pet.CheckReminders();Check(!bubble.Visible,"Dismissed one-time alert stays closed");focus.Close();
                    }
                    pet.Close();
                }
            }
            finally{Cursor.Position=original;if(foreground!=IntPtr.Zero)Native.SetForegroundWindow(foreground);}
        }
        static void AssertPetWalks(PetWindow pet,string context)
        {
            pet.Model.IdleUntil=0;pet.Model.Hovered=false;pet.Model.SetDestination(new PointF(pet.Model.Current.Work.Right-100,pet.Model.Position.Y),pet.Model.CurrentDisplay);
            var before=pet.Model.Position;typeof(PetWindow).GetField("previousTime",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,pet.Now-.1);
            typeof(PetWindow).GetMethod("Tick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pet,new object[]{null,EventArgs.Empty});
            Check(!pet.Model.Paused&&pet.Model.Position!=before&&pet.Model.Walking,"Pet keeps moving while "+context);
        }
    }
}
