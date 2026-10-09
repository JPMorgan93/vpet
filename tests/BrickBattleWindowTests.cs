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
        static void BrickMouse(ArcadeWindow window,string method,int x,int y)
        {typeof(Control).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(MakerField<DoubleBufferedPanel>(window,"canvas"),new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
        static Bitmap BrickCanvas(ArcadeWindow window)
        {var canvas=MakerField<DoubleBufferedPanel>(window,"canvas");var image=new Bitmap(canvas.Width,canvas.Height);canvas.DrawToBitmap(image,canvas.ClientRectangle);return image;}
        static void BrickBattleWindows()
        {
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference"),name="";double now=10;int saves=0;
            var prefs=new ArcadePreferences();
            using(var sprite=SpriteSet.FromReference(Path.Combine(root,"Blue Dragon.png")))using(var window=new ArcadeWindow(()=>sprite,index=>null,prefs,()=>saves++,root,new Random(12),petName:()=>name))
            {
                window.Time=()=>now;window.Show();Application.DoEvents();MakerField<Timer>(window,"timer").Stop();
                CaptureForm(window,"brick-lobby");ClickCabinet(window,2);Application.DoEvents();
                Check(window.Game==ArcadeGame.Brick&&window.Text.Contains("Brick Battle")&&window.Brick.State==BrickState.Ready,"Third cabinet opens Brick Battle");
                var canvas=MakerField<DoubleBufferedPanel>(window,"canvas");var round=MakerField<Label>(window,"brickRound");var timer=MakerField<Label>(window,"brickTimer");var points=MakerField<Label[]>(window,"brickPoints");var names=MakerField<Label[]>(window,"brickNames");var powers=MakerField<CheckBox>(window,"brickPowers");var modes=MakerField<RadioButton[]>(window,"brickModes");
                Check(names[0].Text=="You"&&names[1].Text=="Vpet"&&points.All(label=>label.Text=="0")&&round.Text=="Round 1"&&timer.Text=="2:00","Ready header shows default pet name, match scores, round, and timer");
                name="Skywing";window.Step();Check(names[1].Text==name,"Score card displays the saved pet name");
                name=new string('W',40);window.Step();Check(names[1].AutoEllipsis&&names[1].Text==name,"Long names retain their text and use ellipsis within the score card");name="Skywing";
                Check(powers.Checked&&powers.Enabled&&modes[0].Checked&&!MakerField<ComboBox>(window,"background").Visible&&!MakerField<Panel>(window,"volumePanel").Visible&&!MakerField<Button>(window,"switchKeys").Visible,"Brick controls start on Easy with power ups and hide unrelated music/key controls");
                powers.Checked=false;Check(!prefs.BrickPowerUps&&!window.Brick.PowerUps&&saves>0,"Power-up toggle updates game and saved preference");powers.Checked=true;modes[2].Checked=true;Check(window.Difficulty==ArcadeDifficulty.Hard&&window.Brick.Difficulty==ArcadeDifficulty.Hard,"Difficulty selects the matching NPC behavior");
                foreach(var size in new[]{new Size(1040,790),new Size(684,581),new Size(1200,650),new Size(700,1000)})
                {
                    window.ClientSize=size;Application.DoEvents();window.Step();var header=MakerField<Panel>(window,"brickHeader");var choices=MakerField<FlowLayoutPanel>(window,"brickDifficulties");var scoreboard=MakerField<Panel>(window,"brickScores");
                    Check(header.ClientRectangle.Contains(scoreboard.Bounds)&&header.ClientRectangle.Contains(timer.Bounds)&&header.ClientRectangle.Contains(round.Bounds)&&header.ClientRectangle.Contains(choices.Bounds)&&header.ClientRectangle.Contains(powers.Bounds),"Brick controls fit the resized header: "+size);
                    Check(!scoreboard.Bounds.IntersectsWith(choices.Bounds)&&!scoreboard.Bounds.IntersectsWith(powers.Bounds)&&!scoreboard.Bounds.IntersectsWith(timer.Bounds)&&!round.Bounds.IntersectsWith(choices.Bounds),"Header sections do not overlap: "+size);
                    var field=window.SceneBounds;Check(field.Left==0&&Math.Abs(field.Right-canvas.Width)<.001&&Math.Abs(field.Bottom-canvas.Height)<.001&&Math.Abs(window.Brick.Height*window.SceneScale-field.Height)<.001,"Brick field fills the full canvas width and available height: "+size);
                    Check(window.Brick.PaddleBounds(0).Left>0&&window.Brick.PaddleBounds(1).Right<1000,"Paddles stay inset from the open sides: "+size);
                    if(size.Width==684)CaptureForm(window,"brick-minimum");
                }
                window.ClientSize=new Size(1040,790);Application.DoEvents();window.Step();CaptureForm(window,"brick-ready");
                FindButton(window,"Start").PerformClick();Check(window.Busy&&window.Brick.State==BrickState.Pending&&FindButton(window,"Stop").Visible&&!powers.Enabled&&modes.All(mode=>!mode.Enabled),"Start enters Pending, shows Stop, and locks match options");
                now+=5;window.Step();Check(timer.Text=="2:00"&&window.Brick.Balls.All(ball=>ball.HeldBy>=0),"Initial pending state never auto-launches or runs down the timer");
                int mouseY=window.BrickFieldBounds.Top+(int)((window.Brick.Height/2+45)*window.SceneScale);BrickMouse(window,"OnMouseMove",100,mouseY);Check(window.Brick.Paddles[0].Y>window.Brick.Height/2&&window.Brick.Balls.Single(ball=>ball.HeldBy==0).Y==window.Brick.Paddles[0].Y,"Native mouse motion moves the paddle and its pending ball");
                CaptureForm(window,"brick-pending");BrickMouse(window,"OnMouseClick",100,mouseY);Check(window.Brick.State==BrickState.Playing&&window.Brick.Balls.All(ball=>ball.HeldBy<0),"Native left click launches both initial balls");
                now+=1.1;window.Step();Check(timer.Text=="1:59","Visible timer counts down after serving");
                FindButton(window,"Stop").PerformClick();Check(!window.Busy&&window.Brick.State==BrickState.Stopped&&window.Brick.Balls.Count==0&&FindButton(window,"Start").Visible&&powers.Enabled,"Stop ends play without scoring and restores Start/options");
                FindButton(window,"Start").PerformClick();BrickMouse(window,"OnMouseClick",100,mouseY);window.Brick.Balls.Clear();
                for(int side=0;side<2;side++)window.Brick.Collect(side,side==0?BrickPower.Triple:BrickPower.Sticky);
                for(int i=0;i<4;i++)window.Brick.Orbs.Add(new BrickOrb{X=260+i*75,Y=window.Brick.Height*.65f,Power=(BrickPower)(i+1),VX=-120});
                window.Brick.Balls.Add(new BrickBall{X=300,Y=window.Brick.Height*.4f,VX=360,Bomb=true});window.Step();CaptureForm(window,"brick-powers");
                window.Brick.Balls.Clear();window.Brick.Orbs.Clear();BattleGoal(window.Brick,0,ref now);window.Step();
                using(var dots=BrickCanvas(window))Check(dots.GetPixel(19,17).R>200&&dots.GetPixel(canvas.Width-25,17).R<160,"You's first goal lights the left-most left dot without changing the right dots");
                now+=.2;window.Step();using(var dots=BrickCanvas(window))Check(dots.GetPixel(19,17).R<160,"New score dot flashes during its first two seconds");now+=2;window.Step();using(var dots=BrickCanvas(window))Check(dots.GetPixel(19,17).R>200,"Score dot stays solid red after two seconds");
                for(int i=0;i<4;i++)BattleGoal(window.Brick,0,ref now);window.Step();Check(points[0].Text=="1"&&round.Text=="Round 2"&&timer.Text=="2:00"&&window.Brick.State==BrickState.Pending,"Round winner updates its score tile and begins the next pending round");
                BattleRound(window.Brick,0,ref now);window.Step();Check(!window.Busy&&FindButton(window,"Start").Visible&&MakerField<int>(window,"resultReaction")==4,"Player match victory restores Start and shows Sad for the pet");CaptureForm(window,"brick-player-winner");
                using(var bright=BrickCanvas(window))
                {
                    now+=.34;window.Step();using(var dim=BrickCanvas(window))Check(bright.GetPixel((int)(250*window.SceneScale),window.BrickFieldBounds.Top+(int)(window.Brick.Height/2*window.SceneScale)).ToArgb()!=dim.GetPixel((int)(250*window.SceneScale),window.BrickFieldBounds.Top+(int)(window.Brick.Height/2*window.SceneScale)).ToArgb(),"Winner lettering flashes yellow");
                }
                window.StartGame();BattleRound(window.Brick,1,ref now);BattleRound(window.Brick,1,ref now);window.Step();Check(window.Brick.Winner==1&&MakerField<int>(window,"resultReaction")==7,"NPC match victory shows Proud");CaptureForm(window,"brick-pet-winner");
                window.ShowLobby();Check(window.Game==ArcadeGame.Lobby&&window.Brick==null,"Close Game returns to the lobby and disposes match state");
                ClickCabinet(window,1);Check(window.Game==ArcadeGame.Simon&&window.Simon!=null,"Simon still opens after Brick Battle");window.ShowLobby();ClickCabinet(window,2);Check(window.Brick.State==BrickState.Ready&&powers.Checked,"Brick Battle can reopen with its saved power-up choice");window.Close();
            }
        }
    }
}
