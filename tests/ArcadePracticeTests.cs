using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void PracticeFeatures()
        {
            string path=Path.Combine(artifacts,"practice-preferences.json");File.WriteAllText(path,"{}");var settings=Preferences.Load(path);
            Check(!settings.Arcade.Practice,"Existing preferences default to scored Dance Time");
            settings.Arcade.Practice=true;settings.Arcade.Record(ArcadeGame.Dance,ArcadeDifficulty.Hard,320);settings.Save(path);
            settings=Preferences.Load(path);Check(settings.Arcade.Practice&&settings.Arcade.High(ArcadeGame.Dance,ArcadeDifficulty.Hard)==320,"Practice choice persists independently of existing high scores");
            foreach(ArcadeDifficulty difficulty in Enum.GetValues(typeof(ArcadeDifficulty)))
            {
                var game=new DanceGame(difficulty,30,Enumerable.Range(0,12).Select(i=>new DanceTarget(ArcadeLane.Up,4+i)),true);
                int plays=0;game.MusicStarted+=()=>plays++;game.Start(0);game.Update(2.999);Check(game.State==DanceState.Countdown&&plays==0,"Practice retains the full countdown on "+difficulty);game.Update(3);
                for(int i=0;i<9;i++){game.Update(7+i,4+i);game.Press(ArcadeLane.Up,7+i);}
                Check(game.Practice&&game.State==DanceState.Running&&plays==1&&game.Pending==0&&game.StreakScore==0&&game.Banked==0&&!game.Streak&&game.Chart.Take(9).All(t=>t.Resolved),"Practice hits resolve normally without points, streak bonuses, or score on "+difficulty);
                for(int i=0;i<20;i++)game.Press(ArcadeLane.Left,16);
                Check(game.Misses==20&&game.State==DanceState.Running,"Practice counts unlimited mistimed presses without failing on "+difficulty);
                game.Update(17,13+86/game.Speed+.001);int misses=game.Misses;game.Update(17.01,game.Elapsed);
                Check(misses>=21&&game.Misses==misses&&game.State==DanceState.Running,"Unhit practice targets count once without stopping play on "+difficulty);
                game.Update(40,30);int finalMisses=game.Misses;game.Press(ArcadeLane.Left,41);game.Update(41,31);
                Check(game.State==DanceState.Success&&game.Banked==0&&game.Pending==0&&game.StreakScore==0&&game.Misses==finalMisses,"Practice finishes the song without score or late input on "+difficulty);
                game.Start(50);Check(game.Misses==0&&game.State==DanceState.Countdown,"Practice replay resets its miss count on "+difficulty);game.Stop(51);game.Update(60);
                Check(game.State==DanceState.Stopped&&plays==1,"Practice Stop cancels the countdown on "+difficulty);
                var scored=new DanceGame(difficulty,30);scored.Start(0);scored.Update(3);for(int i=0;i<scored.Lives;i++)scored.Press(ArcadeLane.Left,4);
                Check(!scored.Practice&&scored.State==DanceState.Failed,"Returning to standard mode restores the difficulty's miss limit on "+difficulty);
            }
        }
        static void PracticeWindows(PetWindow pet,ArcadeWindow arcade,ref double now,ref double songTime,Keys[] keys)
        {
            var option=MakerField<CheckBox>(arcade,"practice");var modes=MakerField<RadioButton[]>(arcade,"modes");var difficulty=MakerField<FlowLayoutPanel>(arcade,"difficulties");
            var score=MakerField<Button>(arcade,"score");var calculated=MakerField<Label>(arcade,"calculated");long[] high=(long[])pet.Model.Settings.Arcade.DanceHigh.Clone();
            Check(option.Visible&&option.Enabled&&option.Top>=difficulty.Bottom,"Practice mode is available directly below Difficulty");
            using(var counters=ArcadeRegion(arcade,new Rectangle(75,68,125,32)))
            {
                option.Checked=true;modes[2].Checked=true;WaitForDance(arcade);Check(arcade.Dance.Practice&&!score.Visible&&!calculated.Visible,"Practice hides scoring and rebuilds the selected difficulty without scoring");
                using(var total=ArcadeRegion(arcade,new Rectangle(75,68,125,32)))Check(ArcadeDifferentPixels(counters,total)>50,"Practice replaces the miss-limit circles with a numerical count");
            }
            Check(Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.Practice,"Practice selection is saved immediately");
            songTime=0;FindButton(arcade,"Start").PerformClick();Check(!option.Enabled,"Practice choice is locked during a round");now+=3.01;arcade.Step();
            foreach(var target in arcade.Dance.Chart.Take(6)){songTime=target.HitTime;now+=.1;ArcadeKey(arcade,keys[(int)target.Lane]);ArcadeKeyUp(arcade,keys[(int)target.Lane]);}
            Check(arcade.Dance.Pending==0&&arcade.Dance.Banked==0&&arcade.Dance.StreakScore==0&&!arcade.Dance.Streak,"Practice hits give visual feedback without any score or combo");
            songTime=arcade.Dance.Chart[5].HitTime+.3;now+=.1;arcade.Step();int before=arcade.Dance.Misses;
            for(int i=0;i<8;i++){ArcadeKey(arcade,keys[0]);ArcadeKeyUp(arcade,keys[0]);}
            Check(arcade.Dance.Misses>=before+8&&arcade.Dance.State==DanceState.Running&&FindButton(arcade,"Stop").Visible,"Repeated practice misses keep the Hard song running beyond its normal single-miss limit");
            arcade.ClientSize=new Size(700,590);Application.DoEvents();Check(option.Parent.ClientRectangle.Contains(option.Bounds),"Practice option fits the minimum arcade window size");CaptureForm(arcade,"dance-practice-minimum");arcade.ClientSize=new Size(1040,790);
            songTime=arcade.Dance.Duration;now+=.1;arcade.Step();
            Check(arcade.Dance.State==DanceState.Success&&arcade.Dance.Banked==0&&pet.Model.Settings.Arcade.DanceHigh.SequenceEqual(high),"Practice completes without adding or replacing any high scores");CaptureForm(arcade,"dance-practice-complete");
            FindButton(arcade,"Start").PerformClick();Check(arcade.Dance.Misses==0,"Practice replay clears the displayed miss count");FindButton(arcade,"Stop").PerformClick();
            option.Checked=false;Check(!arcade.Dance.Practice&&score.Visible&&calculated.Visible,"Turning practice off restores scored mode and its scoreboard");
            Check(!Preferences.Load(Path.Combine(pet.DataDirectory,"settings.json")).Arcade.Practice,"Turning practice off persists for future sessions");
        }
    }
}
