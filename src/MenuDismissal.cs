using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vpet
{
    // Observe mouse-down only while a menu is open. Always pass the click through.
    internal sealed class MenuDismissal : IDisposable
    {
        delegate IntPtr MouseHook(int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id,MouseHook callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
        readonly ContextMenuStrip menu;
        readonly MouseHook callback;
        IntPtr hook;
        bool queued;
        int generation;
        public MenuDismissal(ContextMenuStrip menu)
        {this.menu=menu;callback=Observe;menu.Opened+=Opened;menu.Closed+=Closed;}
        void Opened(object sender,EventArgs e)
        {
            queued=false;generation++;
            if(hook==IntPtr.Zero)hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);
            if(hook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        void Closed(object sender,ToolStripDropDownClosedEventArgs e) { generation++;Stop(); }
        internal static bool Contains(ToolStripDropDown dropdown,Point point)
        {
            if(!dropdown.Visible)return false;
            if(dropdown.Bounds.Contains(point))return true;
            foreach(ToolStripItem item in dropdown.Items)
            {var parent=item as ToolStripDropDownItem;if(parent!=null&&parent.HasDropDownItems&&Contains(parent.DropDown,point))return true;}
            return false;
        }
        internal void MouseDownAt(Point point)
        {
            if(menu.Visible&&!queued&&!Contains(menu,point))
            {
                queued=true;
                int opened=generation;
                menu.BeginInvoke((Action)delegate{if(opened!=generation)return;queued=false;if(!menu.IsDisposed&&menu.Visible)menu.Close(ToolStripDropDownCloseReason.AppClicked);});
            }
        }
        IntPtr Observe(int code,IntPtr message,IntPtr data)
        {
            if(code>=0&&(message.ToInt64()==0x201||message.ToInt64()==0x204||message.ToInt64()==0x207||message.ToInt64()==0x20B))
            {
                var position=(Native.POINT)Marshal.PtrToStructure(data,typeof(Native.POINT));
                MouseDownAt(new Point(position.X,position.Y));
            }
            return CallNextHookEx(hook,code,message,data);
        }
        void Stop() { if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;} }
        public void Dispose(){Stop();menu.Opened-=Opened;menu.Closed-=Closed;}
    }
}
