using System;
using System.IO;
using System.Media;

namespace Vpet
{
    // A short, quiet metallic chime, generated locally with no external audio files or network access.
    internal sealed class ToyChime : IDisposable
    {
        readonly MemoryStream wave;
        readonly SoundPlayer player;
        internal static byte[] CreateWave()
        {
            const int rate=22050,samples=11025;
            using(var output=new MemoryStream())using(var writer=new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                for(int i=0;i<samples;i++)
                {
                    double t=i/(double)rate,envelope=Math.Min(1,t/.003)*Math.Exp(-t*12)*(1-t/.5);
                    double value=(Math.Sin(2*Math.PI*1760*t)+.45*Math.Sin(2*Math.PI*2491*t)+.2*Math.Sin(2*Math.PI*3913*t))*.22*envelope;
                    writer.Write((short)(value*short.MaxValue));
                }
                return output.ToArray();
            }
        }
        public ToyChime(){wave=new MemoryStream(CreateWave());player=new SoundPlayer(wave);}
        public void Play()
        {
            // SoundPlayer plays asynchronously, so rapid clicks never block the desktop UI.
            try{player.Play();}catch(InvalidOperationException){}catch(TimeoutException){}
        }
        public void Dispose(){player.Stop();player.Dispose();wave.Dispose();}
    }
}
