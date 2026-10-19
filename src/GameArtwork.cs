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
            int size=(int)Math.Ceiling(46*scale);var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                // Rotation about the horizontal X axis foreshortens height, not width.
                float height=flip>0?Math.Max(3*scale,(size-4*scale)*(float)Math.Abs(Math.Cos(flip*Math.PI*6))):size-4*scale;
                var rect=new RectangleF(2*scale,(size-height)/2,size-4*scale,height);
                ItemArtwork.Draw(g,"Gold Coin.png",rect);
            }
            return image;
        }
        internal static Bitmap Card(float scale,int value,bool revealed,float flip)
        {
            int w=(int)Math.Ceiling(46*scale),h=(int)Math.Ceiling(64*scale);var image=new Bitmap(w,h,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(w/2f,h/2f);
                float squeeze=flip>0?Math.Max(.05f,(float)Math.Abs(Math.Cos(flip*Math.PI))):1;g.ScaleTransform(scale*squeeze,scale);
                var crop=SpriteProject.VisibleBounds(ItemArtwork.Source("Card.png"));
                var rect=ItemArtwork.Fit(crop,new RectangleF(-21,-30,42,60));ItemArtwork.Draw(g,"Card.png",rect,crop);
                if(revealed&&value>=0)
                {
                    float border=rect.Width*2/crop.Width;rect.Inflate(-border,-border);g.FillRectangle(Brushes.White,rect);
                    using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                    using(var font=new Font("Segoe UI Symbol",19,FontStyle.Bold,GraphicsUnit.Pixel))
                    using(var ink=new SolidBrush(CardColor(value)))g.DrawString(CardText(value),font,ink,rect,format);
                }
            }
            return image;
        }
        internal static Bitmap Die(float scale,float angle,int number,bool rolling)
        {
            scale*=ToyModel.DieSizeMultiplier;
            int size=(int)Math.Ceiling(46*scale);var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(size/2f,size/2f);g.ScaleTransform(scale,scale);g.RotateTransform(angle);
                var crop=SpriteProject.VisibleBounds(ItemArtwork.Source("D20 Dice.png"));
                ItemArtwork.Draw(g,"D20 Dice.png",ItemArtwork.Fit(crop,new RectangleF(-21,-21,42,42)),crop);
                if(!rolling)using(var font=new FontFamily("Segoe UI"))using(var label=new GraphicsPath())using(var format=StringFormat.GenericTypographic)
                {
                    label.AddString(number.ToString(),font,(int)FontStyle.Bold,7,PointF.Empty,format);
                    var ink=label.GetBounds();float fit=Math.Min(1,Math.Min(8/ink.Width,5/ink.Height));
                    // Keep the ink inside the wider lower portion of the central triangular face.
                    using(var placement=new Matrix(fit,0,0,fit,-(ink.X+ink.Width/2)*fit,4.5f-(ink.Y+ink.Height/2)*fit))label.Transform(placement);
                    g.FillPath(Brushes.White,label);
                }
            }
            return image;
        }
        public void Dispose(){heads.Dispose();tails.Dispose();foreach(var image in emotes.Values)image.Dispose();}
    }
}
