using System;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void SettingsAboveDesktopAssets(PetWindow pet,LayeredWindow[] items,Form application)
        {
            pet.OpenSettings(0);Application.DoEvents();var settings=MakerField<SettingsWindow>(pet,"settingsWindow");
            Check(Native.SettingsForeground==settings.Handle&&Native.SettingsPet==pet.Handle,"Settings registers its desktop layering boundary when opened");
            foreach(LayerMode mode in new[]{LayerMode.Dynamic,LayerMode.OverEverything,LayerMode.UnderAll})
            {
                pet.Model.Settings.Layer=mode;pet.ApplyLayer();PetFrame(pet);RefreshItems(pet);
                var handles=Native.BackgroundAdornments.Where(Native.IsWindowVisible).Concat(new[]{pet.Handle}).ToArray();
                Check(!settings.TopMost&&(Native.GetWindowLongPtr(settings.Handle,-20).ToInt64()&8)==0,"Settings retains ordinary application stacking in "+mode);
                foreach(var handle in handles)
                {
                    Check(IsAbove(settings.Handle,handle),"Settings stays above every pet/desktop item in "+mode+" / "+handle);
                    Native.SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x213);
                    Check(IsAbove(settings.Handle,handle),"Forced item promotion cannot cover Settings in "+mode+" / "+handle);
                }
                application.Activate();Native.SetWindowPos(application.Handle,IntPtr.Zero,0,0,0,0,0x13);Application.DoEvents();
                var foreground=Native.GetForegroundWindow();
                for(int i=0;i<5;i++){RefreshItems(pet);PetFrame(pet);}
                Check(IsAbove(application.Handle,settings.Handle),"Other applications can appear above Settings in "+mode);
                Check(Native.GetForegroundWindow()==foreground,"Desktop updates never steal keyboard focus from the active application in "+mode);
                Check(items.All(item=>IsAbove(settings.Handle,item.Handle))&&IsAbove(settings.Handle,pet.Handle),"Settings remains above overlapping items and pet after redraws in "+mode);
            }
            pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();settings.Close();Application.DoEvents();RefreshItems(pet);
            Check(Native.SettingsForeground==IntPtr.Zero&&Native.SettingsPet==IntPtr.Zero,"Closing Settings clears the desktop layering boundary");
            foreach(var window in items.Concat(new[]{pet}))Check((Native.GetWindowLongPtr(window.Handle,-20).ToInt64()&8)!=0,"Closing Settings restores Over Everything for "+window.Text);
            pet.OpenSettings(0);Application.DoEvents();settings=MakerField<SettingsWindow>(pet,"settingsWindow");settings.Dispose();Application.DoEvents();
            Check(Native.SettingsForeground==IntPtr.Zero&&Native.SettingsPet==IntPtr.Zero,"Disposing Settings also clears its native layering boundary");
        }
    }
}
