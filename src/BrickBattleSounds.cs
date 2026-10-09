using System;
using System.IO;

namespace Vpet
{
    internal enum BrickSound { Paddle, Brick, PowerUp }
    internal sealed class BrickBattleSounds : IDisposable
    {
        readonly ArcadeTones tones;
        internal static byte[] CreateWave(BrickSound sound,float volume=1)
        {
            const int rate=22050;
            double[] frequencies=sound==BrickSound.Paddle?new[]{784.0}:sound==BrickSound.Brick?new[]{261.63}:new[]{659.25,987.77};
            double duration=sound==BrickSound.Paddle?.085:sound==BrickSound.Brick?.12:.14;
            int noteSamples=(int)(duration*rate),gapSamples=frequencies.Length==2?(int)(.015*rate):0;
            int count=noteSamples*frequencies.Length+gapSamples;
            volume=ToyPreferences.Finite(volume)?Math.Max(0,Math.Min(1,volume)):1;
            double gain=(sound==BrickSound.Brick?.14:.10)*volume;
            using(var output=new MemoryStream())using(var writer=new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int note=0;note<frequencies.Length;note++)
                {
                    if(note>0)for(int i=0;i<gapSamples;i++)writer.Write((short)0);
                    for(int i=0;i<noteSamples;i++)
                    {
                        double t=i/(double)rate,attack=Math.Min(1,t/.006),release=Math.Min(1,(noteSamples-1-i)/(rate*.024));
                        double envelope=(.5-.5*Math.Cos(Math.PI*attack))*(.5-.5*Math.Cos(Math.PI*release));
                        writer.Write((short)(Math.Sin(2*Math.PI*frequencies[note]*t)*envelope*gain*short.MaxValue));
                    }
                }
                return output.ToArray();
            }
        }
        internal BrickBattleSounds(float volume=1)
        {tones=new ArcadeTones(new[]{CreateWave(BrickSound.Paddle,volume),CreateWave(BrickSound.Brick,volume),CreateWave(BrickSound.PowerUp,volume)});}
        internal void Prepare(){tones.Prepare();}
        internal int ActiveVoices {get{return tones.ActiveVoices;}}
        internal void Play(BrickSound sound){tones.Play((int)sound);}
        internal void Stop(){tones.Stop();}
        public void Dispose(){tones.Dispose();}
    }
}
