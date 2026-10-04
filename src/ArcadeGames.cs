using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Vpet
{
    public enum ArcadeDifficulty { Easy, Normal, Hard }
    internal enum ArcadeGame { Lobby, Dance, Simon }
    internal enum ArcadeLane { Up, Down, Left, Right }
    internal enum DanceState { Ready, Countdown, Running, Failed, Success }
    internal enum SimonState { Ready, Countdown, Showing, Replaying, Finished }
    [DataContract] public sealed class ArcadePreferences
    {
        [DataMember] public long[] DanceHigh=new long[3];
        [DataMember] public long[] SimonHigh=new long[3];
        [DataMember] public int Volume=70;
        [DataMember] public bool ArrowKeys;
        [OnDeserializing] void Defaults(StreamingContext context){DanceHigh=new long[3];SimonHigh=new long[3];Volume=70;}
        public void Validate()
        {
            DanceHigh=Clean(DanceHigh);SimonHigh=Clean(SimonHigh);Volume=Math.Max(0,Math.Min(100,Volume));
        }
        static long[] Clean(long[] source){var result=new long[3];if(source!=null)for(int i=0;i<Math.Min(3,source.Length);i++)result[i]=Math.Max(0,Math.Min(1000000000000L,source[i]));return result;}
        internal long High(ArcadeGame game,ArcadeDifficulty difficulty){return (game==ArcadeGame.Dance?DanceHigh:SimonHigh)[(int)difficulty];}
        internal bool Record(ArcadeGame game,ArcadeDifficulty difficulty,long score)
        {var values=game==ArcadeGame.Dance?DanceHigh:SimonHigh;int index=(int)difficulty;if(score<=values[index])return false;values[index]=score;return true;}
    }
    internal sealed class DanceTarget
    {
        public readonly ArcadeLane Lane;public readonly double HitTime;
        public bool Resolved;
        public DanceTarget(ArcadeLane lane,double hitTime){Lane=lane;HitTime=hitTime;}
    }
    internal sealed class DanceGame
    {
        public const double SquareDistance=136,SquareSize=58;
        public readonly ArcadeDifficulty Difficulty;
        public readonly double Duration;
        public readonly List<DanceTarget> Chart;
        public DanceState State {get;private set;}
        public double Elapsed {get;private set;}
        public int Misses {get;private set;}
        public int Lives {get{return 3-(int)Difficulty;}}
        public long Banked {get;private set;}
        public long Pending {get;private set;}
        public int ExcellentRun {get;private set;}
        int bonusTenths=10;
        public double Multiplier {get{return bonusTenths/10.0;}}
        public bool Streak {get;private set;}
        public string Feedback {get;private set;}
        public double FeedbackAt {get;private set;}
        public double FinishedAt {get;private set;}
        double countdownAt,runningAt;
        public event Action MusicStarted;
        public DanceGame(ArcadeDifficulty difficulty,double duration,IEnumerable<DanceTarget> chart=null)
        {
            Difficulty=difficulty;Duration=duration;
            if(double.IsNaN(duration)||double.IsInfinity(duration)||duration<=0)throw new ArgumentException("Song duration must be positive.");
            Chart=chart==null?MakeChart(duration,difficulty):chart.Select(t=>new DanceTarget(t.Lane,t.HitTime)).OrderBy(t=>t.HitTime).ToList();
        }
        static List<DanceTarget> MakeChart(double duration,ArcadeDifficulty difficulty)
        {
            var result=new List<DanceTarget>();var random=new Random(7921+(int)difficulty);double gap=new[]{.95,.75,.6}[(int)difficulty];int last=-1;
            for(double hit=3.2;hit<duration-.85;hit+=gap){int lane=random.Next(4);if(lane==last)lane=(lane+1+random.Next(3))%4;last=lane;result.Add(new DanceTarget((ArcadeLane)lane,hit));}
            return result;
        }
        public double Speed {get{return 160*new[]{1.0,1.5,2.0}[(int)Difficulty];}}
        public static double SpawnDistance(ArcadeLane lane){return lane==ArcadeLane.Left||lane==ArcadeLane.Right?540:350;}
        public double Distance(DanceTarget target){return SquareDistance+(target.HitTime-Elapsed)*Speed;}
        public IEnumerable<DanceTarget> VisibleTargets {get{return Chart.Where(t=>!t.Resolved&&Distance(t)<=SpawnDistance(t.Lane));}}
        internal static double Overlap(double distance){return Math.Max(0,1-Math.Abs(distance-SquareDistance)/SquareSize);}
        internal static int Points(double overlap){return overlap+1e-9>=.9?30:overlap+1e-9>=.5?20:overlap+1e-9>=.01?10:0;}
        public int Countdown(double now){return Math.Max(1,(int)Math.Ceiling(3-(now-countdownAt)));}
        public void Start(double now)
        {
            foreach(var target in Chart)target.Resolved=false;
            State=DanceState.Countdown;countdownAt=now;Elapsed=0;Misses=0;Banked=Pending=0;ExcellentRun=0;bonusTenths=10;Streak=false;Feedback="";
        }
        public void Update(double now,double? musicPosition=null)
        {
            if(State==DanceState.Countdown&&now-countdownAt>=3){State=DanceState.Running;runningAt=now;if(MusicStarted!=null)MusicStarted();}
            if(State!=DanceState.Running)return;
            Elapsed=Math.Max(Elapsed,Math.Max(0,musicPosition??now-runningAt));
            foreach(var target in Chart.Where(t=>!t.Resolved&&Distance(t)<=50).ToArray())
            {target.Resolved=true;Miss(now);if(State!=DanceState.Running)return;}
            if(Elapsed>=Duration){CashIn();State=DanceState.Success;FinishedAt=now;Feedback="Success";FeedbackAt=now;}
        }
        public void Press(ArcadeLane lane,double now)
        {
            if(State!=DanceState.Running)return;
            var target=VisibleTargets.Where(t=>t.Lane==lane&&Points(Overlap(Distance(t)))>0).OrderBy(t=>Math.Abs(Distance(t)-SquareDistance)).FirstOrDefault();
            if(target==null){Miss(now);return;}
            target.Resolved=true;int points=Points(Overlap(Distance(target)));Pending+=points;
            Feedback=points==30?"Excellent!":points==20?"Great!":"Good!";FeedbackAt=now;
            if(points==30){ExcellentRun++;if(Streak)bonusTenths++;else if(ExcellentRun>=5)Streak=true;}else ExcellentRun=0;
        }
        void CashIn(){Banked+=(Pending*bonusTenths+5)/10;Pending=0;bonusTenths=10;Streak=false;ExcellentRun=0;}
        void Miss(double now)
        {
            CashIn();Misses++;Feedback="Miss!";FeedbackAt=now;
            if(Misses>=Lives){State=DanceState.Failed;Banked=Pending=0;FinishedAt=now;Feedback="Failed";foreach(var target in Chart)target.Resolved=true;}
        }
    }
    internal sealed class SimonGame
    {
        readonly Random random;
        public readonly ArcadeDifficulty Difficulty;
        public readonly List<ArcadeLane> Sequence=new List<ArcadeLane>();
        public SimonState State {get;private set;}
        public int InputIndex {get;private set;}
        public long Pending {get;private set;}
        public long Banked {get;private set;}
        public string Feedback {get;private set;}
        public double FeedbackAt {get;private set;}
        double phaseAt;ArcadeLane? inputFlash;double inputFlashUntil;
        public double ShowStep {get{return new[]{.9,.6,.4}[(int)Difficulty];}}
        public double SecondsLeft(double now){return Math.Max(0,5-(now-phaseAt));}
        public SimonGame(ArcadeDifficulty difficulty,Random random){Difficulty=difficulty;this.random=random;}
        public void Start(double now){Sequence.Clear();State=SimonState.Countdown;phaseAt=now;InputIndex=0;Pending=Banked=0;Feedback="";inputFlash=null;}
        public int Countdown(double now){return Math.Max(1,(int)Math.Ceiling(3-(now-phaseAt)));}
        void NewRound(double now){Sequence.Add((ArcadeLane)random.Next(4));InputIndex=0;State=SimonState.Showing;phaseAt=now;}
        public ArcadeLane? Lit(double now)
        {
            if(State==SimonState.Showing)
            {double elapsed=now-phaseAt-.5;if(elapsed<0)return null;int index=(int)(elapsed/ShowStep);return index<Sequence.Count&&elapsed-index*ShowStep<ShowStep*.7?(ArcadeLane?)Sequence[index]:null;}
            return now<inputFlashUntil?inputFlash:null;
        }
        public void Update(double now)
        {
            if(State==SimonState.Countdown&&now-phaseAt+1e-9>=3)NewRound(now);
            else if(State==SimonState.Showing&&now-phaseAt+1e-9>=.5+Sequence.Count*ShowStep){State=SimonState.Replaying;phaseAt=now;}
            else if(State==SimonState.Replaying&&now-phaseAt+1e-9>=5)Finish(now);
        }
        public void Press(ArcadeLane lane,double now)
        {
            Update(now);if(State!=SimonState.Replaying)return;
            inputFlash=lane;inputFlashUntil=now+.18;
            if(lane!=Sequence[InputIndex]){Finish(now);return;}
            InputIndex++;
            if(InputIndex==Sequence.Count){Pending+=50;Feedback="Correct!";FeedbackAt=now;NewRound(now);}
        }
        void Finish(double now){State=SimonState.Finished;Banked=Pending;Pending=0;Feedback="Game Over";FeedbackAt=now;inputFlash=null;}
    }
}
