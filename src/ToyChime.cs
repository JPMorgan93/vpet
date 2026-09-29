using System;
using System.IO;
using System.Media;

namespace Vpet
{
    // Short instrument sounds generated locally, with no external audio files or network access.
    internal sealed class ToyChime : IDisposable
    {
        readonly MemoryStream[] waves=new MemoryStream[3];
        readonly SoundPlayer[] players=new SoundPlayer[3];
        readonly Func<TriangleSound> selectedSound;
        readonly Func<float> selectedVolume;
        readonly float[] volumes={1,1,1};
        internal static byte[] CreateWave(TriangleSound sound=TriangleSound.Chime,float volume=1)
        {
            volume=ToyPreferences.Finite(volume)?Math.Max(0,Math.Min(1,volume)):1;
            const int rate=22050,samples=11025;
            using(var output=new MemoryStream())using(var writer=new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                var random=new Random(733);double previousNoise=0;
                for(int i=0;i<samples;i++)
                {
                    double t=i/(double)rate,envelope=Math.Min(1,t/.003)*Math.Exp(-t*12)*(1-t/.5);
                    double value=(Math.Sin(2*Math.PI*1760*t)+.45*Math.Sin(2*Math.PI*2491*t)+.2*Math.Sin(2*Math.PI*3913*t))*.22*envelope;
                    if(sound==TriangleSound.Honk)
                    {
                        // Two reed-like tones and their harmonics produce a short toy-horn honk.
                        double phase=2*Math.PI*(330*t+15*.07*(1-Math.Exp(-t/.07)));
                        double hornEnvelope=Math.Min(1,t/.008)*Math.Max(0,Math.Min(1,(.36-t)/.10));
                        value=.18*hornEnvelope*(Math.Sin(phase)+.55*Math.Sin(2*Math.PI*440*t)+.3*Math.Sin(phase*3)+.15*Math.Sin(phase*5));
                    }
                    else if(sound==TriangleSound.SnareDrum)
                    {
                        // A falling drum-head tone under a fast, noisy snare-wire rattle.
                        double noise=random.NextDouble()*2-1,high=(noise-.7*previousNoise)/1.7;previousNoise=noise;
                        double phase=2*Math.PI*(160*t+28*(1-Math.Exp(-40*t))/40);
                        value=Math.Min(1,t/.001)*(1-t/.5)*(.34*high*Math.Exp(-22*t)+.20*Math.Sin(phase)*Math.Exp(-35*t));
                    }
                    writer.Write((short)(Math.Max(-1,Math.Min(1,value))*volume*short.MaxValue));
                }
                return output.ToArray();
            }
        }
        public ToyChime(Func<TriangleSound> selectedSound=null,Func<float> selectedVolume=null)
        {
            this.selectedSound=selectedSound??(()=>TriangleSound.Chime);
            this.selectedVolume=selectedVolume??(()=>1);
            for(int i=0;i<players.Length;i++){waves[i]=new MemoryStream(CreateWave((TriangleSound)i));players[i]=new SoundPlayer(waves[i]);}
        }
        public void Play()
        {Play(selectedSound(),selectedVolume());}
        public void Play(TriangleSound sound,float volume)
        {
            // SoundPlayer plays asynchronously, so rapid clicks never block the desktop UI.
            int index=(int)sound;if(index<0||index>=players.Length)index=0;
            volume=ToyPreferences.Finite(volume)?Math.Max(0,Math.Min(1,volume)):1;
            if(volumes[index]!=volume)
            {players[index].Stop();players[index].Dispose();waves[index].Dispose();waves[index]=new MemoryStream(CreateWave((TriangleSound)index,volume));players[index]=new SoundPlayer(waves[index]);volumes[index]=volume;}
            try{players[index].Play();}catch(InvalidOperationException){}catch(TimeoutException){}
        }
        public void Dispose(){for(int i=0;i<players.Length;i++){players[i].Stop();players[i].Dispose();waves[i].Dispose();}}
    }
}
