using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class ArcadeWindow
    {
        internal static RectangleF Cabinet(int index){return new RectangleF(65+index*177,115,160,290);}
        static Color LaneColor(ArcadeLane lane){return new[]{Color.FromArgb(235,71,96),Color.FromArgb(58,133,239),Color.FromArgb(58,198,120),Color.FromArgb(244,198,52)}[(int)lane];}
        static PointF LanePoint(ArcadeLane lane,double distance)
        {return new PointF(500+(lane==ArcadeLane.Left?-(float)distance:lane==ArcadeLane.Right?(float)distance:0),330+(lane==ArcadeLane.Up?-(float)distance:lane==ArcadeLane.Down?(float)distance:0));}
        string KeyName(ArcadeLane lane){return prefs.ArrowKeys?new[]{"\u2191","\u2193","\u2190","\u2192"}[(int)lane]:new[]{"W","S","A","D"}[(int)lane];}
        static void TextAt(Graphics g,string text,float x,float y,float size,Color color,bool bold=false)
        {
            using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var ink=new SolidBrush(color))
            using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(text,font,ink,new PointF(x,y),format);
        }
        void PaintArcade(object sender,PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(SceneOrigin.X,SceneOrigin.Y);g.ScaleTransform(SceneScale,SceneScale);
            var state=g.Save();g.SetClip(new Rectangle(0,0,1000,660));
            using(var background=new LinearGradientBrush(new Rectangle(0,0,1000,660),Color.FromArgb(34,23,63),Color.FromArgb(14,24,46),90))g.FillRectangle(background,0,0,1000,660);
            if(Game==ArcadeGame.Lobby)DrawLobby(g);else DrawGame(g);
            g.Restore(state);
        }
        void DrawLobby(Graphics g)
        {
            TextAt(g,"Pick a game",500,55,30,Color.FromArgb(239,218,143),true);
            using(var floor=new SolidBrush(Color.FromArgb(42,36,67)))g.FillRectangle(floor,0,407,1000,253);
            using(var tile=new Pen(Color.FromArgb(62,51,90),1))
            {for(int x=0;x<1000;x+=80)g.DrawLine(tile,x,407,x,660);for(int y=407;y<660;y+=55)g.DrawLine(tile,0,y,1000,y);}
            for(int i=0;i<5;i++)DrawCabinet(g,Cabinet(i),i);
            DrawPet(g,new PointF((float)lobbyX,520),true,facing,false);
            TextAt(g,"Dance Time and Simon Says are open",500,610,22,Color.FromArgb(214,200,236));
        }
        static void DrawCabinet(Graphics g,RectangleF box,int index)
        {
            bool active=index<2;Color accent=index==0?Color.FromArgb(239,93,178):Color.FromArgb(83,213,199);
            if(!active)accent=Color.FromArgb(108,109,125);
            using(var body=new SolidBrush(active?Color.FromArgb(63,44,94):Color.FromArgb(55,55,65)))
            using(var border=new Pen(accent,3))using(var marquee=new SolidBrush(active?accent:Color.FromArgb(73,73,85)))
            {
                g.FillRectangle(body,box);g.DrawRectangle(border,box.X,box.Y,box.Width,box.Height);
                g.FillRectangle(marquee,box.X+7,box.Y+7,box.Width-14,45);
                if(active)TextAt(g,index==0?"Dance Time":"Simon Says",box.X+box.Width/2,box.Y+29,19,Color.FromArgb(23,22,37),true);
                using(var screen=new SolidBrush(active?Color.FromArgb(15,29,48):Color.FromArgb(37,37,44)))g.FillRectangle(screen,box.X+17,box.Y+70,box.Width-34,110);
                if(active)
                {
                    if(index==0)
                    {TextAt(g,"\u2191",box.X+80,box.Y+92,26,accent,true);TextAt(g,"\u2190",box.X+47,box.Y+123,26,accent,true);TextAt(g,"\u2192",box.X+113,box.Y+123,26,accent,true);TextAt(g,"\u2193",box.X+80,box.Y+154,26,accent,true);}
                    else for(int j=0;j<4;j++){using(var fill=new SolidBrush(LaneColor((ArcadeLane)j)))g.FillRectangle(fill,box.X+38+(j%2)*45,box.Y+89+(j/2)*42,32,32);}
                    TextAt(g,"PLAY",box.X+80,box.Y+257,15,accent,true);
                }
                g.FillRectangle(marquee,box.X+9,box.Y+196,box.Width-18,37);
                using(var stick=new Pen(Color.Silver,5)){g.DrawLine(stick,box.X+45,box.Y+219,box.X+45,box.Y+204);}
                g.FillEllipse(active?Brushes.Crimson:Brushes.DimGray,box.X+38,box.Y+196,14,14);
                g.FillEllipse(active?Brushes.Gold:Brushes.DimGray,box.X+106,box.Y+208,12,12);
            }
        }
        void CabinetClick(object sender,MouseEventArgs e)
        {
            if(Game!=ArcadeGame.Lobby||e.Button!=MouseButtons.Left)return;
            var point=new PointF((e.X-SceneOrigin.X)/SceneScale,(e.Y-SceneOrigin.Y)/SceneScale);
            for(int i=0;i<2;i++)if(Cabinet(i).Contains(point)){OpenGame(i==0?ArcadeGame.Dance:ArcadeGame.Simon);return;}
        }
        void DrawGame(Graphics g)
        {
            double now=Now;TextAt(g,Game==ArcadeGame.Dance?"Dance Time":"Simon Says",500,32,28,Color.White,true);
            if(Game==ArcadeGame.Dance)
            {
                int lives=3-(int)Difficulty,misses=Dance==null?0:Dance.Misses;
                TextAt(g,"Misses",137,52,16,Color.FromArgb(211,203,229));
                for(int i=0;i<lives;i++){using(var fill=new SolidBrush(i<misses?Color.Crimson:Color.FromArgb(207,199,223)))g.FillEllipse(fill,88+i*32,73,20,20);}
                if(Dance!=null&&Dance.Streak&&Dance.State==DanceState.Running)
                    TextAt(g,"Streak Combo "+Dance.Multiplier.ToString("0.0")+"x",210,129,22,Color.FromArgb((int)(now*3)%2==0?255:90,Color.Gold),true);
            }
            ArcadeLane? lit=Game==ArcadeGame.Simon&&Simon!=null?Simon.Lit(now):null;
            foreach(ArcadeLane lane in Enum.GetValues(typeof(ArcadeLane)))DrawSquare(g,LanePoint(lane,DanceGame.SquareDistance),lane,false,lit==lane);
            if(Game==ArcadeGame.Dance&&Dance!=null&&Dance.State==DanceState.Running)
                foreach(var target in Dance.VisibleTargets)DrawSquare(g,LanePoint(target.Lane,Dance.Distance(target)),target.Lane,true,false);
            if(Game==ArcadeGame.Dance&&Dance!=null)
                foreach(var target in Dance.PulsingTargets(now))
                {float progress=(float)((now-target.ScoredAt)/DanceGame.PulseDuration);DrawSquare(g,LanePoint(target.Lane,target.ScoredDistance),target.Lane,true,true,1+.3f*(float)Math.Sin(progress*Math.PI));}
            bool hopping=Game==ArcadeGame.Dance&&Dance!=null&&Dance.State==DanceState.Running;
            float hop=hopping&&now-hopAt<.25?(float)(Math.Sin(Math.PI*Math.Max(0,now-hopAt)/.25)*13):0;
            float shake=hopping&&now-hopAt<.25?(float)Math.Sin((now-hopAt)*65)*3:0;
            DrawPet(g,new PointF(500+shake,380-hop),false,facing,hopping);
            if(Game==ArcadeGame.Dance&&Dance!=null)
            {
                if(Dance.State==DanceState.Countdown)TextAt(g,Dance.Countdown(now).ToString(),500,112,54,Color.White,true);
                else if(Dance.State==DanceState.Running)
                {
                    if(now-Dance.FeedbackAt<.7)TextAt(g,Dance.Feedback,500,252,25,Dance.Feedback=="Miss!"?Color.LightCoral:Color.Gold,true);
                    TextAt(g,"Song: "+TimeSpan.FromSeconds(Math.Min(Dance.Duration,Dance.Elapsed)).ToString(@"m\:ss")+" / "+TimeSpan.FromSeconds(Dance.Duration).ToString(@"m\:ss"),500,587,16,Color.FromArgb(211,203,229));
                }
            }
            if(Game==ArcadeGame.Simon&&Simon!=null)
            {
                if(Simon.State==SimonState.Countdown)TextAt(g,Simon.Countdown(now).ToString(),500,112,54,Color.White,true);
                else if(Simon.State==SimonState.Showing)TextAt(g,"Watch  \u00b7  Round "+Simon.Sequence.Count,500,105,25,Color.Gold,true);
                else if(Simon.State==SimonState.Replaying)TextAt(g,"Your turn  \u00b7  "+Simon.SecondsLeft(now).ToString("0.0")+"s",500,105,25,Color.White,true);
                else if(Simon.State==SimonState.Waiting)TextAt(g,"Next round in a moment",500,105,25,Color.Gold,true);
                if(now-Simon.FeedbackAt<.55&&Simon.Feedback=="Correct!")TextAt(g,"Correct!",500,252,25,Color.Gold,true);
            }
            if(Game==ArcadeGame.Dance&&Dance!=null&&Dance.State==DanceState.Stopped&&now-Dance.FinishedAt<2)
                TextAt(g,"Stopped",500,108,42,Color.FromArgb((int)(255*Math.Max(0,1-(now-Dance.FinishedAt)/2)),Color.LightGray),true);
            if(Game==ArcadeGame.Simon&&Simon!=null&&Simon.State==SimonState.Stopped&&now-Simon.FeedbackAt<2)
                TextAt(g,"Stopped",500,108,42,Color.FromArgb((int)(255*Math.Max(0,1-(now-Simon.FeedbackAt)/2)),Color.LightGray),true);
            if(resultReaction>=0&&now-resultAt<3)
            {
                int alpha=(int)(255*Math.Max(0,1-(now-resultAt)/2));string result=Game==ArcadeGame.Dance&&Dance!=null?Dance.State==DanceState.Success?"Success":"Failed":"Game Over";
                TextAt(g,result,500,108,42,Color.FromArgb(alpha,resultReaction==7?Color.PaleGreen:Color.LightCoral),true);
                using(var bubble=Artwork.Bubble(resultReaction,emote(resultReaction),.8f,false))g.DrawImage(bubble,new PointF(500-bubble.Width/2f,276-bubble.Height));
            }
        }
        void DrawSquare(Graphics g,PointF center,ArcadeLane lane,bool target,bool lit,float pulse=1)
        {
            float side=(float)DanceGame.SquareSize*pulse;var box=new RectangleF(center.X-side/2,center.Y-side/2,side,side);
            Color color=Game==ArcadeGame.Simon?LaneColor(lane):Color.FromArgb(194,174,243);
            using(var fill=new SolidBrush(target?Color.FromArgb(170,97,190):lit?color:Color.FromArgb(Game==ArcadeGame.Simon?130:35,color)))
            using(var border=new Pen(lit?Color.White:color,lit?5:target?3:2))
            {g.FillRectangle(fill,box);g.DrawRectangle(border,box.X,box.Y,box.Width,box.Height);}
            TextAt(g,KeyName(lane),center.X,center.Y,27,Game==ArcadeGame.Simon&&lit?Color.FromArgb(30,25,42):Color.White,true);
        }
        void DrawPet(Graphics g,PointF feet,bool walking,int direction,bool dancing)
        {
            var set=sprites();float factor=Math.Min(80f/set.Cell.Width,100f/set.Cell.Height);
            int renderFacing=set.ResolveFacing(direction,new PointF(direction==0?1:direction==4?-1:0,direction==2?1:direction==6?-1:0),direction);
            Bitmap frame=dancing?set.EmoteAtPhase(0,Now*6):null;
            if(frame==null)frame=set.FrameAtPhase(walking,renderFacing,Now*(walking?8:4));
            var previousMode=g.InterpolationMode;g.InterpolationMode=InterpolationMode.NearestNeighbor;
            g.DrawImage(frame,new RectangleF(feet.X-set.Cell.Width*factor/2,feet.Y-set.Cell.Height*factor,set.Cell.Width*factor,set.Cell.Height*factor));g.InterpolationMode=previousMode;
        }
    }
}
