using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Vpet
{
    internal static partial class Tests
    {
        static double ToneFrequency(byte[] wave,double start,double end)
        {
            int first=-1,last=-1,cycles=0;
            for(int i=(int)(start*22050);i<(int)(end*22050);i++)
                if(BitConverter.ToInt16(wave,44+i*2)>0&&BitConverter.ToInt16(wave,44+(i-1)*2)<=0)
                {if(first<0)first=i;last=i;cycles++;}
            return cycles>1?(cycles-1)*22050.0/(last-first):0;
        }
        static void BrickAudioFeatures()
        {
            foreach(BrickSound sound in Enum.GetValues(typeof(BrickSound)))
            {
                byte[] wave=BrickBattleSounds.CreateWave(sound),muted=BrickBattleSounds.CreateWave(sound,0),half=BrickBattleSounds.CreateWave(sound,.5f);
                Check(BitConverter.ToInt32(wave,4)==wave.Length-8&&BitConverter.ToInt32(wave,40)==wave.Length-44&&BitConverter.ToInt32(wave,24)==22050&&BitConverter.ToInt16(wave,22)==1&&BitConverter.ToInt16(wave,34)==16,"Brick "+sound+" has a complete mono PCM wave");
                int peak=0,jump=0;
                for(int i=44;i<wave.Length;i+=2)
                {
                    short sample=BitConverter.ToInt16(wave,i);peak=Math.Max(peak,Math.Abs((int)sample));
                    if(i>44)jump=Math.Max(jump,Math.Abs(sample-BitConverter.ToInt16(wave,i-2)));
                }
                Check(peak>2000&&peak<4700,"Brick "+sound+" is audible at a gentle level below fifteen percent peak");
                Check(BitConverter.ToInt16(wave,44)==0&&BitConverter.ToInt16(wave,wave.Length-2)==0&&jump<950,"Brick "+sound+" starts and ends at silence without a sharp sample discontinuity");
                Check(muted.Skip(44).All(value=>value==0),"Brick "+sound+" supports exactly silent playback for native verification");
                bool scaled=true;for(int i=44;i<wave.Length;i+=2)scaled&=Math.Abs(BitConverter.ToInt16(half,i)-BitConverter.ToInt16(wave,i)/2.0)<=1;
                Check(scaled,"Brick "+sound+" gain scales every sample evenly");
                double duration=(wave.Length-44)/44100.0;
                Check(sound==BrickSound.PowerUp?duration>.29&&duration<.30:duration>.08&&duration<.13,"Brick "+sound+" remains a short sound cue");
            }
            byte[] paddle=BrickBattleSounds.CreateWave(BrickSound.Paddle),brick=BrickBattleSounds.CreateWave(BrickSound.Brick),chime=BrickBattleSounds.CreateWave(BrickSound.PowerUp);
            Check(Math.Abs(ToneFrequency(paddle,.02,.06)-784)<3,"Paddle hit is a light high tone");
            Check(Math.Abs(ToneFrequency(brick,.02,.06)-261.63)<3,"Brick hit is a distinctly lower tone");
            Check(Math.Abs(ToneFrequency(chime,.02,.08)-659.25)<3&&Math.Abs(ToneFrequency(chime,.175,.235)-987.77)<3,"Power-up chime plays two distinct ascending notes");
            int noteSamples=(int)(.14*22050),gapSamples=(int)(.015*22050);
            Check(chime.Skip(44+noteSamples*2).Take(gapSamples*2).All(value=>value==0)&&BitConverter.ToInt16(chime,44+(noteSamples-1)*2)==0&&BitConverter.ToInt16(chime,44+(noteSamples+gapSamples)*2)==0,"Both chime notes fade through a silent gap");

            foreach(int side in new[]{0,1})foreach(BrickPower power in new[]{BrickPower.None,BrickPower.Sticky,BrickPower.Bomb})
            {
                var game=Battle();var cues=new List<BrickSound>();game.SoundPlayed+=cues.Add;
                if(power!=BrickPower.None)game.Collect(side,power);cues.Clear();
                var ball=PaddleBall(game,side);game.Update(.01);game.Update(.02);
                Check(cues.SequenceEqual(new[]{BrickSound.Paddle}),"Exactly one paddle cue on side "+side+" with "+power);
                Check(power!=BrickPower.Sticky||ball.HeldBy==side,"Sticky audio does not change the catch");
            }
            foreach(bool bomb in new[]{false,true})
            {
                var game=Battle();var cues=new List<BrickSound>();game.SoundPlayed+=cues.Add;
                var target=game.Bricks.Single(item=>item.Row==6&&item.Column==1);game.Bricks.Single(item=>item.Row==6&&item.Column==0).Broken=true;
                var box=game.BrickBounds(target);game.Balls.Add(new BrickBall{X=box.Left-game.Radius*(bomb?2:1)-.1f,Y=box.Top+box.Height/2,VX=360,Bomb=bomb});game.Update(.01);game.Update(.02);
                Check(cues.SequenceEqual(new[]{BrickSound.Brick}),bomb?"Explosive neighbor destruction plays one brick impact cue":"A brick hit plays one lower cue and does not repeat after bouncing");
                Check(game.Bricks.Count(item=>item.Broken)==(bomb?9:2),"Brick audio preserves normal and explosive destruction");
            }
            foreach(int side in new[]{0,1})foreach(BrickPower power in new[]{BrickPower.Triple,BrickPower.Tall,BrickPower.Sticky,BrickPower.Bomb})
            {
                var game=Battle();var cues=new List<BrickSound>();game.SoundPlayed+=cues.Add;
                var box=game.PaddleBounds(side);game.Orbs.Add(new BrickOrb{X=box.Left+box.Width/2,Y=game.Paddles[side].Y,VX=side==0?-120:120,Power=power});game.Update(.01);game.Update(.02);
                Check(cues.SequenceEqual(new[]{BrickSound.PowerUp})&&game.Orbs.Count==0&&game.Paddles[side].Power==power,"Exactly one pickup chime for "+power+" on side "+side);
            }
            var quiet=Battle();var sounds=new List<BrickSound>();quiet.SoundPlayed+=sounds.Add;
            quiet.Balls.Add(new BrickBall{X=200,Y=quiet.Radius+.1f,VX=200,VY=-299.3326f});quiet.Orbs.Add(new BrickOrb{X=15,Y=20,VX=-120,Power=BrickPower.Tall});quiet.Update(.02);
            double now=.02;BattleGoal(quiet,0,ref now);quiet.Resize(500);quiet.Stop(now);quiet.Start(now);quiet.Click(now);quiet.Collect(0,BrickPower.None);
            Check(sounds.Count==0,"Walls, goals, missed orbs, serves, resize, stop, and effect clearing do not play collision cues");
            quiet=Battle();quiet.SoundPlayed+=sounds.Add;PaddleBall(quiet,0,-20);PaddleBall(quiet,0,20);quiet.Update(.01);
            Check(sounds.SequenceEqual(new[]{BrickSound.Paddle,BrickSound.Paddle}),"Two real simultaneous paddle impacts each request their own sound");
            quiet=Battle(true,new BrickRandom(.1));sounds.Clear();quiet.SoundPlayed+=sounds.Add;
            var first=quiet.Bricks[0];var bounds=quiet.BrickBounds(first);quiet.Balls.Add(new BrickBall{X=bounds.Left-quiet.Radius-.1f,Y=bounds.Height/2,VX=360});quiet.Update(.01);
            Check(quiet.Orbs.Count==1&&sounds.SequenceEqual(new[]{BrickSound.Brick}),"Spawning an orb has no pickup chime before it reaches a paddle");
        }
        static void PreparedBrickAudioTests()
        {
            using(var sounds=new BrickBattleSounds(0))
            {
                sounds.Prepare();Check(sounds.ActiveVoices==0,"All Brick cues are prepared before play and initially silent");
                sounds.Play(BrickSound.Paddle);sounds.Play(BrickSound.Brick);sounds.Play(BrickSound.PowerUp);sounds.Play(BrickSound.PowerUp);
                Check(sounds.ActiveVoices==4,"Repeated and distinct Brick cues overlap in prepared native voices");
                Thread.Sleep(350);Check(sounds.ActiveVoices==0,"All Brick cues finish their fade naturally");
                for(int i=0;i<16;i++)sounds.Play(BrickSound.PowerUp);sounds.Play(BrickSound.Brick);
                Check(sounds.ActiveVoices==16,"Sound pool bounds rapid multiball playback without cutting off existing cues");
                GC.Collect();GC.WaitForPendingFinalizers();Check(sounds.ActiveVoices>0,"Pinned cue buffers survive garbage collection during playback");
                sounds.Stop();Thread.Sleep(30);Check(sounds.ActiveVoices==0,"Stopping cancels every queued Brick cue");
                sounds.Play(BrickSound.Brick);Thread.Sleep(180);Check(sounds.ActiveVoices==0,"Prepared cues can replay after Stop");
                sounds.Dispose();Check(sounds.ActiveVoices==0,"Disposal releases every native Brick voice safely");
            }
        }
    }
}
