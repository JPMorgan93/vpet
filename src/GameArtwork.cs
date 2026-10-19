using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Vpet
{
    // These purpose-specific announcements never enter the random/default reaction list.
    internal sealed class GameArtwork : IDisposable
    {
        readonly Bitmap heads,tails;
        readonly Dictionary<string,Bitmap> emotes=new Dictionary<string,Bitmap>();
        internal GameArtwork(string referenceDirectory)
        {heads=SpriteSet.ReadPng(Path.Combine(referenceDirectory,"Heads.png"),512,512);tails=SpriteSet.ReadPng(Path.Combine(referenceDirectory,"Tails.png"),512,512);}
        internal static string CardText(int card)
        {string[] ranks={"A","2","3","4","5","6","7","8","9","10","J","Q","K"};return ranks[card%13]+new[]{"♠","♥","♣","♦"}[card/13];}
        internal static Color CardColor(int card){return card/13==1||card/13==3?Color.Firebrick:Color.FromArgb(35,30,50);}
        internal Bitmap Emote(ToyAnnouncement announcement)
        {
            if(announcement.Kind==SpecialEmoteKind.Heads)return heads;
            if(announcement.Kind==SpecialEmoteKind.Tails)return tails;
            Bitmap image;if(emotes.TryGetValue(announcement.Key,out image))return image;
            image=new Bitmap(192,160,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var font=new Font("Segoe UI Symbol",62,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(announcement.Kind==SpecialEmoteKind.Card?CardColor(announcement.Value):MakerUi.Purple))
            using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
            {g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.DrawString(announcement.Kind==SpecialEmoteKind.Card?CardText(announcement.Value):announcement.Value.ToString(),font,brush,new RectangleF(0,0,192,160),format);}
            emotes.Add(announcement.Key,image);return image;
        }
        internal static Bitmap Coin(float scale,float flip)
        {
            var size=ItemArtwork.CanvasSize(scale);var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                // Rotation about the horizontal X axis foreshortens height, not width.
                float squeeze=flip>0?Math.Max(1/16f,(float)Math.Abs(Math.Cos(flip*Math.PI*6))):1;
                float height=size.Height*squeeze;
                ItemArtwork.Draw(g,"Gold Coin.png",new RectangleF(0,(size.Height-height)/2,size.Width,height),ItemArtwork.Canvas);
            }
            return image;
        }
        internal static Bitmap Card(float scale,int value,bool revealed,float flip)
        {
            var size=ItemArtwork.CanvasSize(scale);var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(size.Width/2f,size.Height/2f);
                float pixel=ItemArtwork.PixelScale(scale),squeeze=flip>0?Math.Max(.05f,(float)Math.Abs(Math.Cos(flip*Math.PI))):1;g.ScaleTransform(pixel*squeeze,pixel);
                ItemArtwork.Draw(g,"Card.png",new RectangleF(-16,-16,32,32),ItemArtwork.Canvas);
                if(revealed&&value>=0)
                {
                    var rect=new RectangleF(-7,-10,14,20);g.FillRectangle(Brushes.White,rect);
                    using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                    using(var font=new Font("Segoe UI Symbol",9,FontStyle.Bold,GraphicsUnit.Pixel))
                    using(var ink=new SolidBrush(CardColor(value)))g.DrawString(CardText(value),font,ink,rect,format);
                }
            }
            return image;
        }
        internal static Bitmap Die(float scale,float angle,int number,bool rolling)
        {
            var size=ItemArtwork.CanvasSize(scale);var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(size.Width/2f,size.Height/2f);g.ScaleTransform(ItemArtwork.PixelScale(scale),ItemArtwork.PixelScale(scale));g.RotateTransform(angle);
                ItemArtwork.Draw(g,"D20 Dice.png",new RectangleF(-16,-16,32,32),ItemArtwork.Canvas);
                if(!rolling)using(var font=new FontFamily("Segoe UI"))using(var label=new GraphicsPath())using(var format=StringFormat.GenericTypographic)
                {
                    label.AddString(number.ToString(),font,(int)FontStyle.Bold,4.5f,PointF.Empty,format);
                    var ink=label.GetBounds();float fit=Math.Min(1,Math.Min(5.5f/ink.Width,3/ink.Height));
                    // Keep the ink inside the wider lower portion of the central triangular face.
                    using(var placement=new Matrix(fit,0,0,fit,-(ink.X+ink.Width/2)*fit,2.75f-(ink.Y+ink.Height/2)*fit))label.Transform(placement);
                    g.FillPath(Brushes.White,label);
                }
            }
            return image;
        }
        public void Dispose(){heads.Dispose();tails.Dispose();foreach(var image in emotes.Values)image.Dispose();}
    }
}
