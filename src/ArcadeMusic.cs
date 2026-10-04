using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Vpet
{
    internal sealed class ArcadeMusic : IDisposable
    {
        [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern int mciSendString(string command,StringBuilder result,int length,IntPtr callback);
        [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern bool mciGetErrorString(int code,StringBuilder result,int length);
        readonly string alias="vpetarcade"+Guid.NewGuid().ToString("N");bool opened;
        public double Duration {get;private set;}
        public bool Opened {get{return opened;}}
        string Command(string command)
        {
            var result=new StringBuilder(256);int code=mciSendString(command,result,result.Capacity,IntPtr.Zero);
            if(code!=0){var error=new StringBuilder(256);mciGetErrorString(code,error,error.Capacity);throw new IOException("Music playback: "+error);}
            return result.ToString();
        }
        public void Open(string path,int volume)
        {
            Close();path=Path.GetFullPath(path);if(!File.Exists(path)||path.Contains("\""))throw new IOException("The selected difficulty's song is unavailable.");
            Command("open \""+path+"\" type mpegvideo alias "+alias);opened=true;
            try{Command("set "+alias+" time format milliseconds");Duration=double.Parse(Command("status "+alias+" length"),CultureInfo.InvariantCulture)/1000;SetVolume(volume);}
            catch{Close();throw;}
        }
        public double Position {get{return opened?double.Parse(Command("status "+alias+" position"),CultureInfo.InvariantCulture)/1000:0;}}
        public void SetVolume(int volume){if(opened)Command("setaudio "+alias+" volume to "+Math.Max(0,Math.Min(100,volume))*10);}
        public void Play(){if(opened)Command("play "+alias+" from 0");}
        public void Stop(){if(opened)Command("stop "+alias);}
        public void Close(){if(!opened)return;mciSendString("close "+alias,null,0,IntPtr.Zero);opened=false;}
        public void Dispose(){Close();}
    }
}
