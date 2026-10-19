using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ReferenceItemArt()
        {
            var names=new[]{"Chest - Closed.png","Chest - Open.png","Rubber Ball.png","Triangle.png","Gold Coin.png","Card.png","D20 Dice.png"};
            foreach(float scale in new[]{1f,1.25f,2f})
            using(var preview=new Bitmap(560,140))using(var graphics=Graphics.FromImage(preview))
            {
                graphics.Clear(Color.FromArgb(235,235,235));
                var images=new[]{ToyArtwork.Chest(scale),ToyArtwork.Chest(scale,true),ToyArtwork.Ball(scale),ToyArtwork.Triangle(scale),GameArtwork.Coin(scale,0),GameArtwork.Card(scale,-1,false,0),GameArtwork.Die(scale,0,20,true)};
                try
                {
                    for(int i=0;i<images.Length;i++)
                    {
                        var source=ItemArtwork.Source(names[i]);var palette=new HashSet<int>();
                        for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if(source.GetPixel(x,y).A>0)palette.Add(source.GetPixel(x,y).ToArgb());
                        bool clean=true,visible=false;var image=images[i];
                        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
                        {var pixel=image.GetPixel(x,y);if(pixel.A>1)visible=true;if(pixel.A==255)clean&=palette.Contains(pixel.ToArgb());}
                        Check(visible&&clean&&image.GetPixel(0,0).A==0,"Reference pixels stay sharp with transparent corners: "+names[i]+" scale "+scale);
                        graphics.DrawImageUnscaled(image,i*80,0);
                    }
                    Check(SpriteProject.VisibleBounds(images[0]).Bottom==SpriteProject.VisibleBounds(images[1]).Bottom,"Opening the chest keeps its base at the same height");
                    using(var coin=GameArtwork.Coin(scale,1/12f))
                    {var face=SpriteProject.VisibleBounds(images[4]);var edge=SpriteProject.VisibleBounds(coin);Check(edge.Width==face.Width&&edge.Height<face.Height/3,"Reference coin still flips around its horizontal X axis");}
                    using(var edge=GameArtwork.Card(scale,-1,false,.5f))Check(SpriteProject.VisibleBounds(edge).Width<SpriteProject.VisibleBounds(images[5]).Width/4,"Reference card retains its edge-on flip");
                    preview.Save(Path.Combine(artifacts,"reference-items-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
                }
                finally{foreach(var image in images)image.Dispose();}
            }
            var stages=new HashSet<string>();int previous=int.MaxValue;
            for(int remaining=3;remaining>=0;remaining--)using(var image=PlateArtwork.Draw(remaining,1))
            {
                int abovePlate=0;for(int y=0;y<25;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>0)abovePlate++;
                Check(abovePlate<=previous,"Pudding gets shorter as each bite selects the next reference PNG");previous=abovePlate;
                using(var stream=new MemoryStream()){image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);stages.Add(Convert.ToBase64String(stream.ToArray()));}
            }
            Check(stages.Count==4,"Whole, partially eaten, final portion and empty plate are distinct");
        }
    }
}
