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
            foreach(float scale in new[]{1f,1.5f,2f})foreach(var cell in new[]{new Size(16,16),new Size(32,36),new Size(100,150)})
            {
                var pet=Pet(MovementMode.Static);pet.FrameSize=cell;
                pet.SetDisplays(new List<DisplayArea>{new DisplayArea("screen",new Rectangle(0,0,(int)(1200*scale),(int)(900*scale)),scale)});
                var toys=Toys(pet);toys.SpawnCard();toys.ResizeZone(toys.Zone,ZoneEdge.Left|ZoneEdge.Top,new PointF(-10000,-10000));
                toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(10000,10000));
                var bounds=toys.CardBounds;double now=0;
                foreach(var point in new[]{toys.Center,new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom),new PointF(bounds.Right,bounds.Bottom)})
                {
                    toys.DragGame(PlayTarget.Card,point);toys.PressCard(now);Until(toys,pet,ref now,()=>toys.WaitingForCardChoice);
                    var size=pet.Current.PetSize(cell);var body=new RectangleF(pet.Position.X-size.Width/2f,pet.Position.Y-size.Height,size.Width,size.Height);
                    var card=new RectangleF(toys.Card.X-23*scale,toys.Card.Y-32*scale,46*scale,64*scale);
                    Check(!body.IntersectsWith(card)&&ToyModel.ContainsInclusive(pet.Current.Allowed(cell),pet.Position),"Pet stands clear of the card and stays on screen at edges: "+scale+" / "+cell+" / "+point);
                }
            }
            var diePet=Pet(MovementMode.Static);
            diePet.SetDisplays(new List<DisplayArea>{new DisplayArea("large",new Rectangle(0,0,2200,1500),2),new DisplayArea("small",new Rectangle(2200,0,1000,760),1)});
            var dice=Toys(diePet);dice.MoveZone(new PointF(2700,400));dice.ResizeZone(dice.Zone,ZoneEdge.Left|ZoneEdge.Top,new PointF(10000,10000));dice.SpawnDie();
            Check(dice.Zone.Width>=192&&dice.Zone.Height>=192,"Fence minimum accommodates the enlarged launch-scale die on a smaller-scale monitor");
            dice.LaunchDie(new PointF(600,-550),0);bool contained=true;
            for(int i=0;i<500;i++)
            {
                dice.AdvanceDie(.01f);var fullDie=new RectangleF(dice.Die.X-dice.DieRadius,dice.Die.Y-dice.DieRadius,dice.DieRadius*2,dice.DieRadius*2);
                contained&=dice.Zone.Contains(fullDie);
            }
            Check(contained&&!dice.DieRolling,"The enlarged full die remains inside the minimum fence throughout ricochets");
            using(var sheet=new Bitmap(5*100,110))using(var g=Graphics.FromImage(sheet))
            {
                g.Clear(Color.White);
                for(int i=0;i<5;i++)using(var coin=GameArtwork.Coin(2,i/24f))g.DrawImageUnscaled(coin,i*100,8);
                sheet.Save(Path.Combine(artifacts,"coin-x-axis-flip.png"));
            }
        }
    }
}
