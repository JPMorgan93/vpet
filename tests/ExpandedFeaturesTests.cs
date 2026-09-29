using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ExpandedFeatures()
        {SoundVolumes();AnimationSpeeds();RectangleFences();NewToyGames();GameInterruptions();ToyPresentation();}
        static void SoundVolumes()
        {
            foreach(TriangleSound sound in Enum.GetValues(typeof(TriangleSound)))
            {
                var loud=ToyChime.CreateWave(sound);var half=ToyChime.CreateWave(sound,.5f);var muted=ToyChime.CreateWave(sound,0);bool scaled=true,silent=true;
                for(int i=44;i<loud.Length;i+=2){scaled&=Math.Abs(BitConverter.ToInt16(half,i)-BitConverter.ToInt16(loud,i)/2.0)<=1;silent&=BitConverter.ToInt16(muted,i)==0;}
                Check(scaled&&silent,"Instrument PCM obeys exact volume and mute: "+sound);
            }
            string path=Path.Combine(artifacts,"new-options.json");File.WriteAllText(path,"{\"Toys\":{}}");var settings=Preferences.Load(path);
            Check(settings.SyncPlayZone&&settings.Toys.Volume==1,"New and old settings default to synced fences and existing instrument volume");
            settings.Toys.Volume=.35f;settings.SyncPlayZone=false;settings.Save(path);var loaded=Preferences.Load(path);
            Check(loaded.Toys.Volume==.35f&&!loaded.SyncPlayZone,"Saved volume and advanced preference survive a restart");
            settings.Toys.Volume=float.NaN;settings.Validate();Check(settings.Toys.Volume==1,"Malformed volume safely restores the default");
            var random=new Random(6);foreach(Frequency frequency in new[]{Frequency.Often,Frequency.Sometimes,Frequency.Rarely})
            {double low=frequency==Frequency.Often?15:frequency==Frequency.Sometimes?30:90,high=frequency==Frequency.Often?30:frequency==Frequency.Sometimes?60:120;bool range=true;for(int i=0;i<1000;i++){double value=Reactions.Interval(frequency,random);range&=value>=low&&value<=high;}Check(range,"New random reaction timing: "+frequency);}
        }
        static void AnimationSpeeds()
        {
            using(var project=MakerFixture())
            {
                Check(Enumerable.Range(0,18).All(i=>project.Speed(i)==1),"All older animation types play at 1x");
                project.Data.Frames[0][1]=new SpriteFrame{X=24,Y=0};
                project.SetSpeed(0,.25f);project.SetSpeed(7,3);project.SetSpeed(11,1.75f);project.Data.EmoteAnimations=true;project.Data.Frames[11][0]=project.Data.Frames[0][0].Copy();project.Data.Frames[11][1]=project.Data.Frames[0][1].Copy();project.SetSize(11,20,24);
                string path=Path.Combine(artifacts,"speed-project.vpetproject");project.Save(path);
                using(var saved=SpriteProject.Load(path))
                {
                    Check(saved.Speed(0)==.25f&&saved.Speed(7)==3&&saved.Speed(11)==1.75f&&saved.Speed(2)==1,"Animation speed is independent and survives project save/load");
                    using(var sprite=saved.Build())
                    {
                        string output=Path.Combine(artifacts,"speed-sprite.vpetsprite");sprite.SavePackage(output);
                        using(var imported=SpriteSet.Import(output))
                        {
                            Check(imported.Speed(false,6)==.25f&&imported.Speed(true,0)==3&&imported.Speeds[11]==1.75f,"Exported animation speeds reach runtime sprites");
                            Check(imported.FrameAtPhase(false,6,4)==imported.Frame(false,6,1),"Quarter-speed idle advances one frame per four baseline frames");
                            Check(imported.FrameAtPhase(true,0,1)==imported.Frame(true,0,3),"Triple-speed side walk advances three frames per baseline frame");
                            Check(imported.EmoteAtPhase(1,2)==imported.EmoteFrame(1,3),"Optional reactions use their own saved animation speed");
                        }
                    }
                    saved.Data.EmoteAnimations=false;using(var sprite=saved.Build()){string output=Path.Combine(artifacts,"speed-movement-only.vpetsprite");sprite.SavePackage(output);using(var imported=SpriteSet.Import(output))Check(imported.Counts.Length==10&&imported.Speed(true,4)==3,"Movement-only speed packages retain the ten-row format");}
                    foreach(float invalid in new[]{0f,3.1f,float.NaN,float.PositiveInfinity}){saved.Data.CycleSpeeds[0]=invalid;Reject(delegate{saved.Save(path);},"Invalid animation speed is rejected without overwriting the project");}
                }
            }
        }
        static void RectangleFences()
        {
            var pet=Pet(MovementMode.Restricted);var area=pet.OwnRestrictedArea;
            Check(area.Width==500&&area.Height==500,"Legacy radius migrates to a rectangle with the same initial span");
            var allowed=pet.RestrictedAllowed;Check(!pet.SetDestination(new PointF(allowed.Left-1,allowed.Top),"primary"),"Restricted destinations keep the full sprite inside the fence");
            for(int i=0;i<1000;i++){pet.IdleUntil=0;pet.Tick(i*.1,.1f);if(pet.Destination.HasValue)Check(pet.InsideRestriction(pet.Destination.Value),"Rectangle wandering stays within its allowed anchors");}
            pet.MoveRestrictedArea(new PointF(780,500));pet.ResizeRestrictedArea(pet.OwnRestrictedArea,ZoneEdge.Left|ZoneEdge.Top,new PointF(1000,1000));
            Check(pet.Current.Work.Contains(Rectangle.Ceiling(pet.OwnRestrictedArea))&&pet.InsideRestriction(pet.Position),"Moving and shrinking a fence recovers the pet inside connected work space");
            var independent=pet.OwnRestrictedArea;var toys=Toys(pet);toys.MoveZone(new PointF(240,300));
            pet.Settings.SyncPlayZone=true;pet.CancelRoute();pet.EnsureInsideRestrictedArea();
            Check(pet.RestrictedArea==toys.Zone&&pet.OwnRestrictedArea==independent&&pet.InsideRestriction(pet.Position),"Sync uses the play zone without destroying the independent fence");
            pet.SetRestrictedAreaVisible(false);Check(!toys.Settings.DisplayZone&&!pet.RestrictedAreaVisible,"Restricted visibility changes the shared play-zone setting");
            toys.Settings.DisplayZone=true;Check(pet.RestrictedAreaVisible,"Play-zone visibility is reflected by restricted controls");
            toys.SetVisible(false,0);Check(pet.RestrictedArea==toys.Zone,"Closing chest keeps the shared restriction active");
            toys.MoveZone(new PointF(800,500));Check(pet.InsideRestriction(pet.Position)&&!pet.Destination.HasValue,"Moving a shared fence recovers the pet and cancels the old wandering route");
            pet.Settings.SyncPlayZone=false;pet.EnsureInsideRestrictedArea();Check(pet.RestrictedArea==independent&&pet.InsideRestriction(pet.Position),"Turning sync off restores the independent fence");
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("new",new Rectangle(-800,0,800,600),1)});toys.RecoverDisplays();
            Check(pet.Current.Work.Contains(Rectangle.Ceiling(pet.OwnRestrictedArea))&&pet.Current.Work.Contains(Rectangle.Ceiling(toys.Zone)),"Both fences recover after their display disconnects");
        }
        static void Until(ToyModel toys,PetModel pet,ref double now,Func<bool> done,double limit=45)
        {double end=now+limit;while(!done()&&now<end){now+=.01;ToyStep(toys,pet,now,.01f);}Check(done(),"Toy action reaches the expected state before timeout");}
        static void NewToyGames()
        {
            var random=new Random(44);bool comparisons=true,excluded=true;
            for(int called=0;called<52;called++)
            {
                for(int drawn=0;drawn<52;drawn++)foreach(bool high in new[]{false,true})
                {int a=called%13,b=drawn%13;comparisons&=ToyModel.CardOutcome(called,drawn,high)==(a==b?2:(high?b>a:b<a)?1:4);}
                var possible=new HashSet<int>();for(int draw=0;draw<1200;draw++){int value=ToyModel.DrawOtherCard(random,called);excluded&=value!=called&&value>=0&&value<52;possible.Add(value);}
                Check(possible.Count==51,"Every other exact card can be drawn against "+GameArtwork.CardText(called));
            }
            Check(comparisons&&excluded,"All high/low outcomes use rank, ties use Question, and the announced exact card is excluded");
            Check(Enumerable.Range(0,52).Select(GameArtwork.CardText).Distinct().Count()==52&&Reactions.Names.Length==8,"All 52 named cards remain separate from customizable reactions");
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                var pet=Pet(mode);pet.Settings.Speed=0;var toys=Toys(pet);toys.RemoveBall(0);toys.SpawnCoin();double now=0;
                toys.PressCoin(now);Check(pet.Playing&&pet.Destination.HasValue&&toys.Target==PlayTarget.Coin,"Coin interrupts normal movement in "+mode);
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Pausing);double start=now;
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Shaking);Near((float)(now-start),.25f,.011f,"Coin waits a quarter second");start=now;
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Flipping);Near((float)(now-start),.5f,.011f,"Coin shakes for half a second");
                Check(toys.Announcement==null,"Coin result is not announced while airborne");
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);Check(toys.Announcement.Kind==(toys.CoinHeads?SpecialEmoteKind.Heads:SpecialEmoteKind.Tails),"Landed coin and announcement agree");
                Until(toys,pet,ref now,()=>!pet.Playing);Check(toys.Announcement==null&&pet.Settings.Movement==mode,"Coin returns to saved movement after its result");
                toys.SpawnCard();int reaction=-1;toys.ReactionPlayed+=r=>reaction=r;toys.PressCard(now);
                Until(toys,pet,ref now,()=>toys.WaitingForCardChoice);int call=toys.CalledCard;
                for(int i=0;i<1000;i++){now+=.01;ToyStep(toys,pet,now,.01f);}
                Check(toys.WaitingForCardChoice&&toys.Announcement.Value==call&&!toys.CardRevealed,"Called card remains displayed until the player chooses");
                toys.ChooseCard(false);Check(toys.Announcement==null&&toys.DrawnCard!=call,"Flip clears the called-card bubble and excludes its exact card");
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);Check(toys.CardRevealed&&reaction==ToyModel.CardOutcome(call,toys.DrawnCard,false),"Low choice reveals a card and reports the right reaction");
                Until(toys,pet,ref now,()=>!pet.Playing);
                toys.PressCard(now,true);Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);
                Check(toys.ChoiceHigh.HasValue&&toys.CardRevealed&&reaction==ToyModel.CardOutcome(toys.CalledCard,toys.DrawnCard,toys.ChoiceHigh.Value),"Autonomous card play chooses High or Low and evaluates the result");
                Until(toys,pet,ref now,()=>!pet.Playing);
                toys.SpawnDie();var standing=pet.Position;toys.LaunchDiePull(new PointF(-100,75),now);Check(!pet.Destination.HasValue&&toys.Fetch==FetchPhase.Watching,"Die launch watches instead of creating a chase route");
                bool contained=true,still=true;float initialAngle=toys.DieAngle;
                while(toys.Fetch==FetchPhase.Watching){now+=.01;ToyStep(toys,pet,now,.01f);contained&=ToyModel.ContainsInclusive(toys.DieBounds,toys.Die);still&=pet.Position==standing;}
                Check(contained&&still&&toys.DieAngle!=initialAngle&&!toys.DieRolling,"Rolling D20 stays inside the fence while the pet stays put");
                Check(toys.DieValue>=1&&toys.DieValue<=20&&toys.Announcement.Kind==SpecialEmoteKind.Number&&toys.Announcement.Value==toys.DieValue,"Stopped die announces its final 1–20 value");
                Until(toys,pet,ref now,()=>!pet.Playing);
                toys.PressCard(now);Until(toys,pet,ref now,()=>toys.WaitingForCardChoice);toys.CancelFetchForPetDrag(now);Check(toys.Announcement==null&&!pet.Playing,"Picking up the pet cancels a waiting card game and clears its announcement");
                toys.PressCoin(now);toys.PressTriangle(now);Check(toys.Target==PlayTarget.Coin,"A missing triangle cannot interrupt a coin flip");
                toys.SetVisible(false,now);Check(!toys.HasCoin&&!toys.HasCard&&!toys.HasDie&&toys.Announcement==null&&!pet.Playing,"Closing chest removes every new toy and cancels interactions");
            }
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference");using(var art=new GameArtwork(root))
            {
                foreach(var kind in new[]{SpecialEmoteKind.Heads,SpecialEmoteKind.Tails,SpecialEmoteKind.Card,SpecialEmoteKind.Number})
                {var message=new ToyAnnouncement(kind,kind==SpecialEmoteKind.Card?25:20);using(var bubble=Artwork.Bubble(-1,art.Emote(message),2,false))bubble.Save(Path.Combine(artifacts,"game-emote-"+kind+".png"));}
                using(var coin=GameArtwork.Coin(2,0))coin.Save(Path.Combine(artifacts,"coin-toy.png"));using(var card=GameArtwork.Card(2,-1,false,0))card.Save(Path.Combine(artifacts,"card-toy.png"));using(var die=GameArtwork.Die(2,0,20,false))die.Save(Path.Combine(artifacts,"d20-toy.png"));
            }
        }
        static void GameInterruptions()
        {
            foreach(PlayTarget target in new[]{PlayTarget.Coin,PlayTarget.Card,PlayTarget.D20})
            {
                var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.RemoveBall(0);
                if(target==PlayTarget.Coin)toys.SpawnCoin();else if(target==PlayTarget.Card)toys.SpawnCard();else toys.SpawnDie();
                double now=0;ToyStep(toys,pet,now,.01f);now=toys.NextPlayAt+.01;ToyStep(toys,pet,now,.01f);
                Check(toys.Target==target&&pet.Playing,"Spontaneous play can select "+target);
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);Until(toys,pet,ref now,()=>!pet.Playing);
                Check(toys.Announcement==null&&toys.NextPlayAt>now,"Autonomous game finishes and schedules another visit: "+target);
            }
            var cardPet=Pet(MovementMode.Static);var cards=Toys(cardPet);cards.SpawnCard();double time=0;cards.PressCard(time);
            Until(cards,cardPet,ref time,()=>cards.WaitingForCardChoice);int called=cards.CalledCard;
            cards.MoveZone(new PointF(cards.Center.X+20,cards.Center.Y));Until(cards,cardPet,ref time,()=>cards.WaitingForCardChoice);
            Check(cards.CalledCard==called&&cards.Announcement.Value==called,"Moving a waiting game preserves the called card");
            cards.ChooseCard(true);cards.DragGame(PlayTarget.Card,new PointF(cards.Card.X-25,cards.Card.Y));
            Until(cards,cardPet,ref time,()=>cards.WaitingForCardChoice);
            Check(!cards.CardRevealed&&!cards.ChoiceHigh.HasValue&&cards.DrawnCard==-1,"Dragging a flipping card safely restores its unflipped choice");
            cards.ChooseCard(false);Until(cards,cardPet,ref time,()=>cards.CardRevealed);
            cards.DragGame(PlayTarget.Card,new PointF(cards.Card.X-25,cards.Card.Y));
            Check(cards.CalledCard==-1&&!cards.CardRevealed&&cards.Announcement==null,"Moving a revealed card starts a fresh round");
            Until(cards,cardPet,ref time,()=>cards.WaitingForCardChoice);cards.SpawnDie();cards.LaunchDiePull(new PointF(100,100),time);
            Check(cards.Announcement==null&&cards.Fetch==FetchPhase.Watching&&!cardPet.Destination.HasValue,"Launching a die interrupts a persistent called-card bubble without chasing");
            cardPet.SetDisplays(new List<DisplayArea>{new DisplayArea("replacement",new Rectangle(-800,0,800,600),1)});cards.RecoverDisplays();
            Until(cards,cardPet,ref time,()=>cards.Fetch==FetchPhase.Result);
            Check(ToyModel.ContainsInclusive(cards.DieBounds,cards.Die)&&cards.Announcement.Kind==SpecialEmoteKind.Number,"A disconnected rolling-die display recovers and announces a valid result");
            foreach(var velocity in new[]{new PointF(720,0),new PointF(-720,0),new PointF(0,720),new PointF(0,-720)})
            {
                var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(-2000,-2000));toys.SpawnDie();
                toys.LaunchDie(velocity,0);double now=0;bool reflected=false,inside=true;
                for(int i=0;i<400;i++){now+=.01;ToyStep(toys,pet,now,.01f);reflected|=velocity.X*toys.DieVelocity.X+velocity.Y*toys.DieVelocity.Y<0;inside&=ToyModel.ContainsInclusive(toys.DieBounds,toys.Die);}
                Check(reflected&&inside&&!toys.DieRolling,"D20 ricochets and stops inside each fence edge: "+velocity);
            }
        }
    }
}
