using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
            var expected=ReminderBubble.PositionFor(sprite,emote.Visible?OnScreen(emote):Rectangle.Empty,pet.Model.Current.Work,reminder.Size);var actual=OnScreen(reminder);
            Check(actual==expected,"Reminder follows the actual rendered pet: expected "+expected+", got "+actual+", actual pet "+sprite);
            Check(pet.Model.Current.Work.Contains(actual),"Reminder remains visible on the pet's current display");
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
                    Check(OnScreen(MakerField<ReminderBubble>(pet,"reminderBubble")).Location!=before.Location,"Reminder moves with repositioned pet");
                    pet.Model.Settings.PetName="Follow me";pet.Model.Settings.NameDisplay=NameVisibility.Always;pet.NameChanged();
                    foreach(LayerMode layer in Enum.GetValues(typeof(LayerMode)))
                    {
                        pet.Model.Settings.Layer=layer;pet.ApplyLayer();
                        foreach(var display in pet.Model.Displays.ToArray())
                        {
                            work=display.Work;pet.Model.Settings.Movement=MovementMode.Static;pet.Model.Place(new PointF(work.Left+work.Width/2,work.Top+work.Height/2));pet.PreviewReaction(Reactions.Love);PetFrame(pet);CheckReminderAnchor(pet);
                            var sprite=OnScreen(pet);var start=new Point(sprite.Left+sprite.Width/2,sprite.Top+sprite.Height/2);before=OnScreen(MakerField<ReminderBubble>(pet,"reminderBubble"));
                            ToyMouse(pet,0x201,start);
                            for(int i=1;i<=4;i++){ToyMouse(pet,0x200,new Point(start.X+i*20,start.Y+i*10));PetFrame(pet);CheckReminderAnchor(pet);}
                            Check(OnScreen(MakerField<ReminderBubble>(pet,"reminderBubble")).Location!=before.Location,"Reminder follows actual mouse dragging in "+layer);
                            ToyMouse(pet,0x202,Cursor.Position);PetFrame(pet);CheckReminderAnchor(pet);
                            Cursor.Position=new Point(work.Left+1,work.Top+1);pet.Model.Settings.Movement=MovementMode.FreeRoam;pet.Model.IdleUntil=0;
                            pet.Model.SetDestination(new PointF(pet.Model.Position.X+100,pet.Model.Position.Y+60),display.Id);
                            for(int i=0;i<5;i++){PetFrame(pet);Check(pet.Model.Walking,"Pet walks with an active reminder in "+layer);CheckReminderAnchor(pet);}
                            pet.Model.CancelRoute();pet.Model.Settings.Movement=MovementMode.Static;
                            foreach(var corner in new[]{new PointF(work.Left,work.Top),new PointF(work.Right,work.Bottom)}){pet.Model.Place(corner);PetFrame(pet);CheckReminderAnchor(pet);}
                        }
                    }
                    if(pet.Model.Displays.Count>1)
                    {
                        var displays=pet.Model.Displays.ToArray();
                        foreach(bool reverse in new[]{false,true})
                        {
                            var from=displays[reverse?1:0];var to=displays[reverse?0:1];
                            var plan=DisplayCrossing.Plan(from,to,pet.Model.FrameSize,new PointF(to.Work.Left+to.Work.Width/2,to.Work.Top+to.Work.Height/2));
                            pet.Model.Place(plan.Exit);PetFrame(pet);var sprite=OnScreen(pet);var start=new Point(sprite.Left+sprite.Width/2,sprite.Top+sprite.Height/2);ToyMouse(pet,0x201,start);
                            foreach(float progress in new[]{.2f,.4f,.6f,.8f})
                            {
                                var point=new Point(start.X+(int)Math.Round((plan.Entry.X-plan.Exit.X)*progress),start.Y+(int)Math.Round((plan.Entry.Y-plan.Exit.Y)*progress));
                                ToyMouse(pet,0x200,point);PetFrame(pet);Check(pet.Model.Crossing!=null,"Drag crosses between the connected displays");CheckReminderAnchor(pet);
                            }
                            ToyMouse(pet,0x202,Cursor.Position);for(int i=0;i<8;i++){PetFrame(pet);CheckReminderAnchor(pet);}
                            Check(pet.Model.Crossing==null&&pet.Model.CurrentDisplay==to.Id,"Reminder follows the settled pet onto the destination display");
                        }
                    }
                    FindButton(MakerField<ReminderBubble>(pet,"reminderBubble"),"Dismiss").PerformClick();PetFrame(pet);Check(!MakerField<ReminderBubble>(pet,"reminderBubble").Visible,"Following does not revive a dismissed reminder");pet.Close();
                }
            }
            finally{Cursor.Position=original;if(foreground!=IntPtr.Zero)Native.SetForegroundWindow(foreground);}
        }
    }
}
