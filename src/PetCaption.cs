using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Vpet
{
    internal static class PetCaption
    {
        public static int Footroom(float scale) { return (int)Math.Ceiling(26*scale)+Math.Max(1,(int)Math.Ceiling(3*scale)); }
        public static Bitmap Draw(string name,float scale,Bitmap reaction,int maxWidth,int petHeight,bool reactionBelow)
        {
            int gap=Math.Max(1,(int)Math.Ceiling(3*scale)),height=(int)Math.Ceiling(26*scale);
            using(var font=new Font("Segoe UI",11*scale,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var measure=new Bitmap(1,1))using(var graphics=Graphics.FromImage(measure))
            {
                int nameWidth=Math.Min(Math.Min(maxWidth,(int)Math.Ceiling(240*scale)),(int)Math.Ceiling(graphics.MeasureString(name,font).Width)+gap*4);
                int width=Math.Max(nameWidth,reaction==null?1:reaction.Width);
                int above=reaction!=null&&!reactionBelow?reaction.Height:0;
                int nameTop=above+petHeight+gap;
                var bitmap=new Bitmap(Math.Max(1,width),petHeight+height+gap+(reaction==null?0:reaction.Height+(reactionBelow?gap:0)),PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(bitmap))
                using(var text=new SolidBrush(Color.FromArgb(54,45,70)))
                using(var border=new Pen(Color.White,3*scale){LineJoin=LineJoin.Round})
                using(var path=new GraphicsPath())
                using(var format=new StringFormat(StringFormat.GenericTypographic){FormatFlags=StringFormatFlags.NoWrap})
                {
                    if(reaction!=null)g.DrawImageUnscaled(reaction,(width-reaction.Width)/2,reactionBelow?nameTop+height+gap:0);
                    string fitted=name;
                    while(fitted.Length>0&&graphics.MeasureString(fitted==name?fitted:fitted+"…",font).Width>nameWidth-gap*4)
                    {int[] elements=System.Globalization.StringInfo.ParseCombiningCharacters(fitted);fitted=fitted.Substring(0,elements[elements.Length-1]);}
                    if(fitted!=name)fitted+="…";
                    path.AddString(fitted,font.FontFamily,(int)FontStyle.Bold,font.Size,Point.Empty,format);
                    var bounds=path.GetBounds();
                    using(var transform=new Matrix())
                    {transform.Translate((width-bounds.Width)/2-bounds.X,nameTop+(height-bounds.Height)/2-bounds.Y);path.Transform(transform);}
                    g.SmoothingMode=SmoothingMode.AntiAlias;
                    g.DrawPath(border,path);g.FillPath(text,path);
                }
                return bitmap;
            }
        }
    }
}
