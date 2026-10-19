using System;
using System.Drawing;
using System.IO;

namespace Vpet
{
    internal static partial class Tests
    {
        static void MatchingItemResolution()
        {
            var names=new[]{"Chest - Closed.png","Chest - Open.png","Rubber Ball.png","Triangle.png","Gold Coin.png","Card.png","D20 Dice.png","Joystick.png"};
            foreach(float scale in new[]{.5f,1f,1.25f,1.5f,2f,3f})
            {
                var images=new[]{ToyArtwork.Chest(scale),ToyArtwork.Chest(scale,true),ToyArtwork.Ball(scale),ToyArtwork.Triangle(scale),GameArtwork.Coin(scale,0),GameArtwork.Card(scale,-1,false,0),GameArtwork.Die(scale,0,20,true),JoystickArtwork.Draw(scale)};
                try
                {
                    int expected=(int)Math.Ceiling(64*scale);var petSize=new DisplayArea("screen",new Rectangle(0,0,4000,3000),scale).PetSize(new Size(32,36));
                    for(int i=0;i<images.Length;i++)
                    {
                        Check(images[i].Size==new Size(expected,expected)&&images[i].Width==petSize.Width,"Item canvas matches default pet resolution: "+names[i]+" scale "+scale);
                        var source=ItemArtwork.Source(names[i]);Check(source.Size==new Size(32,32),"Every item has a 32x32 logical sprite, including the high-resolution joystick");
                        if(scale==1||scale==2)
                        {
                            int magnification=(int)(scale*2);bool matches=true;
                            for(int y=0;y<32;y++)for(int x=0;x<32;x++)if(source.GetPixel(x,y).A==255)
                                for(int dy=0;dy<magnification;dy++)for(int dx=0;dx<magnification;dx++)
                                    matches&=images[i].GetPixel(x*magnification+dx,y*magnification+dy).ToArgb()==source.GetPixel(x,y).ToArgb();
                            Check(matches,"Source pixels expand by the same exact integer factor as default pet pixels: "+names[i]+" scale "+scale);
                        }
                    }
                    for(int remaining=0;remaining<=3;remaining++)using(var plate=PlateArtwork.Draw(remaining,scale))
                        Check(plate.Size==new Size(expected,expected),"All plate/pudding stages share the item grid at scale "+scale);
                    if(scale==1)
                    using(var sheet=new Bitmap(9*88,120))using(var graphics=Graphics.FromImage(sheet))
                    using(var sprites=SpriteSet.FromReference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Blue Dragon.png")))
                    using(var pet=Artwork.Scale(sprites.Frame(false,2,0),petSize))
                    {
                        graphics.Clear(Color.FromArgb(235,235,235));graphics.DrawImageUnscaled(pet,12,8);
                        for(int i=0;i<images.Length;i++)graphics.DrawImageUnscaled(images[i],(i+1)*88+12,16);
                        sheet.Save(Path.Combine(artifacts,"pet-and-matching-item-resolution.png"));
                    }
                }
                finally{foreach(var image in images)image.Dispose();}
            }
            var model=Pet(MovementMode.Static);var toys=Toys(model);toys.SpawnBall(0);toys.SpawnTriangle();toys.SpawnCoin();toys.SpawnCard();toys.SpawnDie();
            foreach(var point in new[]{new PointF(-100000,-100000),new PointF(100000,100000)})
            {
                toys.DragBall(point,0);toys.DragTriangle(point);toys.DragGame(PlayTarget.Coin,point);toys.DragGame(PlayTarget.Card,point);toys.DragDie(point,0);
                var centers=new[]{toys.Ball,toys.Triangle,toys.Coin,toys.Card,toys.Die};
                var images=new[]{ToyArtwork.Ball(1),ToyArtwork.Triangle(1),GameArtwork.Coin(1,0),GameArtwork.Card(1,-1,false,0),GameArtwork.Die(1,45,20,true)};
                try
                {
                    for(int i=0;i<images.Length;i++)
                    {var ink=SpriteProject.VisibleBounds(images[i]);var location=new PointF(centers[i].X-images[i].Width/2f,centers[i].Y-images[i].Height/2f);var bounds=new RectangleF(location.X+ink.X,location.Y+ink.Y,ink.Width,ink.Height);Check(toys.Zone.Contains(bounds),"Matching-scale toy artwork stays inside the fence at either extreme: "+i);}
                }
                finally{foreach(var image in images)image.Dispose();}
            }
        }
        static void MatchingItemWindows(PetWindow pet,LayeredWindow[] items)
        {
            RefreshItems(pet);var expected=ItemArtwork.CanvasSize(pet.Toys.Model.Scale);
            Check(expected.Width==pet.Model.Current.PetSize(pet.Sprites.Cell).Width,"Desktop item canvas width matches the default pet's rendered frame width");
            foreach(var item in items)
            {
                Native.RECT rectangle;Native.GetWindowRect(item.Handle,out rectangle);
                Check(item.Visible&&item.PresentedBounds.Size==expected,"Displayed item uses the shared canvas: "+item.Text);
                Check(rectangle.Right-rectangle.Left==expected.Width&&rectangle.Bottom-rectangle.Top==expected.Height,"Native item window matches rendered artwork size: "+item.Text);
            }
            Check(pet.Toys.Chest.PresentedBounds.Size==expected,"Chest uses the same native canvas as the toys");
        }
    }
}
