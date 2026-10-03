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
            string history="# Vpet 2.0.0\n- Future.\n## Vpet 1.9.0\n- Older.\n## Vpet 1.10.0\n- Middle.\n## Vpet 1.11.0\n- Newest.\n## Vpet 1.8.0\n- Already seen.";
            string combined=Updates.NotesSince(history,"1.8.0.0","1.11.0");
            Check(combined.Contains("- Older.")&&combined.Contains("- Middle.")&&combined.Contains("- Newest.")&&!combined.Contains("Future")&&!combined.Contains("Already seen"),"Completion includes every skipped release, excluding the previous and future versions");
            Check(combined.IndexOf("Vpet 1.11.0")<combined.IndexOf("Vpet 1.10.0")&&combined.IndexOf("Vpet 1.10.0")<combined.IndexOf("Vpet 1.9.0"),"History is ordered by numeric version, newest first");
            Check(!Updates.NotesSince(history,"1.10.0","1.11.0").Contains("Middle"),"Single-step updates show only the new release");
            foreach(string unknown in new[]{null,"","garbage","1.11.0","9.0.0"})
                Check(Updates.NotesSince(history,unknown,"1.11.0")=="Vpet 1.11.0"+Environment.NewLine+"- Newest.","Unknown, same-version, or newer baseline safely falls back to the installed release");
            Check(Updates.NotesSince("",null,"1.11.0").Contains("No update description"),"Missing history has readable fallback text");
            string folder=Path.Combine(artifacts,"update-completion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            Check(!Updates.HasCompletion(folder,version),"Fresh installs do not show update completion");
            string marker=Path.Combine(folder,Updates.CompletionFile);File.WriteAllText(marker,version);
            string previousFile=Path.Combine(folder,Updates.PreviousVersionFile);File.WriteAllText(previousFile,"9.8.5");
            Check(Updates.HasCompletion(folder,version),"Installer marker requests notes after successful update");
            Check(!Updates.HasCompletion(folder,"9.8.6"),"An older running binary never reports a newer update complete");
            Updates.AcknowledgeCompletion(folder,"9.8.6");Check(File.Exists(marker)&&File.Exists(previousFile),"Older binaries cannot acknowledge a pending newer update");
            Updates.AcknowledgeCompletion(folder,version);Check(!Updates.HasCompletion(folder,version)&&!File.Exists(previousFile),"Acknowledged update clears both completion files and does not repeat its popup");
            File.WriteAllText(marker,ReleaseInfo.Version);File.WriteAllText(previousFile,"1.5.1.0");
            string installedNotes=Updates.CompletionNotes(folder,ReleaseInfo.Version);
            Check(installedNotes.Contains("Vpet 1.5.2")&&installedNotes.Contains("Vpet 1.5.3")&&installedNotes.Contains("Vpet "+ReleaseInfo.Version)&&!installedNotes.Contains("Vpet 1.5.1"),"Installed binary bundles all skipped changes for offline completion");
            Updates.RecordLastRun(folder,ReleaseInfo.Version);Updates.AcknowledgeCompletion(folder,ReleaseInfo.Version);
            Check(File.ReadAllText(Path.Combine(folder,Updates.LastRunFile))==ReleaseInfo.Version,"Last-run version survives acknowledgement for the next installer");
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
            {
            string data=Path.Combine(artifacts,"auto-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);new Preferences{AutoUpdate=auto}.Save(Path.Combine(data,"settings.json"));
            using(var pet=new PetWindow(data,Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"auto-smoke")))
            {
                Check(pet.Model.Settings.AutoUpdate==auto&&MakerField<double>(pet,"nextUpdateCheck")== (auto?0:10),"Saved auto-update option schedules an immediate startup check");int installs=0;
                pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();
                typeof(PetWindow).GetField("smokeStep",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(pet,100);
                pet.ReadUpdate=()=>new AvailableUpdate{Version="9.8.7",Notes="Must only display after completion"};
                pet.InstallAvailable=update=>{installs++;Check(update.Version=="9.8.7","Installs discovered newest release");};
                AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(installs==(auto?1:0),"Only enabled startup checks auto-install");
                AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(installs==(auto?1:0),"Periodic checks do not automatically restart the running app");
                AwaitUpdate(pet.CheckForUpdatesAsync(true));Check(installs==(auto?1:0),"Manual Check for updates waits for Update even when automatic startup updates are enabled");
                var check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");
                string text=string.Join(" ",check.Controls[0].Controls.OfType<Label>().Select(l=>l.Text));
                Check(check.Visible&&text.Contains(ReleaseInfo.Version)&&text.Contains("9.8.7")&&!text.Contains("Must only"),"Manual window shows current and available versions without release descriptions");
                foreach(string caption in new[]{"Close","Update"})
                {
                    var button=FindButton(check,caption);var bounds=new Rectangle(check.PointToClient(button.PointToScreen(Point.Empty)),button.Size);
                    Check(check.ClientRectangle.Contains(bounds),"Update status keeps the "+caption+" button fully inside the window");
                }
                Check(check.Controls[0].Controls.OfType<Label>().All(l=>l.Right<=check.Controls[0].ClientSize.Width),"Update status wraps its explanation within the window");
                using(var image=new Bitmap(check.Width,check.Height)){check.DrawToBitmap(image,new Rectangle(Point.Empty,check.Size));image.Save(Path.Combine(artifacts,"update-available.png"));}
                FindButton(check,"Close").PerformClick();Check(installs==(auto?1:0),"Closing update status never installs");
                AwaitUpdate(pet.CheckForUpdatesAsync(true));check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");
                Check(check!=null&&!check.IsDisposed,"Second manual check has a live status window");
                Check(FindButton(check,"Update")!=null,"Second manual check exposes Update");
                FindButton(check,"Update").PerformClick();Check(installs==(auto?2:1)&&check.IsDisposed,"Update installs the displayed version once and closes status");
                pet.ReadUpdate=()=>null;AwaitUpdate(pet.CheckForUpdatesAsync(true));check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");
                Check(check.Controls[0].Controls.OfType<Label>().Any(l=>l.Text.Contains("most recent version"))&&!FindButton(check,"Update").Visible,"Up-to-date window confirms newest version and has no install action");
                using(var image=new Bitmap(check.Width,check.Height)){check.DrawToBitmap(image,new Rectangle(Point.Empty,check.Size));image.Save(Path.Combine(artifacts,"update-current.png"));}
                check.Close();pet.ReadUpdate=()=>{throw new IOException("Offline test");};AwaitUpdate(pet.CheckForUpdatesAsync(false));Check(MakerField<UpdateCheckWindow>(pet,"updateCheckWindow")==null,"Offline automatic check remains quiet");
                AwaitUpdate(pet.CheckForUpdatesAsync(true));check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");
                Check(check.Controls[0].Controls.OfType<Label>().Any(l=>l.Text.Contains("Could not check"))&&!FindButton(check,"Update").Enabled,"Manual network failure shows an error without offering installation");check.Close();
                using(var gate=new System.Threading.ManualResetEventSlim(false))
                {
                    pet.ReadUpdate=()=>{gate.Wait(4000);return new AvailableUpdate{Version="9.8.7"};};
                    var pending=pet.CheckForUpdatesAsync(true);check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");
                    Check(check.Visible&&!FindButton(check,"Update").Enabled,"Manual check shows progress immediately and cannot install before a result");
                    check.Close();gate.Set();AwaitUpdate(pending);
                    Check(MakerField<UpdateCheckWindow>(pet,"updateCheckWindow")==null&&installs==(auto?2:1),"Closing during a check never reopens status or installs later");
                    gate.Reset();typeof(PetWindow).GetField("startupUpdatePending",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(pet,true);
                    var background=pet.CheckForUpdatesAsync(false);AwaitUpdate(pet.CheckForUpdatesAsync(true));
                    check=MakerField<UpdateCheckWindow>(pet,"updateCheckWindow");Check(check!=null&&check.Visible,"Manual check opens a status window even during an automatic startup check");
                    gate.Set();AwaitUpdate(background);
                    Check(FindButton(check,"Update").Enabled&&installs==(auto?2:1),"Joined startup check shows the available version and waits for the user's Update click");check.Close();
                }
                using(var settings=new SettingsWindow(pet))
                {
                    settings.Show();settings.SelectTab(3);Application.DoEvents();
                    var combo=(ComboBox)settings.Controls.Find("AutoUpdate",true).Single();Check(combo.SelectedIndex==(auto?1:0),"Advanced tab reflects saved auto-update choice");
                    combo.SelectedIndex=auto?0:1;Check(pet.Model.Settings.AutoUpdate!=auto,"Advanced auto-update choice saves immediately");
                    using(var image=new Bitmap(settings.Width,settings.Height)){settings.DrawToBitmap(image,new Rectangle(Point.Empty,settings.Size));image.Save(Path.Combine(artifacts,"auto-update-settings-"+auto+".png"));}settings.Close();
                }
                pet.Close();
            }
            }
        }
    }
}
