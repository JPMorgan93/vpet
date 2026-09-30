using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Vpet
{
    internal static partial class Tests
    {
        static Reminder RecurringReminder(DateTime now,DayOfWeek day,ReminderFrequency frequency,int minutes=540)
        {var r=Reminder.New(now);r.Kind=ReminderKind.Recurring;r.MinuteOfDay=minutes;r.Message="Test https://example.com";r.Days.Add(new ReminderDay{Day=day,Frequency=frequency,AnchorDate=Reminder.Local(now.Date)});return r;}
        static void ReminderSchedules()
        {
            var now=new DateTime(2026,9,29,8,0,0);string path=Path.Combine(artifacts,"reminders-"+Guid.NewGuid().ToString("N"),"reminders.json");var store=new ReminderStore(path);
            var once=Reminder.New(now);once.Message="Meeting https://example.com";store.Save(once,now);store.Poll(now.AddMinutes(4));Check(store.Pending==null,"One-time reminder is not early");
            store.Poll(now.AddMinutes(5));Check(store.Pending.Id==once.Id&&!store.Items.Single().Active,"One-time reminder fires and becomes completed");
            var reloaded=new ReminderStore(path);Check(reloaded.Pending.Id==once.Id,"Undismissed message survives restart");reloaded.Dismiss(once.Id);reloaded.Poll(now.AddDays(20));Check(reloaded.Pending==null,"Dismissed one-time reminder never repeats");
            var tomorrow=Reminder.New(now);tomorrow.Date=Reminder.Local(now.AddDays(1));tomorrow.Message=new string('x',200);reloaded.Save(tomorrow,now);reloaded.Poll(now.AddDays(5));Check(reloaded.Pending.Id==tomorrow.Id,"Missed one-time reminder appears after sleep or restart");reloaded.Delete(tomorrow.Id);Check(reloaded.Pending==null,"Deleting removes pending notification");
            var invalid=Reminder.New(now);invalid.Message=new string('x',201);Reject(()=>reloaded.Save(invalid,now),"Messages over 200 characters are rejected");invalid.Message=" ";Reject(()=>reloaded.Save(invalid,now),"Blank messages are rejected");
            invalid.Message="Past";invalid.Date=Reminder.Local(now.AddDays(-1));Reject(()=>reloaded.Save(invalid,now),"Active one-time reminders need a future date");invalid.Active=false;reloaded.Save(invalid,now);reloaded.Poll(now.AddYears(1));Check(reloaded.Pending==null,"Inactive reminders never fire");
            int[][] weekly={new[]{6,13,20,27},new[]{7,14,21,28},new[]{1,8,15,22,29},new[]{2,9,16,23,30},new[]{3,10,17,24},new[]{4,11,18,25},new[]{5,12,19,26}};
            int[][] alternate={new[]{13,27},new[]{14,28},new[]{1,15,29},new[]{2,16,30},new[]{3,17},new[]{4,18},new[]{5,19}};
            foreach(DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                foreach(ReminderFrequency frequency in Enum.GetValues(typeof(ReminderFrequency)))
                {
                    var start=new DateTime(2026,9,1);var r=RecurringReminder(start,day,frequency);
                    int[] expectedDays=frequency==ReminderFrequency.Every?weekly[(int)day]:frequency==ReminderFrequency.EveryOther?alternate[(int)day]:new[]{weekly[(int)day][(int)frequency-2]};
                    for(int i=0;i<30;i++)
                    {
                        var date=start.AddDays(i);bool expected=expectedDays.Contains(i+1);
                        Check(r.Days[0].Matches(date)==expected,"Day/frequency calendar match "+day+" "+frequency+" "+date.ToString("yyyy-MM-dd"));
                    }
                }
            }
            var leap=RecurringReminder(new DateTime(2028,2,1),DayOfWeek.Tuesday,ReminderFrequency.Fourth);
            Check(leap.LatestDue(new DateTime(2028,2,29,10,0,0))==new DateTime(2028,2,22,9,0,0),"Fourth weekday does not fire on leap-day fifth occurrence");
            var spring=RecurringReminder(new DateTime(2027,3,13),DayOfWeek.Sunday,ReminderFrequency.Every,150);
            Check(spring.LatestDue(new DateTime(2027,3,14,3,0,0))==new DateTime(2027,3,14,2,30,0),"Spring clock jump catches a skipped local reminder time");
            var fall=RecurringReminder(new DateTime(2026,10,31),DayOfWeek.Sunday,ReminderFrequency.Every,90);fall.LastFired=Reminder.Local(new DateTime(2026,11,1,1,30,0));
            Check(fall.LatestDue(new DateTime(2026,11,1,1,30,0))==null,"Repeated fall clock hour does not fire twice");
            var recurring=RecurringReminder(now,DayOfWeek.Tuesday,ReminderFrequency.EveryOther);recurring.Days.Add(new ReminderDay{Day=DayOfWeek.Thursday,Frequency=ReminderFrequency.First,AnchorDate=Reminder.Local(now)});
            reloaded.Save(recurring,now);reloaded.Poll(now.AddHours(1));Check(reloaded.Pending.Id==recurring.Id,"Recurring reminder fires on selected weekday at chosen time");reloaded.Dismiss(recurring.Id);
            reloaded.Poll(now.AddHours(1));reloaded.Poll(now.AddMinutes(30));reloaded.Poll(now.AddHours(1));Check(reloaded.Pending==null,"Repeated polling and backward clock changes do not duplicate a due reminder");
            reloaded.Poll(new DateTime(2026,10,1,9,0,0));Check(reloaded.Pending.Id==recurring.Id,"Another selected day uses its own monthly frequency");reloaded.Dismiss(recurring.Id);
            reloaded.Poll(new DateTime(2026,10,6,9,0,0));Check(reloaded.Pending==null,"Every-other frequency skips next week's day");
            reloaded.Poll(new DateTime(2026,10,13,9,0,0));Check(reloaded.Pending.Id==recurring.Id,"Every-other frequency fires two weeks later");reloaded.Dismiss(recurring.Id);
            reloaded.Poll(new DateTime(2028,2,29,23,59,59));Check(reloaded.Pending!=null,"Long offline period catches up latest recurring occurrence");
            string pending=reloaded.Pending.Pending;reloaded.Poll(new DateTime(2029,3,29,23,59,59));Check(reloaded.Pending.Pending==pending,"Undismissed recurring reminder is coalesced rather than flooding the queue");
            reloaded.Dismiss(recurring.Id);reloaded.Poll(new DateTime(2029,3,29,23,59,59));Check(reloaded.Pending==null,"Dismissing coalesced occurrences does not immediately repeat");
            var saved=reloaded.Items.Single(r=>r.Id==recurring.Id);saved.Active=false;reloaded.Save(saved,new DateTime(2029,3,30));reloaded.Poll(new DateTime(2030,1,1));Check(reloaded.Pending==null,"Disabling recurrence cancels future alerts");
            var r1=Reminder.New(now);r1.Message="First";var r2=Reminder.New(now);r2.Message="Second";r2.MinuteOfDay++;reloaded.Save(r1,now);reloaded.Save(r2,now);reloaded.Poll(now.AddMinutes(10));Check(reloaded.Pending.Id==r1.Id,"Simultaneous missed reminders queue by due time");reloaded.Dismiss(r1.Id);Check(reloaded.Pending.Id==r2.Id,"Dismiss advances to the next reminder");
            var copy=reloaded.Items.Single(r=>r.Id==r2.Id);copy.Message="Unsaved";Check(reloaded.Items.Single(r=>r.Id==r2.Id).Message=="Second","Editing a copy cannot mutate saved reminders");
            reloaded=new ReminderStore(path);Check(reloaded.LoadError==null&&File.Exists(path+".bak"),"Reminder file and atomic backup remain readable");
            File.WriteAllText(path,"broken");var broken=new ReminderStore(path);Check(broken.LoadError!=null&&File.ReadAllText(path)=="broken","Unreadable reminder storage is preserved and reported");
            bool blocked=false;var valid=Reminder.New(now);valid.Message="Valid new reminder";try{broken.Save(valid,now);}catch(IOException){blocked=true;}Check(blocked&&File.ReadAllText(path)=="broken","Failed load is never silently overwritten");
            Uri uri;Check(ReminderUi.WebLink("https://example.com/path?q=1",out uri)&&ReminderUi.WebLink("http://example.com",out uri),"Reminder links accept HTTP and HTTPS");
            foreach(string link in new[]{"file:///C:/Windows/notepad.exe","javascript:alert(1)","cmd.exe","ms-settings:privacy"})Check(!ReminderUi.WebLink(link,out uri),"Non-web link cannot launch a local command: "+link);
            var work=new Rectangle(-1200,0,1200,800);
            foreach(var body in new[]{new Rectangle(-1190,0,100,100),new Rectangle(-100,700,100,100),new Rectangle(-650,350,100,100)})
            {var bounds=ReminderBubble.PositionFor(body,Rectangle.Empty,work,new Size(350,200));Check(work.Contains(bounds)&&!bounds.IntersectsWith(body),"Reminder bubble fits a negative-origin display without covering pet");}
            foreach(var target in new[]{PlayTarget.Ball,PlayTarget.Triangle,PlayTarget.Coin,PlayTarget.Card})
            {
                var pet=Pet(MovementMode.FreeRoam);var toys=Toys(pet);toys.SpawnTriangle();toys.SpawnCoin();toys.SpawnCard();pet.Place(new PointF(100,200));
                if(target==PlayTarget.Ball)toys.LaunchPull(new PointF(10,10),0);else if(target==PlayTarget.Triangle)toys.PressTriangle(0);else if(target==PlayTarget.Coin)toys.PressCoin(0);else toys.PressCard(0);
                toys.Editing=true;var before=pet.Position;
                if(target==PlayTarget.Ball)toys.DragBall(toys.Ball,0);else if(target==PlayTarget.Triangle)toys.DragTriangle(toys.Triangle);else toys.DragGame(target,target==PlayTarget.Coin?toys.Coin:toys.Card);
                ToyStep(toys,pet,.1,.1f);Check(pet.Position!=before&&pet.Walking,"Moving an approached toy keeps the pet walking: "+target);
            }
        }
    }
}
