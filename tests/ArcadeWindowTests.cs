using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Timer=System.Windows.Forms.Timer;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ArcadeMusicTests()
        {
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference");
            using(var music=new ArcadeMusic())
            {
                foreach(string song in new[]{"Easy","Normal","Hard"})
                {
                    music.Open(Path.Combine(root,song+".mp3"),0);Check(music.Opened&&music.Duration>10,"Bundled "+song+" MP3 opens and has a valid duration");
                    foreach(int value in new[]{100,70,53,50,10,1,0})
                    {music.SetVolume(value);Check(Math.Abs(music.PlaybackAttenuation-ArcadeMusic.Attenuation(value))<=1,"Actual "+song+" renderer gain follows slider "+value+"% before playback");}
                    music.Play();Thread.Sleep(120);Check(music.Position>0&&music.PlaybackAttenuation==-10000,"Bundled "+song+" advances while preserving true mute on playback");
                    music.Stop();double stopped=music.Position;Thread.Sleep(40);Check(Math.Abs(music.Position-stopped)<.01,"Stop holds the "+song+" playback clock");
                    music.Play();Thread.Sleep(30);Check(music.Position<stopped&&music.PlaybackAttenuation==-10000,"Replay seeks to the beginning and preserves mute for "+song);
                    music.Stop();music.Close();Check(!music.Opened,"Song device closes after "+song);
                }
            }
            using(var tones=new ArcadeTones(0))
            {foreach(ArcadeLane lane in Enum.GetValues(typeof(ArcadeLane))){tones.Play(lane);Thread.Sleep(25);}tones.Stop();Check(true,"All four generated Simon tones dispatch and stop while muted");}
            string silence=Path.Combine(artifacts,"arcade-volume-silence.wav");
            using(var writer=new BinaryWriter(File.Create(silence)))
            {
                const int rate=22050,bytes=rate*2*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(bytes+36);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);writer.Write(new byte[bytes]);
            }
            using(var music=new ArcadeMusic())
            {
                music.Open(silence,0);music.Play();
                foreach(int value in new[]{1,5,10,50,100,0})
                {music.SetVolume(value);Check(music.PlaybackAttenuation==ArcadeMusic.Attenuation(value),"Live renderer updates to "+value+"% while playing silent PCM");}
                music.Stop();music.Close();Check(!music.Opened,"Live volume probe releases its playback graph");
                bool failed=false;try{music.Open(Path.Combine(artifacts,"missing-arcade-song.mp3"),0);}catch(IOException){failed=true;}
                Check(failed&&!music.Opened,"A missing song leaves no open playback graph");
                music.Open(silence,0);Check(music.Opened&&music.PlaybackAttenuation==-10000,"Playback can reopen safely after a failed song load");
            }
        }
        static void ArcadeWindows()
        {
            ArcadeMusicTests();PreparedAudioTests();Point cursor=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();string root=AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using(var pet=new PetWindow(Path.Combine(artifacts,"arcade-ui-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"arcade-smoke")))
                {
                    try
                    {
                        pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();typeof(PetWindow).GetField("smokeStep",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,99);
                        pet.Model.Settings.Movement=MovementMode.Static;pet.Model.Settings.Frequency=Frequency.Off;pet.Model.Settings.Arcade.Volume=0;
                        var menu=MakerField<ContextMenuStrip>(pet,"menu");var items=(ToolStripMenuItem)Item(menu,"Display Items");var joystick=(ToolStripMenuItem)items.DropDownItems.Cast<ToolStripItem>().Single(i=>i.Text=="Joystick");
                        joystick.PerformClick();Check(pet.Joystick.Visible&&pet.Toys.Model.HasJoystick,"Display Items > Joystick shows the independent desktop item");
                        var work=pet.Model.Current.Work;pet.Toys.Model.DragJoystick(new PointF(work.Left+work.Width/2,work.Top+work.Height/2));pet.Joystick.UpdateJoystick();
                        Point point=Point.Round(pet.Toys.Model.JoystickPosition);var old=pet.Toys.Model.JoystickPosition;
                        ToyMouse(pet.Joystick,0x201,point);ToyMouse(pet.Joystick,0x200,new Point(point.X+20,point.Y+15));ToyMouse(pet.Joystick,0x202,Cursor.Position);
                        Check(pet.Toys.Model.JoystickPosition!=old&&pet.Toys.Model.Fetch==FetchPhase.None,"Joystick drag moves the item without starting the arcade visit");
                        point=Point.Round(pet.Toys.Model.JoystickPosition);ToyMouse(pet.Joystick,0x201,point);ToyMouse(pet.Joystick,0x202,point);Cursor.Position=new Point(work.Left+1,work.Top+1);
                        Check(pet.Toys.Model.Target==PlayTarget.Joystick&&!pet.ArcadeOpen,"Joystick click begins walking before the arcade opens");
                        pet.Model.Place(pet.Toys.Model.JoystickApproach);PetFrame(pet);Application.DoEvents();
                        Check(pet.ArcadeOpen&&!pet.Visible&&pet.arcadeWindow.Visible&&pet.Toys.Model.Fetch==FetchPhase.Arcade,"Arrival opens Arcade Window and hides desktop pet");
                        var arcade=pet.arcadeWindow;MakerField<Timer>(arcade,"timer").Stop();var canvas=MakerField<DoubleBufferedPanel>(arcade,"canvas");
                        var tonesPlayed=new System.Collections.Generic.List<ArcadeLane>();arcade.PlayTone=tonesPlayed.Add;
                        ArcadeLayerChecks(pet);
                        CaptureForm(arcade,"arcade-lobby");Check(arcade.Game==ArcadeGame.Lobby&&arcade.Text=="Arcade Window","Arcade opens to the five-cabinet lobby");
                        var left=MakerField<double>(arcade,"lobbyX");double now=arcade.Now;arcade.Time=()=>now;now+=.1;arcade.Step();Check(MakerField<double>(arcade,"lobbyX")!=left,"Lobby pet walks in front of cabinets");
                        ClickCabinet(arcade,4);Check(arcade.Game==ArcadeGame.Lobby,"Grey cabinet does not launch a game");
                        ClickCabinet(arcade,0);Check(arcade.Game==ArcadeGame.Dance&&arcade.Difficulty==ArcadeDifficulty.Easy&&arcade.Dance!=null,"Dance Time cabinet opens Easy with its bundled song");
                        var score=MakerField<Button>(arcade,"score");var volume=MakerField<TrackBar>(arcade,"volume");Check(score.Text=="- -"&&FindButton(arcade,"Start").Visible&&volume.Visible,"Dance opens with blank scoreboard, Start, and visible volume slider");score.PerformClick();Check(score.Text=="High: 0","Score box shows the selected difficulty's zero default high score");
                        var player=MakerField<ArcadeMusic>(arcade,"music");var volumeText=MakerField<Label>(arcade,"volumeValue");
                        volume.Value=100;Check(player.PlaybackAttenuation==0&&volumeText.Text=="100%","Full slider value reaches normal device-controlled gain and displays 100%");
                        volume.Value=5;Check(player.PlaybackAttenuation==ArcadeMusic.Attenuation(5)&&volumeText.Text=="5%","A nearly lowered slider directly attenuates the music renderer");
                        volume.Value=0;Check(player.PlaybackAttenuation==-10000&&volumeText.Text=="0%","Bottom slider value is true mute before starting");
                        var modes=MakerField<RadioButton[]>(arcade,"modes");modes[1].Checked=true;WaitForDance(arcade);Check(arcade.Difficulty==ArcadeDifficulty.Normal&&arcade.Dance.Lives==2&&modes.Count(m=>m.Checked)==1,"Difficulty controls select only Normal and update song/model");
                        FindButton(arcade,"Start").PerformClick();Check(arcade.Dance.State==DanceState.Countdown&&FindButton(arcade,"Stop").Visible&&volume.Visible&&!modes.Any(m=>m.Enabled),"Start becomes Stop during countdown with visible volume and locked difficulty");
                        FindButton(arcade,"Stop").PerformClick();now+=3.1;arcade.Step();Check(arcade.Dance.State==DanceState.Stopped&&FindButton(arcade,"Start").Visible&&score.Text=="- -"&&volume.Visible,"Stop cancels Dance countdown and restores Start without scoring");
                        FindButton(arcade,"Start").PerformClick();now+=3.01;arcade.Step();Check(arcade.Dance.State==DanceState.Running&&volume.Visible&&volume.Orientation==Orientation.Vertical,"Song starts after countdown with vertical music volume");
                        // Send native key messages through the form, including an auto-repeat and release.
                        Native.SendMessage(arcade.Handle,0x100,new IntPtr((int)Keys.A),new IntPtr(1));Check(arcade.Dance.Misses==1,"Keyboard input outside a target overlap counts a miss");Native.SendMessage(arcade.Handle,0x100,new IntPtr((int)Keys.A),new IntPtr(0x40000001));Check(arcade.Dance.Misses==1,"Holding a mapped key does not generate repeat misses");Native.SendMessage(arcade.Handle,0x101,new IntPtr((int)Keys.A),IntPtr.Zero);
                        MakerField<Button>(arcade,"switchKeys").PerformClick();Check(pet.Model.Settings.Arcade.ArrowKeys,"Key switch changes WASD to arrows and persists preference");
                        Check(!ArcadeKey(arcade,Keys.W),"Old letter mapping passes through after switching to arrows");ArcadeKey(arcade,Keys.Left);ArcadeKeyUp(arcade,Keys.Left);
                        Check(arcade.Dance.State==DanceState.Failed&&score.Text=="- -"&&FindButton(arcade,"Start").Visible,"Last miss returns Start, discards score, and stops the song");CaptureForm(arcade,"dance-failed");
                        modes[0].Checked=true;double songTime=0;arcade.SongPosition=()=>songTime;FindButton(arcade,"Start").PerformClick();now+=3.01;arcade.Step();
                        Keys[] arrows={Keys.Up,Keys.Down,Keys.Left,Keys.Right};int noteCount=0;
                        foreach(var target in arcade.Dance.Chart)
                        {
                            songTime=target.HitTime;now+=.1;ArcadeKey(arcade,arrows[(int)target.Lane]);ArcadeKeyUp(arcade,arrows[(int)target.Lane]);
                            if(++noteCount==6)
                            {
                                Check(MakerField<Label>(arcade,"calculated").Text=="Calculated: 60\n1.3x   Streak: 120","Score display separates calculated and active streak points with the multiplier suffix");
                                arcade.ClientSize=new Size(700,590);Application.DoEvents();var pending=MakerField<Label>(arcade,"calculated");
                                Check(pending.Parent.ClientRectangle.Contains(pending.Bounds),"Both calculated and streak score lines fit at the minimum window size");CaptureForm(arcade,"dance-minimum-streak");arcade.ClientSize=new Size(1040,790);Application.DoEvents();
                                CaptureForm(arcade,"dance-streak-pulse");Check(arcade.Dance.PulsingTargets(now).Any(),"A scored target remains visible for its pulse");
                                using(var bright=ArcadeRegion(arcade,new Rectangle(650,110,280,35)))
                                {
                                    now+=.34;arcade.Step();using(var flash=ArcadeRegion(arcade,new Rectangle(650,110,280,35)))
                                        Check(ArcadeDifferentPixels(bright,flash)>50,"Bold Streak Combo text visibly flashes at the top right");
                                }
                            }
                        }
                        songTime=arcade.Dance.Duration;now+=.1;arcade.Step();
                        Check(arcade.Dance.State==DanceState.Success&&arcade.Dance.Banked>0&&score.Text==arcade.Dance.Banked.ToString(),"Successful Dance song banks its streak-adjusted score in the UI");
                        Check(Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.High(ArcadeGame.Dance,ArcadeDifficulty.Easy)==arcade.Dance.Banked,"Successful Dance high score persists independently");CaptureForm(arcade,"dance-success");
                        long high=arcade.Dance.Banked;songTime=0;FindButton(arcade,"Start").PerformClick();now+=3.01;arcade.Step();songTime=arcade.Dance.Chart[0].HitTime;ArcadeKey(arcade,arrows[(int)arcade.Dance.Chart[0].Lane]);ArcadeKeyUp(arcade,arrows[(int)arcade.Dance.Chart[0].Lane]);
                        FindButton(arcade,"Stop").PerformClick();now+=10;songTime=arcade.Dance.Duration;arcade.Step();Check(arcade.Dance.State==DanceState.Stopped&&arcade.Dance.Pending==0&&score.Text=="- -"&&pet.Model.Settings.Arcade.High(ArcadeGame.Dance,ArcadeDifficulty.Easy)==high,"Stopping a scored Dance round preserves the previous high score and prevents later scoring");
                        PracticeWindows(pet,arcade,ref now,ref songTime,arrows);
                        ArcadeFloorWindows(pet,arcade,ref now);
                        ArcadeLayoutWindows(arcade,ref now);
                        FindButton(arcade,"Close Game").PerformClick();Check(arcade.Game==ArcadeGame.Lobby&&pet.ArcadeOpen&&!pet.Visible,"Close Game returns to lobby while desktop pet stays hidden");
                        ClickCabinet(arcade,1);Check(arcade.Game==ArcadeGame.Simon&&!volume.Visible,"Simon Says cabinet opens without music/volume controls");
                        FindButton(arcade,"Start").PerformClick();Check(FindButton(arcade,"Stop").Visible,"Simon Start also becomes Stop");FindButton(arcade,"Stop").PerformClick();now+=3.01;arcade.Step();Check(arcade.Simon.State==SimonState.Stopped&&tonesPlayed.Count==0,"Simon Stop during countdown prevents the first tone and sequence");
                        FindButton(arcade,"Start").PerformClick();now+=3.01;arcade.Step();Check(arcade.Simon.State==SimonState.Showing&&arcade.Simon.Sequence.Count==1&&tonesPlayed.Last()==arcade.Simon.Sequence[0],"Simon begins by highlighting one color with its tone");CaptureForm(arcade,"simon-watch");
                        now+=arcade.Simon.ShowStep+.01;arcade.Step();ArcadeLane lane=arcade.Simon.Sequence[0];ArcadeKey(arcade,arrows[(int)lane]);ArcadeKeyUp(arcade,arrows[(int)lane]);
                        Check(arcade.Simon.Pending==50&&arcade.Simon.Sequence.Count==1&&arcade.Simon.State==SimonState.Waiting,"Correct mapped key adds 50 and waits before the next sequence");
                        int beforeTone=tonesPlayed.Count;Check(beforeTone==2&&tonesPlayed[0]==lane&&tonesPlayed[1]==lane&&arcade.Simon.Lit(now)==lane,"Final player input highlights and plays the exact same tone as the pet's cue");CaptureForm(arcade,"simon-final-input");
                        now+=.18;arcade.Step();Check(!arcade.Simon.Lit(now).HasValue&&MakerField<int>(arcade,"facing")==2,"After its final input highlight, the pet faces down for the rest");CaptureForm(arcade,"simon-rest");
                        now+=.999;arcade.Step();Check(tonesPlayed.Count==beforeTone&&arcade.Simon.State==SimonState.Waiting,"Native preview remains quiet for the full one-second rest");now+=.001;arcade.Step();Check(arcade.Simon.Sequence.Count==2&&tonesPlayed.Count==beforeTone+1,"Native preview starts its next sequence and tone after the rest");
                        now+=2*arcade.Simon.ShowStep+.01;arcade.Step();Keys wrong=arrows[((int)arcade.Simon.Sequence[0]+1)%4];ArcadeKey(arcade,wrong);ArcadeKeyUp(arcade,wrong);
                        Check(arcade.Simon.State==SimonState.Finished&&score.Text=="50"&&FindButton(arcade,"Start").Visible,"Simon mistake banks completed rounds and offers replay");
                        Check(Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.High(ArcadeGame.Simon,ArcadeDifficulty.Easy)==50,"Completed Simon high score persists to the pet's settings");
                        CaptureForm(arcade,"simon-complete");FindButton(arcade,"Start").PerformClick();now+=3.01;arcade.Step();now+=arcade.Simon.ShowStep+.01;arcade.Step();ArcadeKey(arcade,arrows[(int)arcade.Simon.Sequence[0]]);ArcadeKeyUp(arcade,arrows[(int)arcade.Simon.Sequence[0]]);
                        FindButton(arcade,"Stop").PerformClick();beforeTone=tonesPlayed.Count;now+=10;arcade.Step();Check(arcade.Simon.State==SimonState.Stopped&&FindButton(arcade,"Start").Visible&&tonesPlayed.Count==beforeTone&&score.Text=="- -"&&pet.Model.Settings.Arcade.High(ArcadeGame.Simon,ArcadeDifficulty.Easy)==50,"Stopping Simon during the wait cancels pending scoring and tones while preserving high scores");
                        foreach(var size in new[]{new Size(700,590),new Size(1150,830)})
                        {
                            arcade.ClientSize=size;Application.DoEvents();var footer=FindButton(arcade,"Close Game");Check(arcade.ClientRectangle.Contains(new Rectangle(arcade.PointToClient(footer.PointToScreen(Point.Empty)),footer.Size)),"Close Game remains accessible at "+size);
                            Check(canvas.ClientRectangle.Contains(new Rectangle(MakerField<Button>(arcade,"switchKeys").Location,MakerField<Button>(arcade,"switchKeys").Size)),"Key switch remains inside resized game canvas");
                            var pending=MakerField<Label>(arcade,"calculated");Check(pending.Parent.ClientRectangle.Contains(pending.Bounds),"Calculated score text fits the header at "+size);
                        }
                        CaptureForm(arcade,"simon-stopped");pet.Toys.Model.DragJoystick(new PointF(old.X-45,old.Y+20));
                        pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();pet.Toys.SetVisible(true);pet.Toys.Model.SpawnBall(pet.Now);pet.Toys.Model.SpawnTriangle();pet.Toys.Model.SpawnCoin();pet.Toys.Model.SpawnCard();pet.Toys.Model.SpawnDie();pet.Plate.SetVisible(true);PetFrame(pet);
                        var desktopAssets=pet.Toys.Windows.Where(w=>w.Visible).Concat(new LayeredWindow[]{pet.Plate,pet.Joystick}).ToArray();
                        foreach(var asset in desktopAssets)Check((Native.GetWindowLongPtr(asset.Handle,-20).ToInt64()&8)==0,"Arcade temporarily keeps Over Everything asset in the normal band: "+asset.Text);
                        arcade.Close();PetFrame(pet);
                        foreach(var asset in desktopAssets)Check((Native.GetWindowLongPtr(asset.Handle,-20).ToInt64()&8)!=0,"Closing Arcade restores Over Everything for "+asset.Text);
                        Check(!pet.ArcadeOpen&&pet.Visible&&Native.ArcadeForeground==IntPtr.Zero&&Geometry.Distance(pet.Model.Position,pet.Toys.Model.JoystickApproach)<1&&!pet.Model.Playing,"Closing Arcade removes the stacking lock and restores pet behind relocated joystick in Static mode");
                        pet.Toys.Model.PressJoystick(pet.Now);pet.Model.Place(pet.Toys.Model.JoystickApproach);PetFrame(pet);Check(pet.ArcadeOpen,"Joystick can reopen Arcade after closing");Item(pet.Joystick.Menu,"Remove Joystick").PerformClick();Check(!pet.ArcadeOpen&&pet.Visible&&!pet.Joystick.Visible,"Removing joystick closes Arcade and restores desktop pet");
                        pet.Close();
                    }
                    finally{if(!pet.IsDisposed)pet.Close();}
                }
            }
            finally{Cursor.Position=cursor;Native.SetForegroundWindow(foreground);}
        }
        static void ClickCabinet(ArcadeWindow arcade,int index)
        {
            var canvas=MakerField<DoubleBufferedPanel>(arcade,"canvas");var box=ArcadeWindow.Cabinet(index);float scale=Math.Min(canvas.Width/1000f,canvas.Height/660f);
            var point=new Point((int)((canvas.Width-1000*scale)/2+(box.X+box.Width/2)*scale),(int)((canvas.Height-660*scale)/2+(box.Y+box.Height/2)*scale));
            typeof(Control).GetMethod("OnMouseClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)});
            if(index==0&&arcade.Game==ArcadeGame.Dance)WaitForDance(arcade);
        }
        static void WaitForDance(ArcadeWindow arcade)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(arcade.Dance==null&&watch.Elapsed.TotalSeconds<30){Thread.Sleep(10);Application.DoEvents();arcade.Step();}
            Check(arcade.Dance!=null,"Selected music is prepared before the game can start: "+MakerField<Label>(arcade,"guidance").Text);
        }
        static bool ArcadeKey(ArcadeWindow arcade,Keys key)
        {return (bool)typeof(ArcadeWindow).GetMethod("HandleGameKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(arcade,new object[]{key});}
        static void ArcadeKeyUp(ArcadeWindow arcade,Keys key)
        {typeof(Control).GetMethod("OnKeyUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(arcade,new object[]{new KeyEventArgs(key)});}
    }
}
