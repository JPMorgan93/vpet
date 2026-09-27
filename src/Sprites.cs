using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Vpet
{
    public sealed class SpriteSet : IDisposable
    {
        public Bitmap Sheet { get; private set; }
        public Size Cell { get; private set; }
        public bool HasDiagonals {get;private set;}
        public int[] Counts {get;private set;}
        readonly Bitmap[][] frames=new Bitmap[10][];
        readonly Bitmap[][] mirrors=new Bitmap[10][];
        public SpriteSet(Bitmap sheet) : this(sheet,true,new[]{4,4,4,4,4,5,5,5,5,5}) {}
        public SpriteSet(Bitmap sheet,bool diagonals,int[] counts)
        {
            Sheet=sheet;Cell=new Size(sheet.Width/5,sheet.Height/10);HasDiagonals=diagonals;Counts=(int[])counts.Clone();
            for(int row=0;row<10;row++)
            {
                int count=counts[row];frames[row]=new Bitmap[count];mirrors[row]=new Bitmap[count];
                for(int col=0;col<count;col++)
                {
                    frames[row][col]=sheet.Clone(new Rectangle(col*Cell.Width,row*Cell.Height,Cell.Width,Cell.Height),PixelFormat.Format32bppArgb);
                    mirrors[row][col]=(Bitmap)frames[row][col].Clone();mirrors[row][col].RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
            }
        }
        public Bitmap Frame(bool walk,int facing,int index)
        {
            if(!HasDiagonals&&(facing%2==1))facing=facing==1||facing==7?0:4;
            // E, SE, S, SW, W, NW, N, NE.
            int[] rowMap={2,4,1,4,2,3,0,3};int row=rowMap[facing]+(walk?5:0);
            return (facing==0||facing==1||facing==7?mirrors:frames)[row][Math.Max(0,index)%Counts[row]];
        }
        public int ResolveFacing(int facing,PointF movement,int previous)
        {
            if(HasDiagonals)return facing;
            if(movement.X==0&&movement.Y==0)return facing%2==0?facing:previous;
            double angle=Math.Atan2(movement.Y,movement.X)*180/Math.PI;
            double difference=((angle-previous*45+540)%360)-180;
            if(previous%2==0&&Math.Abs(difference)<=50)return previous;
            return (((int)Math.Floor(angle/90+.5))%4+4)%4*2;
        }
        public void SavePackage(string path)
        {SpritePackage.Write(path,new SpriteManifest{Version=2,Kind="sprite",Width=Cell.Width,Height=Cell.Height,Diagonals=HasDiagonals,Counts=Counts},Sheet);}
        public static SpriteSet FromReference(string path)
        {
            int[,] bands={{38,62},{108,134},{177,203},{239,265},{305,335},{372,396},{445,471},{520,546},{596,621},{669,699}};
            using(var source=new Bitmap(path))
            {
                if(source.Width!=154||source.Height!=704)throw new InvalidDataException("The bundled artwork reference has unexpected dimensions.");
                var sheet=new Bitmap(160,360,PixelFormat.Format32bppArgb);
                try
                {
                    for(int row=0;row<10;row++)
                    {
                        int top=bands[row,0],bottom=bands[row,1],start=-1,col=0;
                        for(int x=0;x<=source.Width;x++)
                        {
                            bool occupied=false;
                            if(x<source.Width)for(int y=top;y<=bottom;y++){if(!White(source.GetPixel(x,y))){occupied=true;break;}}
                            if(occupied&&start<0)start=x;
                            if(!occupied&&start>=0)
                            {
                                int width=x-start,height=bottom-top+1;
                                using(var crop=source.Clone(new Rectangle(start,top,width,height),PixelFormat.Format32bppArgb))
                                {
                                    ClearExteriorWhite(crop);
                                    using(var g=Graphics.FromImage(sheet))g.DrawImageUnscaled(crop,col*32+(32-width)/2,row*36+34-height);
                                }
                                col++;start=-1;
                            }
                        }
                        if(col!=(row<5?4:5))throw new InvalidDataException("Could not identify the expected frames in reference row "+(row+1)+".");
                    }
                    return new SpriteSet(sheet);
                }
                catch {sheet.Dispose();throw;}
            }
        }
        static bool White(Color c){return c.R>=245&&c.G>=245&&c.B>=245;}
        static void ClearExteriorWhite(Bitmap image)
        {
            // Flood only the background, preserving enclosed white details such as eye highlights.
            var queue=new Queue<Point>();var seen=new bool[image.Width,image.Height];
            for(int x=0;x<image.Width;x++){queue.Enqueue(new Point(x,0));queue.Enqueue(new Point(x,image.Height-1));}
            for(int y=0;y<image.Height;y++){queue.Enqueue(new Point(0,y));queue.Enqueue(new Point(image.Width-1,y));}
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(p.X<0||p.Y<0||p.X>=image.Width||p.Y>=image.Height||seen[p.X,p.Y])continue;
                seen[p.X,p.Y]=true;if(!White(image.GetPixel(p.X,p.Y)))continue;
                image.SetPixel(p.X,p.Y,Color.Transparent);
                queue.Enqueue(new Point(p.X-1,p.Y));queue.Enqueue(new Point(p.X+1,p.Y));queue.Enqueue(new Point(p.X,p.Y-1));queue.Enqueue(new Point(p.X,p.Y+1));
            }
        }
        public static Bitmap ReadPng(string path,int maxWidth,int maxHeight)
        {
            if(!string.Equals(Path.GetExtension(path),".png",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Choose a PNG image.");
            byte[] data=File.ReadAllBytes(path);
            if(data.Length<24||data.Length>8*1024*1024)throw new InvalidDataException("PNG file is empty or too large (8 MB limit).");
            byte[] signature={137,80,78,71,13,10,26,10};for(int i=0;i<8;i++)if(data[i]!=signature[i])throw new InvalidDataException("The file is not a valid PNG.");
            uint width=((uint)data[16]<<24)|((uint)data[17]<<16)|((uint)data[18]<<8)|data[19];
            uint height=((uint)data[20]<<24)|((uint)data[21]<<16)|((uint)data[22]<<8)|data[23];
            if(width<1||height<1||width>maxWidth||height>maxHeight)throw new InvalidDataException("Image must be no larger than "+maxWidth+" × "+maxHeight+" pixels.");
            using(var stream=new MemoryStream(data))using(var image=Image.FromStream(stream,true,true))
            {var bitmap=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bitmap)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(image,0,0);}return bitmap;}
        }
        public static Bitmap BlankTemplate()
        {
            var sheet=new Bitmap(500,1500,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(sheet))
            using(var border=new Pen(Color.FromArgb(150,105,85,160)))
            using(var text=new SolidBrush(Color.FromArgb(210,70,50,100)))
            using(var font=new Font("Segoe UI",9,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
            {
                string[] directions={"Up","Down","Left","Up-left","Down-left"};
                for(int row=0;row<10;row++)for(int col=0;col<5;col++)
                {
                    int x=col*100,y=row*150;
                    g.DrawRectangle(border,x,y,99,149);
                    string label=row<5&&col==4?"UNUSED\nLeave transparent":(row<5?"IDLE":"WALK")+"\n"+directions[row%5]+"\nFrame "+(col+1)+"\n\n100 x 150 px";
                    g.DrawString(label,font,text,new RectangleF(x+4,y+4,92,142),format);
                }
            }
            return sheet;
        }
        public static SpriteSet Import(string path)
        {
            if(string.Equals(Path.GetExtension(path),".vpetsprite",StringComparison.OrdinalIgnoreCase))
            {SpriteManifest data;var atlas=SpritePackage.Read(path,"sprite",out data);return new SpriteSet(atlas,data.Diagonals,data.Counts);}
            var bitmap=ReadPng(path,500,1500);
            try
            {
                if(bitmap.Width%5!=0||bitmap.Height%10!=0)throw new InvalidDataException("Use a five-column, ten-row grid with equal-size cells.");
                int w=bitmap.Width/5,h=bitmap.Height/10;
                bool transparent=false;
                for(int row=0;row<10;row++)for(int col=0;col<5;col++)
                {
                    bool visible=false;
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                    {
                        byte alpha=bitmap.GetPixel(col*w+x,row*h+y).A;
                        if(alpha<255)transparent=true;if(alpha>0)visible=true;
                    }
                    if(row<5&&col==4&&visible)throw new InvalidDataException("Column five of each idle row must be transparent.");
                    if(!(row<5&&col==4)&&!visible)throw new InvalidDataException("Animation row "+(row+1)+", frame "+(col+1)+" is empty.");
                }
                if(!transparent)throw new InvalidDataException("The sprite sheet must have a transparent background.");
                return new SpriteSet(bitmap);
            }
            catch {bitmap.Dispose();throw;}
        }
        public void Dispose()
        {
            foreach(var row in frames)if(row!=null)foreach(var f in row)if(f!=null)f.Dispose();
            foreach(var row in mirrors)if(row!=null)foreach(var f in row)if(f!=null)f.Dispose();
            Sheet.Dispose();
        }
    }

    public sealed class CustomEmote : IDisposable
    {
        public string Name;public Bitmap Image;
        public void Dispose(){Image.Dispose();}
    }

    internal static class Artwork
    {
        public const int MaximumEmoteSize=512;
        public static Bitmap DisplayFragment(Bitmap frame,Size size,Point location,Rectangle work)
        {
            using(var scaled=Scale(frame,size))
            {
                var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
                using(var graphics=Graphics.FromImage(image))
                {
                    graphics.SetClip(new Rectangle(work.Left-location.X,work.Top-location.Y,work.Width,work.Height));
                    graphics.DrawImageUnscaled(scaled,0,0);
                }
                return image;
            }
        }
        public static Bitmap Scale(Bitmap source,Size size)
        {
            var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawImage(source,new Rectangle(Point.Empty,size),0,0,source.Width,source.Height,GraphicsUnit.Pixel);
            }
            return image;
        }
        public static Bitmap Bubble(int reaction,Bitmap custom,float scale,bool below)
        {
            int w=(int)Math.Ceiling(68*scale),h=(int)Math.Ceiling(62*scale);var image=new Bitmap(w,h,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            {
                g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;
                float top=below?8:0;
                using(var path=BubbleOutline(below))g.FillPath(Brushes.White,path);
                var contentState=g.Save();
                using(var body=Rounded(new RectangleF(3,top+3,61,46),10))g.SetClip(body);
                if(custom!=null)
                {
                    // Transparent canvas padding must not displace the visible emote.
                    Rectangle visible=VisibleBounds(custom);
                    if(!visible.IsEmpty)
                    {
                        float ratio=Math.Min(46f/visible.Width,42f/visible.Height);
                        float cw=visible.Width*ratio,ch=visible.Height*ratio;
                        g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                        using(var cropped=custom.Clone(visible,PixelFormat.Format32bppArgb))
                        using(var attributes=new ImageAttributes())
                        {
                            attributes.SetWrapMode(WrapMode.TileFlipXY);
                            var target=new[]{new PointF(33.5f-cw/2,top+26-ch/2),new PointF(33.5f+cw/2,top+26-ch/2),new PointF(33.5f-cw/2,top+26+ch/2)};
                            g.DrawImage(cropped,target,new RectangleF(0,0,cropped.Width,cropped.Height),GraphicsUnit.Pixel,attributes);
                        }
                    }
                }
                else DrawReaction(g,reaction,top);
                g.Restore(contentState);
                // Stroke the single closed body-and-tail path last, so image backgrounds
                // and the pointer never erase any part of the outer border.
                using(var path=BubbleOutline(below))using(var pen=new Pen(Color.FromArgb(160,143,188),1.5f)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,path);}
            }
            return image;
        }
        static Rectangle VisibleBounds(Bitmap image)
        {
            int left=image.Width,top=image.Height,right=-1,bottom=-1;
            for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
                if(image.GetPixel(x,y).A>0){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
            return right<left?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
        static GraphicsPath BubbleOutline(bool below)
        {
            float top=below?9:1,bottom=top+50;const float left=1,right=66,r=12;
            var path=new GraphicsPath();path.StartFigure();
            if(below){path.AddLine(left+r,top,25,top);path.AddLine(25,top,34,1);path.AddLine(34,1,39,top);path.AddLine(39,top,right-r,top);}
            else path.AddLine(left+r,top,right-r,top);
            path.AddArc(right-2*r,top,2*r,2*r,270,90);path.AddLine(right,top+r,right,bottom-r);
            path.AddArc(right-2*r,bottom-2*r,2*r,2*r,0,90);
            if(!below){path.AddLine(right-r,bottom,39,bottom);path.AddLine(39,bottom,34,60);path.AddLine(34,60,25,bottom);path.AddLine(25,bottom,left+r,bottom);}
            else path.AddLine(right-r,bottom,left+r,bottom);
            path.AddArc(left,bottom-2*r,2*r,2*r,90,90);path.AddLine(left,bottom-r,left,top+r);
            path.AddArc(left,top,2*r,2*r,180,90);path.CloseFigure();return path;
        }
        static void DrawReaction(Graphics graphics,int index,float top)
        {
            graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
            graphics.DrawImage(SystemEmoji.Image(index),new RectangleF(12,top+4,44,44));
        }
        public static GraphicsPath Rounded(RectangleF rect,float radius)
        {
            var path=new GraphicsPath();float d=radius*2;
            path.AddArc(rect.Left,rect.Top,d,d,180,90);path.AddArc(rect.Right-d,rect.Top,d,d,270,90);
            path.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);path.AddArc(rect.Left,rect.Bottom-d,d,d,90,90);path.CloseFigure();return path;
        }
    }
}
