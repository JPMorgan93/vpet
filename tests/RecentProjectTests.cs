using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void RecentProjectWindows()
        {
            string folder=Path.Combine(artifacts,"recent-project-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string settings=Path.Combine(folder,"settings.json"),first=Path.Combine(folder,"first.vpetproject"),second=Path.Combine(folder,"second.vpetproject");
            File.WriteAllText(settings,"{}");var prefs=Preferences.Load(settings);
            Check(prefs.LastSpriteProject=="","Older settings start without a last project");
            File.WriteAllText(settings,"{\"LastSpriteProject\":null}");Check(Preferences.Load(settings).LastSpriteProject=="","Null last-project preference migrates safely");
            using(var project=MakerFixture())
            {project.Save(first);project.Data.FacesRight=true;project.Save(second);}
            using(var maker=new SpriteMakerWindow(prefs,delegate{prefs.Save(settings);}))
            {
                maker.Show();Application.DoEvents();Check(!FindButton(maker,"Load Last Project").Enabled,"Load Last Project starts disabled without a project history");
                Check(maker.OpenProject(first),"Opening a valid project succeeds");
                Check(Preferences.Load(settings).LastSpriteProject==first&&FindButton(maker,"Load Last Project").Enabled,"Opening records the full last-project path immediately");
                maker.ChooseCycle(0);maker.SetDimensions(23,24);maker.Project.Data.Frames[0][0].OffsetY=2;
                Check(maker.SaveProject(false),"Saving current project succeeds before resuming it later");
                maker.Close();
            }
            prefs=Preferences.Load(settings);
            using(var maker=new SpriteMakerWindow(prefs,delegate{prefs.Save(settings);}))
            {
                maker.Show();Application.DoEvents();FindButton(maker,"Load Last Project").PerformClick();
                Check(maker.Project!=null&&maker.Project.Width(0)==23&&maker.Project.Data.Frames[0][0].OffsetY==2,"Last project restores saved dimensions and tweaks after a settings reload");
                Check(maker.OpenProject(second)&&Preferences.Load(settings).LastSpriteProject==second,"Opening another project replaces the saved last-project path");
                var current=maker.Project;
                Check(!maker.OpenProject(Path.Combine(folder,"missing.vpetproject"))&&maker.Project==current,"Missing-file load keeps the current project intact");
                Check(Preferences.Load(settings).LastSpriteProject==second&&MakerField<TextBox>(maker,"status").Text.Contains("use Load Project"),"Failed load preserves history and explains how to locate a moved project");
                string corrupt=Path.Combine(folder,"broken.vpetproject");File.WriteAllText(corrupt,"not a sprite project");
                Check(!maker.OpenProject(corrupt)&&maker.Project==current&&prefs.LastSpriteProject==second,"Corrupt project cannot replace current work or last-project history");
                maker.SetProject(MakerFixture(),null);
                Check(prefs.LastSpriteProject==second,"Uploading an unsaved sheet retains the last saved project link");
                FindButton(maker,"Load Last Project").PerformClick();Check(maker.Project.Data.FacesRight,"Load Last Project uses the most recently opened file");
                File.Move(second,Path.Combine(folder,"moved.vpetproject"));current=maker.Project;
                FindButton(maker,"Load Last Project").PerformClick();
                Check(maker.Project==current&&MakerField<TextBox>(maker,"status").Text.Contains("Could not open"),"Missing recent file reports an error without closing current work");
                using(var image=new Bitmap(maker.Width,maker.Height)){maker.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(artifacts,"load-last-project.png"));}
                maker.Close();
            }
        }
    }
}
