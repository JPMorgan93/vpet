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

        [OnDeserializing]
        void InitializeDefaults(StreamingContext context)
        {
            Speed=50;Radius=250;DisplayRestrictedArea=true;Facing=2;
            X=Y=AnchorX=AnchorY=float.NaN;Frequency=Frequency.Sometimes;
            PetName="";NameDisplay=NameVisibility.Always;
        }

        public void Validate()
        {
            Speed = Math.Max(0, Math.Min(100, Speed)); Radius = Math.Max(30, Math.Min(1000, Radius));
            Facing = ((Facing % 8) + 8) % 8;
            if (!Enum.IsDefined(typeof(MovementMode), Movement)) Movement = MovementMode.FreeRoam;
            if (!Enum.IsDefined(typeof(LayerMode), Layer)) Layer = LayerMode.OverEverything;
            if (!Enum.IsDefined(typeof(Personality), Personality)) Personality = Personality.Sweet;
            if (!Enum.IsDefined(typeof(Frequency), Frequency)) Frequency = Frequency.Sometimes;
            PetName=CleanName(PetName);
            if (!Enum.IsDefined(typeof(NameVisibility), NameDisplay)) NameDisplay=NameVisibility.Always;
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
        public int NameHeadroom;
        public DisplayArea(string id, Rectangle work, float scale) { Id = id; Work = work; Scale = scale; }
        public Size PetSize(Size source) { return new Size((int)Math.Ceiling(source.Width * 2 * Scale), (int)Math.Ceiling(source.Height * 2 * Scale)); }
        public RectangleF Allowed(Size source,bool includeName=true)
        {
            Size size = PetSize(source);
            int headroom=includeName?NameHeadroom:0;
            // Position is the bottom-center ground anchor, with room for the full cell.
            return RectangleF.FromLTRB(Work.Left + size.Width / 2f, Work.Top + size.Height + headroom,
                Math.Max(Work.Left + size.Width / 2f, Work.Right - size.Width / 2f), Math.Max(Work.Top + size.Height + headroom, Work.Bottom));
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
        public double ShakeUntil, IdleUntil;
        public float ActualSpeed;
        public PointF? Destination { get; private set; }
        public string TargetDisplay { get; private set; }
        public string CurrentDisplay { get; private set; }
        readonly Random random;
        DisplayCrossing plannedCrossing;
        public DisplayCrossing Crossing { get; private set; }

        public PetModel(Preferences settings, Random rng)
        {
            Settings = settings; random = rng; Position = new PointF(settings.X, settings.Y);
            Anchor = new PointF(settings.AnchorX, settings.AnchorY); Facing = settings.Facing;
            if(settings.Movement==MovementMode.Restricted&&!float.IsNaN(Anchor.X)&&!float.IsNaN(Anchor.Y))settings.RestrictedAreaCreated=true;
        }
        public DisplayArea Current { get { return Displays.Find(d => d.Id == CurrentDisplay) ?? Displays[0]; } }
        public bool Shaking(double now) { return now < ShakeUntil; }
        public void SetDisplays(List<DisplayArea> displays)
        {
            if (displays.Count == 0) return;
            Displays = displays;
            UpdateNameHeadroom();
            if (float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsInfinity(Position.X) || float.IsInfinity(Position.Y))
                Position = new PointF(displays[0].Work.Left + displays[0].Work.Width * .6f, displays[0].Work.Top + displays[0].Work.Height * .7f);
            Place(Position);
            if (float.IsNaN(Anchor.X) || float.IsNaN(Anchor.Y) || !displays.Exists(d => d.Allowed(FrameSize).Contains(Anchor))) Anchor = Position;
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
        public void UpdateNameHeadroom()
        {
            foreach(var d in Displays)d.NameHeadroom=Settings.HasName?Math.Min((int)Math.Ceiling(62*d.Scale)+(int)Math.Ceiling(26*d.Scale)+2*Math.Max(1,(int)Math.Ceiling(3*d.Scale)),Math.Max(0,d.Work.Height-d.PetSize(FrameSize).Height)):0;
        }
        public void CancelRoute() { if(Crossing!=null)Place(Position);Destination = null; TargetDisplay = null; plannedCrossing=null;Crossing=null; Walking = false; }
        public void Release(double now)
        {
            Dragging=false;Place(Position);CancelRoute();EnsureInsideRestrictedArea();FaceDownIdle();
            ShakeUntil=0;IdleUntil=now+5;
        }
        public void FaceDownIdle(){Facing=2;Walking=false;ActualSpeed=0;}
        public int Click(double now)
        {
            FaceDownIdle();ShakeUntil=now+.5;return Reactions.Love;
        }
        public void MoveRestrictedArea(PointF center)
        {
            PointF nearest=center;float distance=float.MaxValue;
            foreach(var display in Displays)
            {var candidate=Geometry.Clamp(center,display.Allowed(FrameSize));float d=Geometry.Distance(center,candidate);if(d<distance){distance=d;nearest=candidate;}}
            Anchor=nearest;Settings.RestrictedAreaCreated=true;CancelRoute();EnsureInsideRestrictedArea();
        }
        public void SetRadius(int radius)
        {
            Settings.Radius=Math.Max(30,Math.Min(1000,radius));CancelRoute();EnsureInsideRestrictedArea();
        }
        public void EnsureInsideRestrictedArea()
        {
            if(Settings.Movement==MovementMode.Restricted&&Geometry.Distance(Position,Anchor)>Settings.Radius)Place(Anchor);
        }
        public void ChangeMode(MovementMode mode, double now)
        {
            bool entering=mode==MovementMode.Restricted&&Settings.Movement!=mode;
            if(entering&&!Settings.RestrictedAreaCreated){Anchor=Position;Settings.RestrictedAreaCreated=true;}
            Settings.Movement=mode;if(entering)Settings.DisplayRestrictedArea=true;
            CancelRoute();EnsureInsideRestrictedArea();IdleUntil=Math.Max(IdleUntil,now+1);
        }
        public bool SetDestination(PointF target, string displayId)
        {
            var targetArea = Displays.Find(d => d.Id == displayId); if (targetArea == null) return false;
            target = Geometry.Clamp(target, targetArea.Allowed(FrameSize));
            if (Settings.Movement == MovementMode.Restricted && Geometry.Distance(target, Anchor) > Settings.Radius) return false;
            Destination=target;TargetDisplay=displayId;plannedCrossing=null;Crossing=null;
            if (displayId != CurrentDisplay)
            {
                var nextDisplay=DisplayCrossing.NextDisplay(Current,targetArea,Displays,FrameSize);
                plannedCrossing=DisplayCrossing.Plan(Current,nextDisplay,FrameSize,target);
                if (Settings.Movement == MovementMode.Restricted &&
                    !plannedCrossing.FitsCircle(Anchor,Settings.Radius))
                { CancelRoute(); return false; }
            }
            return true;
        }
        void PickDestination()
        {
            for (int attempt=0; attempt<150; attempt++)
            {
                var d = Displays[random.Next(Displays.Count)]; var r = d.Allowed(FrameSize);
                PointF p;
                if (Settings.Movement == MovementMode.Restricted)
                {
                    double angle = random.NextDouble()*Math.PI*2, radius = Math.Sqrt(random.NextDouble())*Settings.Radius;
                    p = new PointF(Anchor.X+(float)(Math.Cos(angle)*radius), Anchor.Y+(float)(Math.Sin(angle)*radius));
                    if (!r.Contains(p)) continue;
                }
                else p = new PointF(r.Left+(float)random.NextDouble()*r.Width, r.Top+(float)random.NextDouble()*r.Height);
                if (Geometry.Distance(p, Position)>3 && SetDestination(p,d.Id)) return;
            }
        }
        public void Tick(double now, float dt)
        {
            Walking = false; ActualSpeed = 0;
            if(Hovered||Dragging||Shaking(now))FaceDownIdle();
            if (Displays.Count==0 || Dragging || Hovered || Paused || now<IdleUntil || Shaking(now) || Settings.Movement==MovementMode.Static||Settings.Speed==0) return;
            if (!Destination.HasValue) PickDestination();
            if (!Destination.HasValue) { IdleUntil=now+2; return; }
            if(Crossing!=null)
            {
                ActualSpeed=Settings.Speed*2*Current.Scale;Walking=true;Facing=Crossing.Direction;
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
                if(plannedCrossing!=null){Crossing=plannedCrossing;Walking=true;ActualSpeed=Settings.Speed*2*scale;Facing=Crossing.Direction;return;}
                CancelRoute(); IdleUntil=now+10+random.NextDouble()*20; return;
            }
            Facing=Geometry.Direction(new PointF(target.X-Position.X,target.Y-Position.Y),Facing);
            ActualSpeed=Settings.Speed*2*scale*(plannedCrossing!=null?1:distance<15*scale?.25f:distance<40*scale?.5f:1);
            Position=Geometry.Toward(Position,target,ActualSpeed*Math.Min(.1f,dt)); Walking=true;
        }
        public void Store()
        {
            Settings.X=Position.X; Settings.Y=Position.Y; Settings.AnchorX=Anchor.X; Settings.AnchorY=Anchor.Y; Settings.Facing=Facing;
        }
    }

    public static class Reactions
    {
        public const int Love=1;
        public static readonly string[] Names = { "Music", "Love", "Question", "Anger", "Sad", "Fear", "Disgust", "Proud" };
        public static readonly int[] CodePoints = { 0x1F3B5,0x2665,0x2753,0x1F4A2,0x1F4A7,0x2757,0x1F300,0x1F3C6 };
        public static string Emoji(int index)
        {
            // U+2665 has text presentation by default; VS16 requests its emoji presentation.
            return char.ConvertFromUtf32(CodePoints[index])+(index==1?"\uFE0F":"");
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
            return f==Frequency.Rarely?180+rng.NextDouble()*120:f==Frequency.Sometimes?60+rng.NextDouble()*60:30+rng.NextDouble()*30;
        }
    }
}
