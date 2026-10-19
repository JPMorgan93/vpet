using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ReferenceItemArt()
        {
            MatchingItemResolution();
            var names=new[]{"Chest - Closed.png","Chest - Open.png","Rubber Ball.png","Triangle.png","Gold Coin.png","Card.png","D20 Dice.png"};
            foreach(float scale in new[]{1f,1.25f,2f})
            using(var preview=new Bitmap(980,150))using(var graphics=Graphics.FromImage(preview))
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
                        graphics.DrawImageUnscaled(image,i*140,0);
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
        static void ChestMenuArtwork(PetWindow pet)
        {
            var windows=pet.Toys;var bounds=windows.Chest.PresentedBounds;
            Check(!MakerField<bool>(windows,"shownChestOpen"),"Chest starts with the closed reference artwork");
            var point=Point.Round(windows.Model.Chest);
            ToyMouse(windows.Chest,0x204,point);ToyMouse(windows.Chest,0x205,point);Application.DoEvents();
            Check(windows.Menu.Visible&&MakerField<bool>(windows,"shownChestOpen"),"Right-clicking chest immediately displays the open reference artwork");
            for(int i=0;i<5;i++)windows.Update();
            Check(windows.Menu.Visible&&MakerField<bool>(windows,"shownChestOpen")&&bounds==windows.Chest.PresentedBounds,"Chest remains open without moving during menu updates");
            windows.Menu.Close(ToolStripDropDownCloseReason.Keyboard);Application.DoEvents();
            Check(!windows.Menu.Visible&&!MakerField<bool>(windows,"shownChestOpen"),"Keyboard dismissal immediately restores closed chest artwork");
            windows.Menu.Show(windows.Chest,new Point(10,10));Application.DoEvents();
            MakerField<MenuDismissal>(windows,"dismissal").MouseDownAt(new Point(windows.Menu.Right+50,windows.Menu.Bottom+50));Application.DoEvents();
            Check(!windows.Menu.Visible&&!MakerField<bool>(windows,"shownChestOpen"),"Clicking away closes the menu and chest lid");
            System.ComponentModel.CancelEventHandler cancel=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=true;};
            windows.Menu.Opening+=cancel;windows.Menu.Show(windows.Chest,new Point(10,10));Application.DoEvents();windows.Menu.Opening-=cancel;
            Check(!windows.Menu.Visible&&!MakerField<bool>(windows,"shownChestOpen"),"Canceled menu opening keeps the chest closed");
        }
    }
}
