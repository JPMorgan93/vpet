using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ArcadeAdjustmentChecks()
        {
            Check(new ArcadePreferences().Volume==53,"New music default is 25 percent below the former 70 percent, rounded to 53");
            string path=Path.Combine(artifacts,"arcade-volume-migration.json");
            File.WriteAllText(path,"{\"Arcade\":{}}");Check(Preferences.Load(path).Arcade.Volume==53,"Missing music volume uses the quieter default");
            File.WriteAllText(path,"{\"Arcade\":{\"Volume\":70}}");Check(Preferences.Load(path).Arcade.Volume==70,"Explicit saved music volume is retained");
            Check(ArcadeMusic.DeviceVolume(53)==265&&ArcadeMusic.DeviceVolume(70)==350&&ArcadeMusic.DeviceVolume(100)==500&&ArcadeMusic.DeviceVolume(0)==0,"Playback gain halves default, saved, and maximum volume while retaining mute");
            using(var image=JoystickArtwork.Draw(1))
            {
                image.Save(Path.Combine(artifacts,"joystick-reference-render.png"));
                int red=0,cyan=0;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
                {var p=image.GetPixel(x,y);if(p.A>200&&p.R>150&&p.G<60&&p.B<60)red++;if(p.A>200&&p.R<30&&p.G>100&&p.B>150)cyan++;}
                Check(image.Size==new Size(80,72)&&image.GetPixel(0,0).A==0&&red>100&&cyan>100,"Supplied joystick artwork renders with red stick, cyan base, and transparent corners");
            }
            foreach(int grade in new[]{10,20})
            {
                var game=new DanceGame(ArcadeDifficulty.Easy,30,Enumerable.Range(0,7).Select(i=>new DanceTarget(ArcadeLane.Up,4+i)));
                game.Start(0);game.Update(3);
                for(int i=0;i<6;i++){game.Update(7+i,4+i);game.Press(ArcadeLane.Up,7+i);}
                Check(game.Streak&&game.Multiplier==1.3&&game.Pending==60&&game.StreakScore==120,"Excellent run separates ordinary points from its active bonus");
                double distance=grade==20?165:180;game.Update(13,10-(distance-136)/game.Speed);game.Press(ArcadeLane.Up,13);
                Check(!game.Streak&&game.Multiplier==1&&game.ExcellentRun==0&&game.StreakScore==0&&game.Pending==216+grade,"Good/Great finalizes the streak bonus before adding ordinary target points");
                var pulse=game.PulsingTargets(13).Single();Check(pulse.Resolved&&Math.Abs(pulse.ScoredDistance-distance)<.001&&!game.VisibleTargets.Contains(pulse),"Hit pulse stays at its scored position outside the live target list");
                Check(game.PulsingTargets(13.1).Any()&&!game.PulsingTargets(13.23).Any(),"Successful target pulses briefly before disappearing");
                game.Stop(13.1);Check(game.State==DanceState.Stopped&&game.Banked==0&&game.Pending==0&&!game.PulsingTargets(13.1).Any(),"Stopping Dance clears scores and pulse effects");
                game.Update(99,30);game.Press(ArcadeLane.Up,99);Check(game.State==DanceState.Stopped&&game.Banked==0,"Stopped Dance cannot score or become a success later");
                game.Start(100);Check(game.State==DanceState.Countdown&&game.Chart.All(t=>!t.Resolved&&double.IsNaN(t.ScoredAt)),"Restart clears resolved targets and old pulses");
            }
            var cancelled=new DanceGame(ArcadeDifficulty.Hard,30);int starts=0;cancelled.MusicStarted+=()=>starts++;
            cancelled.Start(0);cancelled.Stop(1);cancelled.Update(4);Check(starts==0&&cancelled.State==DanceState.Stopped,"Stop during Dance countdown prevents music from starting");
            var separate=new DanceGame(ArcadeDifficulty.Easy,30,Enumerable.Range(0,11).Select(i=>new DanceTarget(ArcadeLane.Up,4+i)));
            separate.Start(0);separate.Update(3);
            for(int i=0;i<11;i++)
            {
                separate.Update(7+i,4+i-(i==6?.5*58/160:0));separate.Press(ArcadeLane.Up,7+i);
                if(i==6)Check(separate.Pending==236&&!separate.Streak,"Completed first streak joins calculated points exactly once");
            }
            Check(separate.Pending==296&&separate.StreakScore==60&&separate.Multiplier==1.1,"A second streak excludes all points from the first streak and its bonus");
            separate.Press(ArcadeLane.Left,18);Check(separate.Banked==362&&separate.StreakScore==0,"A miss cashes two distinct streaks without multiplying earlier calculated points");
            separate.Update(40,30);separate.Update(41,31);Check(separate.Banked==362,"Song completion never reapplies a finalized streak bonus");
            var finish=new DanceGame(ArcadeDifficulty.Easy,30,Enumerable.Range(0,4).Select(i=>new DanceTarget(ArcadeLane.Up,4+i)));
            foreach(bool stop in new[]{false,true})
            {
                finish.Start(0);finish.Update(3);for(int i=0;i<4;i++){finish.Update(7+i,4+i);finish.Press(ArcadeLane.Up,7+i);}
                Check(finish.StreakScore==60&&finish.Pending==60&&finish.Multiplier==1.1,"Success/Stop fixture has unfinalized streak points");
                if(stop)finish.Stop(11);else finish.Update(40,30);
                Check(finish.Banked==(stop?0:126)&&finish.Pending==0&&finish.StreakScore==0&&!finish.Streak,"Success finalizes an active streak; Stop discards it");
            }
            foreach(SimonState stopAt in new[]{SimonState.Countdown,SimonState.Showing,SimonState.Replaying,SimonState.Waiting})
            {
                var game=new SimonGame(ArcadeDifficulty.Easy,new Random(8));int tones=0;game.TonePlayed+=lane=>tones++;game.Start(0);
                if(stopAt!=SimonState.Countdown)game.Update(3);
                if(stopAt==SimonState.Replaying||stopAt==SimonState.Waiting)game.Update(3.9);
                if(stopAt==SimonState.Waiting)game.Press(game.Sequence[0],4);
                Check(game.State==stopAt,"Simon reaches stop fixture "+stopAt);
                int previous=tones;game.Stop(4.1);game.Update(30);game.Press(ArcadeLane.Up,30);
                Check(game.State==SimonState.Stopped&&game.Pending==0&&game.Banked==0&&!game.Lit(30).HasValue&&tones==previous,"Simon Stop prevents later scoring, highlights, and tones in "+stopAt);
                game.Start(31);Check(game.State==SimonState.Countdown&&game.Sequence.Count==0,"Stopped Simon can restart cleanly");
            }
            var simon=new SimonGame(ArcadeDifficulty.Hard,new Random(4));var played=new System.Collections.Generic.List<ArcadeLane>();simon.TonePlayed+=played.Add;
            simon.Start(0);simon.Update(3);simon.Update(3.01);simon.Update(3.15);Check(played.Count==1&&played[0]==simon.Sequence[0],"One tone is emitted per pet highlight, never once per paint/tick");
            simon.Update(3.4);simon.Press(simon.Sequence[0],3.5);
            Check(played.Count==2&&played[1]==played[0]&&simon.Lit(3.679)==simon.Sequence[0],"Player's final input plays the pet's matching tone and keeps the normal highlight");
            simon.Press(ArcadeLane.Down,3.6);simon.Update(4.179);
            Check(simon.State==SimonState.Waiting&&simon.Pending==50&&played.Count==2&&!simon.Lit(4.179).HasValue,"Full inter-round rest ignores keys and emits no tones");
            simon.Update(4.18);Check(simon.Sequence.Count==2&&played.Count==3&&played[2]==simon.Sequence[0],"Next sequence and its tone start half a second after the final highlight ends");
            simon.Sequence[1]=simon.Sequence[0];simon.Update(4.58);simon.Update(4.59);Check(played.Count==4&&played[3]==played[2],"Consecutive identical directions still play two separate tones");
            simon.Update(4.98);simon.Press(simon.Sequence[0],5);simon.Press((ArcadeLane)(((int)simon.Sequence[1]+1)%4),5.1);
            Check(simon.State==SimonState.Finished&&played.Count==6&&played[4]==simon.Sequence[0]&&played[5]!=(simon.Sequence[1]),"Every accepted player press, including a mistake, emits its directional tone");
            double[] frequencies={523.25,329.63,392,659.25};
            foreach(ArcadeLane lane in Enum.GetValues(typeof(ArcadeLane)))
            {
                byte[] wave=ArcadeTones.CreateWave(lane);int crossings=0,peak=0;short prior=0;
                for(int i=44;i<wave.Length;i+=2){short sample=BitConverter.ToInt16(wave,i);if(prior<0&&sample>=0)crossings++;prior=sample;peak=Math.Max(peak,Math.Abs((int)sample));}
                Check(wave.Length==8864&&BitConverter.ToInt32(wave,24)==22050&&System.Text.Encoding.ASCII.GetString(wave,0,4)=="RIFF"&&peak>5000&&peak<8000,"Simon "+lane+" tone is valid short PCM with volume headroom");
                Check(Math.Abs(crossings/.2-frequencies[(int)lane])<10&&BitConverter.ToInt16(wave,44)==0&&BitConverter.ToInt16(wave,wave.Length-2)==0,"Simon "+lane+" tone has its own frequency and smooth silent endpoints");
                byte[] mute=ArcadeTones.CreateWave(lane,0);Check(mute.Skip(44).All(b=>b==0),"Simon "+lane+" muted test wave is silent");
            }
        }
        static void ArcadeLayerChecks(PetWindow pet)
        {
            LayerMode original=pet.Model.Settings.Layer;var model=pet.Toys.Model;
            pet.Toys.SetVisible(true);model.SpawnBall(pet.Now);model.SpawnTriangle();model.SpawnCoin();model.SpawnCard();model.SpawnDie();pet.Plate.SetVisible(true);
            using(var app=new Form{Text="Arcade stacking test",Size=new Size(80,80),StartPosition=FormStartPosition.Manual,Location=new Point(10,10)})
            {
                app.Show();Application.DoEvents();
                foreach(LayerMode mode in Enum.GetValues(typeof(LayerMode)))
                {
                    pet.Model.Settings.Layer=mode;pet.ApplyLayer();PetFrame(pet);Application.DoEvents();
                    Check(!pet.arcadeWindow.TopMost&&(Native.GetWindowLongPtr(pet.arcadeWindow.Handle,-20).ToInt64()&8)==0,"Arcade uses ordinary window stacking independently of "+mode);
                    var assets=pet.Toys.Windows.Where(w=>w.Visible).Concat(new LayeredWindow[]{pet.Plate,pet.Joystick}).ToArray();
                    foreach(var asset in assets)
                    {
                        Native.SetWindowPos(asset.Handle,new IntPtr(-1),0,0,0,0,0x213);Application.DoEvents();
                        var order=new System.Collections.Generic.List<IntPtr>();Native.EnumWindows(delegate(IntPtr h,IntPtr unused){order.Add(h);return true;},IntPtr.Zero);
                        Check(order.IndexOf(pet.arcadeWindow.Handle)<order.IndexOf(asset.Handle),"Arcade stays above "+asset.Text+" after promotion in "+mode);
                    }
                    Native.SetWindowPos(app.Handle,IntPtr.Zero,0,0,0,0,0x213);PetFrame(pet);Application.DoEvents();
                    var appOrder=new System.Collections.Generic.List<IntPtr>();Native.EnumWindows(delegate(IntPtr h,IntPtr unused){appOrder.Add(h);return true;},IntPtr.Zero);
                    Check(appOrder.IndexOf(app.Handle)<appOrder.IndexOf(pet.arcadeWindow.Handle),"Another application can stack above Arcade in "+mode);
                }
            }
            pet.Toys.SetVisible(false);pet.Plate.SetVisible(false);pet.Model.Settings.Layer=original;pet.ApplyLayer();PetFrame(pet);pet.arcadeWindow.Activate();
        }
        static Bitmap ArcadeRegion(ArcadeWindow arcade,Rectangle logical)
        {
            var canvas=MakerField<DoubleBufferedPanel>(arcade,"canvas");float scale=Math.Min(canvas.Width/1000f,canvas.Height/660f);
            var region=new Rectangle((int)((canvas.Width-1000*scale)/2+logical.X*scale),(int)((canvas.Height-660*scale)/2+logical.Y*scale),(int)(logical.Width*scale),(int)(logical.Height*scale));
            using(var image=new Bitmap(canvas.Width,canvas.Height)){canvas.DrawToBitmap(image,canvas.ClientRectangle);return image.Clone(region,System.Drawing.Imaging.PixelFormat.Format32bppArgb);}
        }
        static int ArcadeDifferentPixels(Bitmap first,Bitmap second)
        {int different=0;for(int y=0;y<first.Height;y++)for(int x=0;x<first.Width;x++)if(first.GetPixel(x,y)!=second.GetPixel(x,y))different++;return different;}
    }
}
