using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Vpet
{
    internal static partial class Tests
    {
        static void StartDieVisit(ToyModel toys,PetModel pet,ref double now)
        {
            toys.RemoveBall(now);toys.SpawnDie();ToyStep(toys,pet,now,.01f);now=toys.NextPlayAt+.01;ToyStep(toys,pet,now,.01f);
            Check(toys.Target==PlayTarget.D20&&toys.Fetch==FetchPhase.Approaching&&pet.Destination.HasValue&&!toys.DieRolling,"Spontaneous D20 play approaches the stationary die before launching");
        }
        static void AutonomousDiceAndAces()
        {
            for(int suit=0;suit<4;suit++)
            {
                Check(GameArtwork.CardText(suit*13)=="A"+new[]{"♠","♥","♣","♦"}[suit],"Every suit displays Ace instead of 1");
                Check(ToyModel.CardOutcome(suit*13,suit*13+1,true)==Reactions.Love&&ToyModel.CardOutcome(suit*13+12,suit*13,false)==Reactions.Love,"Ace retains its original low rank in High/Low");
            }
            using(var artwork=new GameArtwork(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference")))
            using(var sheet=new Bitmap(4*150,230))using(var graphics=Graphics.FromImage(sheet))
            {
                graphics.Clear(Color.White);
                for(int suit=0;suit<4;suit++)using(var face=GameArtwork.Card(2,suit*13,true,0))
                using(var bubble=Artwork.Bubble(-1,artwork.Emote(new ToyAnnouncement(SpecialEmoteKind.Card,suit*13)),2,false))
                {graphics.DrawImageUnscaled(face,suit*150+25,0);graphics.DrawImageUnscaled(bubble,suit*150,110);}
                sheet.Save(Path.Combine(artifacts,"ace-cards-and-emotes.png"));
            }
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                var pet=Pet(mode);pet.Settings.Speed=0;var toys=Toys(pet);toys.MoveZone(new PointF(780,550));double now=0;
                var start=pet.Position;StartDieVisit(toys,pet,ref now);var die=toys.Die;
                pet.Paused=true;for(int i=0;i<20;i++){now+=.01;ToyStep(toys,pet,now,.01f);}Check(!toys.DieRolling&&toys.Fetch==FetchPhase.Approaching,"Settings pause cannot remotely launch an autonomous die");pet.Paused=false;
                bool still=true;double deadline=now+45;
                while(toys.Fetch==FetchPhase.Approaching&&now<deadline){still&=toys.Die==die&&!toys.DieRolling;now+=.01;ToyStep(toys,pet,now,.01f);}
                var size=pet.Current.PetSize(pet.FrameSize);var body=new RectangleF(pet.Position.X-size.Width/2f,pet.Position.Y-size.Height,size.Width,size.Height);
                var footprint=new RectangleF(die.X-toys.DieRadius,die.Y-toys.DieRadius,toys.DieRadius*2,toys.DieRadius*2);
                Check(still&&pet.Position!=start&&toys.Fetch==FetchPhase.Watching&&toys.DieRolling,"Pet walks to the stationary die and launches only on arrival in "+mode);
                Check(!body.IntersectsWith(footprint)&&Geometry.Distance(pet.Position,die)<size.Width/2+toys.DieRadius+size.Height&&!pet.Destination.HasValue,"Autonomous launch happens beside the die and cancels its walking route");
                var standing=pet.Position;Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);
                Check(pet.Position==standing&&toys.Announcement.Kind==SpecialEmoteKind.Number,"Pet watches its roll in place and announces the result");
                Until(toys,pet,ref now,()=>!pet.Playing);
                Check(pet.Settings.Movement==mode&&pet.Settings.Speed==0&&(mode!=MovementMode.Restricted||pet.InsideRestriction(pet.Position)),"Autonomous die play restores the saved movement mode and restriction");
            }
            var visitor=Pet(MovementMode.Static);var games=Toys(visitor);double time=0;StartDieVisit(games,visitor,ref time);
            var first=visitor.Destination;games.MoveZone(new PointF(850,600));ToyStep(games,visitor,time+=.01,.01f);
            Check(visitor.Destination!=first&&!games.DieRolling&&games.Fetch==FetchPhase.Approaching,"Moving the play zone reroutes an autonomous die visit before launch");
            visitor.SetDisplays(new List<DisplayArea>{new DisplayArea("replacement",new Rectangle(-1000,0,1000,760),1)});games.RecoverDisplays();
            Until(games,visitor,ref time,()=>games.Fetch==FetchPhase.Watching);
            Check(visitor.CurrentDisplay==games.DisplayId&&games.DieRolling,"Autonomous die visit reaches the recovered toy after display disconnection");
            Until(games,visitor,ref time,()=>!visitor.Playing);
            StartDieVisit(games,visitor,ref time);var before=visitor.Position;games.LaunchDiePull(new PointF(50,30),time);
            Check(games.Fetch==FetchPhase.Watching&&games.DieRolling&&visitor.Position==before&&!visitor.Destination.HasValue,"A user launch interrupts an autonomous visit immediately without walking");
            Until(games,visitor,ref time,()=>!visitor.Playing);
            visitor.Place(new PointF(-950,100));StartDieVisit(games,visitor,ref time);games.CancelFetchForPetDrag(time);
            Check(!games.DieRolling&&!visitor.Playing&&!visitor.Destination.HasValue,"Picking up the pet cancels the pending autonomous launch");
            StartDieVisit(games,visitor,ref time);games.RemoveDie(time);
            Check(!games.DieRolling&&!visitor.Playing&&!visitor.Destination.HasValue,"Removing the die cancels its visit before launch");
            games.SpawnDie();before=visitor.Position;games.RollDie(time);
            Check(games.Fetch==FetchPhase.Watching&&visitor.Position==before&&!visitor.Destination.HasValue,"User click-to-roll keeps its immediate watching behavior");
        }
    }
}
