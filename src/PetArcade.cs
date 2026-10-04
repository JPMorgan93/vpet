using System;
using System.IO;

namespace Vpet
{
    internal sealed partial class PetWindow
    {
        internal ArcadeWindow arcadeWindow;
        internal bool ArcadeOpen {get{return arcadeWindow!=null&&!arcadeWindow.IsDisposed;}}
        internal void OpenArcade()
        {
            if(closing)return;
            if(ArcadeOpen){arcadeWindow.Activate();return;}
            arcadeWindow=new ArcadeWindow(()=>Sprites,index=>Replacements.Get(index),Model.Settings.Arcade,Save,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference"),random){Icon=Icon};
            arcadeWindow.FormClosed+=delegate
            {
                arcadeWindow=null;if(closing)return;
                Toys.Model.LeaveArcade(Now);Model.Paused=false;Show();ApplyLayer();Render();Save();
            };
            FindPet.Spotlight.Hide();HideDesktopPet();arcadeWindow.Show();arcadeWindow.Activate();
        }
        internal void CloseArcade(){if(ArcadeOpen)arcadeWindow.Close();}
        void HideDesktopPet()
        {Hide();crossingWindow.Hide();bubble.Hide();if(reminderBubble!=null)reminderBubble.Hide();}
        void ArcadeDesktopTick(double now)
        {
            Model.Paused=true;Model.Walking=false;FindPet.Spotlight.Hide();
            restrictedOverlay.Update();Toys.Update();Plate.UpdatePlate();Joystick.UpdateJoystick();
            if(now>=nextReminderPoll){nextReminderPoll=now+1;CheckReminders();}
            HideDesktopPet();if(now>=nextSave){Save();nextSave=now+15;}
        }
    }
}
