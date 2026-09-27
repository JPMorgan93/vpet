using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class ToyWindows : IDisposable
    {
        public readonly ToyModel Model;
        readonly PetModel pet;
        readonly LayeredWindow petWindow,crossingWindow;
        readonly Action save;
        readonly Func<double> now;
        internal readonly LayeredWindow Chest=new LayeredWindow(false){Text="Vpet toy chest",Cursor=Cursors.SizeAll};
        internal readonly LayeredWindow Ball=new LayeredWindow(false){Text="Vpet ball",Cursor=Cursors.Hand};
        internal readonly LayeredWindow Fence=new LayeredWindow(false){Text="Vpet play zone"};
        internal readonly LayeredWindow Arrow=new LayeredWindow(true){Text="Ball launch direction"};
        internal readonly ContextMenuStrip Menu=new ContextMenuStrip();
        readonly MenuDismissal dismissal;
        readonly Bitmap chestImage;
        string fenceKey="";
        LayeredWindow captured;
        Point pointerStart;
        PointF chestStart,centerStart,pull;
        RectangleF zoneStart;
        ZoneEdge resizeEdges;
        bool moveZone,dragged,disposed;
        LayerMode? layer;
        internal bool Busy {get{return captured!=null||Menu.Visible;}}
        internal IEnumerable<LayeredWindow> Windows {get{yield return Ball;yield return Arrow;yield return Fence;yield return Chest;}}

        public ToyWindows(PetModel pet,LayeredWindow petWindow,LayeredWindow crossingWindow,Action save,Func<double> now,Random random)
        {
            this.pet=pet;this.petWindow=petWindow;this.crossingWindow=crossingWindow;this.save=save;this.now=now;
            Model=new ToyModel(pet,random);chestImage=ToyArtwork.Chest(Model.Scale);
            foreach(var window in Windows)
            {
                IntPtr handle=window.Handle;Native.BackgroundAdornments.Add(handle);
                window.FormClosed+=delegate{Native.BackgroundAdornments.Remove(handle);};
                window.MouseDown+=Down;window.MouseMove+=Move;window.MouseUp+=Up;
                window.MouseCaptureChanged+=delegate(object sender,EventArgs e){if(captured==sender&&!captured.Capture)EndGesture(false);};
            }
            var display=new ToolStripMenuItem("Display Play Zone"){CheckOnClick=true};
            display.Click+=delegate{Model.Settings.DisplayZone=display.Checked;fenceKey="";Update();save();};
            Menu.Items.Add(display);
            Menu.Items.Add("Ball",null,delegate{Model.SpawnBall(now());Update();});
            Menu.Items.Add(new ToolStripSeparator());
            Menu.Items.Add(new ToolStripMenuItem("Move the center; drag edges or corners to resize"){Enabled=false});
            Menu.Items.Add(new ToolStripMenuItem("Click the ball to bounce; pull back and release to launch"){Enabled=false});
            Menu.Opening+=delegate{display.Checked=Model.Settings.DisplayZone;};
            Chest.ContextMenuStrip=Menu;dismissal=new MenuDismissal(Menu);
        }
        public void SetVisible(bool visible)
        {EndGesture(false);Menu.Close();Model.SetVisible(visible,now());fenceKey="";Update();save();}
        public void RecoverDisplays(){EndGesture(false);Model.RecoverDisplays();fenceKey="";}
        ZoneEdge HitEdge(Point p)
        {
            var zone=Model.Zone;float tolerance=8*Model.Scale;ZoneEdge result=ZoneEdge.None;
            if(Math.Abs(p.X-zone.Left)<=tolerance)result|=ZoneEdge.Left;
            if(Math.Abs(p.X-zone.Right)<=tolerance)result|=ZoneEdge.Right;
            if(Math.Abs(p.Y-zone.Top)<=tolerance)result|=ZoneEdge.Top;
            if(Math.Abs(p.Y-zone.Bottom)<=tolerance)result|=ZoneEdge.Bottom;
            return result;
        }
        static Cursor ResizeCursor(ZoneEdge edges)
        {
            if(edges==(ZoneEdge.Left|ZoneEdge.Top)||edges==(ZoneEdge.Right|ZoneEdge.Bottom))return Cursors.SizeNWSE;
            if(edges==(ZoneEdge.Right|ZoneEdge.Top)||edges==(ZoneEdge.Left|ZoneEdge.Bottom))return Cursors.SizeNESW;
            return (edges&(ZoneEdge.Left|ZoneEdge.Right))!=0?Cursors.SizeWE:Cursors.SizeNS;
        }
        void Down(object sender,MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left||!Model.Settings.DisplayChest)return;
            var window=(LayeredWindow)sender;if(window==Arrow)return;
            pointerStart=Cursor.Position;chestStart=Model.Chest;centerStart=Model.Center;zoneStart=Model.Zone;
            if(window==Fence)
            {
                resizeEdges=HitEdge(pointerStart);moveZone=resizeEdges==ZoneEdge.None&&Geometry.Distance(pointerStart,centerStart)<=17*Model.Scale;
                if(!moveZone&&resizeEdges==ZoneEdge.None)return;
            }
            captured=window;dragged=false;pull=PointF.Empty;
            if(window==Ball)Model.BeginAim();else Model.Editing=true;
            window.Capture=true;
            if(pet.Settings.Layer==LayerMode.Dynamic)Native.SetWindowPos(petWindow.Handle,IntPtr.Zero,0,0,0,0,0x13);
        }
        void Move(object sender,MouseEventArgs e)
        {
            if(captured==null)
            {
                if(sender==Fence){var edges=HitEdge(Cursor.Position);Fence.Cursor=edges==ZoneEdge.None?Cursors.SizeAll:ResizeCursor(edges);}
                return;
            }
            UpdateGesture();Update();
        }
        void UpdateGesture()
        {
            if(captured==null)return;
            var point=Cursor.Position;pull=new PointF(point.X-pointerStart.X,point.Y-pointerStart.Y);
            if(Geometry.Distance(pull,PointF.Empty)>=4)dragged=true;
            if(!dragged)return;
            if(captured==Chest)Model.DragChest(new PointF(chestStart.X+pull.X,chestStart.Y+pull.Y));
            else if(captured==Fence)
            {
                if(moveZone)Model.MoveZone(new PointF(centerStart.X+pull.X,centerStart.Y+pull.Y));
                else Model.ResizeZone(zoneStart,resizeEdges,pull);
                fenceKey="";
            }
        }
        void Up(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){UpdateGesture();EndGesture(true);}}
        void EndGesture(bool released)
        {
            if(captured==null)return;var window=captured;captured=null;window.Capture=false;Model.Editing=false;
            if(window==Ball)
            {
                if(released&&dragged&&Geometry.Distance(pull,PointF.Empty)>=4)Model.LaunchPull(pull,now());
                else if(released)Model.Bounce();else Model.CancelAim();
            }
            Arrow.Hide();pull=PointF.Empty;save();
        }
        public void Update()
        {
            if(disposed)return;
            if(!Model.Settings.DisplayChest){foreach(var window in Windows)window.Hide();return;}
            KeepBelowPet();
            Present(Chest,chestImage,new Point((int)Math.Round(Model.Chest.X-chestImage.Width/2f),(int)Math.Round(Model.Chest.Y-chestImage.Height/2f)));
            if(Model.HasBall)
            {
                // The bounce stays inside the table, including at its upper edge.
                float y=Math.Max(Model.Zone.Top+Model.Radius+3*Model.Scale,Model.Ball.Y-Model.BounceHeight);
                using(var image=ToyArtwork.Ball(Model.Scale))
                    Present(Ball,image,new Point((int)Math.Round(Model.Ball.X-image.Width/2f),(int)Math.Round(y-image.Height/2f)));
            }
            else Ball.Hide();
            if(Model.Settings.DisplayZone)
            {
                string key=Model.Zone.ToString();
                if(key!=fenceKey||!Fence.Visible)
                {
                    var bounds=Rectangle.Ceiling(Model.Zone);
                    using(var image=ToyArtwork.Fence(bounds.Size,Model.Scale))Present(Fence,image,bounds.Location);
                    fenceKey=key;
                }
            }
            else Fence.Hide();
            if(captured==Ball&&dragged&&Geometry.Distance(pull,PointF.Empty)>=4)DrawArrow();else Arrow.Hide();
            KeepBelowPet();
        }
        void DrawArrow()
        {
            var velocity=Model.PullVelocity(pull);float speed=Geometry.Distance(velocity,PointF.Empty);
            if(speed<.001f){Arrow.Hide();return;}
            float length=Math.Min(150*Model.Scale,speed/5)+Model.Radius;
            PointF start=Model.Ball,end=new PointF(start.X+velocity.X/speed*length,start.Y+velocity.Y/speed*length);
            int pad=(int)Math.Ceiling(14*Model.Scale);
            var bounds=Rectangle.FromLTRB((int)Math.Floor(Math.Min(start.X,end.X))-pad,(int)Math.Floor(Math.Min(start.Y,end.Y))-pad,
                (int)Math.Ceiling(Math.Max(start.X,end.X))+pad,(int)Math.Ceiling(Math.Max(start.Y,end.Y))+pad);
            // Clip aim artwork to a connected work area as well.
            var display=pet.Displays.Find(d=>d.Id==Model.DisplayId);bounds=Rectangle.Intersect(bounds,display.Work);
            if(bounds.Width<=0||bounds.Height<=0)return;
            using(var image=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(-bounds.Left,-bounds.Top);
                using(var cap=new AdjustableArrowCap(4,5,true))
                using(var outline=new Pen(Color.White,6*Model.Scale))using(var ink=new Pen(Color.FromArgb(32,115,201),3*Model.Scale))
                {outline.CustomEndCap=cap;ink.CustomEndCap=cap;g.DrawLine(outline,start,end);g.DrawLine(ink,start,end);}
                Present(Arrow,image,bounds.Location);
            }
        }
        static void Present(LayeredWindow window,Bitmap image,Point position)
        {window.Present(image,position);if(!window.Visible)window.Show();}
        void KeepBelowPet()
        {
            IntPtr lowest=petWindow.Handle;
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused)
            {if(window==petWindow.Handle||(crossingWindow!=null&&crossingWindow.Visible&&window==crossingWindow.Handle))lowest=window;return true;},IntPtr.Zero);
            bool changed=layer!=pet.Settings.Layer;layer=pet.Settings.Layer;
            foreach(var window in Windows)
            {
                bool topmost=(Native.GetWindowLongPtr(window.Handle,-20).ToInt64()&8)!=0;
                if(changed||topmost!=(pet.Settings.Layer==LayerMode.OverEverything))
                {window.BehindWindow=IntPtr.Zero;window.SetLayer(pet.Settings.Layer);}
                window.BehindWindow=lowest;
                if(changed||window.Visible)Native.SetWindowPos(window.Handle,lowest,0,0,0,0,0x213);
                lowest=window.Handle;
            }
        }
        public void Dispose()
        {
            if(disposed)return;EndGesture(false);disposed=true;dismissal.Dispose();Menu.Dispose();
            foreach(var window in Windows)window.Close();chestImage.Dispose();
        }
    }

    // Draw at the desktop scale without introducing extra bitmap assets or opaque window backgrounds.
    internal static class ToyArtwork
    {
        public static Bitmap Chest(float scale)
        {
            var image=new Bitmap((int)Math.Ceiling(64*scale),(int)Math.Ceiling(50*scale),PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            using(var wood=new SolidBrush(Color.FromArgb(164,86,42)))
            using(var lid=new SolidBrush(Color.FromArgb(208,128,63)))
            using(var gold=new SolidBrush(Color.FromArgb(255,210,94)))
            using(var edge=new Pen(Color.FromArgb(75,45,38),2))
            {
                g.ScaleTransform(scale,scale);
                g.FillRectangle(wood,5,18,54,27);g.FillRectangle(lid,5,9,54,16);g.FillRectangle(lid,10,5,44,4);
                g.DrawRectangle(edge,5,9,54,36);g.DrawLine(edge,10,5,54,5);g.DrawLine(edge,10,5,5,9);g.DrawLine(edge,54,5,59,9);
                g.FillRectangle(gold,12,9,5,35);g.FillRectangle(gold,47,9,5,35);g.DrawLine(edge,5,25,59,25);
                g.DrawLine(edge,19,36,44,36);g.FillRectangle(gold,27,21,10,12);g.DrawRectangle(edge,27,21,10,12);
                g.FillRectangle(Brushes.SaddleBrown,31,24,3,5);
            }
            return image;
        }
        public static Bitmap Ball(float scale)
        {
            int size=(int)Math.Ceiling(26*scale);var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var fill=new LinearGradientBrush(new Rectangle(0,0,size,size),Color.FromArgb(255,94,88),Color.FromArgb(190,24,37),55))
            using(var edge=new Pen(Color.FromArgb(125,24,39),1.5f*scale))
            {g.SmoothingMode=SmoothingMode.AntiAlias;g.FillEllipse(fill,scale,scale,size-2*scale,size-2*scale);g.DrawEllipse(edge,scale,scale,size-2*scale,size-2*scale);g.FillEllipse(Brushes.MistyRose,6*scale,4*scale,6*scale,4*scale);}
            return image;
        }
        public static Bitmap Fence(Size size,float scale)
        {
            var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var hit=new Pen(Color.FromArgb(1,255,255,255),12*scale))
            using(var white=new Pen(Color.FromArgb(230,255,255,255),5*scale))using(var line=new Pen(Color.FromArgb(32,115,201),2*scale))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;float inset=3*scale;
                var rect=new RectangleF(inset,inset,size.Width-2*inset-1,size.Height-2*inset-1);
                g.DrawRectangle(hit,rect.X,rect.Y,rect.Width,rect.Height);
                g.DrawRectangle(white,rect.X,rect.Y,rect.Width,rect.Height);line.DashStyle=DashStyle.Dash;
                g.DrawRectangle(line,rect.X,rect.Y,rect.Width,rect.Height);line.DashStyle=DashStyle.Solid;
                foreach(var p in new[]{new PointF(rect.Left,rect.Top),new PointF(rect.Right,rect.Top),new PointF(rect.Left,rect.Bottom),new PointF(rect.Right,rect.Bottom)})
                {g.FillRectangle(Brushes.White,p.X-3*scale,p.Y-3*scale,6*scale,6*scale);g.DrawRectangle(line,p.X-3*scale,p.Y-3*scale,6*scale,6*scale);}
                float x=size.Width/2f,y=size.Height/2f,r=14*scale;
                g.FillEllipse(Brushes.White,x-r,y-r,r*2,r*2);g.DrawEllipse(line,x-r,y-r,r*2,r*2);
                g.DrawLine(line,x-8*scale,y,x+8*scale,y);g.DrawLine(line,x,y-8*scale,x,y+8*scale);
                g.DrawLine(line,x-8*scale,y,x-4*scale,y-4*scale);g.DrawLine(line,x+8*scale,y,x+4*scale,y+4*scale);
                g.DrawLine(line,x,y-8*scale,x+4*scale,y-4*scale);g.DrawLine(line,x,y+8*scale,x-4*scale,y+4*scale);
            }
            return image;
        }
    }
}
