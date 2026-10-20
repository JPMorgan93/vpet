using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class CueTextBox : TextBox
    {
        [DllImport("user32.dll",EntryPoint="SendMessageW",CharSet=CharSet.Unicode)]
        static extern IntPtr SetCue(IntPtr window,uint message,IntPtr showWhenFocused,string text);
        internal readonly string Cue;
        internal CueTextBox(string cue){Cue=cue;}
        protected override void OnHandleCreated(EventArgs e)
        {base.OnHandleCreated(e);SetCue(Handle,0x1501,new IntPtr(1),Cue);}
    }
}
