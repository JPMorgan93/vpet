using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Vpet
{
    internal static class Native
    {
        internal static readonly HashSet<IntPtr> BackgroundAdornments=new HashSet<IntPtr>();
        internal static IntPtr ArcadeForeground;
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; public POINT(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int Width,Height; public SIZE(int w,int h){Width=w;Height=h;} }
        [StructLayout(LayoutKind.Sequential, Pack=1)] public struct BLEND { public byte Op,Flags,Alpha,Format; }
        [StructLayout(LayoutKind.Sequential)] public struct WINDOWPOS { public IntPtr Window,InsertAfter;public int X,Y,Width,Height;public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out RECT rect);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr window,IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll",SetLastError=true)] public static extern bool UpdateLayeredWindow(IntPtr window,IntPtr dst,ref POINT location,ref SIZE size,IntPtr src,ref POINT origin,int key,ref BLEND blend,int flags);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr child);
        [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window,StringBuilder name,int length);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr window,uint command);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr param);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback,IntPtr param);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int awareness);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT point,uint flags);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);

        public static void EnableDpi()
        {
            try { if(SetProcessDpiAwarenessContext(new IntPtr(-4)))return; } catch(EntryPointNotFoundException){}
            try { SetProcessDpiAwareness(2); } catch(DllNotFoundException){}
        }
        public static List<DisplayArea> Displays()
        {
            var result=new List<DisplayArea>();
            foreach(var screen in Screen.AllScreens)
            {
                uint x=96,y=96;
                try { GetDpiForMonitor(MonitorFromPoint(new POINT(screen.Bounds.Left+1,screen.Bounds.Top+1),2),0,out x,out y); } catch(DllNotFoundException){}
                result.Add(new DisplayArea(screen.DeviceName,screen.WorkingArea,Math.Max(1,x)/96f));
            }
            return result;
        }
        public static IntPtr UnderAllTarget(IntPtr pet,IntPtr companion,IntPtr otherCompanion=default(IntPtr))
        {
            IntPtr lastApp=IntPtr.Zero;
            // Stay below visible applications, but above the wallpaper/icon surfaces.
            // No Explorer host, reparenting, or undocumented shell messages are needed.
            EnumWindows(delegate(IntPtr hwnd,IntPtr unused)
            {
                if(hwnd==pet||hwnd==companion||hwnd==otherCompanion||BackgroundAdornments.Contains(hwnd)||!IsWindowVisible(hwnd))return true;
                if((GetWindowLongPtr(hwnd,-20).ToInt64()&8)!=0)return true; // Topmost windows are already above us.
                var name=new StringBuilder(256);GetClassName(hwnd,name,name.Capacity);
                string cls=name.ToString();if(cls=="Progman"||cls=="WorkerW")return true;
                lastApp=hwnd;
                return true;
            },IntPtr.Zero);
            return lastApp!=IntPtr.Zero?lastApp:new IntPtr(-2); // No apps: normal non-topmost desktop-visible window.
        }
        internal static bool IsAbove(IntPtr above,IntPtr below)
        {
            for(int i=0;i<10000;i++){below=GetWindow(below,3);if(below==above)return true;if(below==IntPtr.Zero)return false;}
            return false;
        }
        internal static IntPtr? AboveCompanionTarget(IntPtr window,IntPtr companion,IntPtr otherCompanion,LayerMode mode,bool onlyWhenBelow=false)
        {
            IntPtr highest=IntPtr.Zero;bool alreadyAbove=false;
            EnumWindows(delegate(IntPtr hwnd,IntPtr unused)
            {
                if(hwnd==window)alreadyAbove=true;
                if((hwnd==companion||hwnd==otherCompanion)&&IsWindowVisible(hwnd)){highest=hwnd;return false;}
                return true;
            },IntPtr.Zero);
            if(highest==IntPtr.Zero||(onlyWhenBelow&&alreadyAbove))return null;
            IntPtr previous=GetWindow(highest,3); // GW_HWNDPREV: insert immediately above the highest sprite portion.
            if(previous==window)previous=GetWindow(window,3);
            // A normal item must not enter the topmost band by following a topmost application.
            if(mode!=LayerMode.OverEverything&&(previous==IntPtr.Zero||(GetWindowLongPtr(previous,-20).ToInt64()&8)!=0))
                return (GetWindowLongPtr(window,-20).ToInt64()&8)!=0?new IntPtr(-2):IntPtr.Zero;
            if(mode==LayerMode.OverEverything&&previous==IntPtr.Zero)return new IntPtr(-1);
            return previous;
        }
    }

    internal class LayeredWindow : Form
    {
        readonly bool clickThrough;
        // UpdateLayeredWindow moves/resizes the native window without refreshing WinForms' Bounds cache.
        internal Rectangle PresentedBounds {get;private set;}
        public IntPtr CompanionHandle;
        public IntPtr OtherCompanionHandle;
        public IntPtr BehindWindow;
        public bool AboveCompanions;
        LayerMode layerMode;
        bool layerConfigured;
        bool correctingLayer;
        public LayeredWindow(bool clickThrough)
        {
            this.clickThrough=clickThrough; FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false;
            StartPosition=FormStartPosition.Manual; AutoScaleMode=AutoScaleMode.None;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var p=base.CreateParams; p.ExStyle|=0x80000|0x80|0x08000000; if(clickThrough)p.ExStyle|=0x20; return p; }
        }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;} // MA_NOACTIVATE
            if(m.Msg==0x46&&m.LParam!=IntPtr.Zero)
            {
                var position=(Native.WINDOWPOS)Marshal.PtrToStructure(m.LParam,typeof(Native.WINDOWPOS));
                bool behindArcade=BehindArcade,bandMatches=IsTopmost==(layerMode==LayerMode.OverEverything&&!behindArcade),noZOrder=(position.Flags&4u)!=0;
                IntPtr? target=behindArcade?Native.ArcadeForeground:BehindWindow!=IntPtr.Zero?BehindWindow:AboveCompanions?
                    Native.AboveCompanionTarget(Handle,CompanionHandle,OtherCompanionHandle,layerMode,bandMatches&&noZOrder&&!correctingLayer):layerMode==LayerMode.UnderAll?(IntPtr?)Native.UnderAllTarget(Handle,CompanionHandle,OtherCompanionHandle):null;
                if(layerMode==LayerMode.UnderAll&&AboveCompanions)
                {
                    IntPtr under=Native.UnderAllTarget(Handle,CompanionHandle,OtherCompanionHandle);
                    if(!bandMatches||(under.ToInt64()>0&&!Native.IsAbove(under,Handle)))target=under;
                }
                if(target.HasValue||AboveCompanions)
                {
                    // Keep correct existing ordering. Re-inserting each item just above the pet on
                    // every layered redraw makes overlapping toys exchange layers and visibly blink.
                    bool aboveRule=AboveCompanions&&!behindArcade&&BehindWindow==IntPtr.Zero;
                    bool preserve=!correctingLayer&&noZOrder&&bandMatches&&(aboveRule?!target.HasValue:!target.HasValue||target.Value==new IntPtr(-2)||target.Value.ToInt64()>0&&Native.IsAbove(target.Value,Handle));
                    if(preserve)position.Flags|=4u; // NOZORDER also blocks attempts to violate a locked layer.
                    else if(target.HasValue){position.InsertAfter=target.Value;position.Flags&=~4u;}
                    position.Flags|=0x210u; // NOACTIVATE and NOOWNERZORDER.
                    Marshal.StructureToPtr(position,m.LParam,false);
                }
            }
            base.WndProc(ref m);
        }
        bool BehindArcade {get{return layerMode!=LayerMode.UnderAll&&Native.ArcadeForeground!=IntPtr.Zero&&Native.BackgroundAdornments.Contains(Handle)&&Native.IsWindowVisible(Native.ArcadeForeground);}}
        bool IsTopmost {get{return (Native.GetWindowLongPtr(Handle,-20).ToInt64()&8)!=0;}}
        public void Present(Bitmap image,Point screenPosition,byte opacity=255)
        {
            IntPtr screen=Native.GetDC(IntPtr.Zero), dc=Native.CreateCompatibleDC(screen), bitmap=IntPtr.Zero,old=IntPtr.Zero;
            try
            {
                bitmap=image.GetHbitmap(Color.FromArgb(0)); old=Native.SelectObject(dc,bitmap);
                var point=new Native.POINT(screenPosition.X,screenPosition.Y);
                var size=new Native.SIZE(image.Width,image.Height);var origin=new Native.POINT(0,0);
                var blend=new Native.BLEND{Op=0,Flags=0,Alpha=opacity,Format=1};
                if(!Native.UpdateLayeredWindow(Handle,screen,ref point,ref size,dc,ref origin,0,ref blend,2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                PresentedBounds=new Rectangle(screenPosition,image.Size);
            }
            finally
            {
                if(old!=IntPtr.Zero)Native.SelectObject(dc,old);if(bitmap!=IntPtr.Zero)Native.DeleteObject(bitmap);
                Native.DeleteDC(dc);Native.ReleaseDC(IntPtr.Zero,screen);
            }
        }
        public void SetLayer(LayerMode mode)
        {
            bool changed=!layerConfigured||layerMode!=mode;layerConfigured=true;
            layerMode=mode;
            bool topmost=mode==LayerMode.OverEverything&&!BehindArcade;
            if(changed||IsTopmost!=topmost)CorrectLayer(topmost?new IntPtr(-1):new IntPtr(-2));
            EnforceUnderAll();
        }
        public void EnforceUnderAll()
        {
            if(layerMode!=LayerMode.UnderAll||!IsHandleCreated)return;
            IntPtr target=Native.UnderAllTarget(Handle,CompanionHandle,OtherCompanionHandle);
            if(!IsTopmost&&(target==new IntPtr(-2)||Native.IsAbove(target,Handle)))return;
            CorrectLayer(target);
        }
        public void EnforceAboveCompanions()
        {
            if(!AboveCompanions||!IsHandleCreated)return;EnforceUnderAll();
            IntPtr? target=Native.AboveCompanionTarget(Handle,CompanionHandle,OtherCompanionHandle,layerMode,true);
            if(target.HasValue)CorrectLayer(target.Value);
        }
        void CorrectLayer(IntPtr target)
        {
            // Windows can expose a proposed order inside WINDOWPOSCHANGING. An intentional
            // repair must still be applied even when that temporary order already looks valid.
            correctingLayer=true;try{Native.SetWindowPos(Handle,target,0,0,0,0,0x213);}finally{correctingLayer=false;}
        }
    }
}
