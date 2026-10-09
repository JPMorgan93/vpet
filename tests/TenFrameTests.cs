using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static SpriteProject TenFrameFixture()
        {
            var project=new SpriteProject(new Bitmap(320,36,PixelFormat.Format32bppArgb));
            project.Data.Width=32;project.Data.Height=36;project.Data.EmoteAnimations=true;
            for(int slot=0;slot<10;slot++)
            {
                using(var g=Graphics.FromImage(project.Source))using(var brush=new SolidBrush(Color.FromArgb(255,25+slot*20,70,160)))
                    g.FillRectangle(brush,slot*32+3+slot,4,5,10+slot);
                for(int row=0;row<SpriteProject.TotalCycles;row++)project.Data.Frames[row][slot]=new SpriteFrame{X=slot*32};
            }
            return project;
        }
        static void TenFrameProjects()
        {
            string path=Path.Combine(artifacts,"ten-frames.vpetproject"),spritePath=Path.Combine(artifacts,"ten-frames.vpetsprite");
            using(var project=TenFrameFixture())
            {
                project.SetSpeed(0,.5f);project.SetSpeed(10,2);
                project.Save(path);
                using(var loaded=SpriteProject.Load(path))
                {
                    Check(loaded.Data.Version==6&&loaded.Data.Frames.All(row=>row.Length==10),"Version 6 projects retain ten slots for every movement and optional reaction");
                    Check(loaded.Data.Frames[18][9].X==288&&loaded.Speed(0)==.5f&&loaded.Speed(10)==2,"Tenth Hunger selection and independent speeds survive saving");
                    for(int row=0;row<SpriteProject.TotalCycles;row++)loaded.MagicTweak(row);
                    for(int slot=0;slot<10;slot++)using(var frame=loaded.RenderFrame(0,slot))
                        Check(SpriteProject.GroundPoint(frame)==new Point(15,35),"All ten frames share the Magic Tweak ground point");
                    using(var built=loaded.Build())
                    {
                        built.SavePackage(spritePath);
                        using(var runtime=SpriteSet.Import(spritePath))
                        {
                            Check(runtime.Columns==10&&runtime.Sheet.Size==new Size(320,684)&&runtime.Counts.All(count=>count==10),"Ten-frame runtime package retains every movement and reaction count");
                            Check(runtime.Frame(false,6,9).GetPixel(15,35).R==205&&runtime.Frame(false,6,10)==runtime.Frame(false,6,0),"Tenth movement frame renders and playback wraps after ten");
                            Check(runtime.EmoteFrame(8,9).GetPixel(15,35).R==205&&runtime.EmoteFrame(8,10)==runtime.EmoteFrame(8,0),"Tenth optional Hunger frame renders and wraps after ten");
                            Check(runtime.FrameAtPhase(false,6,18)==runtime.Frame(false,6,9)&&runtime.EmoteAtPhase(0,4.5)==runtime.EmoteFrame(0,9),"Independent speeds reach the tenth frame during runtime playback");
                        }
                    }
                    loaded.Data.Frames[0]=new SpriteFrame[10];loaded.Data.Frames[0][2]=new SpriteFrame{X=64};loaded.Data.Frames[0][9]=new SpriteFrame{X=288,OffsetY=35};
                    using(var built=loaded.Build())
                    {
                        Check(built.Counts[0]==2&&built.Frame(false,6,0).GetPixel(5,4).R==65,"Sparse slots export in numbered order");
                        Check(SpriteProject.VisibleBounds(built.Frame(false,6,1)).IsEmpty,"Tenth-slot clipping is retained in the exported animation");
                    }
                    loaded.MagicTweak(0);using(var frame=loaded.RenderFrame(0,9))Check(SpriteProject.GroundPoint(frame)==new Point(15,35),"Magic Tweak restores a clipped tenth selection");
                    loaded.Data.Frames[0]=new SpriteFrame[11];Reject(delegate{loaded.Save(path);},"An eleventh project slot is rejected");
                }
                SpriteManifest manifest;using(var atlas=SpritePackage.Read(spritePath,"sprite",out manifest))
                {
                    manifest.Counts[0]=11;Reject(delegate{SpritePackage.Validate(manifest,"sprite");},"Eleven runtime frames are rejected");manifest.Counts[0]=10;
                    manifest.Version=5;Reject(delegate{SpritePackage.Validate(manifest,"sprite");},"Legacy manifests cannot claim ten-column frame counts");
                    manifest.Counts=Enumerable.Repeat(5,19).ToArray();string bad=Path.Combine(artifacts,"wrong-columns.vpetsprite");SpritePackage.Write(bad,manifest,atlas);
                    Reject(delegate{using(var invalid=SpriteSet.Import(bad)){ }},"Ten-column PNG with a five-column manifest is rejected");
                }
                project.SetSize(18,100,150);project.Data.Frames[18]=new SpriteFrame[10];project.Data.Frames[18][9]=new SpriteFrame();
                // Supply a large enough source for a maximum-size optional frame.
                string large=Path.Combine(artifacts,"large-ten-source.png");using(var image=new Bitmap(320,150,PixelFormat.Format32bppArgb))
                {using(var g=Graphics.FromImage(image))SpritePackage.CopyPixels(g,project.Source,0,0);image.Save(large,ImageFormat.Png);}
                project.ReplaceSource(large);using(var built=project.Build())
                {built.SavePackage(spritePath);using(var loaded=SpriteSet.Import(spritePath))Check(loaded.Sheet.Width==1000&&loaded.Cell==new Size(100,150),"Ten-column atlas accepts maximum 100 by 150 pixel frames");}
            }
            for(int version=1;version<=5;version++)using(var project=TenFrameFixture())
            {
                int rows=version<3?10:version<5?18:19;
                project.Data.Version=version;project.Data.Frames=project.Data.Frames.Take(rows).Select(row=>row.Take(5).ToArray()).ToArray();
                project.Data.Frames[0][4].OffsetX=-2;project.Data.Frames[0][4].OffsetY=3;
                if(version>=2){project.Data.CycleWidths=Enumerable.Repeat(32,rows).ToArray();project.Data.CycleHeights=Enumerable.Repeat(36,rows).ToArray();}
                if(version>=4){project.Data.CycleSpeeds=Enumerable.Repeat(1f,rows).ToArray();project.Data.CycleSpeeds[0]=1.5f;}
                SpritePackage.Write(path,project.Data,project.Source);
                using(var loaded=SpriteProject.Load(path))
                {
                    Check(loaded.Data.Frames.Length==19&&loaded.Data.Frames.All(row=>row.Length==10),"Legacy v"+version+" projects gain ten editable slots");
                    Check(loaded.Data.Frames[0][4].X==128&&loaded.Data.Frames[0][4].OffsetX==-2&&loaded.Data.Frames[0][4].OffsetY==3&&loaded.Data.Frames.All(row=>row[9]==null),"Legacy v"+version+" selections and offsets remain unchanged");
                    loaded.Data.Frames[0][9]=new SpriteFrame{X=288};loaded.Save(path);
                    using(var upgraded=SpriteProject.Load(path))Check(upgraded.Data.Version==6&&upgraded.Slots(0).SequenceEqual(new[]{0,1,2,3,4,9})&&upgraded.Speed(0)==(version>=4?1.5f:1),"Legacy v"+version+" projects save a tenth selection with preserved speeds");
                }
            }
        }
        static void BlueDragonFrames()
        {
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Blue Dragon.png");
            using(var source=new Bitmap(reference))using(var sprites=SpriteSet.FromReference(reference))
            {
                int[] sourceRows={8,0,4,6,2,9,1,5,7,3},facings={6,2,4,5,3};
                Check(sprites.Speeds!=null&&sprites.Speeds.All(speed=>speed==2),"Bundled Blue Dragon uses twice-speed movement animations");
                foreach(bool walking in new[]{false,true})foreach(int facing in Enumerable.Range(0,8))
                {
                    Check(ReferenceEquals(sprites.FrameAtPhase(walking,facing,1.25),sprites.Frame(walking,facing,2)),"Default playback advances two frames at phase 1.25 in direction "+facing+", walking "+walking);
                }
                string package=Path.Combine(artifacts,"twice-speed-default.vpetsprite");sprites.SavePackage(package);
                using(var imported=SpriteSet.Import(package))Check(imported.Speeds.All(speed=>speed==2)&&imported.Counts.SequenceEqual(sprites.Counts),"Default template export/import retains twice-speed playback and frame counts");
                for(int row=0;row<10;row++)for(int slot=0;slot<sprites.Counts[row];slot++)
                {
                    var frame=sprites.Frame(row>=5,facings[row%5],slot);bool equal=true;
                    for(int y=0;y<36;y++)for(int x=0;x<32;x++)
                    {
                        Color actual=frame.GetPixel(x,y),expected=source.GetPixel(slot*32+x,sourceRows[row]*36+y);
                        if(actual.A!=expected.A||(expected.A>0&&actual.ToArgb()!=expected.ToArgb()))equal=false;
                    }
                    Check(equal,"Blue Dragon row "+row+" frame "+slot+" preserves visible pixels and transparency exactly");
                }
            }
        }
        static void TenFrameWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(TenFrameFixture(),null);maker.Show();Application.DoEvents();
                var buttons=MakerField<Button[]>(maker,"slots");Check(buttons.Length==10&&buttons[9].Visible&&buttons[9].Text.StartsWith("10"),"Frame 10 is available in the native Sprite Maker");
                buttons[9].PerformClick();Check(maker.Slot==9,"Tenth numbered button selects its frame");
                var sheet=MakerField<SpriteSheetView>(maker,"sheet");Check(sheet.Draft.X==288,"Tenth frame restores its saved source rectangle");
                maker.ClearFrame();Check(maker.Project.Data.Frames[0][9]==null&&maker.Project.Data.Frames[0][8]!=null,"Clear frame 10 leaves frame 9 intact");
                maker.ChooseSlot(8);sheet.Draft=new SpriteFrame{X=256};maker.SetFrame();Check(maker.Slot==9,"Setting frame 9 advances to frame 10");
                sheet.Draft=new SpriteFrame{X=288};maker.SetFrame();Check(maker.Slot==9&&maker.Project.Data.Frames[0][9]!=null,"Setting frame 10 stays within the last allowed slot");
                maker.Size=maker.MinimumSize;Application.DoEvents();buttons[9].Focus();Application.DoEvents();
                var controlScroll=(Panel)buttons[9].Parent.Parent.Parent;
                Check(controlScroll.RectangleToScreen(controlScroll.ClientRectangle).Contains(buttons[9].RectangleToScreen(buttons[9].ClientRectangle)),"Frame 10 remains fully reachable at minimum editor size");
                CaptureForm(maker,"sprite-maker-ten-minimum");
                maker.ClientSize=new Size(1100,850);Application.DoEvents();CaptureForm(maker,"sprite-maker-ten-frames");
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();FindButton(tweak,"Tweak").PerformClick();Application.DoEvents();
                    var slider=MakerField<TrackBar>(tweak,"slider");var preview=MakerField<TweakPreview>(tweak,"preview");
                    Check(slider.Maximum==9&&slider.Enabled,"Tweak slider exposes all ten populated frames");slider.Value=9;Application.DoEvents();
                    Check(preview.Slot==9,"Tweak preview displays the tenth frame");
                    FindButton(tweak,"Magic Tweak").PerformClick();Application.DoEvents();using(var frame=maker.Project.RenderFrame(0,9))Check(SpriteProject.GroundPoint(frame)==new Point(15,35),"Magic Tweak button aligns the tenth frame");
                    CaptureForm(tweak,"sprite-tweak-ten-frames");FindButton(tweak,"Undo").PerformClick();
                    Check(maker.Project.Data.Frames[0].Length==10&&maker.Project.Data.Frames[0][9].X==288&&maker.Project.Data.Frames[0][9].OffsetY==0,"Undo restores frame 10 without shrinking the animation");
                    tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
            string directory=Path.Combine(artifacts,"dragon-default-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Blue Dragon.png");
            using(var pet=new PetWindow(directory,reference,true,directory))
            {Check(!pet.Model.Settings.CustomPet&&pet.Sprites.Counts[0]==8&&pet.Sprites.Counts[5]==10&&pet.Sprites.Columns==10,"A new profile launches with the Blue Dragon default");pet.Close();}
        }
    }
}
