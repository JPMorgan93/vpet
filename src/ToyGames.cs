using System;
using System.Collections.Generic;
using System.Drawing;

namespace Vpet
{
    internal enum SpecialEmoteKind { Card, Heads, Tails, Number }
    internal sealed class ToyAnnouncement
    {
        public readonly SpecialEmoteKind Kind;public readonly int Value;
        public ToyAnnouncement(SpecialEmoteKind kind,int value=0){Kind=kind;Value=value;}
        public string Key {get{return Kind+":"+Value;}}
    }
    internal sealed partial class ToyModel
    {
        internal const float DieSizeMultiplier=2;
        public PointF Coin {get;private set;}
        public PointF Card {get;private set;}
        public PointF Die {get;private set;}
        public bool HasCoin {get;private set;}
        public bool HasCard {get;private set;}
        public bool HasDie {get;private set;}
        public bool CoinHeads {get;private set;}
        public int CalledCard {get;private set;}
        public int DrawnCard {get;private set;}
        public bool CardRevealed {get;private set;}
        public bool? ChoiceHigh {get;private set;}
        bool automaticCard;
        public PointF DieVelocity {get;private set;}
        public float DieAngle {get;private set;}
        public int DieValue {get;private set;}
        public bool DieAiming {get;private set;}
        public ToyAnnouncement Announcement {get;private set;}
        public event Action<int> ReactionPlayed;
        public bool DieRolling {get{return DieVelocity!=PointF.Empty;}}
        public RectangleF CoinBounds {get{return RectangleF.Inflate(Zone,-25*Scale,-25*Scale);}}
        public RectangleF CardBounds {get{return RectangleF.Inflate(Zone,-25*Scale,-35*Scale);}}
        public float DieRadius {get{return 23*Scale*DieSizeMultiplier;}}
        float ToyMinimumSpan {get{return (DieRadius+2*Scale)*2;}}
        public RectangleF DieBounds {get{return RectangleF.Inflate(Zone,-DieRadius-2*Scale,-DieRadius-2*Scale);}}
        public float CoinFlip {get{return Target==PlayTarget.Coin&&Fetch==FetchPhase.Flipping?Math.Min(1,phaseTime/1.1f):0;}}
        public float CardFlip {get{return Target==PlayTarget.Card&&Fetch==FetchPhase.Flipping?Math.Min(1,phaseTime/.65f):0;}}
        public bool WaitingForCardChoice {get{return HasCard&&Target==PlayTarget.Card&&Fetch==FetchPhase.Waiting;}}

