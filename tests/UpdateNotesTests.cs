using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void UpdateDescriptions()
        {
            string preferencePath=Path.Combine(artifacts,"auto-update.json");File.WriteAllText(preferencePath,"{}");
            var prefs=Preferences.Load(preferencePath);Check(!prefs.AutoUpdate,"Auto-update defaults to No for existing and new users");
            prefs.AutoUpdate=true;prefs.Save(preferencePath);Check(Preferences.Load(preferencePath).AutoUpdate,"Auto-update Yes persists");
            string repo="owner/repo",version="9.8.7",name="Vpet-Setup-9.8.7-Windows-x64.exe",prefix="https://github.com/owner/repo/releases/download/v9.8.7/";
            var release=new GitHubRelease{Tag="v"+version,Body="- Move frames.\n- Keep feet still.",Assets=new[]{new GitHubAsset{Name=name,Url=prefix+name,Size=2000},new GitHubAsset{Name="SHA256SUMS.txt",Url=prefix+"SHA256SUMS.txt",Size=150}}};
            Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo).Notes==release.Body,"Available update carries its GitHub description");
            release.Body="# Vpet 9.8.7\n\n- New behavior.\n\n## Vpet 9.8.6\n\n- Older behavior.";
            Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo).Notes=="- New behavior.","Legacy release bodies show only changes for the target version");
            release.Body=null;Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo).Notes.Contains("No update description"),"Missing release descriptions have readable fallback text");
            Check(Updates.ReleaseNotes(" \r\n ",version).Contains("No update description"),"Blank release descriptions have readable fallback text");
            Check(Updates.ReleaseNotes("<script>never run this</script>",version)=="<script>never run this</script>","Descriptions stay plain text for the read-only viewer");
            Check(!string.IsNullOrWhiteSpace(Updates.CurrentNotes)&&!Updates.CurrentNotes.Contains("## Vpet"),"Installed release embeds only its own description for offline completion");
            string folder=Path.Combine(artifacts,"update-completion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            Check(!Updates.HasCompletion(folder,version),"Fresh installs do not show update completion");
            string marker=Path.Combine(folder,Updates.CompletionFile);File.WriteAllText(marker,version);
            Check(Updates.HasCompletion(folder,version),"Installer marker requests notes after successful update");
            Check(!Updates.HasCompletion(folder,"9.8.6"),"An older running binary never reports a newer update complete");
            Updates.AcknowledgeCompletion(folder,"9.8.6");Check(File.Exists(marker),"Older binaries cannot acknowledge a pending newer update");
            Updates.AcknowledgeCompletion(folder,version);Check(!Updates.HasCompletion(folder,version),"Acknowledged update does not repeat its popup on next launch");
        }
        static void UpdateNotesWindows()
        {
            using(var completed=new UpdateNotesWindow("9.8.7",string.Join("\n",Enumerable.Range(1,100).Select(i=>"- Change "+i))))
            {
                completed.Show();Application.DoEvents();
                var description=completed.Controls[0].Controls.OfType<TextBox>().Single();
                Check(description.ReadOnly&&description.Multiline&&description.ScrollBars==ScrollBars.Vertical,"Long release descriptions can be read and scrolled without editing");
                Check(description.SelectionStart==0&&description.Text.StartsWith("- Change 1"),"Update description opens at its first change");
                Check(completed.Text=="Vpet update complete"&&FindButton(completed,"Install update")==null&&FindButton(completed,"Later")==null,"Descriptions confirm completed updates without any install/continue prompt");
                using(var image=new Bitmap(completed.Width,completed.Height)){completed.DrawToBitmap(image,new Rectangle(Point.Empty,completed.Size));image.Save(Path.Combine(artifacts,"update-complete.png"));}
                FindButton(completed,"Close").PerformClick();Check(completed.DialogResult==DialogResult.OK,"Completion can be acknowledged");completed.Close();
            }
            UpdateStartupWindows();
        }
        static void AwaitUpdate(System.Threading.Tasks.Task task)
        {var limit=DateTime.UtcNow.AddSeconds(5);while(!task.IsCompleted&&DateTime.UtcNow<limit){Application.DoEvents();System.Threading.Thread.Sleep(5);}Check(task.IsCompleted,"Update check finishes without waiting for confirmation");task.GetAwaiter().GetResult();}
        static void UpdateStartupWindows()
        {
            string root=AppDomain.CurrentDomain.BaseDirectory;
            foreach(bool auto in new[]{false,true})
            using(var pet=new PetWindow(Path.Combine(artifacts,"auto-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"auto-smoke")))
            {
                pet.Model.Settings.AutoUpdate=auto;int installs=0,messages=0;
                pet.ReadUpdate=()=>new AvailableUpdate{Version="9.8.7",Notes="Must only display after completion"};
                pet.InstallAvailable=update=>{installs++;Check(update.Version=="9.8.7","Installs discovered newest release");};
                pet.UpdateMessage=(message,title)=>{messages++;Check(!message.Contains("Must only"),"Status message does not display pre-update notes");};
                AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(installs==(auto?1:0),"Only enabled startup checks auto-install");
                AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(installs==(auto?1:0),"Periodic checks do not automatically restart the running app");
                AwaitUpdate(pet.CheckForUpdatesAsync(true));Check(installs==(auto?2:1),"Manual Check for updates installs directly regardless of auto-update setting");
                pet.ReadUpdate=()=>null;AwaitUpdate(pet.CheckForUpdatesAsync(true));Check(messages==1,"Up-to-date check shows status without old release notes");
                pet.ReadUpdate=()=>{throw new IOException("Offline test");};AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(messages==1,"Offline automatic check remains quiet");
                using(var settings=new SettingsWindow(pet))
                {
                    settings.Show();settings.SelectTab(2);Application.DoEvents();
                    var combo=(ComboBox)settings.Controls.Find("AutoUpdate",true).Single();Check(combo.SelectedIndex==(auto?1:0),"Sprite tab reflects saved auto-update choice");
                    combo.SelectedIndex=auto?0:1;Check(pet.Model.Settings.AutoUpdate!=auto,"Sprite auto-update choice saves immediately");
                    using(var image=new Bitmap(settings.Width,settings.Height)){settings.DrawToBitmap(image,new Rectangle(Point.Empty,settings.Size));image.Save(Path.Combine(artifacts,"auto-update-settings-"+auto+".png"));}settings.Close();
                }
                pet.Close();
            }
        }
    }
}
