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
        [DataMember(EmitDefaultValue=false)] public int[] CycleWidths,CycleHeights;
        [DataMember] public bool Diagonals=true;
        [DataMember] public SpriteFrame[][] Frames;
        [DataMember] public int[] Counts;
        [DataMember(EmitDefaultValue=false)] public bool FacesRight,EmoteAnimations;
        [DataMember(EmitDefaultValue=false)] public float[] CycleSpeeds;
    }
    public sealed class SpriteProject : IDisposable
    {
        public const int MovementCycles=10,TotalCycles=18;
        public static readonly string[] Cycles=new[]{"Idle Up","Idle Down","Idle Left/Right","Idle Diag Up","Idle Diag Down","Walk Up","Walk Down","Walk Left/Right","Walk Diag Up","Walk Diag Down"}.Concat(Reactions.Names).ToArray();
        public Bitmap Source {get;private set;}
        public SpriteManifest Data {get;private set;}
        public SpriteProject(Bitmap source)
        {
            Source=source;Data=new SpriteManifest{Kind="project",Frames=new SpriteFrame[TotalCycles][]};
            for(int i=0;i<TotalCycles;i++)Data.Frames[i]=new SpriteFrame[5];
        }
        public bool Enabled(int row){return row>=MovementCycles?Data.EmoteAnimations:Data.Diagonals||row%5<3;}
        public int[] Slots(int row){return Enumerable.Range(0,5).Where(i=>Data.Frames[row][i]!=null).ToArray();}
        public int Width(int row){return Data.CycleWidths==null?Data.Width:Data.CycleWidths[row];}
        public int Height(int row){return Data.CycleHeights==null?Data.Height:Data.CycleHeights[row];}
        public float Speed(int row){return Data.CycleSpeeds==null?1:Data.CycleSpeeds[row];}
        public void SetSpeed(int row,float speed)
        {
            if(float.IsNaN(speed)||float.IsInfinity(speed)||speed<.25f||speed>3)throw new ArgumentOutOfRangeException("speed");
            InitializeSizes();if(Data.CycleSpeeds==null)Data.CycleSpeeds=Enumerable.Repeat(1f,TotalCycles).ToArray();
            Data.CycleSpeeds[row]=speed;Data.Version=4;
        }
        void InitializeSizes()
        {
            if(Data.CycleWidths==null)Data.CycleWidths=Enumerable.Repeat(Data.Width,TotalCycles).ToArray();
            if(Data.CycleHeights==null)Data.CycleHeights=Enumerable.Repeat(Data.Height,TotalCycles).ToArray();
            Data.Version=Math.Max(3,Data.Version);
        }
        public void SetSize(int row,int width,int height)
        {InitializeSizes();Data.CycleWidths[row]=Math.Max(1,Math.Min(100,width));Data.CycleHeights[row]=Math.Max(1,Math.Min(150,height));}
        public Rectangle Selection(int row,SpriteFrame f){return new Rectangle(f.X,f.Y,Width(row),Height(row));}
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
        static Bitmap ReadSheet(string path)
        {
            if(!string.Equals(Path.GetExtension(path),".png",StringComparison.OrdinalIgnoreCase)||new FileInfo(path).Length>SpritePackage.Limit)throw new InvalidDataException("Choose a PNG up to 4096 × 4096 pixels and 64 MiB.");
            var image=SpritePackage.ReadPng(File.ReadAllBytes(path),4096,4096);
            try{if(!HasTransparency(image))throw new InvalidDataException("The sprite sheet needs a transparent background.");return image;}
            catch{image.Dispose();throw;}
        }
        public static SpriteProject FromPng(string path){return new SpriteProject(ReadSheet(path));}
        public void ReplaceSource(string path)
        {
            // Validate and decode before replacing anything. Coordinates remain in original source pixels.
            var replacement=ReadSheet(path);var previous=Source;Source=replacement;previous.Dispose();
        }
        public string FrameProblem(int row,SpriteFrame frame,bool offsets)
        {
            if(frame==null)return "not set";
            if(!new Rectangle(0,0,Source.Width,Source.Height).Contains(Selection(row,frame)))return "selection is outside the sheet";
            using(var crop=Source.Clone(Selection(row,frame),PixelFormat.Format32bppArgb))
            {
                var ink=VisibleBounds(crop);if(ink.IsEmpty)return "frame is transparent";
            }
            return null;
        }
        public List<string> Problems(bool offsets)
        {
            var problems=new List<string>();
            for(int row=0;row<TotalCycles;row++)if(Enabled(row))
            {
                var slots=Slots(row);if(slots.Length==0&&row<MovementCycles)problems.Add(Cycles[row]+": missing frames");
                foreach(int slot in slots){string problem=FrameProblem(row,Data.Frames[row][slot],offsets);if(problem!=null)problems.Add(Cycles[row]+", frame "+(slot+1)+": "+problem);}
            }
            return problems;
        }
        public Bitmap RenderFrame(int row,int slot)
        {
            var frame=Data.Frames[row][slot];string problem=FrameProblem(row,frame,false);
            if(problem!=null)throw new InvalidDataException(problem);
            var result=new Bitmap(Width(row),Height(row),PixelFormat.Format32bppArgb);
            using(var crop=Source.Clone(Selection(row,frame),PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(result))
                SpritePackage.CopyPixels(g,crop,frame.OffsetX,frame.OffsetY);
            return result;
        }
        public static Point GroundPoint(Bitmap image)
        {
            // Anchor the lowest occupied row, not the width of a swinging tail/arm.
            // Midpoints between pixels use the left pixel consistently.
            for(int y=image.Height-1;y>=0;y--)
            {
                int left=image.Width,right=-1;
                for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>0){left=Math.Min(left,x);right=x;}
                if(right>=left)return new Point((left+right)/2,y);
            }
            throw new InvalidDataException("Frame is transparent.");
        }
        public void MagicTweak(int row)
        {
            int[] slots=Slots(row);var anchors=new Point[slots.Length];
            for(int i=0;i<slots.Length;i++)
            {
                var frame=Data.Frames[row][slots[i]];string problem=FrameProblem(row,frame,false);if(problem!=null)throw new InvalidDataException(problem);
                using(var crop=Source.Clone(Selection(row,frame),PixelFormat.Format32bppArgb))
                {
                    anchors[i]=GroundPoint(crop);
                }
            }
            int groundX=(Width(row)-1)/2;
            // Validate the entire cycle before changing any offsets.
            for(int i=0;i<slots.Length;i++)
            {var frame=Data.Frames[row][slots[i]];frame.OffsetX=groundX-anchors[i].X;frame.OffsetY=Height(row)-1-anchors[i].Y;}
        }
        public SpriteSet Build()
        {
            var problems=Problems(true);if(problems.Count>0)throw new InvalidDataException(string.Join("\n",problems));
            var active=Enumerable.Range(0,TotalCycles).Where(row=>Enabled(row)&&Slots(row).Length>0).ToArray();
            int width=active.Max(row=>Width(row)),height=active.Max(row=>Height(row));
            int rows=active.Any(row=>row>=MovementCycles)?TotalCycles:MovementCycles;
            var atlas=new Bitmap(width*5,height*rows,PixelFormat.Format32bppArgb);var counts=new int[rows];
            try
            {
                using(var g=Graphics.FromImage(atlas))foreach(int row in active)
                {
                    // Crop first, then pad to a common runtime cell without scaling the artwork.
                    // Align the same bottom-center pixel for both odd and even cycle widths.
                    int column=0;foreach(int slot in Slots(row))using(var frame=RenderFrame(row,slot))
                    {
                        // Runtime directional rows use left-facing artwork. Normalize only those rows.
                        if(Data.FacesRight&&row<MovementCycles&&row%5>=2)frame.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        SpritePackage.CopyPixels(g,frame,column++*width+(width-1)/2-(frame.Width-1)/2,row*height+height-frame.Height);
                    }
                    counts[row]=column;
                }
                return new SpriteSet(atlas,Data.Diagonals,counts,Data.CycleSpeeds==null?null:Data.CycleSpeeds.Take(rows).ToArray());
            }
            catch{atlas.Dispose();throw;}
        }
        public void Save(string path){InitializeSizes();SpritePackage.Write(path,Data,Source);}
        public static SpriteProject Load(string path)
        {
            SpriteManifest manifest;Bitmap image=SpritePackage.Read(path,"project",out manifest);
            // Older projects keep every selection and offset; new optional rows start empty.
            if(manifest.Frames.Length<TotalCycles)
            {
                Array.Resize(ref manifest.Frames,TotalCycles);
                for(int row=MovementCycles;row<TotalCycles;row++)manifest.Frames[row]=new SpriteFrame[5];
                if(manifest.CycleWidths!=null){Array.Resize(ref manifest.CycleWidths,TotalCycles);for(int row=MovementCycles;row<TotalCycles;row++)manifest.CycleWidths[row]=manifest.Width;}
                if(manifest.CycleHeights!=null){Array.Resize(ref manifest.CycleHeights,TotalCycles);for(int row=MovementCycles;row<TotalCycles;row++)manifest.CycleHeights[row]=manifest.Height;}
            }
            var project=new SpriteProject(image);project.Data=manifest;return project;
        }
        public void Dispose(){Source.Dispose();}
    }
    internal static class SpritePackage
    {
        internal const int Limit=64*1024*1024;
        internal static void CopyPixels(Graphics graphics,Image image,int x,int y)
        {
            // DrawImageUnscaled still honors physical image resolution on GDI+.
            // Explicit source/destination pixel rectangles preserve PNG pixels.
            graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.DrawImage(image,new Rectangle(x,y,image.Width,image.Height),0,0,image.Width,image.Height,GraphicsUnit.Pixel);
        }
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
                    using(var g=Graphics.FromImage(bitmap))CopyPixels(g,image,0,0);return bitmap;
                }
            }
            catch(ArgumentException ex){throw new InvalidDataException("PNG could not be decoded.",ex);}
        }
        internal static void Validate(SpriteManifest data,string kind)
        {
            if(data==null||data.Version<1||data.Version>4||data.Kind!=kind)throw new InvalidDataException("Unsupported sprite file format or version.");
            int rows=data.Version==4&&kind=="sprite"?(data.Counts==null?0:data.Counts.Length):data.Version>=3?SpriteProject.TotalCycles:SpriteProject.MovementCycles;
            if(rows!=10&&rows!=18)throw new InvalidDataException("Invalid animation list.");
            if(data.CycleSpeeds!=null)
            {
                if(data.Version<4||data.CycleSpeeds.Length!=rows||data.CycleSpeeds.Any(s=>float.IsNaN(s)||float.IsInfinity(s)||s<.25f||s>3))throw new InvalidDataException("Animation speeds must be between 0.25x and 3x.");
            }
            if(data.Width<1||data.Width>100||data.Height<1||data.Height>150)throw new InvalidDataException("Frame size must be 1–100 pixels wide and 1–150 pixels tall.");
            if(kind=="project")
            {
                if(data.Version>=2||data.CycleWidths!=null||data.CycleHeights!=null)
                {
                    if(data.CycleWidths==null||data.CycleHeights==null||data.CycleWidths.Length!=rows||data.CycleHeights.Length!=rows)throw new InvalidDataException("Invalid animation frame sizes.");
                    for(int row=0;row<rows;row++)if(data.CycleWidths[row]<1||data.CycleWidths[row]>100||data.CycleHeights[row]<1||data.CycleHeights[row]>150)throw new InvalidDataException("Each animation needs a frame size from 1–100 by 1–150 pixels.");
                }
                if(data.Frames==null||data.Frames.Length!=rows)throw new InvalidDataException("Invalid animation list.");
                foreach(var row in data.Frames)
                {
                    if(row==null||row.Length!=5)throw new InvalidDataException("Invalid frame slots.");
                    foreach(var frame in row)if(frame!=null&&(frame.X<0||frame.Y<0||frame.X>4096||frame.Y>4096||Math.Abs((long)frame.OffsetX)>4096||Math.Abs((long)frame.OffsetY)>4096))throw new InvalidDataException("Invalid frame coordinates.");
                }
            }
            else
            {
                if(data.Counts==null||data.Counts.Length!=rows)throw new InvalidDataException("Invalid animation counts.");
                for(int row=0;row<10;row++)if(data.Counts[row]<(data.Diagonals||row%5<3?1:0)||data.Counts[row]>5||(!data.Diagonals&&row%5>=3&&data.Counts[row]!=0))throw new InvalidDataException("Every enabled animation needs 1–5 frames.");
                for(int row=10;row<rows;row++)if(data.Counts[row]<0||data.Counts[row]>5)throw new InvalidDataException("Optional emote animations may have 0–5 frames.");
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
                var image=ReadPng(ReadEntry(zip.GetEntry(imageName),Limit),kind=="project"?4096:500,kind=="project"?4096:2700);
                if((kind=="project"&&!SpriteProject.HasTransparency(image))||(kind=="sprite"&&(image.Width!=data.Width*5||image.Height!=data.Height*data.Counts.Length)))
                {image.Dispose();throw new InvalidDataException("Sprite image dimensions/transparency do not match the manifest.");}
                if(kind=="sprite"&&data.Version==1)for(int row=0;row<10;row++)for(int col=0;col<data.Counts[row];col++)
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
