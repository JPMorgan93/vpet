using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Vpet
{
    [DataContract]
    public sealed class SpriteFrame
    {
        [DataMember] public int X,Y,OffsetX,OffsetY;
        public SpriteFrame Copy(){return new SpriteFrame{X=X,Y=Y,OffsetX=OffsetX,OffsetY=OffsetY};}
    }
    [DataContract]
    public sealed class SpriteManifest
    {
        [DataMember] public int Version=1;
        [DataMember] public string Kind;
        [DataMember] public int Width=32,Height=36;
        [DataMember] public bool Diagonals=true;
        [DataMember] public SpriteFrame[][] Frames;
        [DataMember] public int[] Counts;
    }
    public sealed class SpriteProject : IDisposable
    {
        public static readonly string[] Cycles={"Idle Up","Idle Down","Idle Left/Right","Idle Diag Up","Idle Diag Down","Walk Up","Walk Down","Walk Left/Right","Walk Diag Up","Walk Diag Down"};
        public Bitmap Source {get;private set;}
        public SpriteManifest Data {get;private set;}
        public SpriteProject(Bitmap source)
        {
            Source=source;Data=new SpriteManifest{Kind="project",Frames=new SpriteFrame[10][]};
            for(int i=0;i<10;i++)Data.Frames[i]=new SpriteFrame[5];
        }
        public bool Enabled(int row){return Data.Diagonals||row%5<3;}
        public int[] Slots(int row){return Enumerable.Range(0,5).Where(i=>Data.Frames[row][i]!=null).ToArray();}
        public Rectangle Selection(SpriteFrame f){return new Rectangle(f.X,f.Y,Data.Width,Data.Height);}
        public static Rectangle VisibleBounds(Bitmap image)
        {
            var bits=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            int left=image.Width,top=image.Height,right=-1,bottom=-1;
            try
            {
                byte[] row=new byte[image.Width*4];
                for(int y=0;y<image.Height;y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),row,0,row.Length);
                    for(int x=0;x<image.Width;x++)if(row[x*4+3]>0){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                }
            }
            finally{image.UnlockBits(bits);}
            return right<left?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
        public static bool HasTransparency(Bitmap image)
        {
            var bits=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
                byte[] row=new byte[image.Width*4];
                for(int y=0;y<image.Height;y++)
                {System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),row,0,row.Length);for(int x=0;x<image.Width;x++)if(row[x*4+3]<255)return true;}
                return false;
            }
            finally{image.UnlockBits(bits);}
        }
        public static SpriteProject FromPng(string path)
        {
            var image=SpritePackage.ReadPng(File.ReadAllBytes(path),4096,4096);
            if(!HasTransparency(image)){image.Dispose();throw new InvalidDataException("The sprite sheet needs a transparent background.");}
            return new SpriteProject(image);
        }
        public string FrameProblem(SpriteFrame frame,bool offsets)
        {
            if(frame==null)return "not set";
            if(!new Rectangle(0,0,Source.Width,Source.Height).Contains(Selection(frame)))return "selection is outside the sheet";
            using(var crop=Source.Clone(Selection(frame),PixelFormat.Format32bppArgb))
            {
                var ink=VisibleBounds(crop);if(ink.IsEmpty)return "frame is transparent";
                ink.Offset(frame.OffsetX,frame.OffsetY);
                if(offsets&&!new Rectangle(0,0,Data.Width,Data.Height).Contains(ink))return "alignment clips the artwork";
            }
            return null;
        }
        public List<string> Problems(bool offsets)
        {
            var problems=new List<string>();
            for(int row=0;row<10;row++)if(Enabled(row))
            {
                var slots=Slots(row);if(slots.Length==0)problems.Add(Cycles[row]+": missing frames");
                foreach(int slot in slots){string problem=FrameProblem(Data.Frames[row][slot],offsets);if(problem!=null)problems.Add(Cycles[row]+", frame "+(slot+1)+": "+problem);}
            }
            return problems;
        }
        public Bitmap RenderFrame(int row,int slot)
        {
            var frame=Data.Frames[row][slot];string problem=FrameProblem(frame,false);
            if(problem!=null)throw new InvalidDataException(problem);
            var result=new Bitmap(Data.Width,Data.Height,PixelFormat.Format32bppArgb);
            using(var crop=Source.Clone(Selection(frame),PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(result))
                g.DrawImageUnscaled(crop,frame.OffsetX,frame.OffsetY);
            return result;
        }
        public void MagicTweak(int row)
        {
            foreach(int slot in Slots(row))
            {
                var frame=Data.Frames[row][slot];string problem=FrameProblem(frame,false);if(problem!=null)throw new InvalidDataException(problem);
                using(var crop=Source.Clone(Selection(frame),PixelFormat.Format32bppArgb))
                {
                    var bounds=VisibleBounds(crop);
                    frame.OffsetX=(Data.Width-bounds.Width)/2-bounds.Left;
                    frame.OffsetY=Data.Height-bounds.Bottom;
                }
            }
        }
        public SpriteSet Build()
        {
            var problems=Problems(true);if(problems.Count>0)throw new InvalidDataException(string.Join("\n",problems));
            var atlas=new Bitmap(Data.Width*5,Data.Height*10,PixelFormat.Format32bppArgb);var counts=new int[10];
            try
            {
                using(var g=Graphics.FromImage(atlas))for(int row=0;row<10;row++)if(Enabled(row))
                {
                    int column=0;foreach(int slot in Slots(row))using(var frame=RenderFrame(row,slot))g.DrawImageUnscaled(frame,column++*Data.Width,row*Data.Height);
                    counts[row]=column;
                }
                return new SpriteSet(atlas,Data.Diagonals,counts);
            }
            catch{atlas.Dispose();throw;}
        }
        public void Save(string path){SpritePackage.Write(path,Data,Source);}
        public static SpriteProject Load(string path)
        {
            SpriteManifest manifest;Bitmap image=SpritePackage.Read(path,"project",out manifest);
            var project=new SpriteProject(image);project.Data=manifest;return project;
        }
        public void Dispose(){Source.Dispose();}
    }
    internal static class SpritePackage
    {
        internal const int Limit=64*1024*1024;
        static byte[] ReadEntry(ZipArchiveEntry entry,int limit)
        {
            if(entry.Length>limit)throw new InvalidDataException("Sprite package is too large.");
            using(var input=entry.Open())using(var output=new MemoryStream())
            {
                byte[] buffer=new byte[8192];int count;
                while((count=input.Read(buffer,0,buffer.Length))>0){if(output.Length+count>limit)throw new InvalidDataException("Sprite package is too large.");output.Write(buffer,0,count);}
                return output.ToArray();
            }
        }
        internal static Bitmap ReadPng(byte[] data,int maxWidth,int maxHeight)
        {
            if(data.Length<24||data.Length>Limit)throw new InvalidDataException("PNG exceeds the 64 MiB limit or is incomplete.");
            byte[] sig={137,80,78,71,13,10,26,10};for(int i=0;i<8;i++)if(data[i]!=sig[i])throw new InvalidDataException("Choose a valid PNG image.");
            uint w=((uint)data[16]<<24)|((uint)data[17]<<16)|((uint)data[18]<<8)|data[19],h=((uint)data[20]<<24)|((uint)data[21]<<16)|((uint)data[22]<<8)|data[23];
            if(w<1||h<1||w>maxWidth||h>maxHeight||w*h*4L>Limit)throw new InvalidDataException("Image exceeds the "+maxWidth+" × "+maxHeight+" pixel limit.");
            try
            {
                using(var stream=new MemoryStream(data))using(var image=Image.FromStream(stream,true,true))
                {
                    if(image.Width!=w||image.Height!=h)throw new InvalidDataException("Invalid PNG dimensions.");
                    var bitmap=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
                    using(var g=Graphics.FromImage(bitmap)){g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.DrawImageUnscaled(image,0,0);}return bitmap;
                }
            }
            catch(ArgumentException ex){throw new InvalidDataException("PNG could not be decoded.",ex);}
        }
        internal static void Validate(SpriteManifest data,string kind)
        {
            if(data==null||data.Version!=1||data.Kind!=kind)throw new InvalidDataException("Unsupported sprite file format or version.");
            if(data.Width<1||data.Width>100||data.Height<1||data.Height>150)throw new InvalidDataException("Frame size must be 1–100 pixels wide and 1–150 pixels tall.");
            if(kind=="project")
            {
                if(data.Frames==null||data.Frames.Length!=10)throw new InvalidDataException("Invalid animation list.");
                foreach(var row in data.Frames)
                {
                    if(row==null||row.Length!=5)throw new InvalidDataException("Invalid frame slots.");
                    foreach(var frame in row)if(frame!=null&&(frame.X<0||frame.Y<0||frame.X>4096||frame.Y>4096||Math.Abs((long)frame.OffsetX)>4096||Math.Abs((long)frame.OffsetY)>4096))throw new InvalidDataException("Invalid frame coordinates.");
                }
            }
            else
            {
                if(data.Counts==null||data.Counts.Length!=10)throw new InvalidDataException("Invalid animation counts.");
                for(int row=0;row<10;row++)if(data.Counts[row]<(data.Diagonals||row%5<3?1:0)||data.Counts[row]>5||(!data.Diagonals&&row%5>=3&&data.Counts[row]!=0))throw new InvalidDataException("Every enabled animation needs 1–5 frames.");
            }
        }
        internal static Bitmap Read(string path,string kind,out SpriteManifest data)
        {
            if(new FileInfo(path).Length>Limit)throw new InvalidDataException("Sprite package exceeds 64 MiB.");
            using(var file=File.OpenRead(path))using(var zip=new ZipArchive(file,ZipArchiveMode.Read))
            {
                string imageName=kind=="project"?"source.png":"atlas.png";
                if(zip.Entries.Count!=2||zip.Entries.Count(e=>e.FullName=="manifest.json")!=1||zip.Entries.Count(e=>e.FullName==imageName)!=1)throw new InvalidDataException("Sprite package must contain one manifest and one PNG.");
                if(zip.Entries.Sum(e=>e.Length)>Limit)throw new InvalidDataException("Sprite package exceeds 64 MiB.");
                try
                {
                    using(var stream=new MemoryStream(ReadEntry(zip.GetEntry("manifest.json"),65536)))data=(SpriteManifest)new DataContractJsonSerializer(typeof(SpriteManifest)).ReadObject(stream);
                }
                catch(SerializationException ex){throw new InvalidDataException("Invalid sprite manifest.",ex);}
                Validate(data,kind);
                var image=ReadPng(ReadEntry(zip.GetEntry(imageName),Limit),kind=="project"?4096:500,kind=="project"?4096:1500);
                if(!SpriteProject.HasTransparency(image)||(kind=="sprite"&&(image.Width!=data.Width*5||image.Height!=data.Height*10)))
                {image.Dispose();throw new InvalidDataException("Sprite image dimensions/transparency do not match the manifest.");}
                if(kind=="sprite")for(int row=0;row<10;row++)for(int col=0;col<data.Counts[row];col++)
                    using(var frame=image.Clone(new Rectangle(col*data.Width,row*data.Height,data.Width,data.Height),PixelFormat.Format32bppArgb))
                        if(SpriteProject.VisibleBounds(frame).IsEmpty){image.Dispose();throw new InvalidDataException("An exported animation frame is empty.");}
                return image;
            }
        }
        internal static void Write(string path,SpriteManifest data,Bitmap image)
        {
            Validate(data,data.Kind);string pending=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var file=File.Create(pending))using(var zip=new ZipArchive(file,ZipArchiveMode.Create))
                {
                    using(var stream=zip.CreateEntry("manifest.json").Open())new DataContractJsonSerializer(typeof(SpriteManifest)).WriteObject(stream,data);
                    using(var stream=zip.CreateEntry(data.Kind=="project"?"source.png":"atlas.png").Open())image.Save(stream,ImageFormat.Png);
                }
                if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);
            }
            finally{if(File.Exists(pending))File.Delete(pending);}
        }
    }
}
