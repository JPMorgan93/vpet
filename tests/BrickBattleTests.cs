using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Vpet
{
    internal static partial class Tests
    {
        sealed class BrickRandom : Random
        {
            readonly double chance;readonly int type;
            public BrickRandom(double chance,int type=0){this.chance=chance;this.type=type;}
            public override double NextDouble(){return chance;}
            public override int Next(int maxValue){return type%maxValue;}
        }
        static BrickBattle Battle(bool powers=false,Random rng=null)
        {var game=new BrickBattle(ArcadeDifficulty.Normal,powers,rng??new Random(17));game.Start(0);game.Click(0);game.Balls.Clear();return game;}
        static void BattleGoal(BrickBattle game,int scorer,ref double now)
        {
            if(game.State==BrickState.Pending)game.Click(now);
            game.Balls.Clear();game.Balls.Add(new BrickBall{X=scorer==0?1000-game.Radius-.1f:game.Radius+.1f,Y=game.Height/2,VX=scorer==0?360:-360,LastTouch=scorer});
            now+=.02;game.Update(now);
        }
        static void BattleRound(BrickBattle game,int winner,ref double now)
        {for(int i=0;i<5;i++)BattleGoal(game,winner,ref now);}
        static BrickBall PaddleBall(BrickBattle game,int side,float offset=0)
        {
            var box=game.PaddleBounds(side);var ball=new BrickBall{X=side==0?box.Right+game.Radius+.1f:box.Left-game.Radius-.1f,Y=game.Paddles[side].Y+offset,VX=side==0?-360:360,LastTouch=1-side};game.Balls.Add(ball);return ball;
        }
        static void BrickBattleFeatures()
        {
            string path=Path.Combine(artifacts,"brick-settings.json");File.WriteAllText(path,"{}");var prefs=Preferences.Load(path);
            Check(prefs.Arcade.BrickPowerUps,"Existing preferences default Brick Battle power ups to On");prefs.Arcade.BrickPowerUps=false;prefs.Save(path);Check(!Preferences.Load(path).Arcade.BrickPowerUps,"Power-up choice survives restart");
            Check(!prefs.Arcade.Record(ArcadeGame.Brick,ArcadeDifficulty.Easy,100)&&prefs.Arcade.SimonHigh.All(value=>value==0),"Match scores cannot overwrite Simon high scores");
            var game=new BrickBattle(ArcadeDifficulty.Easy,true,new Random(8));
            Check(game.State==BrickState.Ready&&game.Round==1&&game.Scores.All(value=>value==0)&&game.SecondsLeft==120,"Brick Battle opens to Round 1, zero scores, and two minutes");
            Check(game.Bricks.Count==36&&game.Bricks.Select(brick=>brick.Column).Distinct().Count()==3,"Center wall has three touching columns and twelve rows");
            RectangleF first=game.BrickBounds(game.Bricks.First()),last=game.BrickBounds(game.Bricks.Last());Near(first.Top,0,0,"Wall begins at the top");Near(last.Bottom,game.Height,.001f,"Wall ends at the bottom");Near(first.Right,game.BrickBounds(game.Bricks[1]).Left,0,"Brick columns touch edge to edge");
            game.Start(10);game.Update(25);Check(game.State==BrickState.Pending&&game.Balls.Count==2&&game.Balls.All(ball=>ball.HeldBy>=0)&&game.SecondsLeft==120,"Initial NPC serve and timer both wait for the player");
            game.MovePlayer(game.Height/2+40,25);Check(game.Balls.Single(ball=>ball.HeldBy==0).Y==game.Paddles[0].Y,"Pending ball follows the player's paddle");
            game.Click(25);Check(game.State==BrickState.Playing&&game.Balls.All(ball=>ball.HeldBy<0)&&game.Balls.Any(ball=>ball.VX>0)&&game.Balls.Any(ball=>ball.VX<0),"First click serves both balls toward the wall together");
            Check(game.Balls.Single(ball=>ball.LastTouch==0).VY>0,"Serve angle follows downward mouse motion");
            foreach(var ball in game.Balls)Near((float)Math.Sqrt(ball.VX*ball.VX+ball.VY*ball.VY),360,.001f,"Served balls have constant pace");
            game.Update(25.05);Near((float)game.SecondsLeft,119.95f,.001f,"Timer begins only at simultaneous serve");
            game.Stop(25.05);game.Update(500);Check(game.State==BrickState.Stopped&&game.Balls.Count==0&&game.Orbs.Count==0&&game.Scores.All(value=>value==0),"Stop clears play and prevents later scoring");game.Start(501);Check(game.Round==1&&game.SecondsLeft==120&&game.Dots.All(value=>value==0),"Start restarts the whole match");

            double now=0;game=Battle();BattleGoal(game,0,ref now);Check(game.Dots[0]==1&&game.Dots[1]==0&&game.DotLit(0,0)&&!game.DotLit(0,4)&&game.DotTimes[0][0]==now,"Right goal credits You and fills the left-most dot first");
            BattleGoal(game,1,ref now);Check(game.Dots[1]==1&&game.DotLit(1,4)&&!game.DotLit(1,0)&&game.DotTimes[1][4]==now,"Left goal credits Vpet and fills the right-most dot first");
            for(int i=0;i<4;i++)BattleGoal(game,0,ref now);Check(game.Round==2&&game.Scores[0]==1&&game.Scores[1]==0&&game.State==BrickState.Pending&&game.Dots.All(value=>value==0)&&game.Orbs.Count==0&&game.Bricks.All(brick=>!brick.Broken),"Five dots win the round and reset the field, dots, and pending serves");
            BattleRound(game,0,ref now);Check(game.State==BrickState.Finished&&game.Round==2&&game.Winner==0&&game.Scores.SequenceEqual(new[]{2,0}),"Two straight wins end the match after Round 2");game.Update(now+400);Check(game.Scores[0]==2,"Finished matches cannot score again");
            game=Battle();now=0;BattleRound(game,0,ref now);BattleRound(game,1,ref now);Check(game.Round==3&&game.Scores.SequenceEqual(new[]{1,1}),"Split first rounds advance to Round 3");BattleRound(game,1,ref now);Check(game.State==BrickState.Finished&&game.Round==3&&game.Winner==1,"Third-round leader wins the match");
            game=Battle();game.Dots[0]=2;game.Dots[1]=2;game.Update(120);Check(game.Round==2&&game.Scores.All(value=>value==0)&&game.State==BrickState.Pending,"A timed draw awards no match point and waits for another serve");game.Click(121);game.Balls.Clear();game.Dots[0]=1;game.Dots[1]=3;game.Update(241);Check(game.Round==3&&game.Scores.SequenceEqual(new[]{0,1}),"Timed round awards the most red dots");game.Click(242);game.Balls.Clear();game.Dots[0]=game.Dots[1]=1;game.Update(362);Check(game.State==BrickState.Finished&&game.Winner==1,"A draw in Round 3 still ends the match when one side already leads");
            game=Battle();game.Update(120);game.Click(121);game.Balls.Clear();game.Update(241);game.Click(242);game.Balls.Clear();game.Update(362);Check(game.Round==4&&game.State==BrickState.Pending&&game.Scores.All(value=>value==0),"Tied match scores after Round 3 continue to extra rounds");
            now=362;BattleRound(game,0,ref now);Check(game.State==BrickState.Finished&&game.Round==4&&game.Winner==0,"First leading score in an extra round ends the match");

            game=Battle();var survivor=new BrickBall{X=200,Y=200,VX=360,LastTouch=1};game.Balls.Add(survivor);game.Balls.Add(new BrickBall{X=game.Radius+.1f,Y=200,VX=-360});game.Update(.02);
            Check(game.Dots[1]==1&&!game.HasPending(0)&&game.HasPending(1)&&game.Balls.Contains(survivor),"A lost ball creates a serve only on a half without a live ball");game.Update(1.1);Check(!game.HasPending(1)&&game.Balls.Any(ball=>ball.LastTouch==1&&ball.VX<0),"NPC can serve its replacement after the initial simultaneous launch");
            game=Battle();var wallBall=new BrickBall{X=200,Y=game.Radius+.1f,VX=200,VY=-299.3326f};game.Balls.Add(wallBall);game.Update(.02);Check(wallBall.VY>0&&game.Dots.All(value=>value==0),"Top wall bounces without scoring");Near((float)Math.Sqrt(wallBall.VX*wallBall.VX+wallBall.VY*wallBall.VY),360,.001f,"Wall preserves ball speed");
            wallBall.Y=game.Height-game.Radius-.1f;wallBall.VY=Math.Abs(wallBall.VY);game.Update(.04);Check(wallBall.VY<0,"Bottom wall bounces the ball upward");
            game=Battle();var paddleBall=PaddleBall(game,0,20);game.Update(.01);Check(paddleBall.VX>0&&paddleBall.VY>0&&paddleBall.LastTouch==0,"Paddle bounce uses contact angle and updates the last toucher");
            float collisionTime;bool horizontal;Check(BrickBattle.Sweep(0,5,10000,0,new RectangleF(10,0,2,10),.01f,out collisionTime,out horizontal)&&horizontal&&Math.Abs(collisionTime-.001)<.000001,"Continuous collision sweep catches fast crossings of thin bricks");
            Check(!BrickBattle.Sweep(0,20,10000,0,new RectangleF(10,0,2,10),.01f,out collisionTime,out horizontal),"Sweep rejects a trajectory missing the brick");
            for(int type=0;type<4;type++)
            {
                game=Battle(true,new BrickRandom(.249,type));var target=game.Bricks.Single(brick=>brick.Row==6&&brick.Column==0);var box=game.BrickBounds(target);
                var ball=new BrickBall{X=box.Left-game.Radius-.1f,Y=box.Top+box.Height/2,VX=360,LastTouch=type%2};game.Balls.Add(ball);game.Update(.01);
                Check(target.Broken&&ball.VX<0&&game.Orbs.Count==1&&game.Orbs[0].Power==(BrickPower)(type+1)&&Math.Sign(game.Orbs[0].VX)==(type%2==0?-1:1),"Brick destruction spawns the selected power toward the last touching paddle");
            }
            foreach(bool powers in new[]{false,true})
            {
                game=Battle(powers,new BrickRandom(.25));var target=game.Bricks[0];var box=game.BrickBounds(target);game.Balls.Add(new BrickBall{X=box.Left-game.Radius-.1f,Y=box.Height/2,VX=360});game.Update(.01);
                Check(target.Broken&&game.Orbs.Count==0,powers?"25 percent boundary does not spawn a power":"Power-ups Off prevents drops");
            }
            game=Battle();game.Orbs.Add(new BrickOrb{X=game.PaddleBounds(0).Right+15,Y=game.Paddles[0].Y,VX=-120,Power=BrickPower.Triple});game.Update(.02);Check(game.Paddles[0].Power==BrickPower.Triple&&game.Orbs.Count==0,"Collecting an orb applies its power and removes it");
            game.Orbs.Add(new BrickOrb{X=15,Y=20,VX=-120,Power=BrickPower.Tall});game.Update(.04);Check(game.Orbs.Count==0,"An uncaught orb disappears at the field edge");
            game=Battle();game.Collect(0,BrickPower.Triple);paddleBall=PaddleBall(game,0);game.Update(.01);Check(game.Balls.Count==3&&game.Paddles[0].Power==BrickPower.None,"Blue power creates exactly two extra balls and clears paddle color");
            double originalAngle=Math.Atan2(paddleBall.VY,paddleBall.VX);var angles=game.Balls.Where(ball=>ball!=paddleBall).Select(ball=>Math.Atan2(ball.VY,ball.VX)-originalAngle).OrderBy(value=>value).ToArray();Near((float)(angles[0]*180/Math.PI),-10,.001f,"First clone is ten degrees below");Near((float)(angles[1]*180/Math.PI),10,.001f,"Second clone is ten degrees above");
            foreach(var ball in game.Balls)Near((float)Math.Sqrt(ball.VX*ball.VX+ball.VY*ball.VY),360,.001f,"Triple copies retain constant speed");
            game.Collect(0,BrickPower.Tall);Near(game.PaddleHeight(0),126,.001f,"Yellow adds twenty percent of base height");game.Collect(0,BrickPower.Tall);Near(game.PaddleHeight(0),136.5f,.001f,"Repeat yellow adds ten percent of base height");game.Collect(0,BrickPower.Bomb);Near(game.PaddleHeight(0),105,0,"Another power overwrites yellow and restores base height");
            game=Battle();game.Collect(0,BrickPower.Sticky);paddleBall=PaddleBall(game,0,17);game.Update(.01);Check(paddleBall.HeldBy==0&&paddleBall.StickyHeld&&game.Paddles[0].Power==BrickPower.Sticky,"Green sticks the next ball at its contact point");game.MovePlayer(game.Paddles[0].Y+30,.02);Near(paddleBall.Y,game.Paddles[0].Y+17,.001f,"Stuck contact offset follows mouse movement");game.Click(.03);Check(paddleBall.HeldBy<0&&game.Paddles[0].Power==BrickPower.None,"Click releases the stuck ball and clears green");
            game=Battle();game.Collect(0,BrickPower.Sticky);var firstCatch=PaddleBall(game,0);game.Update(.01);var later=PaddleBall(game,0,8);game.Update(.02);Check(firstCatch.HeldBy==0&&later.HeldBy<0&&later.VX>0,"Green catches only the next ball; subsequent arrivals bounce normally");
            game=Battle();game.Collect(1,BrickPower.Sticky);paddleBall=PaddleBall(game,1);game.Update(.01);Check(paddleBall.HeldBy==1,"NPC also catches sticky balls");game.Update(1);Check(paddleBall.HeldBy<0&&game.Paddles[1].Power==BrickPower.None,"NPC releases sticky balls automatically");
            game=Battle();game.Collect(0,BrickPower.Bomb);paddleBall=PaddleBall(game,0);game.Update(.01);Check(paddleBall.Bomb&&game.BallRadius(paddleBall)==game.Radius*2&&game.Paddles[0].Power==BrickPower.None,"Red charges a double-size explosive ball for the next hit");
            game=Battle();game.Collect(0,BrickPower.Bomb);game.MovePlayer(0,0);paddleBall=PaddleBall(game,0);paddleBall.Y=game.Radius+1;game.Update(.03);Check(paddleBall.Bomb&&paddleBall.Y>=game.BallRadius(paddleBall)&&paddleBall.VY>0,"A newly enlarged ball near the top remains inside and bounces from the wall");
            game=Battle();var blastTarget=game.Bricks.Single(brick=>brick.Row==6&&brick.Column==1);var blastBox=game.BrickBounds(blastTarget);game.Bricks.Single(brick=>brick.Row==6&&brick.Column==0).Broken=true;
            var blast=new BrickBall{X=blastBox.Left-game.Radius*2-.1f,Y=blastBox.Top+blastBox.Height/2,VX=360,Bomb=true};game.Balls.Add(blast);game.Update(.01);
            Check(!double.IsNaN(blast.PulseAt)&&game.Bricks.Count(brick=>brick.Broken)==9,"Explosive hit destroys the brick and all touching neighbors");game.Update(.24);Check(!game.Balls.Contains(blast)&&game.HasPending(0)&&game.HasPending(1),"Explosive pulse disappears and refills empty halves");
            game=Battle();paddleBall=PaddleBall(game,0);paddleBall.Bomb=true;paddleBall.X=game.PaddleBounds(0).Right+game.Radius*2+.1f;game.Update(.01);float frozen=game.Paddles[0].Y;
            Check(!double.IsNaN(paddleBall.PulseAt)&&Math.Abs(game.Paddles[0].FrozenUntil-.51)<.0001,"Explosive paddle hit pulses and freezes for half a second");game.MovePlayer(frozen+80,.3);Check(game.Paddles[0].Y==frozen,"Frozen paddle ignores mouse movement");game.Update(.6);game.MovePlayer(frozen+80,.6);Check(game.Paddles[0].Y==frozen+80,"Paddle resumes tracking after freeze");
            game=Battle();game.Bricks[5].Broken=true;game.Collect(0,BrickPower.Tall);game.Balls.Add(new BrickBall{X=220,Y=550,VX=360});game.Resize(340);Check(game.Bricks[5].Broken&&game.Height==340&&game.PaddleBounds(0).Bottom<=340&&game.Balls.All(ball=>ball.Y>=game.BallRadius(ball)&&ball.Y<=340-game.BallRadius(ball)),"Resizing keeps destroyed bricks, paddle bounds, and ball positions valid");
            BrickBattle easy=new BrickBattle(ArcadeDifficulty.Easy,true,new Random(1)),normal=new BrickBattle(ArcadeDifficulty.Normal,true,new Random(1)),hard=new BrickBattle(ArcadeDifficulty.Hard,true,new Random(1));Check(easy.NpcSpeed<normal.NpcSpeed&&normal.NpcSpeed<hard.NpcSpeed&&easy.NpcReaction>normal.NpcReaction&&normal.NpcReaction>hard.NpcReaction,"Difficulty increases NPC pace and shortens reaction delay");
            // Play complete simulations to catch runaway balls, duplicated goals, and stuck collisions.
            foreach(ArcadeDifficulty difficulty in Enum.GetValues(typeof(ArcadeDifficulty)))
            {
                game=new BrickBattle(difficulty,true,new Random(31));game.Start(0);game.Click(0);bool validBalls=true,validPaddles=true;
                for(int tick=1;tick<=3600&&game.State!=BrickState.Finished;tick++)
                {
                    double time=tick/60.0;game.MovePlayer(game.Height/2+(float)Math.Sin(time*1.7)*game.Height*.3f,time);if(game.HasPending(0)||game.State==BrickState.Pending)game.Click(time);game.Update(time);
                    validBalls&=game.Balls.All(ball=>!float.IsNaN(ball.X)&&!float.IsNaN(ball.Y)&&ball.X>=-1&&ball.X<=1001&&ball.Y>=-1&&ball.Y<=game.Height+1);
                    validPaddles&=game.Dots.All(value=>value>=0&&value<=5)&&game.PaddleBounds(1).Top>=0&&game.PaddleBounds(1).Bottom<=game.Height+.001f;
                }
                Check(validBalls,"Simulated balls remain finite and inside the field at "+difficulty);Check(validPaddles,"Simulation retains valid scoring and NPC bounds at "+difficulty);
            }
        }
    }
}
