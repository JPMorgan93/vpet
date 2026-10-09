using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Vpet
{
    internal enum BrickState { Ready, Pending, Playing, Finished, Stopped }
    internal enum BrickPower { None, Triple, Tall, Sticky, Bomb }
    internal sealed class BrickPaddle
    {
        public float Y,Velocity,TallBonus;
        public BrickPower Power;
        public double FrozenUntil,LaunchAt;
    }
    internal sealed class BrickBall
    {
        public float X,Y,VX,VY,Offset;
        public int LastTouch,HeldBy=-1;
        public bool Bomb,StickyHeld;
        public double PulseAt=double.NaN;
    }
    internal sealed class BrickOrb
    {
        public float X,Y,VX;
        public BrickPower Power;
    }
    internal sealed class BattleBrick
    {
        public int Row,Column;
        public bool Broken;
    }
    internal sealed class BrickBattle
    {
        internal const float Width=1000,BallSpeed=360,PaddleWidth=12;
        internal const int BrickRows=12,BrickColumns=3;
        internal const double RoundSeconds=120,PulseSeconds=.22;
        readonly Random random;
        public readonly ArcadeDifficulty Difficulty;
        public readonly bool PowerUps;
        public readonly BrickPaddle[] Paddles={new BrickPaddle(),new BrickPaddle()};
        public readonly List<BrickBall> Balls=new List<BrickBall>();
        public readonly List<BrickOrb> Orbs=new List<BrickOrb>();
        public readonly List<BattleBrick> Bricks=new List<BattleBrick>();
        public readonly int[] Dots=new int[2],Scores=new int[2];
        public readonly double[][] DotTimes={new double[5],new double[5]};
        public BrickState State {get;private set;}
        public int Round {get;private set;}
        public int Winner {get;private set;}
        public float Height {get;private set;}
        public float BasePaddleHeight {get{return Math.Min(105,Height*.24f);}}
        public float Radius {get{return Math.Min(8,Height/30);}}
        public double SecondsLeft {get;private set;}
        public double Now {get;private set;}
        double previous,roundStarted,lastMouse=-100,nextThink;
        float npcTarget;
        internal float NpcSpeed {get{return new[]{145f,235f,365f}[(int)Difficulty];}}
        internal double NpcReaction {get{return new[]{.28,.16,.07}[(int)Difficulty];}}
        public BrickBattle(ArcadeDifficulty difficulty,bool powerUps,Random random)
        {
            Difficulty=difficulty;PowerUps=powerUps;this.random=random;Height=600;Round=1;Winner=-1;SecondsLeft=RoundSeconds;
            ResetBricks();Paddles[0].Y=Paddles[1].Y=Height/2;npcTarget=Height/2;
        }
        void ResetBricks()
        {Bricks.Clear();for(int row=0;row<BrickRows;row++)for(int column=0;column<BrickColumns;column++)Bricks.Add(new BattleBrick{Row=row,Column=column});}
        internal RectangleF BrickBounds(BattleBrick brick)
        {return new RectangleF(Width/2-27+brick.Column*18,brick.Row*Height/BrickRows,18,Height/BrickRows);}
        public float PaddleHeight(int side){return Math.Min(Height-4,BasePaddleHeight*(1+Paddles[side].TallBonus));}
        public RectangleF PaddleBounds(int side)
        {return new RectangleF(side==0?46:Width-46-PaddleWidth,Paddles[side].Y-PaddleHeight(side)/2,PaddleWidth,PaddleHeight(side));}
        public float BallRadius(BrickBall ball){return Radius*(ball.Bomb?2:1);}
        static float Clamp(float value,float min,float max){return Math.Max(min,Math.Min(max,value));}
        float ClampPaddle(int side,float y){float half=PaddleHeight(side)/2;return Clamp(y,half+2,Height-half-2);}
        public bool HasPending(int side){return Balls.Any(ball=>ball.HeldBy==side);}
        internal bool DotLit(int side,int index){return side==0?index<Dots[0]:index>=5-Dots[1];}
        public void Resize(float height)
        {
            if(float.IsNaN(height)||float.IsInfinity(height)||height<32)throw new ArgumentOutOfRangeException("height");
            if(Math.Abs(height-Height)<.001)return;
            float ratio=height/Height;Height=height;
            for(int side=0;side<2;side++){Paddles[side].Y=ClampPaddle(side,Paddles[side].Y*ratio);Paddles[side].Velocity=0;}
            foreach(var ball in Balls){ball.Y=Clamp(ball.Y*ratio,BallRadius(ball),Height-BallRadius(ball));ball.Offset*=ratio;}
            foreach(var orb in Orbs)orb.Y=Clamp(orb.Y*ratio,14,Height-14);
            npcTarget=ClampPaddle(1,npcTarget*ratio);FollowHeld();
        }
        public void Start(double now)
        {
            Round=1;Scores[0]=Scores[1]=0;Winner=-1;
            Paddles[0].Y=Paddles[1].Y=Height/2;lastMouse=-100;BeginRound(now);
        }
        void BeginRound(double now)
        {
            State=BrickState.Pending;Now=previous=now;SecondsLeft=RoundSeconds;Dots[0]=Dots[1]=0;Balls.Clear();Orbs.Clear();ResetBricks();
            for(int side=0;side<2;side++)
            {
                Paddles[side].Power=BrickPower.None;Paddles[side].TallBonus=0;Paddles[side].FrozenUntil=0;Paddles[side].Velocity=0;
                Paddles[side].Y=ClampPaddle(side,Paddles[side].Y);for(int i=0;i<5;i++)DotTimes[side][i]=double.NegativeInfinity;
                AddPending(side,now);
            }
            npcTarget=Paddles[1].Y;nextThink=now;
        }
        public void Stop(double now)
        {if(State!=BrickState.Pending&&State!=BrickState.Playing)return;State=BrickState.Stopped;Now=previous=now;Balls.Clear();Orbs.Clear();Winner=-1;ResetBricks();}
        public void MovePlayer(float y,double now)
        {
            var paddle=Paddles[0];if(now<paddle.FrozenUntil)return;
            float target=ClampPaddle(0,y);double elapsed=Math.Max(.008,Math.Min(.15,now-lastMouse));
            paddle.Velocity=Clamp((target-paddle.Y)/(float)elapsed,-1200,1200);paddle.Y=target;lastMouse=now;FollowHeld();
        }
        void AddPending(int side,double now)
        {
            if(HasPending(side))return;
            Balls.Add(new BrickBall{HeldBy=side,LastTouch=side,Y=Paddles[side].Y});
            Paddles[side].LaunchAt=now+.5+random.NextDouble()*.4;FollowHeld();
        }
        void FollowHeld()
        {
            foreach(var ball in Balls)if(ball.HeldBy>=0)
            {
                int side=ball.HeldBy;var box=PaddleBounds(side);float radius=BallRadius(ball);
                ball.Offset=Clamp(ball.Offset,-box.Height/2+radius,box.Height/2-radius);
                ball.X=side==0?box.Right+radius+.1f:box.Left-radius-.1f;ball.Y=Clamp(Paddles[side].Y+ball.Offset,radius,Height-radius);
            }
        }
        public void Click(double now)
        {
            if(State!=BrickState.Pending&&State!=BrickState.Playing)return;
            Update(now);
            if(State==BrickState.Pending)
            {State=BrickState.Playing;roundStarted=now;previous=now;LaunchHeld(0,now);LaunchHeld(1,now);}
            else if(State==BrickState.Playing)LaunchHeld(0,now);
        }
        void LaunchHeld(int side,double now)
        {
            if(now<Paddles[side].FrozenUntil)return;
            foreach(var ball in Balls.Where(ball=>ball.HeldBy==side).ToArray())
            {
                double angle=Clamp(Paddles[side].Velocity/900,-1,1)*Math.PI/3;
                if(side==1&&Math.Abs(Paddles[side].Velocity)<15)angle=(random.NextDouble()-.5)*.7;
                ball.HeldBy=-1;ball.LastTouch=side;ball.VX=(float)Math.Cos(angle)*BallSpeed*(side==0?1:-1);ball.VY=(float)Math.Sin(angle)*BallSpeed;
                if(ball.StickyHeld&&Paddles[side].Power==BrickPower.Sticky)Paddles[side].Power=BrickPower.None;
                ball.StickyHeld=false;
            }
        }
        public void Update(double now)
        {
            if(now<previous)return;Now=now;double elapsed=now-previous;previous=now;
            if(State!=BrickState.Pending&&State!=BrickState.Playing)return;
            if(now-lastMouse>.12)Paddles[0].Velocity=0;
            if(State==BrickState.Playing)
            {SecondsLeft=Math.Max(0,RoundSeconds-(now-roundStarted));if(SecondsLeft==0){EndRound(now);return;}}
            double remaining=Math.Min(.25,elapsed);
            while(remaining>1e-8&&(State==BrickState.Pending||State==BrickState.Playing))
            {
                float dt=(float)Math.Min(1.0/120,remaining);remaining-=dt;MoveNpc(dt,now);FollowHeld();
                if(State!=BrickState.Playing)continue;
                foreach(var ball in Balls.ToArray())
                {
                    if(State!=BrickState.Playing)break;
                    if(!Balls.Contains(ball)||ball.HeldBy>=0)continue;
                    if(!double.IsNaN(ball.PulseAt))
                    {if(now-ball.PulseAt>=PulseSeconds){Balls.Remove(ball);Resupply(now);}continue;}
                    AdvanceBall(ball,dt,now);
                }
                if(State!=BrickState.Playing)break;
                foreach(var orb in Orbs.ToArray())
                {
                    orb.X+=orb.VX*dt;var area=new RectangleF(orb.X-14,orb.Y-14,28,28);
                    bool caught=false;for(int side=0;side<2;side++)if(PaddleBounds(side).IntersectsWith(area)){Collect(side,orb.Power);caught=true;break;}
                    if(caught||orb.X<=14||orb.X>=Width-14)Orbs.Remove(orb);
                }
                if(HasPending(1)&&now>=Paddles[1].LaunchAt)LaunchHeld(1,now);
            }
        }
        void MoveNpc(float dt,double now)
        {
            var paddle=Paddles[1];if(now<paddle.FrozenUntil){paddle.Velocity=0;return;}
            if(now>=nextThink)
            {
                nextThink=now+NpcReaction;
                var threat=Balls.Where(ball=>ball.HeldBy<0&&double.IsNaN(ball.PulseAt)&&ball.VX>0&&ball.X<PaddleBounds(1).Left)
                    .OrderBy(ball=>(PaddleBounds(1).Left-ball.X)/ball.VX).FirstOrDefault();
                if(threat!=null)
                {
                    double travel=(PaddleBounds(1).Left-BallRadius(threat)-threat.X)/threat.VX;
                    float radius=BallRadius(threat),span=Height-2*radius;
                    double y=(threat.Y-radius+threat.VY*Math.Max(0,travel))%(2*span);if(y<0)y+=2*span;if(y>span)y=2*span-y;
                    float error=new[]{58f,28f,7f}[(int)Difficulty];npcTarget=ClampPaddle(1,(float)y+radius+(float)(random.NextDouble()-.5)*error*2);
                }
                else
                {
                    var orb=Orbs.Where(item=>item.VX>0).OrderByDescending(item=>item.X).FirstOrDefault();
                    npcTarget=orb==null?Height/2:ClampPaddle(1,orb.Y);
                }
            }
            float before=paddle.Y;paddle.Y=ClampPaddle(1,before+Clamp(npcTarget-before,-NpcSpeed*dt,NpcSpeed*dt));paddle.Velocity=dt>0?(paddle.Y-before)/dt:0;
        }
        void Resupply(double now)
        {
            if(State!=BrickState.Playing)return;
            for(int side=0;side<2;side++)if(!HasPending(side)&&!Balls.Any(ball=>ball.HeldBy<0&&double.IsNaN(ball.PulseAt)&&(side==0?ball.X<Width/2:ball.X>=Width/2)))AddPending(side,now);
        }
        internal void Collect(int side,BrickPower power)
        {
            var paddle=Paddles[side];float bonus=power==BrickPower.Tall?(paddle.Power==BrickPower.Tall?paddle.TallBonus+.1f:.2f):0;
            paddle.Power=power;paddle.TallBonus=bonus;paddle.Y=ClampPaddle(side,paddle.Y);FollowHeld();
        }
        void AwardGoal(int exitedSide,double now)
        {
            int scoring=1-exitedSide;if(Dots[scoring]>=5)return;
            int index=scoring==0?Dots[scoring]:4-Dots[scoring];DotTimes[scoring][index]=now;Dots[scoring]++;
            if(Dots[scoring]==5)EndRound(now);
        }
        void EndRound(double now)
        {
            int winner=Dots[0]==Dots[1]?-1:Dots[0]>Dots[1]?0:1;if(winner>=0)Scores[winner]++;
            Balls.Clear();Orbs.Clear();ResetBricks();
            bool finished=Round==2&&(Scores[0]==2||Scores[1]==2)||Round>=3&&Scores[0]!=Scores[1];
            if(finished){State=BrickState.Finished;Winner=Scores[0]>Scores[1]?0:1;Now=now;return;}
            Round++;BeginRound(now);
        }
        void BreakBrick(BattleBrick brick,int touchedBy)
        {
            if(brick.Broken)return;brick.Broken=true;
            if(PowerUps&&random.NextDouble()<.25)
            {var box=BrickBounds(brick);Orbs.Add(new BrickOrb{X=box.X+box.Width/2,Y=box.Y+box.Height/2,VX=touchedBy==0?-120:120,Power=(BrickPower)(1+random.Next(4))});}
        }
        void PaddleHit(BrickBall ball,int side,double now)
        {
            var paddle=Paddles[side];ball.LastTouch=side;
            if(ball.Bomb){ball.PulseAt=now;paddle.FrozenUntil=now+.5;paddle.Velocity=0;return;}
            var box=PaddleBounds(side);double angle=Clamp((ball.Y-paddle.Y)/(box.Height/2)*.9f+paddle.Velocity/1200*.15f,-1,1)*Math.PI/3;
            ball.VX=(float)Math.Cos(angle)*BallSpeed*(side==0?1:-1);ball.VY=(float)Math.Sin(angle)*BallSpeed;
            if(paddle.Power==BrickPower.Sticky&&!Balls.Any(item=>item.HeldBy==side&&item.StickyHeld))
            {ball.HeldBy=side;ball.Offset=ball.Y-paddle.Y;ball.StickyHeld=true;paddle.LaunchAt=now+.55+random.NextDouble()*.35;FollowHeld();return;}
            if(paddle.Power==BrickPower.Bomb){ball.Bomb=true;paddle.Power=BrickPower.None;ball.X=side==0?box.Right+BallRadius(ball)+.1f:box.Left-BallRadius(ball)-.1f;ball.Y=Clamp(ball.Y,BallRadius(ball),Height-BallRadius(ball));}
            else if(paddle.Power==BrickPower.Triple)
            {
                double heading=Math.Atan2(ball.VY,ball.VX);
                foreach(int sign in new[]{-1,1}){double rotated=heading+sign*Math.PI/18;Balls.Add(new BrickBall{X=ball.X,Y=ball.Y,VX=(float)Math.Cos(rotated)*BallSpeed,VY=(float)Math.Sin(rotated)*BallSpeed,LastTouch=side});}
                paddle.Power=BrickPower.None;
            }
        }
        // Sweep the ball center against radius-expanded surfaces, then consume the
        // remaining step after each bounce. Small fixed steps also make moving paddles fair.
        internal static bool Sweep(float x,float y,float vx,float vy,RectangleF box,float limit,out float time,out bool horizontal)
        {
            time=0;horizontal=false;float enterX=float.NegativeInfinity,exitX=float.PositiveInfinity,enterY=enterX,exitY=exitX;
            if(Math.Abs(vx)<.00001){if(x<box.Left||x>box.Right)return false;}
            else {float a=(box.Left-x)/vx,b=(box.Right-x)/vx;enterX=Math.Min(a,b);exitX=Math.Max(a,b);}
            if(Math.Abs(vy)<.00001){if(y<box.Top||y>box.Bottom)return false;}
            else {float a=(box.Top-y)/vy,b=(box.Bottom-y)/vy;enterY=Math.Min(a,b);exitY=Math.Max(a,b);}
            float enter=Math.Max(enterX,enterY),exit=Math.Min(exitX,exitY);if(enter>exit||exit<0||enter<-.0001||enter>limit)return false;
            time=Math.Max(0,enter);horizontal=enterX>=enterY;return true;
        }
        void AdvanceBall(BrickBall ball,float dt,double now)
        {
            for(int bounce=0;bounce<12&&dt>1e-7;bounce++)
            {
                float radius=BallRadius(ball),hit=dt;int kind=0,side=-1;bool horizontal=false;BattleBrick brick=null;
                if(ball.VY<0){float t=(radius-ball.Y)/ball.VY;if(t>=0&&t<=hit){hit=t;kind=1;}}
                else if(ball.VY>0){float t=(Height-radius-ball.Y)/ball.VY;if(t>=0&&t<=hit){hit=t;kind=1;}}
                if(ball.VX!=0){float t=((ball.VX<0?radius:Width-radius)-ball.X)/ball.VX;if(t>=0&&t<=hit){hit=t;kind=2;side=ball.VX<0?0:1;}}
                for(int i=0;i<2;i++)if(i==0?ball.VX<0:ball.VX>0)
                {
                    var box=PaddleBounds(i);box.Inflate(radius,radius);float t;bool axis;
                    if(Sweep(ball.X,ball.Y,ball.VX,ball.VY,box,hit,out t,out axis)){hit=t;kind=3;side=i;horizontal=axis;}
                }
                foreach(var target in Bricks)if(!target.Broken)
                {
                    var box=BrickBounds(target);box.Inflate(radius,radius);float t;bool axis;
                    if(Sweep(ball.X,ball.Y,ball.VX,ball.VY,box,hit,out t,out axis)){hit=t;kind=4;brick=target;horizontal=axis;}
                }
                ball.X+=ball.VX*hit;ball.Y+=ball.VY*hit;dt-=hit;
                if(kind==0)return;
                if(kind==2){Balls.Remove(ball);AwardGoal(side,now);Resupply(now);return;}
                if(kind==1)ball.VY=-ball.VY;
                else if(kind==3)
                {
                    PaddleHit(ball,side,now);if(ball.HeldBy>=0||!double.IsNaN(ball.PulseAt))return;
                }
                else if(kind==4)
                {
                    if(ball.Bomb)
                    {foreach(var adjacent in Bricks.Where(item=>Math.Abs(item.Row-brick.Row)<=1&&Math.Abs(item.Column-brick.Column)<=1).ToArray())BreakBrick(adjacent,ball.LastTouch);ball.PulseAt=now;return;}
                    BreakBrick(brick,ball.LastTouch);if(horizontal)ball.VX=-ball.VX;else ball.VY=-ball.VY;
                }
                // Push out along the new travel direction so a zero-time hit cannot repeat.
                ball.X+=ball.VX/BallSpeed*.05f;ball.Y+=ball.VY/BallSpeed*.05f;
            }
        }
    }
}
