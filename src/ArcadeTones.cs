using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Vpet
{
    internal sealed class ArcadeTones : IDisposable
    {
        readonly byte[][] samples;
        readonly GCHandle[] pinned;
        readonly ArcadeVoice[] voices=new ArcadeVoice[16];
        ArcadeAudioDevice device;
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
                    double t=i/(double)rate,attack=Math.Min(1,t/.012),release=Math.Min(1,(samples-1-i)/(rate*.035));
                    double envelope=(.5-.5*Math.Cos(Math.PI*attack))*(.5-.5*Math.Cos(Math.PI*release));
                    writer.Write((short)(Math.Sin(2*Math.PI*frequency*t)*envelope*.22*volume*short.MaxValue));
                }
                return output.ToArray();
            }
        }
        public ArcadeTones(float volume=1) : this(new[]{CreateWave(ArcadeLane.Up,volume),CreateWave(ArcadeLane.Down,volume),CreateWave(ArcadeLane.Left,volume),CreateWave(ArcadeLane.Right,volume)}){}
        internal ArcadeTones(byte[][] waves)
        {
            samples=new byte[waves.Length][];pinned=new GCHandle[waves.Length];
            for(int i=0;i<waves.Length;i++){samples[i]=new byte[waves[i].Length-44];System.Buffer.BlockCopy(waves[i],44,samples[i],0,samples[i].Length);}
        }
        internal void Prepare()
        {
            if(device!=null)return;
            try
            {
                device=new ArcadeAudioDevice();var format=new ArcadeAudioNative.WaveFormat{Tag=1,Channels=1,Rate=22050,BytesPerSecond=44100,BlockAlign=2,Bits=16};
                for(int i=0;i<samples.Length;i++)pinned[i]=GCHandle.Alloc(samples[i],GCHandleType.Pinned);
                for(int i=0;i<voices.Length;i++)voices[i]=device.Create(format);
            }
            catch{Dispose();throw;}
        }
        internal int ActiveVoices {get{int count=0;foreach(var voice in voices)if(voice!=null&&voice.State.Queued>0)count++;return count;}}
        public void Play(ArcadeLane lane)
        {Play((int)lane);}
        internal void Play(int index)
        {
            try
            {
                Prepare();foreach(var voice in voices)if(voice.State.Queued==0)
                {voice.Flush();voice.Submit(pinned[index].AddrOfPinnedObject(),samples[index].Length);voice.Play();return;}
            }
            catch(IOException){}catch(COMException){}
        }
        public void Stop(){foreach(var voice in voices)if(voice!=null)voice.Flush();}
        public void Dispose(){for(int i=0;i<voices.Length;i++)if(voices[i]!=null){voices[i].Dispose();voices[i]=null;}for(int i=0;i<pinned.Length;i++)if(pinned[i].IsAllocated)pinned[i].Free();if(device!=null){device.Dispose();device=null;}}
    }
}
