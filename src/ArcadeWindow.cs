using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class ArcadeWindow : Form
    {
        readonly Func<SpriteSet> sprites;readonly Func<int,Bitmap> emote;
        readonly ArcadePreferences prefs;readonly Action save;
        readonly string musicDirectory;readonly Random random;
        readonly Stopwatch clock=Stopwatch.StartNew();readonly Timer timer=new Timer{Interval=16};
        readonly ArcadeMusic music=new ArcadeMusic();
        readonly ArcadeTones tones=new ArcadeTones();
        readonly DoubleBufferedPanel canvas=new DoubleBufferedPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(23,18,42)};
        readonly FlowLayoutPanel difficulties=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Left,WrapContents=false,Padding=new Padding(8)};
        readonly RadioButton[] modes=new RadioButton[3];
        readonly CheckBox practice=new CheckBox{Name="ArcadePractice",Text="Practice mode",AutoSize=true,ForeColor=MakerUi.Purple,Location=new Point(16,67)};
        readonly Label backgroundLabel=new Label{Text="Background:",AutoSize=true,ForeColor=MakerUi.Purple};
        readonly ComboBox background=new ComboBox{Name="ArcadeBackground",DropDownStyle=ComboBoxStyle.DropDownList,Width=145};
        readonly Panel top=new Panel{Dock=DockStyle.Top,Height=142,BackColor=Color.FromArgb(241,236,249)};
        readonly Bitmap[] danceFloors=new Bitmap[4];Bitmap simonFloor;
        Task<ArcadeClip> musicPreparation;string pendingSong;double backgroundAt;
        readonly Button score=new Button{Name="ArcadeScore",Width=170,Height=42,Text="- -",BackColor=Color.FromArgb(44,33,75),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
        readonly Label calculated=new Label{Name="ArcadePending",AutoSize=true,ForeColor=Color.White,Text="1.0x   0"};
        readonly Button start=MakerUi.Button("Start",null),closeGame=MakerUi.Button("Close Game",null),switchKeys=MakerUi.Button("\u21bb",null);
        readonly TrackBar volume=new TrackBar{Name="ArcadeVolume",Orientation=Orientation.Vertical,Minimum=0,Maximum=100,TickFrequency=10,Width=44};
        readonly Label volumeLabel=new Label{Text="Volume",AutoSize=true,ForeColor=Color.White};
        readonly Label volumeValue=new Label{Name="ArcadeVolumeValue",AutoSize=true,ForeColor=Color.White};
        readonly Label guidance=new Label{AutoSize=true,ForeColor=Color.FromArgb(225,216,245),Text="Choose Dance Time or Simon Says"};
        readonly Label lobbyHeading=new Label{AutoSize=true,Font=new Font("Segoe UI",19,FontStyle.Bold),Text="Vpet Arcade",ForeColor=Color.White,Padding=new Padding(12)};
        readonly HashSet<Keys> held=new HashSet<Keys>();
        readonly ToolTip tips=new ToolTip();
        internal ArcadeGame Game {get;private set;}
        internal ArcadeDifficulty Difficulty {get;private set;}
        internal DanceGame Dance {get;private set;}
        internal SimonGame Simon {get;private set;}
        internal Func<double> Time {get;set;}
        internal Func<double> SongPosition {get;set;}
        internal Action<ArcadeLane> PlayTone {get;set;}
        bool updating,showingHigh,resultHandled,disposed;
        double resultAt=-100,hopAt=-100,lastHop;
        int resultReaction=-1,facing=2;
        double lobbyX=140;int lobbyDirection=1;
        double previous;
        internal double Now {get{return Time==null?clock.Elapsed.TotalSeconds:Time();}}
        internal bool Busy {get{return Game==ArcadeGame.Dance?Dance!=null&&(Dance.State==DanceState.Countdown||Dance.State==DanceState.Running):Game==ArcadeGame.Simon&&Simon!=null&&Simon.State!=SimonState.Ready&&Simon.State!=SimonState.Finished&&Simon.State!=SimonState.Stopped;}}
        public ArcadeWindow(Func<SpriteSet> sprites,Func<int,Bitmap> emote,ArcadePreferences prefs,Action save,string musicDirectory,Random random)
        {
            this.sprites=sprites;this.emote=emote;this.prefs=prefs;this.save=save;this.musicDirectory=musicDirectory;this.random=random;
            Text="Arcade Window";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;BackColor=Color.FromArgb(36,27,58);
            ClientSize=new Size(1040,790);MinimumSize=new Size(700,620);StartPosition=FormStartPosition.CenterScreen;KeyPreview=true;
            SongPosition=()=>music.Position;PlayTone=tones.Play;score.Font=new Font("Consolas",16,FontStyle.Bold);
            lobbyHeading.ForeColor=MakerUi.Purple;LoadFloors();music.Preload(musicDirectory);
            var scoreboard=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,Dock=DockStyle.Right,Width=190,Padding=new Padding(8)};calculated.ForeColor=MakerUi.Purple;
            var difficultyArea=new Panel{Dock=DockStyle.Left,Width=440};difficulties.Dock=DockStyle.Top;difficultyArea.Controls.Add(difficulties);difficultyArea.Controls.Add(practice);difficultyArea.Controls.Add(backgroundLabel);difficultyArea.Controls.Add(background);
            scoreboard.Controls.Add(score);scoreboard.Controls.Add(calculated);top.Controls.Add(scoreboard);top.Controls.Add(difficultyArea);top.Controls.Add(lobbyHeading);
            var difficultyLabel=MakerUi.Label("Difficulty:");difficultyLabel.ForeColor=MakerUi.Purple;difficulties.Controls.Add(difficultyLabel);
            for(int i=0;i<3;i++)
            {
                int index=i;modes[i]=new RadioButton{Text=((ArcadeDifficulty)i).ToString(),AutoSize=true,ForeColor=MakerUi.Purple,Margin=new Padding(6,15,6,8)};
                difficulties.Controls.Add(modes[i]);modes[i].CheckedChanged+=delegate{if(!updating&&modes[index].Checked)ChooseDifficulty((ArcadeDifficulty)index);};
            }
            practice.Checked=prefs.Practice;practice.CheckedChanged+=delegate
            {if(updating||Busy)return;prefs.Practice=practice.Checked;showingHigh=false;resultReaction=-1;CreateGame();RefreshControls();save();};
            tips.SetToolTip(practice,"Play the full song without scoring or a miss limit. Misses are counted for practice.");
            background.SelectedIndexChanged+=delegate
            {if(updating||background.SelectedIndex<0)return;if(Game==ArcadeGame.Dance)prefs.Background=(DanceBackground)background.SelectedIndex;else if(Game==ArcadeGame.Simon)prefs.SimonBackground=background.SelectedIndex==0;else return;backgroundAt=Now;save();canvas.Invalidate();};
            var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=3,RowCount=2,Padding=new Padding(8)};
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,34));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));
            closeGame.Anchor=AnchorStyles.Left;start.Anchor=AnchorStyles.None;footer.Controls.Add(closeGame,0,0);footer.Controls.Add(start,1,0);footer.Controls.Add(guidance,0,1);footer.SetColumnSpan(guidance,3);
            Controls.Add(canvas);Controls.Add(top);Controls.Add(footer);
            canvas.Controls.Add(switchKeys);canvas.Controls.Add(volume);canvas.Controls.Add(volumeLabel);canvas.Controls.Add(volumeValue);
            switchKeys.MinimumSize=new Size(36,34);switchKeys.Size=new Size(42,36);switchKeys.Name="ArcadeKeyBinding";switchKeys.Text="";switchKeys.Image=ArcadeControls.SwitchIcon();switchKeys.AccessibleName="Switch between WASD and arrow keys";
            tips.SetToolTip(switchKeys,"Switch between WASD and arrow keys");tips.SetToolTip(score,"Click to show the high score for this game and difficulty");
            switchKeys.Click+=delegate{prefs.ArrowKeys=!prefs.ArrowKeys;held.Clear();save();Focus();canvas.Invalidate();};
            score.Click+=delegate{showingHigh=!showingHigh;RefreshControls();Focus();};
            start.Click+=delegate{if(Busy)StopGame();else StartGame();};closeGame.Click+=delegate{ShowLobby();};
            volume.Value=prefs.Volume;volumeValue.Text=prefs.Volume+"%";volume.ValueChanged+=delegate{prefs.Volume=volume.Value;volumeValue.Text=prefs.Volume+"%";try{music.SetVolume(prefs.Volume);}catch(Exception ex){MusicError(ex);}save();};
            canvas.Resize+=delegate{PositionControls();};canvas.Paint+=PaintArcade;canvas.MouseClick+=CabinetClick;
            Resize+=delegate{guidance.MaximumSize=new Size(Math.Max(1,ClientSize.Width-32),0);};guidance.MaximumSize=new Size(ClientSize.Width-32,0);
            KeyDown+=delegate(object sender,KeyEventArgs e){if(HandleGameKey(e.KeyData)){e.Handled=true;e.SuppressKeyPress=true;}};
            KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};Deactivate+=delegate{held.Clear();};
            timer.Tick+=delegate{Step();};Shown+=delegate{previous=Now;timer.Start();};
            ShowLobby();
        }
        void MusicError(Exception ex){music.Close();if(Dance!=null)Dance=null;guidance.Text=ex.Message;RefreshControls();}
        internal void ShowLobby()
        {
            tones.Stop();music.Close();musicPreparation=null;Game=ArcadeGame.Lobby;Dance=null;Simon=null;held.Clear();resultReaction=-1;Text="Arcade Window";guidance.Text="Click a lit cabinet to play. Close this window to return your pet to the desktop.";RefreshControls();canvas.Invalidate();
        }
        internal void OpenGame(ArcadeGame game)
        {
            music.Close();Game=game;Difficulty=ArcadeDifficulty.Easy;showingHigh=false;resultReaction=-1;backgroundAt=Now;
            updating=true;background.Items.Clear();background.Items.AddRange(game==ArcadeGame.Dance?new object[]{"Dynamic","Static","Off"}:new object[]{"On","Off"});background.SelectedIndex=game==ArcadeGame.Dance?(int)prefs.Background:prefs.SimonBackground?0:1;updating=false;
            Text=game==ArcadeGame.Dance?"Arcade Window \u00b7 Dance Time":"Arcade Window \u00b7 Simon Says";CreateGame();RefreshControls();Focus();canvas.Invalidate();
        }
        void ChooseDifficulty(ArcadeDifficulty value)
        {if(Busy)return;Difficulty=value;showingHigh=false;resultReaction=-1;CreateGame();RefreshControls();canvas.Invalidate();}
        void CreateGame()
        {
            tones.Stop();held.Clear();resultHandled=false;Dance=null;Simon=null;music.Close();musicPreparation=null;
            if(Game==ArcadeGame.Dance)
            {
                try{pendingSong=Path.Combine(musicDirectory,Difficulty+".mp3");musicPreparation=music.Prepare(pendingSong);guidance.Text="Preparing music\u2026 Start will be ready when the entire song is loaded.";FinishPreparingMusic();}
                catch(Exception ex){MusicError(ex);}
            }
            else if(Game==ArcadeGame.Simon){try{tones.Prepare();}catch(IOException){}Simon=new SimonGame(Difficulty,random);Simon.TonePlayed+=delegate(ArcadeLane lane){PlayTone(lane);};guidance.Text="Watch the lights and listen, then repeat the entire sequence within five seconds. Start with "+Simon.InitialLength+" button"+(Simon.InitialLength==1?".":"s.");}
        }
        void FinishPreparingMusic()
        {
            if(Game!=ArcadeGame.Dance||musicPreparation==null||!musicPreparation.IsCompleted)return;
            musicPreparation=null;
            try{music.Open(pendingSong,prefs.Volume);Dance=new DanceGame(Difficulty,music.Duration,null,prefs.Practice);Dance.MusicStarted+=delegate{music.Play();lastHop=0;hopAt=Now;};guidance.Text=prefs.Practice?"Practice: play the full song without scoring or a miss limit. Misses are counted at the top left.":"Hit matching keys while a target overlaps its square. Three Excellents start a streak.";}
            catch(Exception ex){MusicError(ex);}
        }
        internal void StartGame()
        {
            if(Busy||Game==ArcadeGame.Lobby)return;
            if(Game==ArcadeGame.Dance&&Dance==null)return;
            resultReaction=-1;resultHandled=false;showingHigh=false;held.Clear();facing=2;
            if(Game==ArcadeGame.Dance){try{music.PreparePlayback();}catch(Exception ex){MusicError(ex);return;}Dance.Start(Now);}else Simon.Start(Now);
            RefreshControls();Focus();canvas.Invalidate();
        }
        internal void StopGame()
        {
            if(!Busy)return;
            if(Game==ArcadeGame.Dance){Dance.Stop(Now);music.Stop();}else Simon.Stop(Now);
            tones.Stop();held.Clear();showingHigh=false;resultReaction=-1;resultHandled=true;
            RefreshControls();Focus();canvas.Invalidate();
        }
        internal void Step()
        {
            double now=Now,dt=Math.Max(0,Math.Min(.1,now-previous));previous=now;
            FinishPreparingMusic();
            if(Game==ArcadeGame.Lobby)
            {lobbyX+=lobbyDirection*80*dt;if(lobbyX>880){lobbyX=880;lobbyDirection=-1;}else if(lobbyX<120){lobbyX=120;lobbyDirection=1;}facing=lobbyDirection>0?0:4;}
            else if(Game==ArcadeGame.Dance&&Dance!=null)
            {
                try{Dance.Update(now,Dance.State==DanceState.Running?(double?)SongPosition():null);}
                catch(Exception ex){MusicError(ex);}
                if(Dance!=null&&Dance.State==DanceState.Running)
                {
                    if(Dance.Elapsed-lastHop>=.55){lastHop=Dance.Elapsed;hopAt=now;}
                    var target=Dance.VisibleTargets.OrderBy(t=>Dance.Distance(t)).FirstOrDefault();if(target!=null)facing=Facing(target.Lane);
                }
                if(Dance!=null&&!resultHandled&&(Dance.State==DanceState.Success||Dance.State==DanceState.Failed))
                {resultHandled=true;resultAt=now;resultReaction=Dance.State==DanceState.Success?7:4;music.Stop();if(Dance.State==DanceState.Success&&!Dance.Practice){prefs.Record(Game,Difficulty,Dance.Banked);save();}}
            }
            else if(Game==ArcadeGame.Simon&&Simon!=null)
            {
                Simon.Update(now);var lit=Simon.Lit(now);if(lit.HasValue)facing=Facing(lit.Value);else if(Simon.State==SimonState.Waiting)facing=2;
                if(!resultHandled&&Simon.State==SimonState.Finished){resultHandled=true;resultAt=now;resultReaction=4;prefs.Record(Game,Difficulty,Simon.Banked);save();}
            }
            RefreshControls();canvas.Invalidate();
        }
        bool HandleGameKey(Keys keyData)
        {
            if(Game==ArcadeGame.Lobby||(keyData&Keys.Modifiers)!=Keys.None)return false;Keys key=keyData&Keys.KeyCode;var lane=LaneFor(key,prefs.ArrowKeys);if(!lane.HasValue)return false;
            if(!held.Add(key))return true;Step();if(Game==ArcadeGame.Dance&&Dance!=null)Dance.Press(lane.Value,Now);else if(Simon!=null)Simon.Press(lane.Value,Now);Step();return true;
        }
        internal static ArcadeLane? LaneFor(Keys key,bool arrows)
        {
            Keys[] keys=arrows?new[]{Keys.Up,Keys.Down,Keys.Left,Keys.Right}:new[]{Keys.W,Keys.S,Keys.A,Keys.D};
            for(int i=0;i<keys.Length;i++)if(key==keys[i])return (ArcadeLane)i;return null;
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keyData){return HandleGameKey(keyData)||base.ProcessCmdKey(ref message,keyData);}
        internal static int Facing(ArcadeLane lane){return lane==ArcadeLane.Up?6:lane==ArcadeLane.Down?2:lane==ArcadeLane.Left?4:0;}
        void RefreshControls()
        {
            bool game=Game!=ArcadeGame.Lobby;updating=true;for(int i=0;i<3;i++){modes[i].Checked=i==(int)Difficulty;modes[i].Enabled=!Busy;}updating=false;
            difficulties.Visible=closeGame.Visible=switchKeys.Visible=game;lobbyHeading.Visible=!game;
            practice.Visible=Game==ArcadeGame.Dance;practice.Enabled=!Busy;
            background.Visible=backgroundLabel.Visible=game;top.Height=Game==ArcadeGame.Dance?142:game?114:110;
            backgroundLabel.Location=new Point(16,Game==ArcadeGame.Dance?100:72);background.Location=new Point(115,Game==ArcadeGame.Dance?94:66);
            score.Visible=calculated.Visible=game&&!(Game==ArcadeGame.Dance&&prefs.Practice);
            start.Visible=game;start.Text=Busy?"Stop":"Start";start.Enabled=Game==ArcadeGame.Simon||Dance!=null;
            volume.Visible=volumeLabel.Visible=volumeValue.Visible=Game==ArcadeGame.Dance;
            if(game)
            {
                long banked=Game==ArcadeGame.Dance?Dance==null?0:Dance.Banked:Simon==null?0:Simon.Banked;
                bool blank=Game==ArcadeGame.Dance?Dance==null||Dance.State==DanceState.Ready||Dance.State==DanceState.Countdown||Dance.State==DanceState.Failed||Dance.State==DanceState.Stopped:Simon==null||Simon.State==SimonState.Ready||Simon.State==SimonState.Countdown||Simon.State==SimonState.Stopped;
                score.Text=showingHigh?"High: "+prefs.High(Game,Difficulty):blank?"- -":banked.ToString();
                calculated.Text=Game==ArcadeGame.Dance?"Calculated: "+(Dance==null?0:Dance.Pending)+"\n"+(Dance==null?1:Dance.Multiplier).ToString("0.0")+"x   Streak: "+(Dance==null?0:Dance.StreakScore):"Ready to score: "+(Simon==null?0:Simon.Pending);
            }
            PositionControls();
        }
        void PositionControls()
        {
            float scale=SceneScale;PointF origin=SceneOrigin;
            switchKeys.Location=new Point((int)(origin.X+552*scale),(int)(origin.Y+174*scale));
            volume.Location=new Point(14,58);volume.Height=Math.Max(80,canvas.Height-110);volumeLabel.Location=new Point(8,32);volumeValue.Location=new Point(14,canvas.Height-44);
        }
        float SceneScale {get{return Math.Max(.1f,Math.Min(canvas.Width/1000f,canvas.Height/660f));}}
        PointF SceneOrigin {get{return new PointF((canvas.Width-1000*SceneScale)/2,(canvas.Height-660*SceneScale)/2);}}
        protected override void Dispose(bool disposing)
        {if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();music.Dispose();tones.Dispose();tips.Dispose();switchKeys.Image.Dispose();foreach(var floor in danceFloors)if(floor!=null)floor.Dispose();if(simonFloor!=null)simonFloor.Dispose();save();}base.Dispose(disposing);}
    }
    internal static class ArcadeControls
    {
        internal static Bitmap SwitchIcon()
        {
            var image=new Bitmap(26,26);using(var g=Graphics.FromImage(image))using(var cap=new AdjustableArrowCap(3,4,true))using(var pen=new Pen(MakerUi.Purple,2))
            {g.SmoothingMode=SmoothingMode.AntiAlias;pen.CustomEndCap=cap;g.DrawArc(pen,5,5,16,16,205,140);g.DrawArc(pen,5,5,16,16,25,140);}return image;
        }
    }
}
