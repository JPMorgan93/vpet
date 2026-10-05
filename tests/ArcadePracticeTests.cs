using System;
using System.IO;
using System.Linq;

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
    }
}
