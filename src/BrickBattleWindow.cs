using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class ArcadeWindow
    {
        readonly Panel brickHeader=new Panel{Name="BrickHeader",Dock=DockStyle.Fill};
        readonly FlowLayoutPanel brickDifficulties=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false};
        readonly RadioButton[] brickModes=new RadioButton[3];
        readonly CheckBox brickPowers=new CheckBox{Name="BrickPowerUps",Text="Power ups",AutoSize=true,ForeColor=MakerUi.Purple};
        readonly Label brickRound=new Label{Name="BrickRound",Text="Round 1",TextAlign=ContentAlignment.MiddleCenter,Size=new Size(240,28)};
        readonly Label brickTimer=new Label{Name="BrickTimer",Text="2:00",TextAlign=ContentAlignment.MiddleRight,Size=new Size(108,50)};
        readonly Panel brickScores=new Panel{Name="BrickScores",Size=new Size(240,74),BackColor=Neutral};
        readonly Label[] brickNames=new Label[2],brickPoints=new Label[2];
        Func<string> nameOfPet;
        internal Rectangle BrickFieldBounds {get{return new Rectangle(0,40,canvas.Width,Math.Max(1,canvas.Height-40));}}
        internal RectangleF BrickPetBounds
        {
            get
            {
                if(Brick==null)return RectangleF.Empty;
                var paddle=Brick.PaddleBounds(1);var cell=sprites().Cell;
                float factor=Math.Min(80f/cell.Width,Math.Min(100,Brick.BasePaddleHeight)/cell.Height);
                float shake=Now<Brick.Paddles[1].FrozenUntil?(float)Math.Sin(Now*75)*3:0;
                return new RectangleF(paddle.Left+paddle.Width/2+shake-cell.Width*factor/2,Brick.Paddles[1].Y-cell.Height*factor/2,cell.Width*factor,cell.Height*factor);
            }
        }
        void InitializeBrickControls(Func<string> petName)
        {
            nameOfPet=petName??(()=>"Vpet");top.Controls.Add(brickHeader);
            brickHeader.Controls.Add(brickDifficulties);brickHeader.Controls.Add(brickPowers);brickHeader.Controls.Add(brickRound);brickHeader.Controls.Add(brickTimer);brickHeader.Controls.Add(brickScores);
            brickDifficulties.Controls.Add(new Label{Text="Difficulty:",AutoSize=true,ForeColor=MakerUi.Purple,Margin=new Padding(0,8,5,3)});
            for(int i=0;i<3;i++)
            {
                int index=i;brickModes[i]=new RadioButton{Text=((ArcadeDifficulty)i).ToString(),AutoSize=true,ForeColor=MakerUi.Purple,Margin=new Padding(4,7,4,3)};
                brickDifficulties.Controls.Add(brickModes[i]);brickModes[i].CheckedChanged+=delegate{if(!updating&&brickModes[index].Checked)ChooseDifficulty((ArcadeDifficulty)index);};
            }
            brickPowers.Checked=prefs.BrickPowerUps;brickPowers.CheckedChanged+=delegate
            {if(updating||Busy)return;prefs.BrickPowerUps=brickPowers.Checked;resultReaction=-1;CreateGame();RefreshControls();save();canvas.Invalidate();};
            tips.SetToolTip(brickPowers,"Allow bricks to drop random power ups (25% chance). Choose before starting a match.");
            brickRound.Font=new Font("Segoe UI",12,FontStyle.Bold);brickRound.ForeColor=MakerUi.Purple;brickTimer.Font=new Font("Consolas",23,FontStyle.Bold);brickTimer.ForeColor=MakerUi.Purple;
            for(int side=0;side<2;side++)
            {
                brickNames[side]=new Label{Name=side==0?"BrickYouName":"BrickPetName",AutoEllipsis=true,Text=side==0?"You":"Vpet",Bounds=new Rectangle(side*120,3,120,24),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.White};
                brickPoints[side]=new Label{Name=side==0?"BrickYouScore":"BrickPetScore",Text="0",Bounds=new Rectangle(side*120,27,120,44),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.White,Font=new Font("Consolas",23,FontStyle.Bold)};
                brickScores.Controls.Add(brickNames[side]);brickScores.Controls.Add(brickPoints[side]);
            }
            brickScores.Paint+=delegate(object sender,PaintEventArgs e){using(var pen=new Pen(Color.FromArgb(171,154,202),1))e.Graphics.DrawLine(pen,120,0,120,74);};
            canvas.MouseMove+=delegate(object sender,MouseEventArgs e)
            {if(Game==ArcadeGame.Brick&&Brick!=null){Brick.MovePlayer((e.Y-BrickFieldBounds.Top)/SceneScale,Now);canvas.Invalidate();}};
        }
        void PositionBrickControls()
        {
            int width=brickHeader.ClientSize.Width;
            // Give the scoreboard a lower row on narrow windows to avoid the difficulty choices.
            brickDifficulties.Location=new Point(12,10);brickPowers.Location=new Point(12,55);
            brickRound.Location=new Point(Math.Max(0,(width-240)/2),width<920?48:2);brickScores.Location=new Point(Math.Max(0,(width-240)/2),width<920?78:32);
            brickTimer.Location=new Point(Math.Max(0,width-brickTimer.Width-16),width<920?78:18);
        }
        void RefreshBrickControls()
        {
            updating=true;brickPowers.Checked=prefs.BrickPowerUps;
            for(int i=0;i<3;i++){brickModes[i].Checked=i==(int)Difficulty;brickModes[i].Enabled=!Busy;}updating=false;brickPowers.Enabled=!Busy;
            if(Brick==null)return;
            string name=nameOfPet();brickNames[1].Text=string.IsNullOrWhiteSpace(name)?"Vpet":name.Trim();tips.SetToolTip(brickNames[1],brickNames[1].Text);
            brickRound.Text="Round "+Brick.Round;brickTimer.Text=TimeSpan.FromSeconds(Math.Ceiling(Brick.SecondsLeft)).ToString(@"m\:ss");
            for(int side=0;side<2;side++)brickPoints[side].Text=Brick.Scores[side].ToString();
        }
        static Color PowerColor(BrickPower power)
        {return power==BrickPower.Triple?Color.FromArgb(79,151,255):power==BrickPower.Tall?Color.Gold:power==BrickPower.Sticky?Color.FromArgb(80,221,133):power==BrickPower.Bomb?Color.FromArgb(255,91,105):Color.FromArgb(225,216,245);}
        void DrawBrickBattle(Graphics g)
        {
            g.Clear(Neutral);if(Brick==null)return;double now=Now;
            for(int side=0;side<2;side++)for(int index=0;index<5;index++)
            {
                bool red=Brick.DotLit(side,index);double age=now-Brick.DotTimes[side][index];int alpha=red&&age<2&&(int)(age*6)%2==1?65:255;
                using(var brush=new SolidBrush(red?Color.FromArgb(alpha,Color.FromArgb(255,66,83)):Color.FromArgb(109,106,128)))
                    g.FillEllipse(brush,(side==0?12:canvas.Width-112)+index*20,10,14,14);
            }
            var saved=g.Save();var bounds=BrickFieldBounds;g.SetClip(bounds);g.TranslateTransform(0,bounds.Top);g.ScaleTransform(SceneScale,SceneScale);
            using(var wall=new Pen(Color.FromArgb(183,168,217),4)){g.DrawLine(wall,0,2,1000,2);g.DrawLine(wall,0,Brick.Height-2,1000,Brick.Height-2);}
            using(var line=new Pen(Color.FromArgb(56,45,78),1)){line.DashStyle=DashStyle.Dash;g.DrawLine(line,500,0,500,Brick.Height);}
            foreach(var brick in Brick.Bricks.Where(item=>!item.Broken))
            {
                var box=Brick.BrickBounds(brick);using(var fill=new SolidBrush(new[]{Color.FromArgb(230,123,102),Color.FromArgb(240,183,90),Color.FromArgb(167,117,207)}[brick.Column]))g.FillRectangle(fill,box);
                using(var pen=new Pen(Color.FromArgb(249,222,174),1))g.DrawRectangle(pen,box.X,box.Y,box.Width,box.Height);
            }
            var petBounds=BrickPetBounds;var set=sprites();
            bool reacting=Brick.State==BrickState.Finished&&resultReaction>=0&&now-resultAt<3;
            Bitmap frame=reacting?set.EmoteAtPhase(resultReaction,(now-resultAt)*6):null;
            frame=frame??set.FrameAtPhase(Math.Abs(Brick.Paddles[1].Velocity)>5,4,now*(Math.Abs(Brick.Paddles[1].Velocity)>5?8:4));
            for(int side=0;side<2;side++)
            {
                var box=Brick.PaddleBounds(side);float shake=now<Brick.Paddles[side].FrozenUntil?(float)Math.Sin(now*75)*3:0;box.X+=shake;
                using(var fill=new SolidBrush(PowerColor(Brick.Paddles[side].Power)))g.FillRectangle(fill,box);
                using(var outline=new Pen(Color.White,1))g.DrawRectangle(outline,box.X,box.Y,box.Width,box.Height);
            }
            // The NPC is centered over its bar and drawn in front of it, including during a freeze shake.
            var interpolation=g.InterpolationMode;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(frame,petBounds);g.InterpolationMode=interpolation;
            foreach(var orb in Brick.Orbs)
            {
                Color color=PowerColor(orb.Power);using(var fill=new SolidBrush(Color.FromArgb(128,color)))g.FillEllipse(fill,orb.X-14,orb.Y-14,28,28);
                using(var pen=new Pen(Color.FromArgb(128,Color.White),1))g.DrawEllipse(pen,orb.X-14,orb.Y-14,28,28);
                string symbol=orb.Power==BrickPower.Triple?"x3":orb.Power==BrickPower.Tall?"\u2195":orb.Power==BrickPower.Sticky?"S":"!";
                TextAt(g,symbol,orb.X,orb.Y,17,Color.FromArgb(128,Color.White),true);
            }
            foreach(var ball in Brick.Balls)
            {
                float radius=Brick.BallRadius(ball);int alpha=ball.Bomb&&(int)(now*14)%2==1?80:255;
                if(!double.IsNaN(ball.PulseAt)){float progress=(float)Math.Min(1,(now-ball.PulseAt)/BrickBattle.PulseSeconds);radius*=1+progress*2;alpha=(int)(255*(1-progress));}
                using(var fill=new SolidBrush(Color.FromArgb(alpha,ball.Bomb?Color.FromArgb(255,95,106):Color.White)))g.FillEllipse(fill,ball.X-radius,ball.Y-radius,radius*2,radius*2);
                using(var pen=new Pen(Color.FromArgb(alpha,ball.LastTouch==0?Color.FromArgb(104,199,255):Color.FromArgb(209,153,255)),1.5f))g.DrawEllipse(pen,ball.X-radius,ball.Y-radius,radius*2,radius*2);
            }
            if(Brick.State==BrickState.Pending)TextAt(g,"Click to launch both balls",250,Brick.Height/2,24,Color.FromArgb(224,213,243),true);
            else if(Brick.State==BrickState.Ready)TextAt(g,"Press Start to begin",250,Brick.Height/2,24,Color.FromArgb(224,213,243),true);
            else if(Brick.State==BrickState.Stopped)TextAt(g,"Stopped",250,Brick.Height/2,32,Color.LightGray,true);
            else if(Brick.State==BrickState.Finished)
            {
                TextAt(g,"WINNER!",Brick.Winner==0?250:750,Brick.Height/2,48,Color.FromArgb((int)(now*3)%2==0?255:90,Color.Gold),true);
                if(reacting)using(var bubble=Artwork.Bubble(resultReaction,emote(resultReaction),.8f,false))g.DrawImage(bubble,new PointF(Math.Min(1000-bubble.Width,petBounds.Left+petBounds.Width/2-bubble.Width/2),Math.Max(4,petBounds.Top-bubble.Height)));
            }
            g.Restore(saved);
        }
    }
}
