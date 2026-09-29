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
        {string[] ranks={"1","2","3","4","5","6","7","8","9","10","J","Q","K"};return ranks[card%13]+new[]{"♠","♥","♣","♦"}[card/13];}
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
            using(var g=Graphics.FromImage(image))using(var gold=new Pen(Color.DarkGoldenrod,2*scale))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                // Rotation about the horizontal X axis foreshortens height, not width.
                float height=flip>0?Math.Max(3*scale,(size-4*scale)*(float)Math.Abs(Math.Cos(flip*Math.PI*6))):size-4*scale;
                var rect=new RectangleF(2*scale,(size-height)/2,size-4*scale,height);
                g.FillEllipse(Brushes.Gold,rect);g.DrawEllipse(gold,rect);
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
                var rect=new RectangleF(-21,-30,42,60);using(var path=Artwork.Rounded(rect,5))
                using(var edge=new Pen(MakerUi.Purple,2)){g.FillPath(revealed?Brushes.White:Brushes.Lavender,path);g.DrawPath(edge,path);}
                using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                {
                    if(revealed&&value>=0)using(var font=new Font("Segoe UI Symbol",19,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(CardColor(value)))g.DrawString(CardText(value),font,ink,rect,format);
                    else
                    {
                        using(var font=new Font("Segoe UI Symbol",23,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(MakerUi.Purple))
                        {g.DrawString("↑",font,ink,new RectangleF(-21,-30,42,30),format);g.DrawString("↓",font,ink,new RectangleF(-21,0,42,30),format);}
                        g.DrawLine(Pens.MediumPurple,-17,0,17,0);
                    }
                }
            }
            return image;
        }
        internal static Bitmap Die(float scale,float angle,int number,bool rolling)
        {
            scale*=ToyModel.DieSizeMultiplier;
            int size=(int)Math.Ceiling(46*scale);var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var edge=new Pen(Color.FromArgb(66,46,105),1.5f))
            using(var fill=new SolidBrush(Color.FromArgb(196,175,238)))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(size/2f,size/2f);g.ScaleTransform(scale,scale);g.RotateTransform(angle);
                var points=new PointF[6];for(int i=0;i<6;i++){double a=(i*60-90)*Math.PI/180;points[i]=new PointF((float)Math.Cos(a)*21,(float)Math.Sin(a)*21);}
                g.FillPolygon(fill,points);g.DrawPolygon(edge,points);var a1=new PointF(0,-11);var a2=new PointF(-11,9);var a3=new PointF(11,9);
                g.DrawPolygon(edge,new[]{a1,a2,a3});g.DrawLine(edge,points[0],a1);g.DrawLine(edge,points[1],a1);g.DrawLine(edge,points[1],a3);g.DrawLine(edge,points[2],a3);g.DrawLine(edge,points[3],a3);g.DrawLine(edge,points[3],a2);g.DrawLine(edge,points[4],a2);g.DrawLine(edge,points[5],a2);g.DrawLine(edge,points[5],a1);
                if(!rolling)using(var font=new Font("Segoe UI",10,FontStyle.Bold,GraphicsUnit.Pixel))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                    g.DrawString(number.ToString(),font,Brushes.DarkSlateBlue,new RectangleF(-10,-8,20,17),format);
            }
            return image;
        }
        public void Dispose(){heads.Dispose();tails.Dispose();foreach(var image in emotes.Values)image.Dispose();}
    }
}
