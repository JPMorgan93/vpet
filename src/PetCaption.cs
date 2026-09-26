using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Vpet
{
    internal static class PetCaption
    {
        public static Bitmap Draw(string name,float scale,Bitmap reaction,int maxWidth)
        {
            int gap=Math.Max(1,(int)Math.Ceiling(3*scale)),height=(int)Math.Ceiling(26*scale);
            using(var font=new Font("Segoe UI",11*scale,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var measure=new Bitmap(1,1))using(var graphics=Graphics.FromImage(measure))
            {
                int nameWidth=Math.Min(Math.Min(maxWidth,(int)Math.Ceiling(240*scale)),(int)Math.Ceiling(graphics.MeasureString(name,font).Width)+gap*4);
                int width=Math.Max(nameWidth,reaction==null?1:reaction.Width);
                int top=reaction==null?0:reaction.Height+gap;
                var bitmap=new Bitmap(Math.Max(1,width),top+height+gap,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(bitmap))
                using(var background=new SolidBrush(Color.FromArgb(240,255,255,255)))
                using(var text=new SolidBrush(Color.FromArgb(54,45,70)))
                using(var border=new Pen(Color.FromArgb(160,130,160,190)))
                using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})
                {
                    if(reaction!=null)g.DrawImageUnscaled(reaction,(width-reaction.Width)/2,0);
                    var box=new Rectangle((width-nameWidth)/2,top,Math.Max(1,nameWidth-1),height-1);
                    g.FillRectangle(background,box);g.DrawRectangle(border,box);
                    g.DrawString(name,font,text,new RectangleF(box.X+gap,box.Y,Math.Max(1,box.Width-gap*2),box.Height),format);
                }
                return bitmap;
            }
        }
    }
}
