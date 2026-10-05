using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Vpet
{
    internal sealed class ArcadeMusic : IDisposable
    {
        object graph;MediaControl control;MediaPosition position;BasicAudio audio;int requestedVolume;
        public double Duration {get;private set;}
        public bool Opened {get{return graph!=null;}}
        static void Check(int result)
        {
            if(result<0)throw new IOException("Music playback: "+Marshal.GetExceptionForHR(result).Message);
        }
        public void Open(string path,int volume)
        {
            Close();path=Path.GetFullPath(path);if(!File.Exists(path)||path.Contains("\""))throw new IOException("The selected difficulty's song is unavailable.");
            try
            {
                graph=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("e436ebb3-524f-11ce-9f53-0020af0ba770")));
                control=(MediaControl)graph;Check(control.RenderFile(path));position=(MediaPosition)graph;audio=(BasicAudio)graph;
                double duration;Check(position.GetDuration(out duration));Duration=duration;SetVolume(volume);
            }
            catch{Close();throw;}
        }
        public double Position {get{if(!Opened)return 0;double value;Check(position.GetPosition(out value));return value;}}
        // IBasicAudio uses hundredths of a decibel: 0 is unity gain; -10000 is mute.
        // A squared amplitude curve gives useful quiet settings instead of crowding them near zero.
        internal static int Attenuation(int volume)
        {volume=Math.Max(0,Math.Min(100,volume));return volume==0?-10000:(int)Math.Round(4000*Math.Log10(volume/100.0));}
        internal int PlaybackAttenuation {get{int value;Check(audio.GetVolume(out value));return value;}}
        public void SetVolume(int volume){requestedVolume=Math.Max(0,Math.Min(100,volume));if(Opened)Check(audio.SetVolume(Attenuation(requestedVolume)));}
        public void Play(){if(Opened){Check(position.SetPosition(0));SetVolume(requestedVolume);Check(control.Run());}}
        public void Stop(){if(Opened)Check(control.Stop());}
        public void Close()
        {
            if(!Opened)return;
            if(control!=null)control.Stop();var old=graph;graph=null;control=null;position=null;audio=null;Duration=0;Marshal.FinalReleaseComObject(old);
        }
        public void Dispose(){Close();}

        // Native vtable order from the Windows SDK control.h; all three interfaces derive from IDispatch.
        [ComImport,Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"),InterfaceType(ComInterfaceType.InterfaceIsDual)]
        interface MediaControl
        {
            [PreserveSig] int Run();[PreserveSig] int Pause();[PreserveSig] int Stop();
            [PreserveSig] int GetState(int timeout,out int state);
            [PreserveSig] int RenderFile([MarshalAs(UnmanagedType.BStr)] string path);
        }
        [ComImport,Guid("56a868b2-0ad4-11ce-b03a-0020af0ba770"),InterfaceType(ComInterfaceType.InterfaceIsDual)]
        interface MediaPosition
        {
            [PreserveSig] int GetDuration(out double seconds);
            [PreserveSig] int SetPosition(double seconds);
            [PreserveSig] int GetPosition(out double seconds);
        }
        [ComImport,Guid("56a868b3-0ad4-11ce-b03a-0020af0ba770"),InterfaceType(ComInterfaceType.InterfaceIsDual)]
        interface BasicAudio
        {
            [PreserveSig] int SetVolume(int attenuation);
            [PreserveSig] int GetVolume(out int attenuation);
        }
    }
}
