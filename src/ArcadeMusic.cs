using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Vpet
{
    internal sealed class ArcadeMusic : IDisposable
    {
        readonly Dictionary<string,Task<ArcadeClip>> prepared=new Dictionary<string,Task<ArcadeClip>>(StringComparer.OrdinalIgnoreCase);
        readonly CancellationTokenSource cancellation=new CancellationTokenSource();
        ArcadeAudioDevice device;ArcadeVoice voice;ArcadeClip clip;GCHandle pinned;
        int requestedVolume;bool playing,primed,disposed;ulong startSamples;double stoppedPosition;
        public double Duration {get{return clip==null?0:clip.Duration;}}
        public bool Opened {get{return voice!=null;}}
        internal Task<ArcadeClip> Prepare(string path)
        {
            if(disposed)throw new ObjectDisposedException("ArcadeMusic");path=Path.GetFullPath(path);Task<ArcadeClip> task;
            if(!prepared.TryGetValue(path,out task))
            {string song=path;var token=cancellation.Token;task=Task.Run(()=>ArcadeClip.Decode(song,token),token);prepared.Add(path,task);}
            return task;
        }
        internal void Preload(string directory){foreach(string song in new[]{"Easy","Normal","Hard"})Prepare(Path.Combine(directory,song+".mp3"));}
        public void Open(string path,int volume)
        {
            Close();
            try
            {
                clip=Prepare(path).GetAwaiter().GetResult();if(device==null)device=new ArcadeAudioDevice();
                voice=device.Create(clip.Format);pinned=GCHandle.Alloc(clip.Data,GCHandleType.Pinned);SetVolume(volume);PreparePlayback();
            }
            catch{Close();throw;}
        }
        internal void PreparePlayback()
        {
            if(!Opened)return;voice.Flush();stoppedPosition=0;playing=false;
            startSamples=voice.State.Samples;voice.Submit(pinned.AddrOfPinnedObject(),clip.Data.Length);primed=true;
        }
        public double Position
        {
            get
            {
                if(!Opened)return 0;if(!playing)return stoppedPosition;
                var state=voice.State;if(state.Queued==0)return Duration;
                return Math.Min(Duration,(state.Samples>=startSamples?state.Samples-startSamples:state.Samples)/(double)clip.Format.Rate);
            }
        }
        internal static float Gain(int volume){double fraction=Math.Max(0,Math.Min(100,volume))/100.0;return (float)(fraction*fraction);}
        internal static int Attenuation(int volume)
        {volume=Math.Max(0,Math.Min(100,volume));return volume==0?-10000:(int)Math.Round(4000*Math.Log10(volume/100.0));}
        internal int PlaybackAttenuation {get{return voice.Gain<=0?-10000:(int)Math.Round(2000*Math.Log10(voice.Gain));}}
        internal float PlaybackGain {get{return voice.Gain;}}
        internal uint QueuedBuffers {get{return voice==null?0:voice.State.Queued;}}
        public void SetVolume(int volume){requestedVolume=Math.Max(0,Math.Min(100,volume));if(Opened)voice.Gain=Gain(requestedVolume);}
        public void Play(){if(Opened){if(!primed)PreparePlayback();SetVolume(requestedVolume);voice.Play();playing=true;primed=false;}}
        public void Stop(){if(Opened){stoppedPosition=Position;voice.Stop();playing=false;primed=false;}}
        public void Close()
        {if(voice!=null){voice.Dispose();voice=null;}if(pinned.IsAllocated)pinned.Free();clip=null;playing=primed=false;stoppedPosition=0;}
        public void Dispose(){if(disposed)return;disposed=true;cancellation.Cancel();Close();if(device!=null){device.Dispose();device=null;}prepared.Clear();cancellation.Dispose();}
    }
}
