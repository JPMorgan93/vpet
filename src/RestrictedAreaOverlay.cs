using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class RestrictedAreaOverlay : IDisposable
    {
        readonly PetModel model;readonly Action changed;readonly LayeredWindow petWindow,crossingWindow;
        readonly LayeredWindow fence=new LayeredWindow(false){Text="Restricted area fence",Cursor=Cursors.SizeAll};
        Point pointerStart;PointF centerStart;RectangleF original;ZoneEdge edges;string signature="";
        public bool Dragging {get;private set;}
        internal bool IsDisplayed {get{return fence.Visible;}}
        internal Rectangle HandleBounds {get{return new Rectangle((int)model.Anchor.X-20,(int)model.Anchor.Y-20,40,40);}}
        internal IntPtr HandleWindow {get{return fence.Handle;}}
        internal IEnumerable<IntPtr> RingWindows {get{if(fence.Visible)yield return fence.Handle;}}
        public RestrictedAreaOverlay(PetModel model,Action changed,LayeredWindow petWindow=null,LayeredWindow crossingWindow=null)
        {
            this.model=model;this.changed=changed;this.petWindow=petWindow;this.crossingWindow=crossingWindow;
            IntPtr id=fence.Handle;Native.BackgroundAdornments.Add(id);fence.FormClosed+=delegate{Native.BackgroundAdornments.Remove(id);};
            fence.MouseDown+=delegate(object sender,MouseEventArgs e)
            {
                if(e.Button!=MouseButtons.Left)return;pointerStart=Cursor.Position;centerStart=model.Anchor;original=model.OwnRestrictedArea;
                edges=FenceGeometry.Hit(original,pointerStart,model.Current.Scale);
                if(edges==ZoneEdge.None&&Geometry.Distance(pointerStart,centerStart)>22*model.Current.Scale)return;
                Dragging=true;fence.Capture=true;
            };
            fence.MouseMove+=delegate
            {
                if(!Dragging)return;var delta=new PointF(Cursor.Position.X-pointerStart.X,Cursor.Position.Y-pointerStart.Y);
                if(edges==ZoneEdge.None)model.MoveRestrictedArea(new PointF(centerStart.X+delta.X,centerStart.Y+delta.Y));
                else model.ResizeRestrictedArea(original,edges,delta);Update();
            };
            fence.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)EndDrag();};
            fence.MouseCaptureChanged+=delegate{if(Dragging&&!fence.Capture)EndDrag();};
        }
        void EndDrag(){if(!Dragging)return;Dragging=false;fence.Capture=false;changed();}
        public void Update()
        {
            if(model.Settings.Movement!=MovementMode.Restricted||model.Settings.SyncPlayZone||!model.Settings.DisplayRestrictedArea)
            {fence.Hide();signature="";EndDrag();return;}
            var zone=model.OwnRestrictedArea;var display=FenceGeometry.Nearest(model.Displays,model.Anchor);string key=zone.ToString()+display.Scale;
            if(key!=signature||!fence.Visible)
            {
                signature=key;var bounds=Rectangle.Ceiling(zone);
                using(var image=ToyArtwork.Fence(bounds.Size,display.Scale)){fence.Present(image,bounds.Location);if(!fence.Visible)fence.Show();}
            }
            if(petWindow==null||petWindow.IsDisposed)return;
            IntPtr lowest=petWindow.Handle;
            Native.EnumWindows(delegate(IntPtr w,IntPtr unused){if(w==petWindow.Handle||(crossingWindow!=null&&crossingWindow.Visible&&w==crossingWindow.Handle))lowest=w;return true;},IntPtr.Zero);
            fence.BehindWindow=IntPtr.Zero;fence.SetLayer(model.Settings.Layer);fence.BehindWindow=lowest;Native.SetWindowPos(fence.Handle,lowest,0,0,0,0,0x213);
        }
        public void Dispose(){EndDrag();fence.Close();}
    }
}
