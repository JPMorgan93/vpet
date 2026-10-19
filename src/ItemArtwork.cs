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
        static readonly Dictionary<string,Bitmap> sources=new Dictionary<string,Bitmap>();
        internal static Bitmap Source(string name)
        {
            Bitmap image;
            if(!sources.TryGetValue(name,out image))
            {
                image=SpriteSet.ReadPng(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference",name),32,32);
                sources.Add(name,image);
            }
            return image;
        }
        internal static void Pixels(Graphics graphics)
        {graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;}
        internal static RectangleF Fit(Rectangle source,RectangleF target)
        {
            float factor=Math.Min(target.Width/source.Width,target.Height/source.Height);
            float width=source.Width*factor,height=source.Height*factor;
            return new RectangleF(target.X+(target.Width-width)/2,target.Y+(target.Height-height)/2,width,height);
        }
        internal static void Draw(Graphics graphics,string name,RectangleF target,Rectangle? source=null)
        {
            var image=Source(name);var crop=source??SpriteProject.VisibleBounds(image);
            Pixels(graphics);graphics.DrawImage(image,target,crop,GraphicsUnit.Pixel);
        }
        internal static Bitmap Fit(string name,Size size,float padding=0,Rectangle? source=null)
        {
            var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            var crop=source??SpriteProject.VisibleBounds(Source(name));
            using(var graphics=Graphics.FromImage(image))
                Draw(graphics,name,Fit(crop,new RectangleF(padding,padding,size.Width-2*padding,size.Height-2*padding)),crop);
            return image;
        }
    }
}
