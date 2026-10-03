using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class SpotlightFrame
    {
        public readonly Rectangle Bounds;
        public readonly Bitmap Image;
        public SpotlightFrame(Rectangle bounds,Bitmap image){Bounds=bounds;Image=image;}
    }
    internal sealed class FindPetSpotlight : IDisposable
    {
        readonly Dictionary<Rectangle,LayeredWindow> windows=new Dictionary<Rectangle,LayeredWindow>();
        double started=double.NegativeInfinity,lastPaint=double.NegativeInfinity;
        internal IEnumerable<LayeredWindow> Windows {get{return windows.Values;}}
        public bool Active {get;private set;}
        public void Trigger(double now){started=now;lastPaint=double.NegativeInfinity;Active=true;}
        internal static float Fade(double elapsed){return (float)Math.Max(0,Math.Min(1,1-elapsed));}
        internal static RectangleF Circle(Rectangle body)
        {
            float radius=(float)Math.Sqrt((double)body.Width*body.Width+(double)body.Height*body.Height)/2+20;
            return new RectangleF(body.Left+body.Width/2f-radius,body.Top+body.Height/2f-radius,radius*2,radius*2);
        }
        internal static Bitmap Draw(Rectangle screen,IEnumerable<SpotlightFrame> frames)
        {
            var image=new Bitmap(screen.Width,screen.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(170,0,0,0));g.TranslateTransform(-screen.Left,-screen.Top);g.SmoothingMode=SmoothingMode.AntiAlias;
                var visible=frames.Where(frame=>frame.Image!=null&&Circle(frame.Bounds).IntersectsWith(screen)).ToArray();
                g.CompositingMode=CompositingMode.SourceCopy;
                foreach(var frame in visible){var circle=Circle(frame.Bounds);g.FillEllipse(Brushes.Transparent,circle);}
                g.CompositingMode=CompositingMode.SourceOver;
                // A temporary copy reveals pets underneath other apps without changing Window Location.
                foreach(var frame in visible)g.DrawImage(frame.Image,frame.Bounds,new Rectangle(Point.Empty,frame.Image.Size),GraphicsUnit.Pixel);
                using(var border=new Pen(Color.FromArgb(235,235,221,255),3))foreach(var frame in visible)g.DrawEllipse(border,Circle(frame.Bounds));
            }
            return image;
        }
        public void Update(double now,SpotlightFrame[] frames,Rectangle[] screens)
        {
            if(!Active)return;
            float fade=Fade(now-started);if(fade<=0){Hide();return;}
            if(now-lastPaint<1.0/30)return;lastPaint=now;
            foreach(var screen in windows.Keys.Where(s=>!screens.Contains(s)).ToArray()){windows[screen].Close();windows[screen].Dispose();windows.Remove(screen);}
            foreach(var screen in screens)
            {
                LayeredWindow window;
                if(!windows.TryGetValue(screen,out window))
                {
                    window=new LayeredWindow(true){Text="Find My Vpet spotlight"};IntPtr handle=window.Handle;
                    Native.BackgroundAdornments.Add(handle);window.FormClosed+=delegate{Native.BackgroundAdornments.Remove(handle);};
                    window.SetLayer(LayerMode.OverEverything);windows.Add(screen,window);
                }
                using(var image=Draw(screen,frames))window.Present(image,screen.Location,(byte)Math.Round(255*fade));
                if(!window.Visible)window.Show();
                Native.SetWindowPos(window.Handle,new IntPtr(-1),0,0,0,0,0x213);
            }
        }
        public void Hide(){Active=false;foreach(var window in windows.Values)window.Hide();}
        public void Dispose(){foreach(var window in windows.Values){window.Close();window.Dispose();}windows.Clear();Active=false;}
    }
}
