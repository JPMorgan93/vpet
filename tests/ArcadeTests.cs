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
        static void ArcadeFeatures()
        {
            string path=Path.Combine(artifacts,"arcade-settings.json");File.WriteAllText(path,"{}");var prefs=Preferences.Load(path);
            Check(!prefs.Joystick.Visible&&prefs.Arcade.DanceHigh.All(x=>x==0)&&prefs.Arcade.SimonHigh.All(x=>x==0)&&!prefs.Arcade.ArrowKeys,"Older preferences default to no joystick, zero high scores, and WASD");
            prefs.Joystick.Visible=true;prefs.Joystick.X=-220;prefs.Joystick.Y=320;prefs.Arcade.Record(ArcadeGame.Dance,ArcadeDifficulty.Hard,1930);prefs.Arcade.Record(ArcadeGame.Simon,ArcadeDifficulty.Normal,250);prefs.Arcade.Volume=25;prefs.Arcade.ArrowKeys=true;prefs.Save(path);var restored=Preferences.Load(path);
            Check(restored.Joystick.Visible&&restored.Joystick.X==-220&&restored.Arcade.High(ArcadeGame.Dance,ArcadeDifficulty.Hard)==1930&&restored.Arcade.High(ArcadeGame.Simon,ArcadeDifficulty.Normal)==250&&restored.Arcade.Volume==25&&restored.Arcade.ArrowKeys,"Joystick and separate game/difficulty scores persist with music and key controls");
            Check(!prefs.Arcade.Record(ArcadeGame.Dance,ArcadeDifficulty.Hard,1900)&&prefs.Arcade.High(ArcadeGame.Dance,ArcadeDifficulty.Easy)==0,"Lower scores and other difficulties do not overwrite a high score");
            prefs.Arcade.DanceHigh=new long[]{-1,70};prefs.Arcade.SimonHigh=null;prefs.Arcade.Volume=140;prefs.Validate();Check(prefs.Arcade.DanceHigh.SequenceEqual(new long[]{0,70,0})&&prefs.Arcade.SimonHigh.Length==3&&prefs.Arcade.Volume==100,"Invalid/older score arrays migrate safely");
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                var pet=Pet(mode);pet.Settings.Speed=0;var toys=Toys(pet);toys.SetJoystickVisible(true,0);toys.DragJoystick(new PointF(850,600));
                Check(toys.JoystickPosition==new PointF(850,600),"Joystick ignores the pet/play fences in "+mode);
                int opens=0;toys.ArcadeRequested+=delegate{opens++;};toys.PressJoystick(0);double now=0;Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Arcade);
                Check(opens==1&&pet.Playing&&Geometry.Distance(pet.Position,toys.JoystickApproach)<=1&&!pet.Walking,"Pet approaches behind the joystick before entering Arcade in "+mode);
                var position=pet.Position;for(int i=0;i<10;i++){now+=.05;ToyStep(toys,pet,now,.05f);}Check(pet.Position==position&&opens==1,"Arcade holds desktop route and never opens twice");
                toys.LeaveArcade(now);Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.None);Check(!pet.Playing&&pet.Settings.Movement==mode&&pet.Settings.Speed==0&&(mode!=MovementMode.Restricted||pet.InsideRestriction(pet.Position)),"Arcade exit returns to unchanged movement rules in "+mode);
                toys.PressJoystick(now);toys.SetJoystickVisible(false,now);Check(!toys.HasJoystick&&toys.Fetch!=FetchPhase.Approaching,"Removing joystick cancels its approach");
            }
            var crossingPet=Pet(MovementMode.Static);crossingPet.SetDisplays(new System.Collections.Generic.List<DisplayArea>{new DisplayArea("one",new Rectangle(0,0,1000,760),1),new DisplayArea("two",new Rectangle(-1000,120,900,700),1.5f)});
            var crossToys=Toys(crossingPet);crossToys.SetJoystickVisible(true,0);crossToys.DragJoystick(new PointF(-500,600));crossToys.PressJoystick(0);double travel=0;Until(crossToys,crossingPet,ref travel,()=>crossToys.Fetch==FetchPhase.Arcade,60);
            Check(crossingPet.CurrentDisplay=="two","Joystick visit crosses separated displays before opening Arcade");
            crossingPet.SetDisplays(new System.Collections.Generic.List<DisplayArea>{crossingPet.Displays[0]});crossToys.RecoverDisplays();Check(crossToys.JoystickDisplayId=="one"&&crossingPet.Current.Work.Contains(Point.Round(crossToys.JoystickPosition)),"Joystick recovers from a disconnected monitor even in Arcade");
            Check(DanceGame.Points(.009)==0&&DanceGame.Points(.01)==10&&DanceGame.Points(.499)==10&&DanceGame.Points(.5)==20&&DanceGame.Points(.899)==20&&DanceGame.Points(.9)==30,"Dance scoring honors 1, 50, and 90 percent overlap boundaries");
            Check(DanceGame.Overlap(136)==1&&DanceGame.Overlap(194)==0&&Math.Abs(DanceGame.Overlap(165)-.5)<1e-6,"Dance overlap measures matching square intersection");
            var notes=Enumerable.Range(0,7).Select(i=>new DanceTarget(ArcadeLane.Up,4+i)).ToArray();var dance=new DanceGame(ArcadeDifficulty.Easy,20,notes);int music=0;dance.MusicStarted+=delegate{music++;};dance.Start(10);dance.Update(12.99);Check(dance.State==DanceState.Countdown&&dance.Countdown(12.1)==1&&music==0,"Dance countdown lasts all three seconds");dance.Update(13);Check(dance.State==DanceState.Running&&music==1,"Music starts once after countdown");
            for(int i=0;i<6;i++){dance.Update(17+i,4+i);dance.Press(ArcadeLane.Up,17+i);if(i==2)Check(dance.Streak&&dance.Multiplier==1&&dance.Pending==60&&dance.StreakScore==30,"Third consecutive Excellent starts a 1.0x streak with its own points");}
            Check(dance.Pending==60&&dance.StreakScore==120&&dance.Multiplier==1.3&&dance.Banked==0,"Sixth Excellent increases the separate streak score and bonus to 1.3x");dance.Press(ArcadeLane.Left,23);Check(dance.Misses==1&&dance.Banked==216&&dance.Pending==0&&dance.StreakScore==0&&dance.Multiplier==1,"Miss multiplies only streak points, banks calculated points, and resets the streak");
            dance.Update(33,20);Check(dance.State==DanceState.Success&&dance.Banked==216,"Surviving the whole song preserves banked score");
            foreach(ArcadeDifficulty difficulty in Enum.GetValues(typeof(ArcadeDifficulty)))
            {
                var game=new DanceGame(difficulty,30,new[]{new DanceTarget(ArcadeLane.Left,5)});game.Start(0);game.Update(3);Check(game.Lives==3-(int)difficulty&&game.Speed==160*(1+(int)difficulty*.5),"Dance lives and target pace match "+difficulty);
                game.Update(8,5);game.Press(ArcadeLane.Left,8);Check(game.Pending==30,"Perfect target earns 30 at "+difficulty);
                for(int miss=0;miss<game.Lives;miss++)game.Press(ArcadeLane.Right,8);
                Check(game.State==DanceState.Failed&&game.Banked==0&&game.Pending==0&&!game.VisibleTargets.Any(),"Final miss immediately clears targets and discards failed score at "+difficulty);
                game.Start(9);Check(game.State==DanceState.Countdown&&game.Misses==0&&game.Pending==0&&game.Chart.All(t=>!t.Resolved),"Restart resets all Dance round state");
                var chart=new DanceGame(difficulty,100);Check(chart.Chart.Count>0&&chart.Chart.All(t=>t.HitTime<99.15),"Targets finish before song end at "+difficulty);
            }
            var missed=new DanceGame(ArcadeDifficulty.Easy,15,new[]{new DanceTarget(ArcadeLane.Down,4)});missed.Start(0);missed.Update(3);missed.Update(7.6,4.6);Check(missed.Misses==1&&missed.Chart[0].Resolved,"An unhit target reaching the pet counts one miss");missed.Update(8,5);Check(missed.Misses==1,"A disappeared target never counts twice");
            var streak=new DanceGame(ArcadeDifficulty.Easy,30,new[]{new DanceTarget(ArcadeLane.Up,4),new DanceTarget(ArcadeLane.Down,5)});streak.Start(0);streak.Update(3);streak.Update(7,4);streak.Press(ArcadeLane.Up,7);streak.Update(8,5-.5*58/160);streak.Press(ArcadeLane.Down,8);Check(streak.Pending==50&&streak.ExcellentRun==0,"Great earns 20 and breaks consecutive Excellents");streak.Update(40,30);Check(streak.State==DanceState.Success&&streak.Banked==50&&streak.Pending==0,"Success banks remaining pending points exactly once");streak.Update(41,31);Check(streak.Banked==50,"Repeated result ticks do not add score twice");
            foreach(ArcadeDifficulty difficulty in Enum.GetValues(typeof(ArcadeDifficulty)))
            {
                int initial=new[]{1,3,5}[(int)difficulty];
                var game=new SimonGame(difficulty,new Random(21));game.Start(0);game.Update(2.9);Check(game.State==SimonState.Countdown,"Simon gives a three-second countdown");game.Update(3);
                Check(game.State==SimonState.Showing&&game.Sequence.Count==initial,"Simon starts with "+initial+" buttons at "+difficulty);
                Check(game.Lit(3.01)==game.Sequence[0]&&game.Lit(3+game.ShowStep*.8)==null,"Simon lights the indicated square then leaves a gap");
                double now=3+initial*game.ShowStep;game.Update(now);Check(game.State==SimonState.Replaying,"Simon waits until the entire starting sequence is displayed");
                for(int i=0;i<initial;i++)game.Press(game.Sequence[i],now+.1+i*.1);
                double final=now+initial*.1;
                Check(game.Pending==50&&game.Sequence.Count==initial&&game.State==SimonState.Waiting,"Completed combination earns 50 and waits before extending sequence");
                Check(game.Lit(final)==game.Sequence.Last()&&game.Lit(final+.179)==game.Sequence.Last(),"Final correct input retains its full highlight");
                game.Update(final+1.179);Check(game.State==SimonState.Waiting&&!game.Lit(final+1.179).HasValue,"No next highlight until the final flash and full one-second rest finish");
                now=final+1.18;game.Update(now);Check(game.Sequence.Count==initial+1&&game.State==SimonState.Showing&&game.Lit(now)==game.Sequence[0],"Next sequence grows by one after the one-second rest");
                now+=(initial+1)*game.ShowStep;game.Update(now);for(int i=0;i<initial+1;i++)game.Press(game.Sequence[i],now+.1+i*.1);
                Check(game.Pending==100&&game.State==SimonState.Waiting,"Simon scores 50 per completed sequence without a multiplier");
                now+=(initial+1)*.1+1.18;game.Update(now);Check(game.Sequence.Count==initial+2,"Third Simon sequence grows after the wait");now+=(initial+2)*game.ShowStep;game.Update(now);game.Press((ArcadeLane)(((int)game.Sequence[0]+1)%4),now+.1);
                Check(game.State==SimonState.Finished&&game.Banked==100&&game.Pending==0,"Wrong Simon key banks the completed rounds at "+difficulty);
                game.Start(now+1);game.Update(now+4);game.Update(now+4+initial*game.ShowStep);double expiry=now+4+initial*game.ShowStep+5;game.Update(expiry-.01);
                Check(game.State==SimonState.Replaying,"Simon allows the full five-second replay window");game.Update(expiry);Check(game.State==SimonState.Finished&&game.Banked==0,"Simon timeout ends without incomplete-round points");
            }
            Check(new SimonGame(ArcadeDifficulty.Easy,new Random()).ShowStep>new SimonGame(ArcadeDifficulty.Normal,new Random()).ShowStep&&new SimonGame(ArcadeDifficulty.Normal,new Random()).ShowStep>new SimonGame(ArcadeDifficulty.Hard,new Random()).ShowStep,"Simon display speed increases across difficulties");
            Check(ArcadeWindow.LaneFor(Keys.W,false)==ArcadeLane.Up&&ArcadeWindow.LaneFor(Keys.Left,true)==ArcadeLane.Left&&ArcadeWindow.LaneFor(Keys.W,true)==null&&ArcadeWindow.LaneFor(Keys.Up,false)==null,"WASD/arrow switching maps directions exclusively");
            ArcadeAdjustmentChecks();
            PracticeFeatures();
        }
    }
}
