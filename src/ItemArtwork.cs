using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Vpet
{
    // Keep the tiny reference PNGs decoded for the lifetime of the app, including animated redraws.
    internal static class ItemArtwork
    {
        internal const int Cell=32;
        internal static readonly Rectangle Canvas=new Rectangle(0,0,Cell,Cell);
        static readonly Dictionary<string,Bitmap> sources=new Dictionary<string,Bitmap>();
        static readonly Dictionary<string,Rectangle> bounds=new Dictionary<string,Rectangle>();
        static readonly Dictionary<string,float> radii=new Dictionary<string,float>();
        internal static float PixelScale(float scale){return DisplayArea.SpriteMagnification*scale;}
        internal static float CardMagnification {get{return Bounds("D20 Dice.png").Height/(float)Bounds("Card.png").Height;}}
        internal static Size CanvasSize(float scale){int side=(int)Math.Ceiling(Cell*PixelScale(scale));return new Size(side,side);}
        internal static SizeF HalfSize(string name,float scale)
        {var box=Bounds(name);float pixel=PixelScale(scale)*(name=="Card.png"?CardMagnification:1);return new SizeF(Math.Max(Cell/2-box.Left,box.Right-Cell/2)*pixel,Math.Max(Cell/2-box.Top,box.Bottom-Cell/2)*pixel);}
        internal static float Radius(string name,float scale)
        {
            float radius;
            if(!radii.TryGetValue(name,out radius))
            {
                var source=Source(name);
                for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)if(source.GetPixel(x,y).A>0)
                {float dx=Math.Max(Math.Abs(x-Cell/2),Math.Abs(x+1-Cell/2)),dy=Math.Max(Math.Abs(y-Cell/2),Math.Abs(y+1-Cell/2));radius=Math.Max(radius,(float)Math.Sqrt(dx*dx+dy*dy));}
                radii.Add(name,radius);
            }
            return radius*PixelScale(scale);
        }
        internal static Bitmap Source(string name)
        {
            Bitmap image;
            if(!sources.TryGetValue(name,out image))
            {
                image=SpriteSet.ReadPng(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference",name),name=="Joystick.png"?512:Cell,name=="Joystick.png"?512:Cell);
                if(image.Width!=Cell||image.Height!=Cell)
                {
                    var normalized=new Bitmap(Cell,Cell,PixelFormat.Format32bppArgb);
                    using(image)using(var graphics=Graphics.FromImage(normalized))using(var attributes=new ImageAttributes())
                    {
                        graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;attributes.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image,Canvas,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
                    }
                    image=normalized;
                }
                sources.Add(name,image);
                bounds.Add(name,SpriteProject.VisibleBounds(image));
            }
            return image;
        }
        internal static Bitmap Sprite(string name,float scale)
        {
            var size=CanvasSize(scale);var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var graphics=Graphics.FromImage(image))Draw(graphics,name,new RectangleF(0,0,size.Width,size.Height),Canvas);
            return image;
        }
        internal static Rectangle Bounds(string name){Source(name);return bounds[name];}
        internal static void Pixels(Graphics graphics)
        {graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;}
        internal static void Draw(Graphics graphics,string name,RectangleF target,Rectangle? source=null)
        {
            var image=Source(name);var crop=source??Bounds(name);
            Pixels(graphics);graphics.DrawImage(image,target,crop,GraphicsUnit.Pixel);
        }
    }
}
