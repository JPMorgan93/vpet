using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ArcadeFloorFeatures()
        {
            string path=Path.Combine(artifacts,"arcade-floor-settings.json");File.WriteAllText(path,"{\"Arcade\":{}}");
            var prefs=Preferences.Load(path);Check(prefs.Arcade.Background==DanceBackground.Dynamic&&prefs.Arcade.SimonBackground&&prefs.Arcade.Volume==25,"Old and new profiles use rotating Dance floors, Simon floors, and the quieter default");
            prefs.Arcade.Background=DanceBackground.Static;prefs.Arcade.SimonBackground=false;prefs.Save(path);prefs=Preferences.Load(path);
            Check(prefs.Arcade.Background==DanceBackground.Static&&!prefs.Arcade.SimonBackground,"Independent background choices persist");
            prefs.Arcade.Background=DanceBackground.Off;prefs.Save(path);Check(Preferences.Load(path).Arcade.Background==DanceBackground.Off,"Dance Off survives a restart");
            prefs.Arcade.Background=(DanceBackground)900;prefs.Validate();Check(prefs.Arcade.Background==DanceBackground.Dynamic,"Invalid background values recover to Dynamic");
            Check(Math.Abs(ArcadeMusic.Gain(100)-1)<1e-6&&ArcadeMusic.Gain(0)==0&&ArcadeMusic.Gain(25)<ArcadeMusic.Gain(53)*.51,"Full volume remains normal, mute is exact, and the new default is substantially quieter");
            foreach(ArcadeLane lane in Enum.GetValues(typeof(ArcadeLane)))
            {
                byte[] wave=ArcadeTones.CreateWave(lane);int edgePeak=0,edgeJump=0;short prior=0;
                for(int index=0;index<66;index++)
                {short sample=BitConverter.ToInt16(wave,44+index*2);edgePeak=Math.Max(edgePeak,Math.Abs((int)sample));edgeJump=Math.Max(edgeJump,Math.Abs(sample-prior));prior=sample;}
                Check(edgePeak<1500&&edgeJump<300,"Simon "+lane+" has a gentle first three milliseconds without a sharp initial discontinuity");
                Check(Math.Abs((int)BitConverter.ToInt16(wave,wave.Length-4))<4&&BitConverter.ToInt16(wave,wave.Length-2)==0,"Simon "+lane+" releases to silence smoothly");
            }
        }
        static void PreparedAudioTests()
        {
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference");
            using(var music=new ArcadeMusic())
            {
                var stopwatch=Stopwatch.StartNew();music.Preload(root);
                var tasks=new[]{"Easy","Normal","Hard"}.Select(s=>music.Prepare(Path.Combine(root,s+".mp3"))).ToArray();
                Check(Task.WaitAll(tasks,30000),"All three songs fully decode before playing");
                foreach(var task in tasks)
                {var clip=task.Result;Check(clip.Data.Length>100000&&clip.Format.Bits==16&&clip.Format.Rate>=22050&&Math.Abs(clip.Duration*clip.Format.BytesPerSecond-clip.Data.Length)<1,"Prepared songs retain their native sample rate and contain complete PCM");}
                Console.WriteLine("Prepared all three songs in "+stopwatch.Elapsed.TotalSeconds.ToString("0.000")+"s; "+tasks.Sum(t=>(long)t.Result.Data.Length)+" PCM bytes.");
                string copied=Path.Combine(artifacts,"prepared-song.mp3");File.Copy(Path.Combine(root,"Easy.mp3"),copied,true);
                music.Prepare(copied).GetAwaiter().GetResult();File.Delete(copied);
                music.Open(copied,0);Check(music.QueuedBuffers==1&&music.Position==0,"Prepared music opens and queues the entire song after its source file has gone");
                for(int replay=0;replay<3;replay++)
                {
                    music.PreparePlayback();stopwatch.Restart();music.Play();Check(stopwatch.Elapsed.TotalMilliseconds<150,"Prepared Start does not decode, read a song, or build an audio graph");
                    var playback=Stopwatch.StartNew();double previous=0;
                    for(int sample=0;sample<8;sample++)
                    {Thread.Sleep(50);double position=music.Position;Check(position>=previous&&Math.Abs(position-playback.Elapsed.TotalSeconds)<.12&&music.QueuedBuffers==1,"PCM advances at normal speed without disk access or UI pumping");previous=position;}
                    GC.Collect();GC.WaitForPendingFinalizers();Thread.Sleep(100);Check(Math.Abs(music.Position-playback.Elapsed.TotalSeconds)<.12,"Pinned music continues through collection and a blocked UI thread");
                    music.Stop();double stopped=music.Position;Thread.Sleep(50);Check(Math.Abs(music.Position-stopped)<.001,"Stop freezes the prepared audio clock");
                }
                music.Close();
                music.Open(Path.Combine(artifacts,"arcade-volume-silence.wav"),0);music.Play();Thread.Sleep(2200);
                Check(music.QueuedBuffers==0&&music.Position==music.Duration,"The final audio buffer reports song completion instead of resetting the game clock");
                music.Play();Thread.Sleep(50);Check(music.Position<.2&&music.QueuedBuffers==1,"Completed songs restart from sample zero");
            }
            using(var tones=new ArcadeTones(0))
            {
                tones.Prepare();tones.Play(ArcadeLane.Up);tones.Play(ArcadeLane.Up);tones.Play(ArcadeLane.Down);tones.Play(ArcadeLane.Left);
                Check(tones.ActiveVoices==4,"Rapid identical and different Simon tones overlap without cutting off their fade-out");Thread.Sleep(260);Check(tones.ActiveVoices==0,"All overlapping tones finish naturally");
                tones.Play(ArcadeLane.Right);tones.Stop();Thread.Sleep(30);Check(tones.ActiveVoices==0,"Stop cancels all remaining Simon tones on the next audio quantum");
            }
            using(var gate=new ManualResetEventSlim(false))using(var music=new ArcadeMusic((path,token)=>
            {gate.Wait(token);return new ArcadeClip(new byte[88200],new ArcadeAudioNative.WaveFormat{Tag=1,Channels=1,Rate=22050,BytesPerSecond=44100,BlockAlign=2,Bits=16});}))
            using(var sprites=SpriteSet.FromReference(Path.Combine(root,"Blue Dragon.png")))
            using(var arcade=new ArcadeWindow(()=>sprites,index=>null,new ArcadePreferences{Volume=0},()=>{},root,new Random(1),music))
            {
                arcade.Show();Application.DoEvents();MakerField<System.Windows.Forms.Timer>(arcade,"timer").Stop();arcade.OpenGame(ArcadeGame.Dance);
                Check(arcade.Dance==null&&!FindButton(arcade,"Start").Enabled,"A slow cold load leaves Start disabled until complete PCM is ready");
                arcade.StartGame();Check(arcade.Dance==null,"Start cannot bypass preparation");var modes=MakerField<RadioButton[]>(arcade,"modes");modes[2].Checked=true;
                arcade.OpenGame(ArcadeGame.Simon);Check(arcade.Simon!=null&&FindButton(arcade,"Start").Enabled,"The window remains usable and can switch games while music prepares");
                gate.Set();Task.WaitAll(new[]{"Easy","Normal","Hard"}.Select(s=>music.Prepare(Path.Combine(root,s+".mp3"))).ToArray());arcade.Step();
                Check(arcade.Game==ArcadeGame.Simon&&arcade.Dance==null,"Old preparation cannot replace the selected Simon game");
                arcade.OpenGame(ArcadeGame.Dance);Check(arcade.Dance!=null&&FindButton(arcade,"Start").Enabled&&music.QueuedBuffers==1,"A cached song is fully queued before Start becomes available");
                arcade.Close();
            }
        }
        static void ArcadeFloorWindows(PetWindow pet,ArcadeWindow arcade,ref double now)
        {
            var originalTone=arcade.PlayTone;arcade.PlayTone=lane=>{};
            var picker=MakerField<ComboBox>(arcade,"background");var practice=MakerField<CheckBox>(arcade,"practice");var modes=MakerField<RadioButton[]>(arcade,"modes");
            Check(picker.Visible&&picker.SelectedItem.ToString()=="Dynamic"&&picker.Top>=practice.Bottom&&picker.Items.Cast<object>().Select(x=>x.ToString()).SequenceEqual(new[]{"Dynamic","Static","Off"}),"Dance backgrounds sit below Practice with Dynamic selected by default");
            now=100;picker.SelectedIndex=1;picker.SelectedIndex=0;arcade.Step();
            using(var zero=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))
            {
                now=100.999;arcade.Step();using(var before=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(zero,before)==0&&arcade.FloorOrientation==0,"Dynamic floor holds still until one second");
                now=101;arcade.Step();using(var rotated=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(zero,rotated)>1000&&arcade.FloorOrientation==1,"Floor snaps to exactly 90 degrees at one second");
                now=102;arcade.Step();Check(arcade.FloorOrientation==2,"Floor rotates 180 degrees at two seconds");now=103;arcade.Step();Check(arcade.FloorOrientation==3,"Floor rotates 270 degrees at three seconds");
                now=104;arcade.Step();using(var full=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(zero,full)==0,"Dynamic floor returns to its original orientation at four seconds");
                picker.SelectedIndex=1;now+=20;arcade.Step();using(var stationary=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(zero,stationary)==0,"Static displays the unrotated floor indefinitely");
                picker.SelectedIndex=2;using(var off=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(zero,off)>1000,"Off restores the neutral background");
                Check(Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.Background==DanceBackground.Off,"Dance background saves immediately");picker.SelectedIndex=0;
            }
            CaptureForm(arcade,"dance-floor-dynamic");
            arcade.OpenGame(ArcadeGame.Simon);Check(!practice.Visible&&picker.SelectedItem.ToString()=="On"&&picker.Top>=MakerField<FlowLayoutPanel>(arcade,"difficulties").Bottom&&picker.Items.Cast<object>().Select(x=>x.ToString()).SequenceEqual(new[]{"On","Off"}),"Simon offers On and Off below Difficulty with On selected");
            using(var enabled=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))
            {picker.SelectedIndex=1;using(var off=ArcadeRegion(arcade,new Rectangle(220,470,100,100)))Check(ArcadeDifferentPixels(enabled,off)>1000,"Simon Off restores the neutral background");}
            Check(!Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.SimonBackground,"Simon choice saves independently");picker.SelectedIndex=0;
            foreach(int difficulty in new[]{1,2})
            {
                modes[difficulty].Checked=true;arcade.StartGame();now+=3;arcade.Step();int length=difficulty==1?3:5;
                Check(arcade.Simon.Sequence.Count==length&&arcade.Simon.State==SimonState.Showing,"Simon preview starts with "+length+" buttons at "+arcade.Difficulty);
                now+=arcade.Simon.ShowStep*(length-1);arcade.Step();Check(arcade.Simon.State==SimonState.Showing,"The initial sequence includes every button before player input");
                now+=arcade.Simon.ShowStep;arcade.Step();Check(arcade.Simon.State==SimonState.Replaying,"Full "+length+"-button sequence finishes before the five-second player timer starts");arcade.StopGame();
            }
            foreach(var size in new[]{new Size(700,590),new Size(1040,790)})
            {arcade.ClientSize=size;Application.DoEvents();Check(picker.Parent.ClientRectangle.Contains(picker.Bounds),"Background picker remains fully visible at "+size);}
            CaptureForm(arcade,"simon-floor-on");arcade.OpenGame(ArcadeGame.Dance);arcade.PlayTone=originalTone;
        }
    }
}
