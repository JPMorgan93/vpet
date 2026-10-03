using System;
using System.IO;
using System.Media;

namespace Vpet
{
    // Three separate notes, independent of the triangle's selected instrument and volume.
    internal sealed class ReminderChime : IDisposable
    {
        readonly MemoryStream wave;
        readonly SoundPlayer player;
        public ReminderChime(){wave=new MemoryStream(CreateWave());player=new SoundPlayer(wave);}
        internal static byte[] CreateWave()
        {
            const int rate=22050,noteSamples=5292,toneSamples=3969,samples=noteSamples*3;
            double[] notes={523.25,659.25,783.99};
            using(var output=new MemoryStream())using(var writer=new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                for(int i=0;i<samples;i++)
                {
                    int within=i%noteSamples;double t=within/(double)rate;
                    double envelope=within<toneSamples?Math.Min(1,t/.006)*Math.Min(1,(toneSamples-within)/(rate*.02))*Math.Exp(-t*8):0;
                    writer.Write((short)(Math.Sin(2*Math.PI*notes[i/noteSamples]*t)*envelope*.28*short.MaxValue));
                }
                return output.ToArray();
            }
        }
        public void Play(){try{player.Play();}catch(InvalidOperationException){}catch(TimeoutException){}}
        public void Dispose(){player.Stop();player.Dispose();wave.Dispose();}
    }
}
