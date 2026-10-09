using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Vpet
{
    public enum ArcadeDifficulty { Easy, Normal, Hard }
    public enum DanceBackground { Dynamic, Static, Off }
    internal enum ArcadeGame { Lobby, Dance, Simon, Brick }
    internal enum ArcadeLane { Up, Down, Left, Right }
    internal enum DanceState { Ready, Countdown, Running, Failed, Success, Stopped }
    internal enum SimonState { Ready, Countdown, Showing, Replaying, Finished, Waiting, Stopped }
    [DataContract] public sealed class ArcadePreferences
    {
        [DataMember] public long[] DanceHigh=new long[3];
        [DataMember] public long[] SimonHigh=new long[3];
        internal const int DefaultVolume=25;
        [DataMember] public int Volume=DefaultVolume;
        [DataMember] public bool ArrowKeys;
        [DataMember] public bool Practice;
        [DataMember] public DanceBackground Background=DanceBackground.Dynamic;
        [DataMember] public bool SimonBackground=true;
        [DataMember] public bool BrickPowerUps=true;
        [OnDeserializing] void Defaults(StreamingContext context){DanceHigh=new long[3];SimonHigh=new long[3];Volume=DefaultVolume;SimonBackground=true;BrickPowerUps=true;}
        public void Validate()
        {
            DanceHigh=Clean(DanceHigh);SimonHigh=Clean(SimonHigh);Volume=Math.Max(0,Math.Min(100,Volume));
            if(!Enum.IsDefined(typeof(DanceBackground),Background))Background=DanceBackground.Dynamic;
        }
        static long[] Clean(long[] source){var result=new long[3];if(source!=null)for(int i=0;i<Math.Min(3,source.Length);i++)result[i]=Math.Max(0,Math.Min(1000000000000L,source[i]));return result;}
        internal long High(ArcadeGame game,ArcadeDifficulty difficulty){return game==ArcadeGame.Dance?DanceHigh[(int)difficulty]:game==ArcadeGame.Simon?SimonHigh[(int)difficulty]:0;}
        internal bool Record(ArcadeGame game,ArcadeDifficulty difficulty,long score)
        {if(game!=ArcadeGame.Dance&&game!=ArcadeGame.Simon)return false;var values=game==ArcadeGame.Dance?DanceHigh:SimonHigh;int index=(int)difficulty;if(score<=values[index])return false;values[index]=score;return true;}
    }
    internal sealed class DanceTarget
    {
        public readonly ArcadeLane Lane;public readonly double HitTime;
        public bool Resolved;
        public double ScoredAt=double.NaN,ScoredDistance;
        public DanceTarget(ArcadeLane lane,double hitTime){Lane=lane;HitTime=hitTime;}
    }
    internal sealed class DanceGame
    {
        public const double SquareDistance=136,SquareSize=58,PulseDuration=.22;
        public readonly ArcadeDifficulty Difficulty;
        public readonly bool Practice;
        public readonly double Duration;
        public readonly List<DanceTarget> Chart;
        public DanceState State {get;private set;}
        public double Elapsed {get;private set;}
        public int Misses {get;private set;}
        public int Lives {get{return 3-(int)Difficulty;}}
        public long Banked {get;private set;}
        public long Pending {get;private set;}
        public long StreakScore {get;private set;}
        public int ExcellentRun {get;private set;}
        int bonusTenths=10;
        public double Multiplier {get{return bonusTenths/10.0;}}
        public bool Streak {get;private set;}
        public string Feedback {get;private set;}
        public double FeedbackAt {get;private set;}
        public double FinishedAt {get;private set;}
        double countdownAt,runningAt;
        public event Action MusicStarted;
        public DanceGame(ArcadeDifficulty difficulty,double duration,IEnumerable<DanceTarget> chart=null,bool practice=false)
        {
            Difficulty=difficulty;Duration=duration;Practice=practice;
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
        public static double SpawnDistance(ArcadeLane lane){return 540;}
        public double Distance(DanceTarget target){return SquareDistance+(target.HitTime-Elapsed)*Speed;}
        public IEnumerable<DanceTarget> VisibleTargets {get{return Chart.Where(t=>!t.Resolved&&Distance(t)<=SpawnDistance(t.Lane));}}
        public IEnumerable<DanceTarget> PulsingTargets(double now){return Chart.Where(t=>!double.IsNaN(t.ScoredAt)&&now>=t.ScoredAt&&now-t.ScoredAt<PulseDuration);}
        internal static double Overlap(double distance){return Math.Max(0,1-Math.Abs(distance-SquareDistance)/SquareSize);}
        internal static int Points(double overlap){return overlap+1e-9>=.9?30:overlap+1e-9>=.5?20:overlap+1e-9>=.01?10:0;}
        public int Countdown(double now){return Math.Max(1,(int)Math.Ceiling(3-(now-countdownAt)));}
        public void Start(double now)
        {
            foreach(var target in Chart){target.Resolved=false;target.ScoredAt=double.NaN;}
            State=DanceState.Countdown;countdownAt=now;Elapsed=0;Misses=0;Banked=Pending=StreakScore=0;ExcellentRun=0;bonusTenths=10;Streak=false;Feedback="";
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
            target.Resolved=true;target.ScoredAt=now;target.ScoredDistance=Distance(target);int points=Points(Overlap(target.ScoredDistance));
            Feedback=points==30?"Excellent!":points==20?"Great!":"Good!";FeedbackAt=now;
            if(Practice)return;
            if(points==30)
            {
                ExcellentRun++;if(Streak)bonusTenths++;else if(ExcellentRun>=3)Streak=true;
                if(Streak)StreakScore+=points;else Pending+=points;
            }
            else {CompleteStreak();Pending+=points;}
        }
        void ResetStreak(){bonusTenths=10;Streak=false;ExcellentRun=0;StreakScore=0;}
        void CompleteStreak(){Pending+=(StreakScore*bonusTenths+5)/10;ResetStreak();}
        void ClearTargets(){foreach(var target in Chart){target.Resolved=true;target.ScoredAt=double.NaN;}}
        public void Stop(double now)
        {
            if(State!=DanceState.Countdown&&State!=DanceState.Running)return;
            State=DanceState.Stopped;FinishedAt=FeedbackAt=now;Feedback="Stopped";Banked=Pending=0;ResetStreak();ClearTargets();
        }
        void CashIn(){CompleteStreak();Banked+=Pending;Pending=0;}
        void Miss(double now)
        {
            Misses++;Feedback="Miss!";FeedbackAt=now;if(Practice)return;CashIn();
            if(Misses>=Lives){State=DanceState.Failed;Banked=Pending=0;FinishedAt=now;Feedback="Failed";ClearTargets();}
        }
    }
    internal sealed class SimonGame
    {
        internal const double InputHighlightDuration=.18,RestDuration=1;
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
        int lastTone=-1;
        public event Action<ArcadeLane> TonePlayed;
        public double ShowStep {get{return new[]{.9,.6,.4}[(int)Difficulty];}}
        internal int InitialLength {get{return new[]{1,3,5}[(int)Difficulty];}}
        public double SecondsLeft(double now){return Math.Max(0,5-(now-phaseAt));}
        public SimonGame(ArcadeDifficulty difficulty,Random random){Difficulty=difficulty;this.random=random;}
        public void Start(double now){Sequence.Clear();State=SimonState.Countdown;phaseAt=now;InputIndex=0;Pending=Banked=0;Feedback="";inputFlash=null;lastTone=-1;}
        public int Countdown(double now){return Math.Max(1,(int)Math.Ceiling(3-(now-phaseAt)));}
        void NewRound(double now){int length=Sequence.Count==0?InitialLength:Sequence.Count+1;while(Sequence.Count<length)Sequence.Add((ArcadeLane)random.Next(4));InputIndex=0;State=SimonState.Showing;phaseAt=now;lastTone=-1;inputFlash=null;}
        public ArcadeLane? Lit(double now)
        {
            if(State==SimonState.Showing)
            {double elapsed=now-phaseAt;if(elapsed<0)return null;int index=(int)(elapsed/ShowStep);return index<Sequence.Count&&elapsed-index*ShowStep<ShowStep*.7?(ArcadeLane?)Sequence[index]:null;}
            return now<inputFlashUntil?inputFlash:null;
        }
        public void Update(double now)
        {
            if(State==SimonState.Countdown&&now-phaseAt+1e-9>=3)NewRound(now);
            else if(State==SimonState.Waiting&&now-phaseAt+1e-9>=RestDuration)NewRound(now);
            else if(State==SimonState.Showing&&now-phaseAt+1e-9>=Sequence.Count*ShowStep){State=SimonState.Replaying;phaseAt=now;}
            else if(State==SimonState.Replaying&&now-phaseAt+1e-9>=5)Finish(now);
            if(State==SimonState.Showing&&Lit(now).HasValue)
            {int index=(int)((now-phaseAt)/ShowStep);if(index!=lastTone){lastTone=index;if(TonePlayed!=null)TonePlayed(Sequence[index]);}}
        }
        public void Press(ArcadeLane lane,double now)
        {
            Update(now);if(State!=SimonState.Replaying)return;
            inputFlash=lane;inputFlashUntil=now+InputHighlightDuration;if(TonePlayed!=null)TonePlayed(lane);
            if(lane!=Sequence[InputIndex]){Finish(now);return;}
            InputIndex++;
            if(InputIndex==Sequence.Count){Pending+=50;Feedback="Correct!";FeedbackAt=now;State=SimonState.Waiting;phaseAt=inputFlashUntil;}
        }
        public void Stop(double now)
        {
            if(State==SimonState.Ready||State==SimonState.Finished||State==SimonState.Stopped)return;
            State=SimonState.Stopped;Banked=Pending=0;Feedback="Stopped";FeedbackAt=now;inputFlash=null;
        }
        void Finish(double now){State=SimonState.Finished;Banked=Pending;Pending=0;Feedback="Game Over";FeedbackAt=now;inputFlash=null;}
    }
}
