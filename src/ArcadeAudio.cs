using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Vpet
{
    // Windows SDK vtables; XAudio2 structures use pack(1), WAVEFORMATEX uses pack(2).
    internal static class ArcadeAudioNative
    {
        internal static T Method<T>(IntPtr instance,int slot)
        {return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance),slot*IntPtr.Size),typeof(T));}
        internal static void Check(int result)
        {if(result<0)throw new IOException("Arcade audio: "+Marshal.GetExceptionForHR(result).Message);}
        internal static void Release(ref IntPtr value){if(value!=IntPtr.Zero){Marshal.Release(value);value=IntPtr.Zero;}}
        [StructLayout(LayoutKind.Sequential,Pack=2)] internal struct WaveFormat
        {public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort BlockAlign,Bits,Extra;}
        [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct Buffer
        {public uint Flags,Bytes;public IntPtr Data;public uint PlayBegin,PlayLength,LoopBegin,LoopLength,LoopCount;public IntPtr Context;}
        [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct VoiceState
        {public IntPtr Context;public uint Queued;public ulong Samples;}
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetGuid(IntPtr self,ref Guid key,ref Guid value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetUInt(IntPtr self,ref Guid key,uint value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SelectStream(IntPtr self,uint stream,int selected);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetType(IntPtr self,uint stream,IntPtr reserved,IntPtr type);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetType(IntPtr self,uint stream,out IntPtr type);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int ReadSample(IntPtr self,uint stream,uint control,out uint actual,out uint flags,out long timestamp,out IntPtr sample);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetBuffer(IntPtr self,out IntPtr buffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int LockBuffer(IntPtr self,out IntPtr data,out uint maximum,out uint length);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int NoArgs(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int CreateMaster(IntPtr self,out IntPtr voice,uint channels,uint rate,uint flags,IntPtr device,IntPtr effects,int category);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int CreateSource(IntPtr self,out IntPtr voice,ref WaveFormat format,uint flags,float ratio,IntPtr callback,IntPtr sends,IntPtr effects);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int VoiceAction(IntPtr self,uint flags,uint operation);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int Submit(IntPtr self,ref Buffer buffer,IntPtr wma);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetVolume(IntPtr self,float gain,uint operation);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate void GetVolume(IntPtr self,out float gain);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate void GetState(IntPtr self,out VoiceState state,uint flags);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate void Destroy(IntPtr self);
        [DllImport("mfplat.dll",ExactSpelling=true)] internal static extern int MFStartup(uint version,uint flags);
        [DllImport("mfplat.dll",ExactSpelling=true)] internal static extern int MFShutdown();
        [DllImport("mfplat.dll",ExactSpelling=true)] internal static extern int MFCreateMediaType(out IntPtr type);
        [DllImport("mfplat.dll",ExactSpelling=true)] internal static extern int MFCreateWaveFormatExFromMFMediaType(IntPtr type,out IntPtr format,out uint size,uint flags);
        [DllImport("mfreadwrite.dll",ExactSpelling=true,CharSet=CharSet.Unicode)] internal static extern int MFCreateSourceReaderFromURL(string url,IntPtr attributes,out IntPtr reader);
        [DllImport("xaudio2_9.dll",ExactSpelling=true)] internal static extern int XAudio2Create(out IntPtr engine,uint flags,uint processor);
    }
    internal sealed class ArcadeClip
    {
        public readonly byte[] Data;public readonly ArcadeAudioNative.WaveFormat Format;
        public double Duration {get{return Data.Length/(double)Format.BytesPerSecond;}}
        internal ArcadeClip(byte[] data,ArcadeAudioNative.WaveFormat format){Data=data;Format=format;}
        internal static ArcadeClip Decode(string path,CancellationToken cancellation)
        {
            if(!File.Exists(path))throw new IOException("The selected difficulty's song is unavailable.");
            IntPtr reader=IntPtr.Zero,type=IntPtr.Zero,actualType=IntPtr.Zero,formatPointer=IntPtr.Zero;
            ArcadeAudioNative.Check(ArcadeAudioNative.MFStartup(0x20070,0));
            try
            {
                ArcadeAudioNative.Check(ArcadeAudioNative.MFCreateSourceReaderFromURL(path,IntPtr.Zero,out reader));
                var select=ArcadeAudioNative.Method<ArcadeAudioNative.SelectStream>(reader,4);
                ArcadeAudioNative.Check(select(reader,0xfffffffe,0));ArcadeAudioNative.Check(select(reader,0xfffffffd,1));
                ArcadeAudioNative.Check(ArcadeAudioNative.MFCreateMediaType(out type));
                Guid major=new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f"),audio=new Guid("73647561-0000-0010-8000-00aa00389b71");
                Guid subtype=new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5"),pcm=new Guid("00000001-0000-0010-8000-00aa00389b71");
                Guid bits=new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");
                var setGuid=ArcadeAudioNative.Method<ArcadeAudioNative.SetGuid>(type,24);
                ArcadeAudioNative.Check(setGuid(type,ref major,ref audio));ArcadeAudioNative.Check(setGuid(type,ref subtype,ref pcm));
                ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.SetUInt>(type,21)(type,ref bits,16));
                ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.SetType>(reader,7)(reader,0xfffffffd,IntPtr.Zero,type));
                ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.GetType>(reader,6)(reader,0xfffffffd,out actualType));
                uint formatSize;ArcadeAudioNative.Check(ArcadeAudioNative.MFCreateWaveFormatExFromMFMediaType(actualType,out formatPointer,out formatSize,0));
                var format=(ArcadeAudioNative.WaveFormat)Marshal.PtrToStructure(formatPointer,typeof(ArcadeAudioNative.WaveFormat));
                if(format.Bits!=16||format.Channels<1||format.Channels>2||format.Rate<8000||format.BytesPerSecond==0)throw new IOException("Unsupported decoded song format.");
                // Media Foundation may describe mono/stereo PCM as WAVEFORMATEXTENSIBLE.
                format.Tag=1;format.Extra=0;
                var read=ArcadeAudioNative.Method<ArcadeAudioNative.ReadSample>(reader,9);
                using(var output=new MemoryStream())
                {
                    while(true)
                    {
                        cancellation.ThrowIfCancellationRequested();uint stream,flags;long time;IntPtr sample=IntPtr.Zero,buffer=IntPtr.Zero;
                        try
                        {
                            ArcadeAudioNative.Check(read(reader,0xfffffffd,0,out stream,out flags,out time,out sample));
                            if((flags&1)!=0)throw new IOException("The song decoder reported an error.");
                            if((flags&0x20)!=0)throw new IOException("The song changed format while decoding.");
                            if(sample!=IntPtr.Zero)
                            {
                                ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.GetBuffer>(sample,41)(sample,out buffer));
                                IntPtr data;uint maximum,length;ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.LockBuffer>(buffer,3)(buffer,out data,out maximum,out length));
                                try{var bytes=new byte[length];Marshal.Copy(data,bytes,0,bytes.Length);output.Write(bytes,0,bytes.Length);}
                                finally{ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.NoArgs>(buffer,4)(buffer));}
                            }
                            if(output.Length>128*1024*1024)throw new IOException("The song is too large to prepare in memory.");
                            if((flags&2)!=0)break;
                        }
                        finally{ArcadeAudioNative.Release(ref buffer);ArcadeAudioNative.Release(ref sample);}
                    }
                    if(output.Length==0||output.Length%format.BlockAlign!=0)throw new IOException("The song contains no complete audio samples.");
                    return new ArcadeClip(output.ToArray(),format);
                }
            }
            finally
            {if(formatPointer!=IntPtr.Zero)Marshal.FreeCoTaskMem(formatPointer);ArcadeAudioNative.Release(ref actualType);ArcadeAudioNative.Release(ref type);ArcadeAudioNative.Release(ref reader);ArcadeAudioNative.MFShutdown();}
        }
    }
    internal sealed class ArcadeAudioDevice : IDisposable
    {
        IntPtr engine,master;
        internal ArcadeAudioDevice()
        {
            try{ArcadeAudioNative.Check(ArcadeAudioNative.XAudio2Create(out engine,0,0xffffffff));ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.CreateMaster>(engine,7)(engine,out master,0,0,0,IntPtr.Zero,IntPtr.Zero,6));}
            catch{Dispose();throw;}
        }
        internal ArcadeVoice Create(ArcadeAudioNative.WaveFormat format){return new ArcadeVoice(engine,format);}
        public void Dispose(){if(master!=IntPtr.Zero){ArcadeAudioNative.Method<ArcadeAudioNative.Destroy>(master,18)(master);master=IntPtr.Zero;}ArcadeAudioNative.Release(ref engine);}
    }
    internal sealed class ArcadeVoice : IDisposable
    {
        IntPtr voice;readonly ArcadeAudioNative.GetState state;
        internal ArcadeVoice(IntPtr engine,ArcadeAudioNative.WaveFormat format)
        {ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.CreateSource>(engine,5)(engine,out voice,ref format,0,2,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero));state=ArcadeAudioNative.Method<ArcadeAudioNative.GetState>(voice,25);}
        internal ArcadeAudioNative.VoiceState State {get{ArcadeAudioNative.VoiceState value;state(voice,out value,0);return value;}}
        internal float Gain {get{float value;ArcadeAudioNative.Method<ArcadeAudioNative.GetVolume>(voice,13)(voice,out value);return value;}set{ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.SetVolume>(voice,12)(voice,value,0));}}
        internal void Submit(IntPtr data,int bytes)
        {var buffer=new ArcadeAudioNative.Buffer{Flags=0x40,Data=data,Bytes=(uint)bytes};ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.Submit>(voice,21)(voice,ref buffer,IntPtr.Zero));}
        internal void Play(){ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.VoiceAction>(voice,19)(voice,0,0));}
        internal void Stop(){ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.VoiceAction>(voice,20)(voice,0,0));}
        internal void Flush(){Stop();ArcadeAudioNative.Check(ArcadeAudioNative.Method<ArcadeAudioNative.NoArgs>(voice,22)(voice));}
        public void Dispose(){if(voice!=IntPtr.Zero){ArcadeAudioNative.Method<ArcadeAudioNative.Destroy>(voice,18)(voice);voice=IntPtr.Zero;}}
    }
}
