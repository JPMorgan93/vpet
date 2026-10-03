using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void FindPetFeatures()
        {
            string path=Path.Combine(artifacts,"finder-preferences.json");File.WriteAllText(path,"{}");var prefs=Preferences.Load(path);
            Check(!prefs.FindPet.Enabled&&prefs.FindPet.Modifier==FindPetModifier.Alt&&prefs.FindPet.Key==(int)Keys.Menu,"Existing users get Find My Vpet off with ALT + ALT defaults");
            prefs.FindPet.Enabled=true;prefs.FindPet.Modifier=FindPetModifier.Ctrl;prefs.FindPet.Key=(int)Keys.F24;prefs.Save(path);var loaded=Preferences.Load(path);
            Check(loaded.FindPet.Enabled&&loaded.FindPet.Modifier==FindPetModifier.Ctrl&&loaded.FindPet.Key==(int)Keys.F24,"Find shortcut persists independently of startup preferences");
            prefs.FindPet.Key=0;prefs.Save(path);Check(Preferences.Load(path).FindPet.Key==0,"Cleared mapping stays unmapped across restart");
            var keys=new FindPetKeys();var binding=new FindPetPreferences();
            Check(!keys.Input((int)Keys.LMenu,true,0,binding),"Disabled locator ignores key presses");keys.Reset();binding.Enabled=true;
            Check(!keys.Input((int)Keys.LMenu,true,0,binding)&&!keys.Input((int)Keys.LMenu,true,.1,binding),"Single Alt and auto-repeat do not activate");
            keys.Input((int)Keys.LMenu,false,.12,binding);Check(keys.Input((int)Keys.LMenu,true,.3,binding),"Two separate Alt presses activate the default shortcut");
            Check(!keys.Input((int)Keys.LMenu,true,.32,binding),"Held second tap cannot retrigger");keys.Input((int)Keys.LMenu,false,.35,binding);
            Check(!keys.Input((int)Keys.LMenu,true,.4,binding),"Third tap begins a fresh pair");keys.Input((int)Keys.LMenu,false,.45,binding);
            Check(!keys.Input((int)Keys.LMenu,true,1,binding),"Slow taps do not activate");keys.Input((int)Keys.LMenu,false,1.1,binding);
            keys.Input((int)Keys.K,true,1.2,binding);keys.Input((int)Keys.K,false,1.21,binding);Check(!keys.Input((int)Keys.LMenu,true,1.3,binding),"Intervening typing cancels double-tap detection");
            keys.Reset();keys.Input((int)Keys.LMenu,true,2,binding);Check(!keys.Input((int)Keys.RMenu,true,2.1,binding),"Holding both Alt keys is not a double tap");
            keys.Reset();binding.Modifier=FindPetModifier.Ctrl;binding.Key=(int)Keys.K;
            Check(!keys.Input((int)Keys.K,true,3,binding),"Mapped key alone does not activate");keys.Input((int)Keys.K,false,3.1,binding);
            Check(!keys.Input((int)Keys.RControlKey,true,4,binding)&&keys.Input((int)Keys.K,true,4.1,binding),"Right Ctrl plus the mapped key activates");
            Check(!keys.Input((int)Keys.K,true,4.2,binding),"Chord ignores auto-repeat");keys.Input((int)Keys.K,false,4.3,binding);Check(keys.Input((int)Keys.K,true,4.4,binding),"Releasing and repressing the mapped key can reactivate");
            keys.Reset();keys.Input((int)Keys.ControlKey,true,5,binding);keys.Input((int)Keys.ShiftKey,true,5.1,binding);Check(!keys.Input((int)Keys.K,true,5.2,binding),"Extra modifiers do not accidentally trigger the configured chord");
            keys.Reset();binding.Key=(int)Keys.ControlKey;keys.Input((int)Keys.LControlKey,true,6,binding);keys.Input((int)Keys.LControlKey,false,6.1,binding);Check(keys.Input((int)Keys.RControlKey,true,6.2,binding),"CTRL + CTRL supports two taps across either control key");
            Check(!FindPetKeys.Valid(0)&&!FindPetKeys.Valid(1)&&!FindPetKeys.Valid(999)&&FindPetKeys.Valid((int)Keys.F24),"Only mappable keyboard virtual keys are accepted");
            Check(FindPetKeys.Name((int)Keys.RMenu)=="ALT"&&FindPetKeys.Name((int)Keys.D7)=="7","Mapped key labels are readable");
            Check(FindPetSpotlight.Fade(-1)==1&&FindPetSpotlight.Fade(0)==1&&FindPetSpotlight.Fade(.5)==.5f&&FindPetSpotlight.Fade(1)==0,"Spotlight fades away completely in one second");
            using(var sprite=new Bitmap(40,60))
            {
                using(var g=Graphics.FromImage(sprite))g.Clear(Color.Magenta);
                var first=new Rectangle(-170,70,40,60);var second=new Rectangle(160,140,40,60);
                foreach(var body in new[]{first,second})
                {
                    var screen=new Rectangle(-250,0,600,300);using(var image=FindPetSpotlight.Draw(screen,new[]{new SpotlightFrame(body,sprite)}))
                    {
                        Check(image.GetPixel(0,0).A==170,"Screen outside the circle is dimmed");
                        Check(image.GetPixel(body.Left-screen.Left+20,body.Top+30).ToArgb()==Color.Magenta.ToArgb(),"Locator shows the pet's copy even when its own window is below apps");
                        Check(image.GetPixel(body.Left-screen.Left-8,body.Top+30).A==0,"Clear spotlight follows pet geometry at negative and positive coordinates");
                        if(body==first)image.Save(Path.Combine(artifacts,"find-pet-spotlight.png"));
                    }
                }
                using(var edge=FindPetSpotlight.Draw(new Rectangle(0,0,200,300),new[]{new SpotlightFrame(new Rectangle(-45,70,40,60),sprite)}))
                    Check(edge.GetPixel(3,100).A==0,"Spotlight stays continuous across a monitor boundary even when the pet body is on the other screen");
            }
            var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.SetPlateVisible(true,0);toys.ServeFood(FoodKind.Pudding,0);pet.Place(toys.EatingPosition);
            double now=0;ToyStep(toys,pet,now,.001f);
            Check(toys.Fetch==FetchPhase.Shaking,"Meal begins with a shake");
            for(int i=0;i<25;i++){now+=.005;ToyStep(toys,pet,now,.005f);}
            Check(toys.Fetch==FetchPhase.Waiting&&!pet.Shaking(now)&&toys.FoodRemaining==3,"Shake stops before any food disappears");
            for(int i=0;i<24;i++){now+=.005;ToyStep(toys,pet,now,.005f);}
            Check(toys.FoodRemaining==3&&!pet.Shaking(now),"Food stays visible throughout the eighth-second pause");
            now+=.005;ToyStep(toys,pet,now,.005f);Check(toys.FoodRemaining==2,"A third disappears at the end of the pause");
            byte[] wave=ReminderChime.CreateWave();Check(System.Text.Encoding.ASCII.GetString(wave,0,4)=="RIFF"&&BitConverter.ToInt32(wave,24)==22050&&wave.Length==31796,"Reminder chime is valid three-note PCM audio");
            double previousFrequency=0;
            for(int note=0;note<3;note++)
            {
                int crossings=0,peak=0;bool quiet=true;short previous=0;
                for(int i=0;i<5292;i++)
                {
                    short sample=BitConverter.ToInt16(wave,44+(note*5292+i)*2);
                    if(i<3969){peak=Math.Max(peak,Math.Abs((int)sample));if(previous<=0&&sample>0)crossings++;}else quiet&=sample==0;
                    previous=sample;
                }
                Check(peak>2000&&peak<12000&&quiet&&crossings>previousFrequency,"Reminder note "+note+" is distinct, ascending, and separated by silence");previousFrequency=crossings;
            }
            File.WriteAllBytes(Path.Combine(artifacts,"reminder-three-tone.wav"),wave);
        }
        [DllImport("user32.dll")] static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
        static void FinderKey(Keys key,bool down)
        {keybd_event((byte)key,0,down?0u:2u,UIntPtr.Zero);var clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<25){Application.DoEvents();System.Threading.Thread.Sleep(1);}}
        static void FindPetWindows()
        {
            Point original=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();string root=AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using(var pet=new PetWindow(Path.Combine(artifacts,"finder-ui-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"finder-smoke")))
                using(var focus=new Form{Text="Vpet locator test input",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(40,40,240,140)})
                {
                    try
                    {
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();
                    typeof(PetWindow).GetField("smokeStep",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,99);
                    pet.Model.Settings.Frequency=Frequency.Off;pet.Model.Settings.Movement=MovementMode.Static;Cursor.Position=new Point(2,2);
                    Check(!pet.FindPet.Hooked,"Disabled Find My Vpet installs no keyboard hook");
                    pet.OpenSettings(3);Application.DoEvents();var settings=MakerField<SettingsWindow>(pet,"settingsWindow");var tabs=MakerField<TabControl>(settings,"tabs");
                    var advanced=Descendants(tabs.TabPages[3]).ToArray();
                    Check(advanced.Any(c=>c.Text=="Load Vpet on PC startup")&&advanced.Any(c=>c.Text=="Auto-update on app startup")&&!Descendants(tabs.TabPages[2]).Any(c=>c.Text=="Load Vpet on PC startup"||c.Name=="AutoUpdate"),"Both startup options are on Advanced and removed from Sprite");
                    var autoUpdate=(ComboBox)settings.Controls.Find("AutoUpdate",true).Single();autoUpdate.SelectedIndex=1;
                    Check(pet.Model.Settings.AutoUpdate&&Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).AutoUpdate,"Advanced auto-update selection saves immediately");autoUpdate.SelectedIndex=0;
                    Check(!Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).AutoUpdate,"Advanced auto-update can be switched back off");
                    var enabled=(CheckBox)settings.Controls.Find("FindPetEnabled",true).Single();var modifier=(ComboBox)settings.Controls.Find("FindPetModifier",true).Single();
                    var key=(TextBox)settings.Controls.Find("FindPetKey",true).Single();var status=(Label)settings.Controls.Find("FindPetStatus",true).Single();
                    Check(!enabled.Checked&&modifier.Text=="ALT"&&key.Text=="ALT"&&key.ReadOnly,"Finder settings show the requested default controls");
                    key.Focus();typeof(Control).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(key,new object[]{EventArgs.Empty});Application.DoEvents();
                    Check(key.Text==""&&pet.Model.Settings.FindPet.Key==0&&status.Text.Contains("Press any key")&&pet.FindPet.Capturing,"Click clears the saved mapping and asks for a key");
                    FinderKey(Keys.F24,true);FinderKey(Keys.F24,false);Application.DoEvents();
                    Check(key.Text=="F24"&&pet.Model.Settings.FindPet.Key==(int)Keys.F24&&status.Text.Contains("successfully")&&!pet.FindPet.Capturing,"Native key capture maps one key and confirms success");
                    Check(!pet.FindPet.Hooked,"Disabled feature releases its temporary capture hook");
                    modifier.SelectedIndex=1;enabled.Checked=true;Check(pet.FindPet.Hooked,"Enabling valid mapping installs shortcut observer");
                    var saved=Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json"));Check(saved.FindPet.Enabled&&saved.FindPet.Modifier==FindPetModifier.Ctrl&&saved.FindPet.Key==(int)Keys.F24,"UI changes save the new shortcut");
                    settings.Close();focus.Show();focus.Activate();Application.DoEvents();IntPtr focused=Native.GetForegroundWindow();
                    FinderKey(Keys.ControlKey,true);FinderKey(Keys.F24,true);FinderKey(Keys.F24,false);FinderKey(Keys.ControlKey,false);
                    Check(pet.FindPet.Spotlight.Active,"Global shortcut activates from another application");
                    Check(Native.GetForegroundWindow()==focused,"Global shortcut keeps the other application's keyboard focus");
                    var frame=new SpotlightFrame(pet.PresentedBounds,MakerField<Bitmap>(pet,"rendered"));var screens=Screen.AllScreens.Select(s=>s.Bounds).ToArray();
                    pet.FindPet.Spotlight.Update(pet.Now,new[]{frame},screens);Application.DoEvents();
                    Check(pet.FindPet.Spotlight.Windows.Count(w=>w.Visible)==screens.Length,"Spotlight dims every connected display");
                    foreach(var window in pet.FindPet.Spotlight.Windows)
                    {
                        long styles=Native.GetWindowLongPtr(window.Handle,-20).ToInt64();
                        Check((styles&0x08000028)==0x08000028,"Spotlight window is topmost, click-through, and nonactivating");
                    }
                    Check(Native.GetForegroundWindow()==focused,"Painting the spotlight does not activate a Vpet window");
                    double moment=pet.Now;pet.FindPet.Spotlight.Trigger(moment);pet.FindPet.Spotlight.Update(moment+1.01,new[]{frame},screens);
                    Check(!pet.FindPet.Spotlight.Active&&pet.FindPet.Spotlight.Windows.All(w=>!w.Visible),"Spotlight windows disappear after the one-second fade");
                    pet.Model.Settings.FindPet.Key=(int)Keys.Menu;pet.Model.Settings.FindPet.Modifier=FindPetModifier.Alt;pet.FindPet.ApplySettings();
                    // Queue both taps before pumping: Alt can enter another window's modal menu loop.
                    keybd_event((byte)Keys.Menu,0,0,UIntPtr.Zero);keybd_event((byte)Keys.Menu,0,2,UIntPtr.Zero);
                    keybd_event((byte)Keys.Menu,0,0,UIntPtr.Zero);FinderKey(Keys.Menu,false);
                    Check(pet.FindPet.Spotlight.Active,"Native double-tap Alt activates the default binding");
                    pet.Model.Settings.FindPet.Enabled=false;pet.FindPet.ApplySettings();Check(!pet.FindPet.Hooked&&!pet.FindPet.Spotlight.Active,"Turning feature off removes hook and spotlight");
                    FinderDieHover(pet);
                    pet.Close();Check(!pet.FindPet.Hooked&&!pet.FindPet.Spotlight.Windows.Any(),"Closing pet removes global input and overlay windows");
                    }
                    finally{if(!pet.IsDisposed)pet.Close();}
                }
            }
            finally{FinderKey(Keys.F24,false);FinderKey(Keys.ControlKey,false);FinderKey(Keys.Menu,false);Cursor.Position=original;Native.SetForegroundWindow(foreground);}
        }
        static void FinderDieHover(PetWindow pet)
        {
            var windows=pet.Toys;var toys=windows.Model;pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();windows.SetVisible(true);toys.SpawnDie();windows.Update();
            pet.Model.Settings.Movement=MovementMode.FreeRoam;pet.Model.IdleUntil=0;pet.Model.Place(new PointF(toys.Zone.Left+60,toys.Zone.Bottom-10));
            pet.Model.SetDestination(new PointF(pet.Model.Position.X+100,pet.Model.Position.Y),pet.Model.CurrentDisplay);var position=pet.Model.Position;
            Cursor.Position=Point.Round(toys.Die);PetFrame(pet);
            Check(toys.InspectingDie&&pet.Model.Position==position&&!pet.Model.Walking&&pet.Model.Facing==Geometry.Direction(new PointF(toys.Die.X-position.X,toys.Die.Y-position.Y),2),"D20 hover stops walking and faces the die");
            var announcement=windows.CurrentAnnouncement;
            Check(announcement!=null&&announcement.Value==toys.DieValue&&MakerField<LayeredWindow>(pet,"bubble").Visible&&!pet.ShowPause,"D20 hover shows its number without the Settings pause emote");
            Cursor.Position=new Point(pet.Model.Current.Work.Left+1,pet.Model.Current.Work.Top+1);PetFrame(pet);
            Check(!toys.InspectingDie&&pet.Model.Position!=position&&pet.Model.Walking,"Leaving die hover resumes the preserved walk");
            toys.SetPlateVisible(true,pet.Now);toys.ServeFood(FoodKind.Pudding,pet.Now);pet.Model.Place(toys.EatingPosition);Cursor.Position=Point.Round(toys.Die);
            PetFrame(pet);var phase=toys.Fetch;int portions=toys.FoodRemaining;for(int i=0;i<12;i++)PetFrame(pet);
            Check(toys.FoodRemaining==portions&&toys.Fetch==phase&&!pet.Model.Walking,"D20 hover also pauses feeding without consuming food");
            Cursor.Position=new Point(2,2);PetFrame(pet);Check(!toys.InspectingDie&&toys.Fetch==FetchPhase.Shaking,"Leaving die hover resumes the meal");
        }
    }
}
