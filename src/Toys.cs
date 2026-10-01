using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Vpet
{
    public enum TriangleSound { Chime, Honk, SnareDrum }
    [DataContract]
    public sealed class ToyPreferences
    {
        [DataMember] public bool DisplayChest;
        [DataMember] public bool DisplayZone=true;
        [DataMember] public bool HelpMessages=true;
        [DataMember] public TriangleSound Sound=TriangleSound.Chime;
        [DataMember] public float Volume=1;
        [DataMember] public float X=float.NaN,Y=float.NaN,Width=480,Height=320;
        [DataMember] public float ChestX=float.NaN,ChestY=float.NaN;
        [OnDeserializing] void Defaults(StreamingContext context)
        {DisplayZone=true;HelpMessages=true;Sound=TriangleSound.Chime;Volume=1;X=Y=ChestX=ChestY=float.NaN;Width=480;Height=320;}
        internal static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        public void Validate()
        {
            Width=Finite(Width)?Math.Max(160,Math.Min(8000,Width)):480;
            Height=Finite(Height)?Math.Max(140,Math.Min(8000,Height)):320;
            if(!Enum.IsDefined(typeof(TriangleSound),Sound))Sound=TriangleSound.Chime;
            Volume=Finite(Volume)?Math.Max(0,Math.Min(1,Volume)):1;
        }
    }

    internal enum BallLauncher { None, User, Pet }
    internal enum FetchPhase { None, Approaching, Pausing, Shaking, Returning, Repeating, Waiting, Flipping, Watching, Result, TurningDown, Leaving }
    internal enum PlayTarget { Ball, Triangle, Coin, Card, D20, Plate }
    [Flags] internal enum ZoneEdge { None=0, Left=1, Top=2, Right=4, Bottom=8 }

    // Ground-plane physics is independent of the bounce drawing and of the fence's visibility.
    internal sealed partial class ToyModel
    {
        readonly PetModel pet;
        readonly Random random;
        public readonly ToyPreferences Settings;
        public readonly float Scale;
        public RectangleF Zone {get;private set;}
        public PointF Chest {get;private set;}
        public PointF Ball {get;private set;}
        public PointF Triangle {get;private set;}
        public bool HasTriangle {get;private set;}
        public PlayTarget Target {get;private set;}
        public event Action ChimePlayed;
        readonly List<double> tune=new List<double>();
        double lastPress,firstPress,repeatStarted;
        int playedNotes;
        public double NextPlayAt {get;private set;}
        internal int RememberedNotes {get{return tune.Count;}}
        public PointF Velocity {get;private set;}
        public bool HasBall {get;private set;}
        public bool Aiming {get;private set;}
        public BallLauncher Launcher {get;private set;}
        public FetchPhase Fetch {get;private set;}
        public string DisplayId {get;private set;}
        public bool Editing;
        float bounceTime=1,phaseTime;
        PointF approach;
        string approachDisplay;
        public float Radius {get{return 13*Scale;}}
        public SizeF ChestSize {get{return new SizeF(64*Scale,50*Scale);}}
        public PointF Center {get{return new PointF(Zone.X+Zone.Width/2,Zone.Y+Zone.Height/2);}}
        public bool Rolling {get{return Velocity.X!=0||Velocity.Y!=0;}}
        public RectangleF BallBounds {get{return RectangleF.Inflate(Zone,-Radius-3*Scale,-Radius-3*Scale);}}
        RectangleF ChestBounds {get{return RectangleF.Inflate(Zone,-ChestSize.Width/2-3*Scale,-ChestSize.Height/2-3*Scale);}}
        public RectangleF TriangleBounds {get{return RectangleF.Inflate(Zone,-25*Scale,-25*Scale);}}
        public float BounceHeight {get{return BounceOffset(bounceTime,Scale);}}
        internal static float BounceOffset(float time,float scale)
        {
            float height=18*scale;
            for(int bounce=0;bounce<3;bounce++)
            {
                float duration=.36f*(float)Math.Pow(.5,bounce/2f);
                if(time<duration){float progress=Math.Max(0,time)/duration;return 4*progress*(1-progress)*height;}
                time-=duration;height*=.5f;
            }
            return 0;
        }

        public ToyModel(PetModel pet,Random random)
        {
            this.pet=pet;this.random=random;Settings=pet.Settings.Toys;Settings.Validate();
            NextPlayAt=double.NaN;
            Scale=Math.Max(.5f,Math.Min(3,pet.Current.Scale));
            Zone=new RectangleF(Settings.X,Settings.Y,Settings.Width,Settings.Height);
            if(!ToyPreferences.Finite(Zone.X)||!ToyPreferences.Finite(Zone.Y))
                Zone=new RectangleF(pet.Position.X-240*Scale,pet.Position.Y-160*Scale,480*Scale,320*Scale);
            Chest=new PointF(Settings.ChestX,Settings.ChestY);
            PlatePosition=new PointF(pet.Settings.Plate.X,pet.Settings.Plate.Y);
            bool firstChest=!ToyPreferences.Finite(Chest.X)||!ToyPreferences.Finite(Chest.Y);
            RecoverDisplays();
            // Leave the central movement control exposed when creating the table for the first time.
            if(firstChest){Chest=Geometry.Clamp(new PointF(Center.X-Zone.Width/4,Center.Y+Zone.Height/10),ChestBounds);Store();}
        }
        DisplayArea Nearest(PointF point)
        {
            DisplayArea best=pet.Displays[0];float distance=float.MaxValue;
            foreach(var d in pet.Displays)
            {float score=Geometry.Distance(point,Geometry.Clamp(point,d.Work));if(score<distance){best=d;distance=score;}}
            return best;
        }
        RectangleF Fit(RectangleF requested,DisplayArea display) {return FenceGeometry.Fit(requested,display,pet.FrameSize,ToyMinimumSpan);}
        public void RecoverDisplays()
        {
            var display=Nearest(Center);DisplayId=display.Id;
            Zone=Fit(Zone,display);ContainObjects();Store();pet.SetSharedRestrictedArea(Zone);
            RecoverPlate();
            if(Fetch!=FetchPhase.None){pet.CancelRoute();if(Fetch!=FetchPhase.Returning&&Fetch!=FetchPhase.Leaving&&(Target!=PlayTarget.D20||Fetch==FetchPhase.Approaching))RestartApproach();}
        }
        void ContainObjects()
        {
            if(!ToyPreferences.Finite(Chest.X)||!ToyPreferences.Finite(Chest.Y)||!ContainsInclusive(ChestBounds,Chest))Chest=Center;
            if(HasBall&&!ContainsInclusive(BallBounds,Ball)){Ball=BesideChest();Velocity=PointF.Empty;}
            if(HasTriangle&&!ContainsInclusive(TriangleBounds,Triangle))Triangle=Geometry.Clamp(Center,TriangleBounds);
            ContainGames();
        }
        internal static bool ContainsInclusive(RectangleF r,PointF p)
        {return p.X>=r.Left&&p.X<=r.Right&&p.Y>=r.Top&&p.Y<=r.Bottom;}
        public void MoveZone(PointF center)
        {
            var display=Nearest(center);DisplayId=display.Id;
            ChangeZone(Fit(new RectangleF(center.X-Zone.Width/2,center.Y-Zone.Height/2,Zone.Width,Zone.Height),display));
        }
        public void ResizeZone(RectangleF original,ZoneEdge edges,PointF delta)
        {
            ChangeZone(FenceGeometry.Resize(original,edges,delta,pet.Displays.Find(d=>d.Id==DisplayId),pet.FrameSize,ToyMinimumSpan));
        }
        void ChangeZone(RectangleF zone)
        {
            Zone=zone;ContainObjects();Store();pet.SetSharedRestrictedArea(Zone);
            if(Target!=PlayTarget.Plate&&Fetch!=FetchPhase.None&&Fetch!=FetchPhase.Returning&&Fetch!=FetchPhase.Leaving&&(Target!=PlayTarget.D20||Fetch==FetchPhase.Approaching))RestartApproach();
        }
        public void DragChest(PointF point){Chest=Geometry.Clamp(point,ChestBounds);Store();}
        public void SetVisible(bool visible,double now)
        {
            Settings.DisplayChest=visible;Aiming=false;Editing=false;
            if(!visible)CleanUp(now);
        }
        public void CleanUp(double now)
        {Aiming=DieAiming=Editing=false;RemoveBall(now);RemoveTriangle(now);RemoveCoin(now);RemoveCard(now);RemoveDie(now);ClearAnnouncement();}
        public void DragBall(PointF point,double now)
        {if(!HasBall)return;Ball=Geometry.Clamp(point,BallBounds);Velocity=PointF.Empty;Launcher=BallLauncher.None;bounceTime=1;if(Target==PlayTarget.Ball&&Fetch!=FetchPhase.None&&Fetch!=FetchPhase.Approaching)FinishFetch(now);}
        public void RemoveBall(double now)
        {HasBall=false;Aiming=false;Velocity=PointF.Empty;Launcher=BallLauncher.None;bounceTime=1;if(Target==PlayTarget.Ball)FinishFetch(now);}
        public void SpawnBall(double now)
        {
            if(!Settings.DisplayChest)return;
            if(Target==PlayTarget.Ball)FinishFetch(now);HasBall=true;Aiming=false;Launcher=BallLauncher.None;Velocity=PointF.Empty;
            Ball=BesideChest();bounceTime=1;
        }
        public void SpawnTriangle()
        {
            if(!Settings.DisplayChest)return;
            HasTriangle=true;Triangle=Geometry.Clamp(new PointF(Center.X+Zone.Width/4,Center.Y-Zone.Height/5),TriangleBounds);
        }
        public void RemoveTriangle(double now)
        {HasTriangle=false;tune.Clear();if(Target==PlayTarget.Triangle)FinishFetch(now);}
        public void DragTriangle(PointF point)
        {
            if(!HasTriangle)return;Triangle=Geometry.Clamp(point,TriangleBounds);
            if(Target==PlayTarget.Triangle&&Fetch!=FetchPhase.None&&Fetch!=FetchPhase.Returning)
            {Fetch=FetchPhase.Approaching;phaseTime=0;pet.ShakeUntil=0;pet.CancelRoute();}
        }
        void Chime(){if(ChimePlayed!=null)ChimePlayed();}
        public void PressTriangle(double now)
        {
            if(!HasTriangle||!Settings.DisplayChest)return;
            // Presses before playback form one phrase. A new press during playback starts a new phrase.
            if(Target!=PlayTarget.Triangle||Fetch==FetchPhase.None||Fetch==FetchPhase.Returning||Fetch==FetchPhase.Repeating)
            {tune.Clear();playedNotes=0;firstPress=now;}
            double offset=tune.Count==0?0:Math.Max(now-firstPress,tune[tune.Count-1]+.10);
            tune.Add(offset);lastPress=now;Chime();
            // Additional taps must not cancel an in-progress display crossing to this same instrument.
            if(Target!=PlayTarget.Triangle||Fetch!=FetchPhase.Approaching)BeginTriangle();
        }
        void BeginTriangle()
        {ClearAnnouncement();Target=PlayTarget.Triangle;pet.BeginPlay();Fetch=FetchPhase.Approaching;phaseTime=0;RouteTo(Triangle);}
        void SchedulePlay(double now){NextPlayAt=now+60+random.NextDouble()*60;}
        void ConsiderPlay(double now)
        {
            if(double.IsNaN(NextPlayAt))SchedulePlay(now);
            if(now<NextPlayAt||!Settings.DisplayChest||Fetch!=FetchPhase.None||pet.Paused||pet.Hovered||pet.Dragging||Aiming||DieAiming||Editing)return;
            var choices=new List<PlayTarget>();if(HasBall&&!Rolling)choices.Add(PlayTarget.Ball);if(HasTriangle)choices.Add(PlayTarget.Triangle);
            if(HasCoin)choices.Add(PlayTarget.Coin);if(HasCard&&!CardTurningDown)choices.Add(PlayTarget.Card);if(HasDie&&!DieRolling)choices.Add(PlayTarget.D20);
            if(choices.Count==0)return;var selected=choices[random.Next(choices.Count)];if(ConsiderGame(now,selected))return;
            if(selected==PlayTarget.Triangle)
            {
                tune.Clear();playedNotes=0;int notes=random.Next(1,4);for(int i=0;i<notes;i++)tune.Add(i*.3);
                lastPress=now-1;BeginTriangle();
            }
            else if(!Rolling){Target=PlayTarget.Ball;pet.BeginPlay();Fetch=FetchPhase.Approaching;phaseTime=0;RouteToBall();}
        }
        PointF BesideChest()
        {
            float x=Chest.X+ChestSize.Width/2+Radius+12*Scale;
            if(x>BallBounds.Right)x=Chest.X-ChestSize.Width/2-Radius-12*Scale;
            return Geometry.Clamp(new PointF(x,Chest.Y),BallBounds);
        }
        public void BeginAim(){if(HasBall&&Settings.DisplayChest)Aiming=true;}
        public void CancelAim(){Aiming=false;}
        public void Bounce(){if(HasBall){Aiming=false;bounceTime=0;}}
        public PointF PullVelocity(PointF pull)
        {
            float length=Geometry.Distance(PointF.Empty,pull);
            if(length<.001f)return PointF.Empty;
            float speed=Math.Min(720*Scale,length*5);
            return new PointF(-pull.X/length*speed,-pull.Y/length*speed);
        }
        public void LaunchPull(PointF pull,double now){Launch(PullVelocity(pull),BallLauncher.User,now);}
        public void Launch(PointF velocity,BallLauncher launcher,double now)
        {
            if(!HasBall||!Settings.DisplayChest)return;
            Aiming=false;bounceTime=1;Velocity=velocity;Launcher=launcher;
            if(launcher==BallLauncher.User)
            {ClearAnnouncement();tune.Clear();Target=PlayTarget.Ball;pet.BeginPlay();Fetch=FetchPhase.Approaching;phaseTime=0;RouteToBall();}
        }
        static float Reflect(float position,float delta,float low,float high,ref float velocity)
        {
            double width=high-low;if(width<=0){velocity=0;return low;}
            double value=(position-low+delta)%(width*2);if(value<0)value+=width*2;
            bool reflected=value>width;
            float result=(float)(low+(reflected?2*width-value:value));
            if(reflected)velocity=-velocity;
            if(result<=low+.0001f)velocity=Math.Abs(velocity);
            if(result>=high-.0001f)velocity=-Math.Abs(velocity);
            return result;
        }
        public PointF RestingPoint
        {
            get
            {
                float speed=Geometry.Distance(PointF.Empty,Velocity);if(speed<.001f)return Ball;
                float duration=speed/(2*220*Scale),vx=Velocity.X,vy=Velocity.Y;var bounds=BallBounds;
                return new PointF(Reflect(Ball.X,vx*duration,bounds.Left,bounds.Right,ref vx),Reflect(Ball.Y,vy*duration,bounds.Top,bounds.Bottom,ref vy));
            }
        }
        public void AdvanceBall(float dt)
        {
            if(!Settings.DisplayChest||!HasBall||Aiming||Editing)return;
            dt=Math.Max(0,Math.Min(.1f,dt));bounceTime+=dt;
            float speed=Geometry.Distance(PointF.Empty,Velocity);if(speed<.001f){Velocity=PointF.Empty;return;}
            float deceleration=220*Scale,time=Math.Min(dt,speed/deceleration),distance=speed*time-.5f*deceleration*time*time;
            float vx=Velocity.X,vy=Velocity.Y;var bounds=BallBounds;
            Ball=new PointF(Reflect(Ball.X,vx/speed*distance,bounds.Left,bounds.Right,ref vx),Reflect(Ball.Y,vy/speed*distance,bounds.Top,bounds.Bottom,ref vy));
            float next=Math.Max(0,speed-deceleration*time);Velocity=next<.01f?PointF.Empty:new PointF(vx/speed*next,vy/speed*next);
        }
        void RouteToBall()
        {RouteTo(RestingPoint);}
        void RouteTo(PointF target,string targetDisplay=null)
        {
            var display=pet.Displays.Find(d=>d.Id==(targetDisplay??DisplayId))??Nearest(target);
            // Stand as close as the full sprite and its name can fit at an outside screen edge.
            approach=Geometry.Clamp(target,display.Allowed(pet.FrameSize));approachDisplay=display.Id;
            if(!pet.Destination.HasValue||Geometry.Distance(pet.Destination.Value,approach)>.5f||pet.TargetDisplay!=approachDisplay)
            {pet.IdleUntil=0;pet.SetDestination(approach,approachDisplay);}
        }
        bool Arrived {get{return pet.Crossing==null&&pet.CurrentDisplay==approachDisplay&&Geometry.Distance(pet.Position,approach)<=pet.Current.Scale;}}
        public void BeforePetTick(double now,float dt)
        {
            AdvanceBall(dt);
            AdvanceDie(dt);
            AdvanceCard(dt);
            ConsiderPlay(now);
            // Re-route visits while toys are being moved; editing only delays the interaction at arrival.
            if(Fetch==FetchPhase.None||pet.Dragging)return;
            if(Fetch==FetchPhase.Approaching){if(Target==PlayTarget.Plate)RouteToPlate();else if(Target==PlayTarget.Triangle)RouteTo(Triangle);else if(Target==PlayTarget.Coin)RouteTo(CoinApproach());else if(Target==PlayTarget.Card)RouteTo(CardApproach());else if(Target==PlayTarget.D20)RouteTo(DieApproach());else RouteToBall();}
            else if(Fetch==FetchPhase.Returning)
            {
                if(pet.Settings.Movement!=MovementMode.Restricted||pet.InsideRestriction(pet.Position))
                {pet.EndPlay(now);Fetch=FetchPhase.None;return;}
                var display=Nearest(pet.RestrictedCenter);approach=Geometry.Clamp(pet.RestrictedCenter,display.Allowed(pet.FrameSize));approachDisplay=display.Id;
                if(!pet.Destination.HasValue){pet.IdleUntil=0;pet.SetDestination(approach,approachDisplay);}
            }
            if(Fetch==FetchPhase.Shaking)pet.ShakeUntil=now+Math.Max(.001f,(Target==PlayTarget.Plate?.125f:.5f)-phaseTime);
        }
        public void AfterPetTick(double now,float dt)
        {
            if(Fetch==FetchPhase.None)return;
            // Once the coin is airborne it completes its flip while unrelated toys are handled.
            bool flippingCoin=Target==PlayTarget.Coin&&Fetch==FetchPhase.Flipping;
            if((!flippingCoin&&Target!=PlayTarget.Plate&&(Aiming||DieAiming||Editing))||pet.Dragging||pet.Paused||pet.Hovered){if(Fetch==FetchPhase.Repeating)repeatStarted+=Math.Max(0,dt);return;}
            if(Fetch==FetchPhase.Leaving){if(!pet.Destination.HasValue&&pet.Crossing==null)FinishFetch(now);return;}
            if(FoodTick(now,dt))return;
            if(GameTick(now,dt))return;
            if(Fetch==FetchPhase.Repeating)
            {
                // Play at most one note per tick; keep rapid taps audible after a delayed UI tick.
                if(playedNotes<tune.Count&&now-repeatStarted>=tune[playedNotes])
                {Chime();pet.ShakeUntil=now+.12;playedNotes++;}
                if(playedNotes==tune.Count)FinishFetch(now);
                return;
            }
            if(Fetch==FetchPhase.Approaching)
            {
                if((Target==PlayTarget.Triangle?now-lastPress>=.75:!Rolling)&&Arrived){pet.CancelRoute();pet.FaceDownIdle();Fetch=FetchPhase.Pausing;phaseTime=0;}
                return;
            }
            if(Fetch==FetchPhase.Returning)return;
            phaseTime+=Math.Max(0,Math.Min(.1f,dt));
            if(Fetch==FetchPhase.Pausing&&phaseTime>=.25f)
            {
                if(Target==PlayTarget.Triangle){Fetch=FetchPhase.Repeating;repeatStarted=now-(playedNotes<tune.Count?tune[playedNotes]:0);}
                else{Fetch=FetchPhase.Shaking;phaseTime=0;pet.ShakeUntil=now+.5;}
            }
            else if(Fetch==FetchPhase.Shaking&&phaseTime>=.5f)
            {
                double angle=random.NextDouble()*Math.PI*2;float speed=(140+(float)random.NextDouble()*580)*Scale;
                Launch(new PointF((float)Math.Cos(angle)*speed,(float)Math.Sin(angle)*speed),BallLauncher.Pet,now);FinishFetch(now);
            }
        }
        void FinishFetch(double now)
        {
            ClearAnnouncement();
            SchedulePlay(now);
            if(Fetch==FetchPhase.None)return;
            pet.CancelRoute();pet.ShakeUntil=0;phaseTime=0;
            if(pet.Settings.Movement==MovementMode.Restricted&&!pet.InsideRestriction(pet.Position))
            {Fetch=FetchPhase.Returning;pet.IdleUntil=0;}
            else{Fetch=FetchPhase.None;pet.EndPlay(now);}
        }
        public void CancelFetchForPetDrag(double now)
        {ClearAnnouncement();if(Fetch==FetchPhase.None)return;Fetch=FetchPhase.None;tune.Clear();pet.EndPlay(now);SchedulePlay(now);}
        public void Store()
        {Settings.X=Zone.X;Settings.Y=Zone.Y;Settings.Width=Zone.Width;Settings.Height=Zone.Height;Settings.ChestX=Chest.X;Settings.ChestY=Chest.Y;}
    }
}
