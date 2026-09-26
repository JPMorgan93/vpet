using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class RestrictedAreaOverlay : IDisposable
    {
        readonly PetModel model;
        readonly Action changed;
        readonly List<LayeredWindow> rings=new List<LayeredWindow>();
        readonly LayeredWindow handle=new LayeredWindow(false);
        readonly LayeredWindow petWindow,crossingWindow;
        Point dragStart;PointF anchorStart;
        string signature="";
        public bool Dragging {get;private set;}
        internal bool IsDisplayed {get{return handle.Visible;}}
        internal Rectangle HandleBounds {get{Native.RECT rect;Native.GetWindowRect(handle.Handle,out rect);return Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom);}}
        internal IntPtr HandleWindow {get{return handle.Handle;}}
        internal IEnumerable<IntPtr> RingWindows {get{return rings.Where(r=>r.Visible).Select(r=>r.Handle);}}
        public RestrictedAreaOverlay(PetModel model,Action changed,LayeredWindow petWindow=null,LayeredWindow crossingWindow=null)
        {
            this.model=model;this.changed=changed;this.petWindow=petWindow;this.crossingWindow=crossingWindow;
            handle.Text="Move restricted area";handle.Cursor=Cursors.SizeAll;Register(handle);
            handle.MouseDown+=delegate(object sender,MouseEventArgs e)
            {if(e.Button!=MouseButtons.Left)return;Dragging=true;dragStart=Cursor.Position;anchorStart=model.Anchor;handle.Capture=true;};
            handle.MouseMove+=delegate
            {
                if(!Dragging)return;var cursor=Cursor.Position;
                model.MoveRestrictedArea(new PointF(anchorStart.X+cursor.X-dragStart.X,anchorStart.Y+cursor.Y-dragStart.Y));Update();
            };
            handle.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)EndDrag();};
            handle.MouseCaptureChanged+=delegate{if(Dragging&&!handle.Capture)EndDrag();};
        }
        void EndDrag(){if(!Dragging)return;Dragging=false;handle.Capture=false;changed();}
        public void Update()
        {
            bool visible=model.Settings.Movement==MovementMode.Restricted&&model.Settings.DisplayRestrictedArea;
            if(!visible){foreach(var ring in rings)ring.Hide();handle.Hide();signature="";EndDrag();return;}
            string key=model.Anchor.ToString()+":"+model.Settings.Radius+":"+string.Join("|",model.Displays.Select(d=>d.Work.ToString()));
            if(key==signature){KeepBelowPet();return;}signature=key;
            while(rings.Count<model.Displays.Count){var ring=new LayeredWindow(true){Text="Restricted area fence"};Register(ring);rings.Add(ring);}
            KeepBelowPet();
            int radius=model.Settings.Radius;
            var bounds=Rectangle.FromLTRB((int)Math.Floor(model.Anchor.X-radius-4),(int)Math.Floor(model.Anchor.Y-radius-4),
                (int)Math.Ceiling(model.Anchor.X+radius+4),(int)Math.Ceiling(model.Anchor.Y+radius+4));
            for(int i=0;i<rings.Count;i++)
            {
                if(i>=model.Displays.Count){rings[i].Hide();continue;}
                var region=Rectangle.Intersect(bounds,model.Displays[i].Work);
                if(region.Width<=0||region.Height<=0){rings[i].Hide();continue;}
                using(var image=new Bitmap(region.Width,region.Height,PixelFormat.Format32bppArgb))using(var graphics=Graphics.FromImage(image))
                {
                    graphics.SmoothingMode=SmoothingMode.AntiAlias;
                    var circle=new RectangleF(model.Anchor.X-radius-region.Left,model.Anchor.Y-radius-region.Top,radius*2,radius*2);
                    using(var outline=new Pen(Color.FromArgb(215,255,255,255),5))graphics.DrawEllipse(outline,circle);
                    using(var fence=new Pen(Color.FromArgb(230,119,73,182),2)){fence.DashStyle=DashStyle.Dash;graphics.DrawEllipse(fence,circle);}
                    if(!rings[i].Visible)rings[i].Show();rings[i].Present(image,region.Location);
                }
            }
            using(var image=new Bitmap(30,30,PixelFormat.Format32bppArgb))using(var graphics=Graphics.FromImage(image))
            {
                graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.FillEllipse(Brushes.White,1,1,27,27);
                using(var purple=new Pen(Color.FromArgb(119,73,182),2))
                {graphics.DrawEllipse(purple,1,1,27,27);graphics.DrawLine(purple,7,15,23,15);graphics.DrawLine(purple,15,7,15,23);}
                if(!handle.Visible)handle.Show();
                handle.Present(image,new Point((int)model.Anchor.X-15,(int)model.Anchor.Y-15));
            }
            KeepBelowPet();
        }
        static void Register(LayeredWindow window)
        {
            IntPtr id=window.Handle;Native.BackgroundAdornments.Add(id);
            window.FormClosed+=delegate{Native.BackgroundAdornments.Remove(id);};
        }
        void KeepBelowPet()
        {
            if(petWindow==null||petWindow.IsDisposed)return;
            IntPtr lowest=petWindow.Handle;
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused)
            {if(window==petWindow.Handle||(crossingWindow!=null&&crossingWindow.Visible&&window==crossingWindow.Handle))lowest=window;return true;},IntPtr.Zero);
            foreach(var ring in rings){ring.BehindWindow=lowest;if(ring.Visible)Native.SetWindowPos(ring.Handle,lowest,0,0,0,0,0x213);}
            handle.BehindWindow=lowest;if(handle.Visible)Native.SetWindowPos(handle.Handle,lowest,0,0,0,0,0x213);
        }
        public void Dispose(){EndDrag();handle.Close();foreach(var ring in rings)ring.Close();}
    }
}
