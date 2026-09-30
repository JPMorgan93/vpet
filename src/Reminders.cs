using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Vpet
{
    internal enum ReminderKind { OneTime, Recurring }
    internal enum ReminderFrequency { Every, EveryOther, First, Second, Third, Fourth }

    [DataContract]
    internal sealed class ReminderDay
    {
        [DataMember] public DayOfWeek Day;
        [DataMember] public ReminderFrequency Frequency;
        [DataMember] public string AnchorDate;
        public ReminderDay Copy(){return new ReminderDay{Day=Day,Frequency=Frequency,AnchorDate=AnchorDate};}
        public bool Matches(DateTime date)
        {
            if(date.DayOfWeek!=Day)return false;
            if(Frequency==ReminderFrequency.Every)return true;
            if(Frequency==ReminderFrequency.EveryOther)
            {
                var anchor=Reminder.ReadLocal(AnchorDate).Date;
                var week=anchor.AddDays(-(int)anchor.DayOfWeek);
                int weeks=(int)(date.Date-week).TotalDays/7;
                return date.Date>=week&&weeks%2==0;
            }
            return (date.Day-1)/7==(int)Frequency-2;
        }
        public string Description
        {
            get
            {
                if(Frequency==ReminderFrequency.Every)return "Every "+Day;
                if(Frequency==ReminderFrequency.EveryOther)return "Every other "+Day;
                return Frequency+" "+Day+" of the month";
            }
        }
    }

    [DataContract]
    internal sealed class Reminder
    {
        [DataMember] public string Id=Guid.NewGuid().ToString("N");
        [DataMember] public ReminderKind Kind;
        // Store wall-clock dates as ISO text so changing PC time zones keeps the chosen local time.
        [DataMember] public string Date;
        [DataMember] public int MinuteOfDay;
        [DataMember] public List<ReminderDay> Days=new List<ReminderDay>();
        [DataMember] public string Message="";
        [DataMember] public bool Active=true;
        [DataMember] public string ActiveFrom;
        [DataMember] public string LastFired;
        [DataMember] public string Pending;
        internal static readonly CultureInfo English=CultureInfo.GetCultureInfo("en-US");
        internal static string Local(DateTime value){return value.ToString("yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture);}
        internal static DateTime ReadLocal(string value)
        {return DateTime.SpecifyKind(DateTime.ParseExact(value,"yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture),DateTimeKind.Unspecified);}
        public static Reminder New(DateTime now)
        {
            var next=now.AddMinutes(5);
            return new Reminder{Date=Local(next.Date),MinuteOfDay=next.Hour*60+next.Minute,ActiveFrom=Local(now)};
        }
        public Reminder Copy()
        {return new Reminder{Id=Id,Kind=Kind,Date=Date,MinuteOfDay=MinuteOfDay,Days=Days.Select(d=>d.Copy()).ToList(),Message=Message,Active=Active,ActiveFrom=ActiveFrom,LastFired=LastFired,Pending=Pending};}
        public DateTime? LatestDue(DateTime now)
        {
            if(!Active)return null;
            var start=ReadLocal(ActiveFrom);var last=string.IsNullOrEmpty(LastFired)?DateTime.MinValue:ReadLocal(LastFired);
            if(Kind==ReminderKind.OneTime)
            {
                var due=ReadLocal(Date).Date.AddMinutes(MinuteOfDay);
                return due<=now&&due>last?(DateTime?)due:null;
            }
            // Every supported recurrence has an occurrence within 35 days. Catch up only the latest
            // missed occurrence, including after sleep/restart, rather than flooding the user.
            for(int back=0;back<=35;back++)
            {
                var day=now.Date.AddDays(-back);var due=day.AddMinutes(MinuteOfDay);
                if(due>now)continue;if(due<start||due<=last)break;
                if(Days.Any(rule=>rule.Matches(day)))return due;
            }
            return null;
        }
        public string Schedule
        {
            get
            {
                string time=DateTime.Today.AddMinutes(MinuteOfDay).ToString("h:mm tt",English);
                return Kind==ReminderKind.OneTime?ReadLocal(Date).ToString("ddd, MMM d, yyyy",English)+" at "+time:
                    string.Join("; ",Days.OrderBy(d=>(int)d.Day).Select(d=>d.Description))+" at "+time;
            }
        }
        internal void Validate()
        {
            Guid id;if(!Guid.TryParse(Id,out id))throw new InvalidDataException("Invalid reminder identifier.");
            if(!Enum.IsDefined(typeof(ReminderKind),Kind)||MinuteOfDay<0||MinuteOfDay>=1440)throw new InvalidDataException("Choose a valid reminder type and time.");
            if(string.IsNullOrWhiteSpace(Message)||Message.Length>200)throw new InvalidDataException("Enter a reminder message from 1 to 200 characters.");
            ReadLocal(Date);ReadLocal(ActiveFrom);
            if(!string.IsNullOrEmpty(LastFired))ReadLocal(LastFired);if(!string.IsNullOrEmpty(Pending))ReadLocal(Pending);
            if(Days==null||Days.Count>7||Days.Any(d=>d==null)||Days.Select(d=>d.Day).Distinct().Count()!=Days.Count)throw new InvalidDataException("Choose each recurring day once.");
            foreach(var rule in Days)
            {if(!Enum.IsDefined(typeof(DayOfWeek),rule.Day)||!Enum.IsDefined(typeof(ReminderFrequency),rule.Frequency))throw new InvalidDataException("Invalid recurring day or frequency.");ReadLocal(rule.AnchorDate);}
            if(Kind==ReminderKind.Recurring&&Days.Count==0)throw new InvalidDataException("Select at least one recurring day.");
        }
    }

    [DataContract]
    internal sealed class ReminderData
    {
        [DataMember] public int Version=1;
        [DataMember] public List<Reminder> Items=new List<Reminder>();
    }

    internal sealed class ReminderStore
    {
        readonly string path;
        ReminderData data=new ReminderData();
        public string LoadError {get;private set;}
        public event Action Changed;
        public IEnumerable<Reminder> Items {get{return data.Items.Select(r=>r.Copy());}}
        public Reminder Pending {get{return data.Items.Where(r=>!string.IsNullOrEmpty(r.Pending)).OrderBy(r=>r.Pending,StringComparer.Ordinal).Select(r=>r.Copy()).FirstOrDefault();}}
        public ReminderStore(string path)
        {
            this.path=path;
            if(!File.Exists(path))return;
            try
            {
                using(var stream=File.OpenRead(path))data=(ReminderData)new DataContractJsonSerializer(typeof(ReminderData)).ReadObject(stream);
                if(data==null||data.Version!=1||data.Items==null)throw new InvalidDataException("Unsupported reminder file.");
                foreach(var item in data.Items){if(item==null)throw new InvalidDataException("Invalid reminder.");item.Validate();}
                if(data.Items.Select(r=>r.Id).Distinct().Count()!=data.Items.Count)throw new InvalidDataException("Duplicate reminder identifiers.");
            }
            catch(Exception ex){data=new ReminderData();LoadError="Reminders could not be loaded. Your file has been kept at "+path+". "+ex.Message;}
        }
        void Commit(ReminderData next)
        {
            if(LoadError!=null)throw new IOException(LoadError);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temporary=path+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
                {new DataContractJsonSerializer(typeof(ReminderData)).WriteObject(stream,next);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
            data=next;if(Changed!=null)Changed();
        }
        ReminderData Copy(){return new ReminderData{Items=data.Items.Select(r=>r.Copy()).ToList()};}
        public void Save(Reminder item,DateTime now)
        {
            item=item.Copy();item.Validate();
            if(item.Active&&item.Kind==ReminderKind.OneTime&&Reminder.ReadLocal(item.Date).Date.AddMinutes(item.MinuteOfDay)<=now)
                throw new InvalidDataException("Choose a future date and time for an active one-time reminder.");
            item.ActiveFrom=Reminder.Local(now);item.LastFired=item.Pending=null;
            var next=Copy();int index=next.Items.FindIndex(r=>r.Id==item.Id);
            if(index<0)next.Items.Add(item);else next.Items[index]=item;Commit(next);
        }
        public void Delete(string id){var next=Copy();if(next.Items.RemoveAll(r=>r.Id==id)>0)Commit(next);}
        public void Dismiss(string id){var next=Copy();var item=next.Items.Find(r=>r.Id==id);if(item==null||item.Pending==null)return;item.Pending=null;Commit(next);}
        public void Poll(DateTime now)
        {
            if(LoadError!=null)return;var next=Copy();bool changed=false;
            foreach(var item in next.Items)
            {
                var due=item.LatestDue(now);if(!due.HasValue)continue;
                item.LastFired=Reminder.Local(due.Value);if(item.Pending==null)item.Pending=item.LastFired;
                if(item.Kind==ReminderKind.OneTime)item.Active=false;changed=true;
            }
            if(changed)Commit(next);
        }
    }
}
