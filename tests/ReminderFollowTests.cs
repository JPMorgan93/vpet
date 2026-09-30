using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static Rectangle OnScreen(Form window)
        {Native.RECT bounds;Check(Native.GetWindowRect(window.Handle,out bounds),"Read actual screen bounds");return Rectangle.FromLTRB(bounds.Left,bounds.Top,bounds.Right,bounds.Bottom);}
        static void PetFrame(PetWindow pet)
        {typeof(PetWindow).GetField("previousTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,pet.Now-.03);typeof(PetWindow).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{null,EventArgs.Empty});}
        static void CheckReminderAnchor(PetWindow pet)
        {
            var reminder=MakerField<ReminderBubble>(pet,"reminderBubble");var crossing=MakerField<LayeredWindow>(pet,"crossingWindow");var emote=MakerField<LayeredWindow>(pet,"bubble");
            var sprite=OnScreen(pet.Model.Crossing!=null&&pet.Model.Crossing.Progress>=.5f?crossing:pet);
            var expected=ReminderBubble.PositionFor(sprite,emote.Visible?OnScreen(emote):Rectangle.Empty,Screen.FromRectangle(sprite).WorkingArea,reminder.Size);
            Check(OnScreen(reminder)==expected,"Reminder follows the actual rendered pet: expected "+expected+", got "+OnScreen(reminder)+", managed pet "+pet.Bounds+", actual pet "+sprite);
        }
        static void ReminderFollowing()
        {
            var original=Cursor.Position;var foreground=Native.GetForegroundWindow();string root=AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using(var pet=new PetWindow(Path.Combine(artifacts,"reminder-follow-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"follow-smoke")))
                {
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();typeof(PetWindow).GetField("smokeStep",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,100);
                    pet.Model.Settings.Movement=MovementMode.Static;var work=pet.Model.Current.Work;Cursor.Position=new Point(work.Left+1,work.Top+1);
                    var now=new DateTime(2026,9,30,8,0,0);pet.ReminderNow=()=>now.AddMinutes(5);var r=Reminder.New(now);r.Message="Follow the pet while moving.";pet.Reminders.Save(r,now);
                    pet.Model.Place(new PointF(work.Left+work.Width/2,work.Top+work.Height/2));PetFrame(pet);CheckReminderAnchor(pet);
                    var before=OnScreen(MakerField<ReminderBubble>(pet,"reminderBubble"));pet.Model.Place(new PointF(pet.Model.Position.X+100,pet.Model.Position.Y+70));PetFrame(pet);CheckReminderAnchor(pet);
                    Check(OnScreen(MakerField<ReminderBubble>(pet,"reminderBubble")).Location!=before.Location,"Reminder moves with repositioned pet");pet.Close();
                }
            }
            finally{Cursor.Position=original;if(foreground!=IntPtr.Zero)Native.SetForegroundWindow(foreground);}
        }
    }
}
