using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        [DllImport("user32.dll",EntryPoint="SendMessageW",CharSet=CharSet.Unicode)]
        static extern IntPtr ReadCue(IntPtr window,uint message,StringBuilder text,IntPtr capacity);
        static void SettingsTextAndPreviewOrder(SettingsWindow settings,PetWindow pet)
        {
            settings.SelectTab(1);Application.DoEvents();var tabs=MakerField<TabControl>(settings,"tabs");
            var name=Descendants(tabs.TabPages[1]).OfType<CueTextBox>().Single();string savedName=name.Text;
            name.Text="";name.Focus();Application.DoEvents();var cue=new StringBuilder(64);
            Check(ReadCue(name.Handle,0x1502,cue,new IntPtr(cue.Capacity))!=IntPtr.Zero&&cue.ToString()=="Pyro","Empty pet name field has a real native Pyro cue banner");
            Check(name.Text==""&&pet.Model.Settings.PetName=="","Example name never becomes the saved pet name");
            Check(Descendants(tabs.TabPages[1]).OfType<Label>().Any(label=>label.Text=="Try emotes"),"Personality uses the Try emotes heading");
            settings.SelectTab(2);Application.DoEvents();var page=tabs.TabPages[2];page.AutoScrollPosition=Point.Empty;
            var current=MakerField<Label>(settings,"importStatus");var upload=FindButton(settings,"Upload Custom Sprite");
            Check(current.Text=="Current pet: Pyro"&&OnForm(settings,current).Bottom<OnForm(settings,upload).Top,"Current pet is named Pyro above Upload Custom Sprite");
            var title=Descendants(page).Single(control=>control.Name=="RuntimePreviewTitle");
            Check(OnForm(settings,title).Bottom<OnForm(settings,MakerField<ComboBox>(settings,"previewActivity")).Top&&OnForm(settings,title).Bottom<OnForm(settings,MakerField<ComboBox>(settings,"previewDirection")).Top,"Runtime sheet preview title precedes the type and direction picklists");
            Check(!Descendants(page).Any(control=>control.Text.Contains("Each animation can use frames")||control.Text.StartsWith("Rows: idle"))&&!Descendants(page).OfType<Button>().Any(button=>button.Text.StartsWith("Save default template")),"Sprite page removes obsolete frame text, row explanation, and default-template button");
            var download=Descendants(page).OfType<Button>().Single(button=>button.Text.StartsWith("Download current sprite sheet"));var sheet=MakerField<PictureBox>(settings,"sheetPreview");var stack=(TableLayoutPanel)sheet.Parent;
            Check(stack.GetRow(download.Parent)==stack.GetRow(sheet)+1,"Download current sprite sheet follows the sheet directly");
            name.Text="Ember";string package=Path.Combine(artifacts,"settings-preview.vpetsprite");pet.Sprites.SavePackage(package);
            typeof(SettingsWindow).GetMethod("LoadSprite",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,new object[]{package});
            Check(current.Text.Contains("settings-preview.vpetsprite")&&MakerField<Button>(settings,"useButton").Enabled,"Uploading a sprite still offers a named pending preview");
            MakerField<Button>(settings,"useButton").PerformClick();Check(pet.Model.Settings.CustomPet&&current.Text.Contains("custom artwork"),"Use this pet updates the active custom-sprite status");
            FindButton(settings,"Restore default").PerformClick();
            Check(!pet.Model.Settings.CustomPet&&current.Text=="Current pet: Pyro"&&pet.Model.Settings.PetName=="Ember","Restoring Pyro updates status without overwriting the optional pet name");
            name.Text=savedName;
        }
    }
}
