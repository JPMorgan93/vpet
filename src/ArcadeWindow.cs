using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
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
        readonly DoubleBufferedPanel canvas=new DoubleBufferedPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(23,18,42)};
        readonly FlowLayoutPanel difficulties=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Left,WrapContents=false,Padding=new Padding(8)};
        readonly RadioButton[] modes=new RadioButton[3];
        readonly Button score=new Button{Name="ArcadeScore",Width=170,Height=42,Text="- -",BackColor=Color.FromArgb(44,33,75),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
        readonly Label calculated=new Label{Name="ArcadePending",AutoSize=true,ForeColor=Color.White,Text="x1   0"};
        readonly Button start=MakerUi.Button("Start",null),closeGame=MakerUi.Button("Close Game",null),switchKeys=MakerUi.Button("\u21bb",null);
        readonly TrackBar volume=new TrackBar{Name="ArcadeVolume",Orientation=Orientation.Vertical,Minimum=0,Maximum=100,TickFrequency=10,Width=44};
        readonly Label volumeLabel=new Label{Text="Volume",AutoSize=true,ForeColor=Color.White};
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
        bool updating,showingHigh,resultHandled,disposed;
        double resultAt=-100,hopAt=-100,lastHop;
        int resultReaction=-1,facing=2;
        double lobbyX=140;int lobbyDirection=1;
        double previous;
        internal double Now {get{return Time==null?clock.Elapsed.TotalSeconds:Time();}}
        internal bool Busy {get{return Game==ArcadeGame.Dance?Dance!=null&&(Dance.State==DanceState.Countdown||Dance.State==DanceState.Running):Game==ArcadeGame.Simon&&Simon!=null&&Simon.State!=SimonState.Ready&&Simon.State!=SimonState.Finished;}}
        public ArcadeWindow(Func<SpriteSet> sprites,Func<int,Bitmap> emote,ArcadePreferences prefs,Action save,string musicDirectory,Random random)
        {
            this.sprites=sprites;this.emote=emote;this.prefs=prefs;this.save=save;this.musicDirectory=musicDirectory;this.random=random;
            Text="Arcade Window";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;BackColor=Color.FromArgb(36,27,58);
            ClientSize=new Size(1040,790);MinimumSize=new Size(700,620);StartPosition=FormStartPosition.CenterScreen;KeyPreview=true;
            SongPosition=()=>music.Position;score.Font=new Font("Consolas",16,FontStyle.Bold);
            var top=new Panel{Dock=DockStyle.Top,Height=92,BackColor=Color.FromArgb(241,236,249)};lobbyHeading.ForeColor=MakerUi.Purple;
            var scoreboard=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,Dock=DockStyle.Right,Width=190,Padding=new Padding(8)};calculated.ForeColor=MakerUi.Purple;
            scoreboard.Controls.Add(score);scoreboard.Controls.Add(calculated);top.Controls.Add(scoreboard);top.Controls.Add(difficulties);top.Controls.Add(lobbyHeading);
            var difficultyLabel=MakerUi.Label("Difficulty:");difficultyLabel.ForeColor=MakerUi.Purple;difficulties.Controls.Add(difficultyLabel);
            for(int i=0;i<3;i++)
            {
                int index=i;modes[i]=new RadioButton{Text=((ArcadeDifficulty)i).ToString(),AutoSize=true,ForeColor=MakerUi.Purple,Margin=new Padding(6,15,6,8)};
                difficulties.Controls.Add(modes[i]);modes[i].CheckedChanged+=delegate{if(!updating&&modes[index].Checked)ChooseDifficulty((ArcadeDifficulty)index);};
            }
            var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=3,RowCount=2,Padding=new Padding(8)};
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,34));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));
            closeGame.Anchor=AnchorStyles.Left;start.Anchor=AnchorStyles.None;footer.Controls.Add(closeGame,0,0);footer.Controls.Add(start,1,0);footer.Controls.Add(guidance,0,1);footer.SetColumnSpan(guidance,3);
            Controls.Add(canvas);Controls.Add(top);Controls.Add(footer);
            canvas.Controls.Add(switchKeys);canvas.Controls.Add(volume);canvas.Controls.Add(volumeLabel);
            switchKeys.MinimumSize=new Size(36,34);switchKeys.Size=new Size(42,36);switchKeys.Name="ArcadeKeyBinding";switchKeys.Text="";switchKeys.Image=ArcadeControls.SwitchIcon();switchKeys.AccessibleName="Switch between WASD and arrow keys";
            tips.SetToolTip(switchKeys,"Switch between WASD and arrow keys");tips.SetToolTip(score,"Click to show the high score for this game and difficulty");
            switchKeys.Click+=delegate{prefs.ArrowKeys=!prefs.ArrowKeys;held.Clear();save();Focus();canvas.Invalidate();};
            score.Click+=delegate{showingHigh=!showingHigh;RefreshControls();Focus();};
            start.Click+=delegate{StartGame();};closeGame.Click+=delegate{ShowLobby();};
            volume.Value=prefs.Volume;volume.ValueChanged+=delegate{prefs.Volume=volume.Value;try{music.SetVolume(prefs.Volume);}catch(Exception ex){MusicError(ex);}save();};
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
            music.Close();Game=ArcadeGame.Lobby;Dance=null;Simon=null;held.Clear();resultReaction=-1;Text="Arcade Window";guidance.Text="Click a lit cabinet to play. Close this window to return your pet to the desktop.";RefreshControls();canvas.Invalidate();
        }
        internal void OpenGame(ArcadeGame game)
        {
            music.Close();Game=game;Difficulty=ArcadeDifficulty.Easy;showingHigh=false;resultReaction=-1;
            Text=game==ArcadeGame.Dance?"Arcade Window \u00b7 Dance Time":"Arcade Window \u00b7 Simon Says";CreateGame();RefreshControls();Focus();canvas.Invalidate();
        }
        void ChooseDifficulty(ArcadeDifficulty value)
        {if(Busy)return;Difficulty=value;showingHigh=false;resultReaction=-1;CreateGame();RefreshControls();canvas.Invalidate();}
        void CreateGame()
        {
            held.Clear();resultHandled=false;Dance=null;Simon=null;music.Close();
            if(Game==ArcadeGame.Dance)
            {
                try{music.Open(Path.Combine(musicDirectory,Difficulty+".mp3"),prefs.Volume);Dance=new DanceGame(Difficulty,music.Duration);Dance.MusicStarted+=delegate{music.Play();lastHop=0;hopAt=Now;};guidance.Text="Hit matching keys while a target overlaps its square. Five Excellents start a streak.";}
                catch(Exception ex){MusicError(ex);}
            }
            else if(Game==ArcadeGame.Simon){Simon=new SimonGame(Difficulty,random);guidance.Text="Watch the lights, then repeat the entire sequence within five seconds.";}
        }
        internal void StartGame()
        {
            if(Busy||Game==ArcadeGame.Lobby)return;if(Game==ArcadeGame.Dance&&Dance==null)CreateGame();
            if(Game==ArcadeGame.Dance&&Dance==null)return;
            resultReaction=-1;resultHandled=false;showingHigh=false;held.Clear();facing=2;
            if(Game==ArcadeGame.Dance)Dance.Start(Now);else Simon.Start(Now);
            RefreshControls();Focus();canvas.Invalidate();
        }
        internal void Step()
        {
            double now=Now,dt=Math.Max(0,Math.Min(.1,now-previous));previous=now;
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
                {resultHandled=true;resultAt=now;resultReaction=Dance.State==DanceState.Success?7:4;music.Stop();if(Dance.State==DanceState.Success){prefs.Record(Game,Difficulty,Dance.Banked);save();}}
            }
            else if(Game==ArcadeGame.Simon&&Simon!=null)
            {
                Simon.Update(now);var lit=Simon.Lit(now);if(lit.HasValue)facing=Facing(lit.Value);
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
            difficulties.Visible=score.Visible=calculated.Visible=closeGame.Visible=switchKeys.Visible=game;lobbyHeading.Visible=!game;
            start.Visible=game&&!Busy;start.Enabled=Game==ArcadeGame.Simon||Dance!=null;
            volume.Visible=volumeLabel.Visible=Game==ArcadeGame.Dance&&Dance!=null&&(Dance.State==DanceState.Running||Dance.State==DanceState.Success||Dance.State==DanceState.Failed);
            if(game)
            {
                long banked=Game==ArcadeGame.Dance?Dance==null?0:Dance.Banked:Simon==null?0:Simon.Banked;
                bool blank=Game==ArcadeGame.Dance?Dance==null||Dance.State==DanceState.Ready||Dance.State==DanceState.Countdown||Dance.State==DanceState.Failed:Simon==null||Simon.State==SimonState.Ready||Simon.State==SimonState.Countdown;
                score.Text=showingHigh?"High: "+prefs.High(Game,Difficulty):blank?"- -":banked.ToString();
                calculated.Text=Game==ArcadeGame.Dance?"x"+(Dance==null?1:Dance.Multiplier).ToString("0.0")+"   "+(Dance==null?0:Dance.Pending):"Ready to score: "+(Simon==null?0:Simon.Pending);
            }
            PositionControls();
        }
        void PositionControls()
        {
            float scale=SceneScale;PointF origin=SceneOrigin;
            switchKeys.Location=new Point((int)(origin.X+552*scale),(int)(origin.Y+174*scale));
            volume.Location=new Point(14,58);volume.Height=Math.Max(80,canvas.Height-110);volumeLabel.Location=new Point(8,32);
        }
        float SceneScale {get{return Math.Max(.1f,Math.Min(canvas.Width/1000f,canvas.Height/660f));}}
        PointF SceneOrigin {get{return new PointF((canvas.Width-1000*SceneScale)/2,(canvas.Height-660*SceneScale)/2);}}
        protected override void Dispose(bool disposing)
        {if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();music.Dispose();tips.Dispose();switchKeys.Image.Dispose();save();}base.Dispose(disposing);}
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
