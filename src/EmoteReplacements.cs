using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Vpet
{
    public sealed class EmoteReplacements : IDisposable
    {
        readonly string directory;
        readonly Bitmap[] images=new Bitmap[8];
        public EmoteReplacements(string directory)
        {
            this.directory=directory;Directory.CreateDirectory(directory);
            for(int i=0;i<8;i++)if(File.Exists(PathFor(i)))
                try{images[i]=SpriteSet.ReadPng(PathFor(i),50,50);}catch(Exception){/* Corrupt replacements fall back to the built-in symbol. */}
        }
        string PathFor(int index){if(index<0||index>=8)throw new ArgumentOutOfRangeException("index");return Path.Combine(directory,Reactions.Names[index]+".png");}
        public Bitmap Get(int index){return images[index];}
        public void Replace(int index,string source)
        {
            var candidate=SpriteSet.ReadPng(source,50,50);
            try
            {
                string path=PathFor(index),temp=path+".pending";candidate.Save(temp,ImageFormat.Png);
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                var old=images[index];images[index]=candidate;candidate=null;if(old!=null)old.Dispose();
            }
            finally{if(candidate!=null)candidate.Dispose();}
        }
        public void Restore(int index)
        {
            string path=PathFor(index);if(File.Exists(path))File.Delete(path);
            if(images[index]!=null){images[index].Dispose();images[index]=null;}
        }
        public void Dispose(){foreach(var image in images)if(image!=null)image.Dispose();}
    }
}
