using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ToyPresentation()
        {
            foreach(PlayTarget target in new[]{PlayTarget.Card,PlayTarget.Coin})foreach(float scale in new[]{1f,1.5f,2f})foreach(var cell in new[]{new Size(16,16),new Size(32,36),new Size(100,150)})
            {
                var pet=Pet(MovementMode.Static);pet.FrameSize=cell;
                pet.SetDisplays(new List<DisplayArea>{new DisplayArea("screen",new Rectangle(0,0,(int)(1200*scale),(int)(900*scale)),scale)});
                var toys=Toys(pet);toys.SpawnCard();toys.SpawnCoin();toys.ResizeZone(toys.Zone,ZoneEdge.Left|ZoneEdge.Top,new PointF(-10000,-10000));
                toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(10000,10000));
                var bounds=target==PlayTarget.Card?toys.CardBounds:toys.CoinBounds;double now=0;
                foreach(var point in new[]{toys.Center,new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom),new PointF(bounds.Right,bounds.Bottom)})
                {
                    toys.DragGame(target,point);if(target==PlayTarget.Card)toys.PressCard(now);else toys.PressCoin(now);
                    Until(toys,pet,ref now,()=>target==PlayTarget.Card?toys.WaitingForCardChoice:toys.Fetch==FetchPhase.Pausing);
                    var size=pet.Current.PetSize(cell);var body=new RectangleF(pet.Position.X-size.Width/2f,pet.Position.Y-size.Height,size.Width,size.Height);
                    var center=target==PlayTarget.Card?toys.Card:toys.Coin;float height=(target==PlayTarget.Card?64:46)*scale;
                    var toy=new RectangleF(center.X-23*scale,center.Y-height/2,46*scale,height);
                    Check(!body.IntersectsWith(toy)&&ToyModel.ContainsInclusive(pet.Current.Allowed(cell),pet.Position),"Pet stands clear of "+target+" and stays on screen at edges: "+scale+" / "+cell+" / "+point);
                }
            }
            var diePet=Pet(MovementMode.Static);
            diePet.SetDisplays(new List<DisplayArea>{new DisplayArea("large",new Rectangle(0,0,2200,1500),2),new DisplayArea("small",new Rectangle(2200,0,1000,760),1)});
            var dice=Toys(diePet);dice.MoveZone(new PointF(2700,400));dice.ResizeZone(dice.Zone,ZoneEdge.Left|ZoneEdge.Top,new PointF(10000,10000));dice.SpawnDie();
            Check(dice.Zone.Width>=96&&dice.Zone.Height>=96,"Fence minimum accommodates the original-size launch-scale die on a smaller-scale monitor");
            dice.LaunchDie(new PointF(600,-550),0);bool contained=true;
            for(int i=0;i<500;i++)
            {
                dice.AdvanceDie(.01f);var fullDie=new RectangleF(dice.Die.X-dice.DieRadius,dice.Die.Y-dice.DieRadius,dice.DieRadius*2,dice.DieRadius*2);
                contained&=dice.Zone.Contains(fullDie);
            }
            Check(contained&&!dice.DieRolling,"The original-size full die remains inside the minimum fence throughout ricochets");
            foreach(float scale in new[]{1f,2f})foreach(float angle in new[]{0f,45f,135f})
            {
                bool clear=true,visible=true;
                using(var blank=GameArtwork.Die(scale,angle,20,true))for(int number=1;number<=20;number++)using(var die=GameArtwork.Die(scale,angle,number,false))
                {
                    bool found=false;
                    for(int y=0;y<die.Height;y++)for(int x=0;x<die.Width;x++)
                    {
                        if(die.GetPixel(x,y).ToArgb()==blank.GetPixel(x,y).ToArgb())continue;
                        found=true;clear&=blank.GetPixel(x,y).ToArgb()==Color.FromArgb(196,175,238).ToArgb();
                    }
                    visible&=found;
                }
                Check(clear&&visible,"All D20 values stay inside their face without painting over edge lines at scale/angle "+scale+" / "+angle);
            }
            foreach(int scale in new[]{1,3})using(var sheet=new Bitmap(5*50*scale,4*50*scale))using(var g=Graphics.FromImage(sheet))
            {
                g.Clear(Color.White);for(int n=1;n<=20;n++)using(var die=GameArtwork.Die(scale,0,n,false))g.DrawImageUnscaled(die,((n-1)%5)*50*scale,((n-1)/5)*50*scale);
                sheet.Save(Path.Combine(artifacts,"d20-values-"+scale+"x.png"));
            }
            using(var sheet=new Bitmap(5*100,110))using(var g=Graphics.FromImage(sheet))
            {
                g.Clear(Color.White);
                for(int i=0;i<5;i++)using(var coin=GameArtwork.Coin(2,i/24f))g.DrawImageUnscaled(coin,i*100,8);
                sheet.Save(Path.Combine(artifacts,"coin-x-axis-flip.png"));
            }
        }
    }
}
