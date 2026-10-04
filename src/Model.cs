using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Vpet
{
    public enum MovementMode { FreeRoam, Restricted, Static }
    // Keep value 1 so saved Desktop Only preferences migrate to Under All.
    public enum LayerMode { OverEverything = 0, UnderAll = 1, Dynamic = 2 }
    public enum Personality { Sweet, Sassy, Bashful }
    public enum Frequency { Rarely, Sometimes, Often, Off }
    public enum NameVisibility { Hidden, OnHover, Always }

    [DataContract]
    public sealed class Preferences
    {
        [DataMember] public MovementMode Movement = MovementMode.FreeRoam;
        [DataMember] public LayerMode Layer = LayerMode.OverEverything;
        [DataMember] public Personality Personality = Personality.Sweet;
        [DataMember] public Frequency Frequency = Frequency.Sometimes;
        [DataMember] public int Speed = 50;
        [DataMember] public int Radius = 250;
        [DataMember] public float RestrictedWidth=float.NaN,RestrictedHeight=float.NaN;
        [DataMember] public bool SyncPlayZone=true;
        [DataMember] public bool DisplayRestrictedArea = true;
        [DataMember] public bool RestrictedAreaCreated;
        [DataMember] public float X = float.NaN;
        [DataMember] public float Y = float.NaN;
        [DataMember] public float AnchorX = float.NaN;
        [DataMember] public float AnchorY = float.NaN;
        [DataMember] public int Facing = 2;
        [DataMember] public bool CustomPet;
        [DataMember] public string PetName = "";
        [DataMember] public NameVisibility NameDisplay = NameVisibility.Always;
        [DataMember] public bool LaunchOnStartup;
        [DataMember] public bool AutoUpdate;
        [DataMember] public string LastSpriteProject = "";
        [DataMember] public ToyPreferences Toys = new ToyPreferences();
        [DataMember] public PlatePreferences Plate = new PlatePreferences();
        [DataMember] public FindPetPreferences FindPet = new FindPetPreferences();
        [DataMember] public JoystickPreferences Joystick = new JoystickPreferences();
        [DataMember] public ArcadePreferences Arcade = new ArcadePreferences();

        [OnDeserializing]
        void InitializeDefaults(StreamingContext context)
        {
            Speed=50;Radius=250;DisplayRestrictedArea=true;Facing=2;
            RestrictedWidth=RestrictedHeight=float.NaN;SyncPlayZone=true;
            X=Y=AnchorX=AnchorY=float.NaN;Frequency=Frequency.Sometimes;
            PetName="";NameDisplay=NameVisibility.Always;LaunchOnStartup=false;AutoUpdate=false;LastSpriteProject="";
            Toys=new ToyPreferences();Plate=new PlatePreferences();FindPet=new FindPetPreferences();
            Joystick=new JoystickPreferences();Arcade=new ArcadePreferences();
        }

        public void Validate()
        {
            Speed = Math.Max(0, Math.Min(100, Speed)); Radius = Math.Max(30, Math.Min(1000, Radius));
            RestrictedWidth=ToyPreferences.Finite(RestrictedWidth)?Math.Max(160,Math.Min(8000,RestrictedWidth)):Radius*2;
            RestrictedHeight=ToyPreferences.Finite(RestrictedHeight)?Math.Max(140,Math.Min(8000,RestrictedHeight)):Radius*2;
            Facing = ((Facing % 8) + 8) % 8;
            if (!Enum.IsDefined(typeof(MovementMode), Movement)) Movement = MovementMode.FreeRoam;
            if (!Enum.IsDefined(typeof(LayerMode), Layer)) Layer = LayerMode.OverEverything;
            if (!Enum.IsDefined(typeof(Personality), Personality)) Personality = Personality.Sweet;
            if (!Enum.IsDefined(typeof(Frequency), Frequency)) Frequency = Frequency.Sometimes;
            PetName=CleanName(PetName);
            if(LastSpriteProject==null)LastSpriteProject="";
            if (!Enum.IsDefined(typeof(NameVisibility), NameDisplay)) NameDisplay=NameVisibility.Always;
            if(Toys==null)Toys=new ToyPreferences();Toys.Validate();
            if(Plate==null)Plate=new PlatePreferences();Plate.Validate();
            if(FindPet==null)FindPet=new FindPetPreferences();FindPet.Validate();
            if(Joystick==null)Joystick=new JoystickPreferences();
            if(Arcade==null)Arcade=new ArcadePreferences();Arcade.Validate();
        }
        public static string CleanName(string name)
        {
            var result=new System.Text.StringBuilder();
            foreach(char c in name??"")if(!char.IsControl(c))result.Append(c);
            string text=result.ToString().Trim();
            if(text.Length>40)text=text.Substring(0,char.IsHighSurrogate(text[39])?39:40);
            return text;
        }
        public bool HasName { get { return !string.IsNullOrWhiteSpace(PetName)&&NameDisplay!=NameVisibility.Hidden; } }
        public bool ShowName(bool hovered) { return HasName&&(NameDisplay==NameVisibility.Always||hovered); }

        public static Preferences Load(string path)
        {
            try { using (var stream = File.OpenRead(path)) { var p = (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream); p.Validate(); return p; } }
            catch { return new Preferences(); }
        }

        public void Save(string path)
        {
            string pending = path + ".tmp";
            using (var stream = File.Create(pending)) new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream, this);
            if (File.Exists(path)) File.Replace(pending, path, null); else File.Move(pending, path);
        }
    }

    public sealed class DisplayArea
    {
        public string Id;
        public Rectangle Work;
        public float Scale;
        public int NameFootroom;
        public DisplayArea(string id, Rectangle work, float scale) { Id = id; Work = work; Scale = scale; }
        public Size PetSize(Size source) { return new Size((int)Math.Ceiling(source.Width * 2 * Scale), (int)Math.Ceiling(source.Height * 2 * Scale)); }
        public RectangleF Allowed(Size source,bool includeName=true)
        {
            Size size = PetSize(source);
            int footroom=includeName?NameFootroom:0;
            // Position is the bottom-center ground anchor, with room for the full cell.
            return RectangleF.FromLTRB(Work.Left + size.Width / 2f, Work.Top + size.Height,
                Math.Max(Work.Left + size.Width / 2f, Work.Right - size.Width / 2f), Math.Max(Work.Top + size.Height, Work.Bottom-footroom));
        }
    }

    public static class Geometry
    {
        public static float Distance(PointF a, PointF b) { float x = a.X - b.X, y = a.Y - b.Y; return (float)Math.Sqrt(x*x + y*y); }
        public static PointF Clamp(PointF p, RectangleF r) { return new PointF(Math.Max(r.Left, Math.Min(r.Right, p.X)), Math.Max(r.Top, Math.Min(r.Bottom, p.Y))); }
        public static PointF Toward(PointF a, PointF b, float distance)
        {
            float length = Distance(a, b); if (length <= distance || length < 0.001f) return b;
            return new PointF(a.X + (b.X-a.X)/length*distance, a.Y + (b.Y-a.Y)/length*distance);
        }
        public static int Direction(PointF delta, int previous)
        {
            double angle = Math.Atan2(delta.Y, delta.X) * 180 / Math.PI;
            double difference = ((angle - previous * 45 + 540) % 360) - 180;
            if (Math.Abs(difference) <= 27.5) return previous;
            return (((int)Math.Floor(angle / 45 + 0.5)) % 8 + 8) % 8;
        }
    }

    public sealed class PetModel
    {
        public readonly Preferences Settings;
        public List<DisplayArea> Displays = new List<DisplayArea>();
        public PointF Position, Anchor;
        public Size FrameSize = new Size(32, 36);
        public int Facing = 2;
        public bool Hovered, Dragging, Paused, Walking;
        // A deliberate toy interaction temporarily owns the route, without changing saved movement preferences.
        public bool Playing { get; private set; }
        public double ShakeUntil, IdleUntil;
        public float ActualSpeed;
        public PointF LastMotion;
        public PointF? Destination { get; private set; }
        public string TargetDisplay { get; private set; }
        public string CurrentDisplay { get; private set; }
        readonly Random random;
        DisplayCrossing plannedCrossing;
        public DisplayCrossing Crossing { get; private set; }
        bool settlingDrag;
        float settleTarget;
        RectangleF? sharedRestrictedArea;
        public RectangleF OwnRestrictedArea {get{return new RectangleF(Anchor.X-Settings.RestrictedWidth/2,Anchor.Y-Settings.RestrictedHeight/2,Settings.RestrictedWidth,Settings.RestrictedHeight);}}
        public RectangleF RestrictedArea {get{return Settings.SyncPlayZone&&sharedRestrictedArea.HasValue?sharedRestrictedArea.Value:OwnRestrictedArea;}}
        public RectangleF RestrictedAllowed
        {
            get
            {
                var zone=RestrictedArea;var display=FenceGeometry.Nearest(Displays,FenceGeometry.Center(zone));var size=display.PetSize(FrameSize);float pad=4*display.Scale;
                var inside=RectangleF.FromLTRB(zone.Left+size.Width/2f+pad,zone.Top+size.Height+pad,zone.Right-size.Width/2f-pad,zone.Bottom-display.NameFootroom-pad);
                if(inside.Width<0||inside.Height<0)return new RectangleF(Geometry.Clamp(FenceGeometry.Center(zone),display.Allowed(FrameSize)),SizeF.Empty);
                return RectangleF.Intersect(inside,display.Allowed(FrameSize));
            }
        }
        public PointF RestrictedCenter {get{return Geometry.Clamp(FenceGeometry.Center(RestrictedArea),RestrictedAllowed);}}
        public bool InsideRestriction(PointF point){return ToyModel.ContainsInclusive(RestrictedAllowed,point);}
        public bool RestrictedAreaVisible {get{return Settings.SyncPlayZone?Settings.Toys.DisplayZone:Settings.DisplayRestrictedArea;}}
        public void SetRestrictedAreaVisible(bool value){if(Settings.SyncPlayZone)Settings.Toys.DisplayZone=value;else Settings.DisplayRestrictedArea=value;}
        public void SetSharedRestrictedArea(RectangleF zone){bool changed=sharedRestrictedArea!=zone;sharedRestrictedArea=zone;if(changed&&Settings.SyncPlayZone){CancelRoute();EnsureInsideRestrictedArea();}}

        public PetModel(Preferences settings, Random rng)
        {
            settings.Validate();Settings = settings; random = rng; Position = new PointF(settings.X, settings.Y);
            Anchor = new PointF(settings.AnchorX, settings.AnchorY); Facing = settings.Facing;
            if(settings.Movement==MovementMode.Restricted&&!float.IsNaN(Anchor.X)&&!float.IsNaN(Anchor.Y))settings.RestrictedAreaCreated=true;
        }
        public DisplayArea Current { get { return Displays.Find(d => d.Id == CurrentDisplay) ?? Displays[0]; } }
        public bool Shaking(double now) { return now < ShakeUntil; }
        public void SetDisplays(List<DisplayArea> displays)
        {
            if (displays.Count == 0) return;
            Displays = displays;
            UpdateNameFootroom();
            if (float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsInfinity(Position.X) || float.IsInfinity(Position.Y))
                Position = new PointF(displays[0].Work.Left + displays[0].Work.Width * .6f, displays[0].Work.Top + displays[0].Work.Height * .7f);
            Place(Position);
            if (float.IsNaN(Anchor.X) || float.IsNaN(Anchor.Y) || !displays.Exists(d => d.Allowed(FrameSize).Contains(Anchor))) Anchor = Position;
            FitRestrictedArea();
            CancelRoute();EnsureInsideRestrictedArea();
        }
        public void Place(PointF requested)
        {
            DisplayArea best = null; PointF candidate = requested; float score = float.MaxValue;
            foreach (var d in Displays)
            {
                PointF p = Geometry.Clamp(requested, d.Allowed(FrameSize)); float distance = Geometry.Distance(p, requested);
                if (distance < score) { best = d; candidate = p; score = distance; }
            }
            if (best != null) { Position = candidate; CurrentDisplay = best.Id; }
        }
        public void DragTo(PointF requested)
        {
            settlingDrag=false;Destination=null;TargetDisplay=null;plannedCrossing=null;
            DisplayCrossing best=null;float bestDistance=float.MaxValue;
            for(int i=0;i<Displays.Count;i++)for(int j=i+1;j<Displays.Count;j++)
            {
                var from=Displays[i];var to=Displays[j];
                if(DisplayCrossing.NextDisplay(from,to,Displays,FrameSize).Id!=to.Id)continue;
                var crossing=DisplayCrossing.Plan(from,to,FrameSize,requested);
                var a=from.Allowed(FrameSize,false);var b=to.Allowed(FrameSize,false);
                // Each side follows the pointer independently on offset displays.
                if(crossing.Horizontal)
                {
                    if(requested.Y<Math.Min(a.Top,b.Top)||requested.Y>Math.Max(a.Bottom,b.Bottom))continue;
                    crossing.Exit.Y=Geometry.Clamp(requested,a).Y;
                    crossing.Entry.Y=Geometry.Clamp(requested,b).Y;
                }
                else
                {
                    if(requested.X<Math.Min(a.Left,b.Left)||requested.X>Math.Max(a.Right,b.Right))continue;
                    crossing.Exit.X=Geometry.Clamp(requested,a).X;
                    crossing.Entry.X=Geometry.Clamp(requested,b).X;
                }
                float start=crossing.Horizontal?crossing.Exit.X:crossing.Exit.Y;
                float end=crossing.Horizontal?crossing.Entry.X:crossing.Entry.Y;
                float axis=crossing.Horizontal?requested.X:requested.Y;
                if(Math.Abs(end-start)<.001f)continue;
                float progress=(axis-start)/(end-start);
                if(progress<=0||progress>=1)continue;
                crossing.Progress=progress;
                PointF bridge=new PointF(crossing.Exit.X+(crossing.Entry.X-crossing.Exit.X)*progress,crossing.Exit.Y+(crossing.Entry.Y-crossing.Exit.Y)*progress);
                float distance=Geometry.Distance(requested,bridge);
                // Don't attach to a distant seam while moving inside another display.
                float ordinary=float.MaxValue;
                foreach(var d in Displays)ordinary=Math.Min(ordinary,Geometry.Distance(requested,Geometry.Clamp(requested,d.Allowed(FrameSize,false))));
                if((ordinary<.01f&&distance>.01f)||distance>=bestDistance)continue;
                best=crossing;bestDistance=distance;
            }
            Crossing=best;
            if(best==null)Place(requested);else UpdateCrossingPosition();
            FaceDownIdle();
        }
        void UpdateCrossingPosition()
        {
            bool arriving=Crossing.Progress>=.5f;
            Position=arriving?Crossing.DestinationAnchor:Crossing.SourceAnchor;
            CurrentDisplay=arriving?Crossing.To.Id:Crossing.From.Id;
        }
        public void UpdateNameFootroom()
        {
            foreach(var d in Displays)d.NameFootroom=Settings.HasName?Math.Min(PetCaption.Footroom(d.Scale),Math.Max(0,d.Work.Height-d.PetSize(FrameSize).Height)):0;
        }
        public void CancelRoute() { if(Crossing!=null)Place(Position);Destination = null; TargetDisplay = null; plannedCrossing=null;Crossing=null;settlingDrag=false; Walking = false; }
        public void BeginPlay(){CancelRoute();Playing=true;IdleUntil=0;ShakeUntil=0;}
        public void EndPlay(double now){CancelRoute();Playing=false;ShakeUntil=0;IdleUntil=now+2;}
        public void Release(double now)
        {
            Dragging=false;
            if(Crossing!=null&&(Settings.Movement!=MovementMode.Restricted||InsideRestriction(Position)))
            {settlingDrag=true;settleTarget=Crossing.Progress>=.5f?1:0;Destination=null;TargetDisplay=null;plannedCrossing=null;}
            else {Place(Position);CancelRoute();EnsureInsideRestrictedArea();}
            FaceDownIdle();
            ShakeUntil=0;IdleUntil=now+5;
        }
        public void FaceDownIdle(){Facing=2;Walking=false;ActualSpeed=0;}
        public int Click(double now)
        {
            FaceDownIdle();ShakeUntil=now+.5;return Reactions.Love;
        }
        public void MoveRestrictedArea(PointF center)
        {
            Anchor=center;FitRestrictedArea();Settings.RestrictedAreaCreated=true;CancelRoute();EnsureInsideRestrictedArea();
        }
        void FitRestrictedArea()
        {var zone=FenceGeometry.Fit(OwnRestrictedArea,FenceGeometry.Nearest(Displays,Anchor),FrameSize);Anchor=FenceGeometry.Center(zone);Settings.RestrictedWidth=zone.Width;Settings.RestrictedHeight=zone.Height;}
        internal void ResizeRestrictedArea(RectangleF original,ZoneEdge edges,PointF delta)
        {
            var zone=FenceGeometry.Resize(original,edges,delta,FenceGeometry.Nearest(Displays,Anchor),FrameSize);
            Anchor=FenceGeometry.Center(zone);Settings.RestrictedWidth=zone.Width;Settings.RestrictedHeight=zone.Height;CancelRoute();EnsureInsideRestrictedArea();
        }
        public void SetRadius(int radius)
        {
            // Retain legacy migration/API support; the UI now resizes the fence directly.
            Settings.Radius=Math.Max(30,Math.Min(1000,radius));Settings.RestrictedWidth=Settings.RestrictedHeight=Settings.Radius*2;FitRestrictedArea();CancelRoute();EnsureInsideRestrictedArea();
        }
        public void EnsureInsideRestrictedArea()
        {
            if(Displays.Count>0&&!Playing&&Settings.Movement==MovementMode.Restricted&&!InsideRestriction(Position))Place(RestrictedCenter);
        }
        public void ChangeMode(MovementMode mode, double now)
        {
            bool entering=mode==MovementMode.Restricted&&Settings.Movement!=mode;
            if(entering&&!Settings.RestrictedAreaCreated){Anchor=Position;FitRestrictedArea();Settings.RestrictedAreaCreated=true;}
            Settings.Movement=mode;if(entering){Settings.DisplayRestrictedArea=true;SetRestrictedAreaVisible(true);}
            CancelRoute();EnsureInsideRestrictedArea();IdleUntil=Math.Max(IdleUntil,now+1);
        }
        public bool SetDestination(PointF target, string displayId)
        {
            var targetArea = Displays.Find(d => d.Id == displayId); if (targetArea == null) return false;
            target = Geometry.Clamp(target, targetArea.Allowed(FrameSize));
            if (!Playing && Settings.Movement == MovementMode.Restricted && !InsideRestriction(target)) return false;
            Destination=target;TargetDisplay=displayId;plannedCrossing=null;Crossing=null;
            if (displayId != CurrentDisplay)
            {
                var nextDisplay=DisplayCrossing.NextDisplay(Current,targetArea,Displays,FrameSize);
                plannedCrossing=DisplayCrossing.Plan(Current,nextDisplay,FrameSize,target);
                if (!Playing && Settings.Movement == MovementMode.Restricted &&
                    (!InsideRestriction(plannedCrossing.Exit)||!InsideRestriction(plannedCrossing.Entry)))
                { CancelRoute(); return false; }
            }
            return true;
        }
        internal bool BeginToyDeparture(){BeginPlay();PickDestination();return Destination.HasValue;}
        void PickDestination()
        {
            for (int attempt=0; attempt<150; attempt++)
            {
                var d = Displays[random.Next(Displays.Count)]; var r = d.Allowed(FrameSize);
                PointF p;
                if (Settings.Movement == MovementMode.Restricted)
                {
                    var allowed=RestrictedAllowed;
                    p = new PointF(allowed.Left+(float)random.NextDouble()*allowed.Width,allowed.Top+(float)random.NextDouble()*allowed.Height);
                    if (!r.Contains(p)) continue;
                }
                else p = new PointF(r.Left+(float)random.NextDouble()*r.Width, r.Top+(float)random.NextDouble()*r.Height);
                if (Geometry.Distance(p, Position)>3 && SetDestination(p,d.Id)) return;
            }
        }
        public void Tick(double now, float dt)
        {
            Walking = false; ActualSpeed = 0;
            if(settlingDrag&&Crossing!=null)
            {
                float step=Math.Max(0,Math.Min(.1f,dt))*5;
                Crossing.Progress=settleTarget==1?Math.Min(1,Crossing.Progress+step):Math.Max(0,Crossing.Progress-step);
                UpdateCrossingPosition();FaceDownIdle();
                if(Crossing.Progress==settleTarget)
                {Crossing=null;settlingDrag=false;Place(Position);EnsureInsideRestrictedArea();}
                return;
            }
            if(Hovered||Dragging||Shaking(now))FaceDownIdle();
            if (Displays.Count==0 || Dragging || Hovered || Paused || now<IdleUntil || Shaking(now) || (!Playing&&(Settings.Movement==MovementMode.Static||Settings.Speed==0))) return;
            if (!Destination.HasValue && !Playing) PickDestination();
            if (Playing&&!Destination.HasValue)return;
            if (!Destination.HasValue) { IdleUntil=now+2; return; }
            float speed=Playing&&Settings.Speed==0?50:Settings.Speed;
            if(Crossing!=null)
            {
                ActualSpeed=speed*2*Current.Scale;Walking=true;Facing=Crossing.Direction;LastMotion=new PointF((float)Math.Cos(Facing*Math.PI/4),(float)Math.Sin(Facing*Math.PI/4));
                Crossing.Progress=Math.Min(1,Crossing.Progress+ActualSpeed*Math.Min(.1f,dt)/Crossing.TravelLength);
                bool arriving=Crossing.Progress>=.5f;Position=arriving?Crossing.DestinationAnchor:Crossing.SourceAnchor;
                CurrentDisplay=arriving?Crossing.To.Id:Crossing.From.Id;
                if(Crossing.Progress>=1)
                {
                    Position=Crossing.Entry;CurrentDisplay=Crossing.To.Id;Crossing=null;plannedCrossing=null;
                    if(CurrentDisplay!=TargetDisplay){var final=Destination.Value;var id=TargetDisplay;SetDestination(final,id);}
                }
                return;
            }
            PointF target = plannedCrossing!=null?plannedCrossing.Exit:Destination.Value;
            float distance=Geometry.Distance(Position,target), scale=Current.Scale;
            if (distance <= scale)
            {
                Position=target;
                if(plannedCrossing!=null){Crossing=plannedCrossing;Walking=true;ActualSpeed=speed*2*scale;Facing=Crossing.Direction;LastMotion=new PointF((float)Math.Cos(Facing*Math.PI/4),(float)Math.Sin(Facing*Math.PI/4));return;}
                CancelRoute(); IdleUntil=now+10+random.NextDouble()*20; return;
            }
            LastMotion=new PointF(target.X-Position.X,target.Y-Position.Y);
            Facing=Geometry.Direction(LastMotion,Facing);
            ActualSpeed=speed*2*scale*(plannedCrossing!=null?1:distance<15*scale?.25f:distance<40*scale?.5f:1);
            Position=Geometry.Toward(Position,target,ActualSpeed*Math.Min(.1f,dt)); Walking=true;
        }
        public void Store()
        {
            Settings.X=Position.X; Settings.Y=Position.Y; Settings.AnchorX=Anchor.X; Settings.AnchorY=Anchor.Y; Settings.Facing=Facing;
        }
    }

    public static class Reactions
    {
        public const int Love=1,Hunger=8;
        public static readonly string[] Names = { "Music", "Love", "Question", "Anger", "Sad", "Fear", "Disgust", "Proud", "Hunger" };
        public static readonly int[] CodePoints = { 0x1F3B5,0x2665,0x2753,0x1F4A2,0x1F4A7,0x2757,0x1F300,0x1F3C6,0x1F37D };
        public static string Emoji(int index)
        {
            // U+2665 has text presentation by default; VS16 requests its emoji presentation.
            return char.ConvertFromUtf32(CodePoints[index])+(index==Love||index==Hunger?"\uFE0F":"");
        }
        static readonly int[][] Priorities = { new[]{1,1,1,3,2,3,2,2}, new[]{3,3,2,1,2,1,2,1}, new[]{2,2,1,3,1,2,1,3} };
        public static int Weight(Personality p,int index) { int priority=index<8?Priorities[(int)p][index]:2; return priority==1?6:priority==2?3:1; }
        public static int Choose(Personality p,int count,Random rng)
        {
            int total=0; for(int i=0;i<count;i++)total+=Weight(p,i);
            int pick=rng.Next(total); for(int i=0;i<count;i++){pick-=Weight(p,i);if(pick<0)return i;} return 0;
        }
        public static int Hover(Personality p) { return p==Personality.Sweet?0:p==Personality.Sassy?2:1; }
        public static int Pickup(Personality p) { return p==Personality.Sweet?Love:p==Personality.Sassy?5:4; }
        public static double Interval(Frequency f, Random rng)
        {
            if(f==Frequency.Off)return double.PositiveInfinity;
            return f==Frequency.Rarely?90+rng.NextDouble()*30:f==Frequency.Sometimes?30+rng.NextDouble()*30:15+rng.NextDouble()*15;
        }
    }
}
