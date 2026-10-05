using System;
using System.IO;
using System.Media;

namespace Vpet
{
    internal sealed class ArcadeTones : IDisposable
    {
        readonly MemoryStream[] waves=new MemoryStream[4];
        readonly SoundPlayer[] players=new SoundPlayer[4];
        internal static byte[] CreateWave(ArcadeLane lane,float volume=1)
        {
            const int rate=22050,samples=4410;
            double frequency=new[]{523.25,329.63,392.0,659.25}[(int)lane];
            volume=ToyPreferences.Finite(volume)?Math.Max(0,Math.Min(1,volume)):1;
            using(var output=new MemoryStream())using(var writer=new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                for(int i=0;i<samples;i++)
                {
                    double t=i/(double)rate,envelope=Math.Min(1,t/.006)*Math.Min(1,(samples-1-i)/(rate*.02));
                    writer.Write((short)(Math.Sin(2*Math.PI*frequency*t)*envelope*.22*volume*short.MaxValue));
                }
                return output.ToArray();
            }
        }
        public ArcadeTones(float volume=1)
        {for(int i=0;i<4;i++){waves[i]=new MemoryStream(CreateWave((ArcadeLane)i,volume));players[i]=new SoundPlayer(waves[i]);}}
        public void Play(ArcadeLane lane){try{players[(int)lane].Play();}catch(InvalidOperationException){}catch(TimeoutException){}}
        public void Stop(){foreach(var player in players)player.Stop();}
        public void Dispose(){Stop();for(int i=0;i<4;i++){players[i].Dispose();waves[i].Dispose();}}
    }
}
