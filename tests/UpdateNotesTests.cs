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
            using(var offer=new UpdateNotesWindow("9.8.7",string.Join("\n",Enumerable.Range(1,100).Select(i=>"- Change "+i)),true,false))
            {
                offer.Show();Application.DoEvents();
                var description=offer.Controls[0].Controls.OfType<TextBox>().Single();
                Check(description.ReadOnly&&description.Multiline&&description.ScrollBars==ScrollBars.Vertical,"Long release descriptions can be read and scrolled without editing");
                Check(description.SelectionStart==0&&description.Text.StartsWith("- Change 1"),"Update description opens at its first change");
                Check(FindButton(offer,"Install update")!=null&&FindButton(offer,"Later")!=null,"Available update offers install and postpone controls");
                using(var image=new Bitmap(offer.Width,offer.Height)){offer.DrawToBitmap(image,new Rectangle(Point.Empty,offer.Size));image.Save(Path.Combine(artifacts,"update-available.png"));}
                FindButton(offer,"Later").PerformClick();Check(offer.DialogResult==DialogResult.Cancel,"Later dismisses the offer without approving installation");offer.Close();
            }
            using(var completed=new UpdateNotesWindow(ReleaseInfo.Version,Updates.CurrentNotes,false,true))
            {
                completed.Show();Application.DoEvents();Check(completed.Text=="Vpet update complete"&&FindButton(completed,"Install update")==null,"Completion confirms success without offering another installation");
                using(var image=new Bitmap(completed.Width,completed.Height)){completed.DrawToBitmap(image,new Rectangle(Point.Empty,completed.Size));image.Save(Path.Combine(artifacts,"update-complete.png"));}
                FindButton(completed,"Close").PerformClick();Check(completed.DialogResult==DialogResult.OK,"Completion can be acknowledged");completed.Close();
            }
        }
    }
}