        public void SpawnCoin(){if(!Settings.DisplayChest)return;HasCoin=true;CoinHeads=true;Coin=Geometry.Clamp(new PointF(Center.X-Zone.Width/4,Center.Y-Zone.Height/4),CoinBounds);}
        public void SpawnCard(){if(!Settings.DisplayChest)return;HasCard=true;CardRevealed=false;CalledCard=DrawnCard=-1;Card=Geometry.Clamp(new PointF(Center.X+Zone.Width/4,Center.Y+Zone.Height/4),CardBounds);}
        public void SpawnDie(){if(!Settings.DisplayChest)return;HasDie=true;DieValue=20;DieAngle=0;DieVelocity=PointF.Empty;Die=Geometry.Clamp(new PointF(Center.X,Center.Y+Zone.Height/4),DieBounds);}
        public void RemoveCoin(double now){HasCoin=false;if(Target==PlayTarget.Coin)FinishFetch(now);}
        public void RemoveCard(double now){HasCard=false;if(Target==PlayTarget.Card)FinishFetch(now);}
        public void RemoveDie(double now){HasDie=false;DieAiming=false;DieVelocity=PointF.Empty;if(Target==PlayTarget.D20)FinishFetch(now);}
        void ContainGames()
        {
            if(HasCoin&&!ContainsInclusive(CoinBounds,Coin))Coin=Geometry.Clamp(Center,CoinBounds);
            if(HasCard&&!ContainsInclusive(CardBounds,Card))Card=Geometry.Clamp(Center,CardBounds);
            if(HasDie&&!ContainsInclusive(DieBounds,Die)){Die=Geometry.Clamp(Center,DieBounds);DieVelocity=PointF.Empty;}
        }
        public void DragGame(PlayTarget target,PointF position)
        {
            if(target==PlayTarget.Coin&&HasCoin)Coin=Geometry.Clamp(position,CoinBounds);
            if(target==PlayTarget.Card&&HasCard)Card=Geometry.Clamp(position,CardBounds);
            if(Target==target&&Fetch!=FetchPhase.None&&Fetch!=FetchPhase.Returning)
                RestartApproach();
        }
        void RestartApproach()
        {
            if(Target==PlayTarget.Card)
            {
                if(CardRevealed)CalledCard=-1;
                CardRevealed=false;DrawnCard=-1;ChoiceHigh=null;
                if(CalledCard<0)ClearAnnouncement();
                else Announcement=new ToyAnnouncement(SpecialEmoteKind.Card,CalledCard);
            }
            else ClearAnnouncement();
            Fetch=FetchPhase.Approaching;phaseTime=0;pet.ShakeUntil=0;pet.CancelRoute();
        }
        void ClearAnnouncement(){Announcement=null;}
        void BeginGame(PlayTarget target,PointF point)
        {ClearAnnouncement();tune.Clear();Target=target;pet.BeginPlay();Fetch=FetchPhase.Approaching;phaseTime=0;RouteTo(point);}
        public void PressCoin(double now)
        {if(!HasCoin||!Settings.DisplayChest)return;BeginGame(PlayTarget.Coin,Coin);}
        public void PressCard(double now,bool autonomous=false)
        {
            if(!HasCard||!Settings.DisplayChest)return;
            automaticCard=autonomous;CalledCard=DrawnCard=-1;CardRevealed=false;ChoiceHigh=null;BeginGame(PlayTarget.Card,CardApproach());
        }
        PointF CardApproach()
        {
            var display=pet.Displays.Find(d=>d.Id==DisplayId)??Nearest(Card);
            var size=display.PetSize(pet.FrameSize);var allowed=display.Allowed(pet.FrameSize);
            float gap=8*Scale,halfWidth=23*Scale,halfHeight=32*Scale;
            var card=new RectangleF(Card.X-halfWidth,Card.Y-halfHeight,halfWidth*2,halfHeight*2);
            float side=halfWidth+gap+size.Width/2f,bottom=Card.Y+halfHeight;
            var candidates=new[]{new PointF(Card.X-side,bottom),new PointF(Card.X+side,bottom),
                new PointF(Card.X,card.Top-gap-display.NameFootroom),new PointF(Card.X,card.Bottom+gap+size.Height)};
            PointF best=Geometry.Clamp(candidates[0],allowed);float distance=float.MaxValue;
            for(int i=0;i<candidates.Length;i++)
            {
                // Prefer standing beside the card; use above/below only if neither side fits.
                if(i==2&&distance<float.MaxValue)break;
                var point=Geometry.Clamp(candidates[i],allowed);
                var body=new RectangleF(point.X-size.Width/2f,point.Y-size.Height,size.Width,size.Height+display.NameFootroom);
                var clearance=RectangleF.Inflate(card,gap/2,gap/2);
                if(body.IntersectsWith(clearance))continue;
                float travel=Geometry.Distance(pet.Position,point);
                if(travel<distance){best=point;distance=travel;}
            }
            return best;
        }
        // Every round starts from all 52 cards. The second draw excludes the exact announced card.
        internal static int DrawOtherCard(Random random,int called)
        {int draw=random.Next(51);return draw>=called?draw+1:draw;}
        internal static int CardOutcome(int called,int drawn,bool high)
        {
            int difference=drawn%13-called%13;if(difference==0)return 2;
            return (high?difference>0:difference<0)?Reactions.Love:4;
        }
        public void ChooseCard(bool high)
        {
            if(!WaitingForCardChoice)return;ChoiceHigh=high;DrawnCard=DrawOtherCard(random,CalledCard);
            ClearAnnouncement();Fetch=FetchPhase.Flipping;phaseTime=0;
        }
        public void BeginDieAim(){if(HasDie&&Settings.DisplayChest)DieAiming=true;}
        public void CancelDieAim(){DieAiming=false;}
        public void LaunchDiePull(PointF pull,double now){LaunchDie(PullVelocity(pull),now);}
        public void RollDie(double now)
        {double angle=random.NextDouble()*Math.PI*2;float strength=(160+(float)random.NextDouble()*560)*Scale;LaunchDie(new PointF((float)Math.Cos(angle)*strength,(float)Math.Sin(angle)*strength),now);}
        public void LaunchDie(PointF velocity,double now)
        {
            if(!HasDie||!Settings.DisplayChest)return;ClearAnnouncement();tune.Clear();Target=PlayTarget.D20;pet.BeginPlay();
            DieVelocity=velocity;DieAiming=false;Fetch=FetchPhase.Watching;phaseTime=0;FaceDie();
        }
        void FaceDie(){pet.Walking=false;pet.ActualSpeed=0;pet.Facing=Geometry.Direction(new PointF(Die.X-pet.Position.X,Die.Y-pet.Position.Y),pet.Facing);}
        public void AdvanceDie(float dt)
        {
            if(!HasDie||!Settings.DisplayChest||DieAiming||Editing)return;
            dt=Math.Max(0,Math.Min(.1f,dt));float speed=Geometry.Distance(PointF.Empty,DieVelocity);if(speed<.001f){DieVelocity=PointF.Empty;return;}
            float deceleration=220*Scale,time=Math.Min(dt,speed/deceleration),distance=speed*time-.5f*deceleration*time*time,vx=DieVelocity.X,vy=DieVelocity.Y;var bounds=DieBounds;
            Die=new PointF(Reflect(Die.X,vx/speed*distance,bounds.Left,bounds.Right,ref vx),Reflect(Die.Y,vy/speed*distance,bounds.Top,bounds.Bottom,ref vy));
            DieAngle=(DieAngle+distance/(20*Scale*DieSizeMultiplier)*180/(float)Math.PI)%360;
            float next=Math.Max(0,speed-deceleration*time);DieVelocity=next<.01f?PointF.Empty:new PointF(vx/speed*next,vy/speed*next);
        }
        bool GameTick(double now,float dt)
        {
            if(Target!=PlayTarget.Coin&&Target!=PlayTarget.Card&&Target!=PlayTarget.D20)return false;
            if(Fetch==FetchPhase.Returning)return false;
            if(Fetch==FetchPhase.Watching)
            {
                FaceDie();if(!DieRolling){DieValue=random.Next(1,21);Announcement=new ToyAnnouncement(SpecialEmoteKind.Number,DieValue);Fetch=FetchPhase.Result;phaseTime=0;}
                return true;
            }
            if(Fetch==FetchPhase.Approaching)
            {
                if(!Arrived)return true;pet.CancelRoute();pet.FaceDownIdle();phaseTime=0;
                if(Target==PlayTarget.Coin)Fetch=FetchPhase.Pausing;
                else
                {if(CalledCard<0)CalledCard=random.Next(52);Announcement=new ToyAnnouncement(SpecialEmoteKind.Card,CalledCard);Fetch=FetchPhase.Waiting;}
                return true;
            }
            phaseTime+=Math.Max(0,Math.Min(.1f,dt));
            if(Fetch==FetchPhase.Waiting&&automaticCard&&phaseTime>=1.25f)ChooseCard(random.Next(2)==0);
            else if(Fetch==FetchPhase.Pausing&&phaseTime>=.25f){Fetch=FetchPhase.Shaking;phaseTime=0;pet.ShakeUntil=now+.5;}
            else if(Fetch==FetchPhase.Shaking&&phaseTime>=.5f){CoinHeads=random.Next(2)==0;Fetch=FetchPhase.Flipping;phaseTime=0;pet.ShakeUntil=0;}
            else if(Fetch==FetchPhase.Flipping&&phaseTime>=(Target==PlayTarget.Coin?1.1f:.65f))
            {
                if(Target==PlayTarget.Coin)Announcement=new ToyAnnouncement(CoinHeads?SpecialEmoteKind.Heads:SpecialEmoteKind.Tails);
                else{CardRevealed=true;if(ReactionPlayed!=null)ReactionPlayed(CardOutcome(CalledCard,DrawnCard,ChoiceHigh.Value));}
                Fetch=FetchPhase.Result;phaseTime=0;
            }
            else if(Fetch==FetchPhase.Result&&phaseTime>=3)FinishFetch(now);
            return true;
        }
        bool ConsiderGame(double now,PlayTarget target)
        {
            if(target==PlayTarget.Coin){PressCoin(now);return true;}
            if(target==PlayTarget.Card){PressCard(now,true);return true;}
            if(target==PlayTarget.D20){RollDie(now);return true;}return false;
        }
    }
}
