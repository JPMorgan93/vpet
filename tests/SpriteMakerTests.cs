using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static SpriteProject MakerFixture()
        {
            var image=new Bitmap(128,256,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))for(int row=0;row<10;row++)for(int slot=0;slot<5;slot++)
            {
                using(var brush=new SolidBrush(Color.FromArgb(255,70+slot*30,50+row*15,150)))g.FillRectangle(brush,slot*24+2+slot%3,row*25+2+slot,6+slot,9+slot%2);
            }
            var project=new SpriteProject(image);project.Data.Width=20;project.Data.Height=24;
            for(int row=0;row<10;row++)for(int slot=0;slot<row%5+1;slot++)project.Data.Frames[row][slot]=new SpriteFrame{X=slot*24,Y=row*25};
            return project;
        }
        static void MakerProjects()
        {
            using(var project=MakerFixture())
            {
                Check(project.Problems(true).Count==0,"Variable-count project is complete");
                using(var original=(Bitmap)project.Source.Clone())
                {
                    for(int row=0;row<10;row++)
                    {
                        project.MagicTweak(row);
                        foreach(int slot in project.Slots(row))using(var frame=project.RenderFrame(row,slot))
                        {
                            var bounds=SpriteProject.VisibleBounds(frame);
                            Check(bounds.Bottom==24,"Magic Tweak anchors feet to cell bottom");
                            Near(bounds.Left+bounds.Width/2f,10,.5f,"Magic Tweak centers the visible artwork horizontally");
                            Check(bounds.Width==6+slot&&bounds.Height==9+slot%2,"Magic Tweak never stretches artwork");
                        }
                    }
                    for(int y=0;y<original.Height;y+=3)for(int x=0;x<original.Width;x+=3)if(original.GetPixel(x,y)!=project.Source.GetPixel(x,y))throw new Exception("Magic Tweak changed source pixels");
                    Check(true,"Magic Tweak preserves the original sheet");
                }
                string spritePath=Path.Combine(artifacts,"variable.vpetsprite");
                using(var built=project.Build())
                {
                    built.SavePackage(spritePath);
                    using(var loaded=SpriteSet.Import(spritePath))
                    {
                        Check(loaded.Cell==new Size(20,24)&&loaded.HasDiagonals,"Runtime metadata survives export/import");
                        for(int row=0;row<10;row++)Check(loaded.Counts[row]==row%5+1,"Each animation retains its actual frame count");
                        for(int direction=0;direction<8;direction++)for(int index=0;index<20;index++)Check(loaded.Frame(false,direction,index)!=null&&loaded.Frame(true,direction,index)!=null,"All directions loop variable frames");
                    }
                }
                project.Data.Diagonals=false;
                project.Data.Frames[2][1]=null;project.Data.Frames[2][4]=new SpriteFrame{X=96,Y=50};
                project.Data.Frames[0][0]=null; // Incomplete projects must remain editable.
                string path=Path.Combine(artifacts,"portable.vpetproject");project.Save(path);
                using(var loaded=SpriteProject.Load(path))
                {
                    Check(!loaded.Data.Diagonals&&loaded.Data.Frames[3][0]!=null,"Disabled diagonal frames remain in saved project");
                    Check(loaded.Data.Frames[0][0]==null&&loaded.Problems(false).Count==1,"Incomplete project saves and loads");
                    Check(loaded.Source.Size==project.Source.Size&&loaded.Source.GetPixel(3,3)==project.Source.GetPixel(3,3),"Project embeds its source artwork");
                    Check(loaded.Data.Frames[2][0].OffsetY==project.Data.Frames[2][0].OffsetY,"Alignment offsets persist");
                    loaded.Data.Frames[0][0]=new SpriteFrame{X=0,Y=0};
                    using(var built=loaded.Build())
                    {
                        Check(built.Counts[2]==3&&built.Counts[3]==0,"Nonconsecutive slots compact in order; disabled cycles omitted");
                        Check(!built.HasDiagonals&&built.Frame(false,1,0)==built.Frame(false,0,0),"Missing diagonal uses cardinal artwork");
                        Check(built.ResolveFacing(1,new PointF(10,2),2)==0,"Cardinal fallback follows actual shallow angle");
                        Check(built.ResolveFacing(1,new PointF(2,10),0)==2,"Cardinal fallback follows actual steep angle");
                        Check(built.ResolveFacing(1,new PointF(10,11),0)==0,"Direction hysteresis avoids flicker around 45 degrees");
                        Check(built.ResolveFacing(1,PointF.Empty,2)==2,"Idle retains last cardinal direction");
                        Check(built.ResolveFacing(2,PointF.Empty,0)==2,"Hover/pickup faces down");
                    }
                    loaded.Data.Frames[0][0].OffsetY=30;Reject(delegate{using(var invalid=loaded.Build()){}},"Export blocks clipped tweaks");
                    loaded.MagicTweak(0);Check(loaded.Problems(true).Count==0,"Magic Tweak fixes clipped offsets");
                    loaded.Data.Frames[0][0].X=120;Check(loaded.Problems(false).Count>0,"Global resizing/out-of-bounds selections remain flagged");
                }
                var data=new SpriteManifest{Kind="sprite",Width=20,Height=24,Counts=new int[10]};
                Reject(delegate{SpritePackage.Validate(data,"sprite");},"Runtime zero-count enabled animation rejected");
                data.Version=999;Reject(delegate{SpritePackage.Validate(data,"sprite");},"Unknown file version rejected");
                string duplicate=Path.Combine(artifacts,"duplicate.vpetsprite");
                using(var file=File.Create(duplicate))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){zip.CreateEntry("manifest.json");zip.CreateEntry("manifest.json");zip.CreateEntry("atlas.png");}
                Reject(delegate{using(var invalid=SpriteSet.Import(duplicate)){}},"Duplicate archive entries rejected");
                string invalidPng=Path.Combine(artifacts,"opaque.png");using(var opaque=new Bitmap(32,32)){using(var g=Graphics.FromImage(opaque))g.Clear(Color.White);opaque.Save(invalidPng);}
                Reject(delegate{using(var invalid=SpriteProject.FromPng(invalidPng)){}},"Opaque sheet rejected");
                using(var transparent=new Bitmap(32,32))using(var blank=new SpriteProject(transparent))
                {blank.Data.Width=20;blank.Data.Height=20;Check(blank.FrameProblem(new SpriteFrame(),false)=="frame is transparent","Empty selection rejected");}
            }
        }
        static void MakerWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();
                Check(maker.Project.Problems(false).Count==0,"Complete project opens in editor");
                maker.ChooseCycle(4);maker.ChooseSlot(4);
                var sheet=(SpriteSheetView)typeof(SpriteMakerWindow).GetField("sheet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(maker);
                Check(sheet.Draft!=null&&sheet.Draft.X==96,"Selecting numbered frame restores its rectangle");
                maker.ClearFrame();Check(maker.Project.Data.Frames[4][4]==null,"Clear removes selected frame only");
                sheet.Draft=new SpriteFrame{X=96,Y=100};maker.SetFrame();Check(maker.Slot==4&&maker.Project.Data.Frames[4][4]!=null,"Set frame five keeps last slot selected");
                maker.ChooseSlot(0);sheet.Draft=new SpriteFrame{X=0,Y=100};maker.SetFrame();Check(maker.Slot==1,"Set advances to the next slot");
                maker.SetDimensions(21,24);Check(maker.Project.Selection(maker.Project.Data.Frames[0][0]).Width==21,"Shared frame size applies across animations");
                sheet.Zoom=2;sheet.Draft=new SpriteFrame{X=0,Y=100};
                typeof(SpriteSheetView).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,1,42,248,0)});
                typeof(SpriteSheetView).GetMethod("OnMouseMove",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,0,44,250,0)});
                typeof(SpriteSheetView).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,1,44,250,0)});
                Check(maker.Project.Data.Width==22&&maker.Project.Data.Height==25,"Corner dragging converts zoomed coordinates to shared pixel dimensions");
                maker.SetDimensions(21,24);sheet.Zoom=2;maker.ChooseSlot(0);
                using(var bitmap=new Bitmap(maker.Width,maker.Height)){maker.DrawToBitmap(bitmap,new Rectangle(Point.Empty,maker.Size));bitmap.Save(Path.Combine(artifacts,"sprite-maker.png"));}
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{4});
                    typeof(SpriteTweakWindow).GetMethod("ToggleTweak",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,null);Application.DoEvents();
                    var slider=(TrackBar)typeof(SpriteTweakWindow).GetField("slider",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tweak);
                    Check(slider.Visible&&slider.Maximum==4&&slider.Value==0,"Tweak reveals a zero-based frame slider");
                    var magic=FindButton(tweak,"Magic Tweak");magic.PerformClick();Application.DoEvents();
                    foreach(int slot in maker.Project.Slots(4))using(var image=maker.Project.RenderFrame(4,slot))Check(SpriteProject.VisibleBounds(image).Bottom==24,"Magic Tweak UI aligns bottoms");
                    FindButton(tweak,"Undo").PerformClick();Check(maker.Project.Data.Frames[4][0].OffsetY==0,"Undo restores alignment offsets");
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{0});
                    Check(slider.Maximum==0&&!slider.Enabled,"One-frame animation has a fixed zero slider");
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{4});
                    using(var bitmap=new Bitmap(tweak.Width,tweak.Height)){tweak.DrawToBitmap(bitmap,new Rectangle(Point.Empty,tweak.Size));bitmap.Save(Path.Combine(artifacts,"sprite-tweak.png"));}
                    tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
            string dataDirectory=Path.Combine(artifacts,"maker-pet-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dataDirectory);
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Base Vpet Sprite Sheet.png");
            using(var project=MakerFixture())using(var pet=new PetWindow(dataDirectory,reference,true,dataDirectory))
            {
                project.Data.Diagonals=false;pet.UseCustom(project.Build());
                Check(pet.Sprites.Counts[4]==0&&pet.Sprites.Counts[2]==3,"Applying custom sprite keeps metadata");pet.Close();
            }
            using(var restarted=new PetWindow(dataDirectory,reference,true,dataDirectory))
            {Check(restarted.Model.Settings.CustomPet&&!restarted.Sprites.HasDiagonals&&restarted.Sprites.Counts[2]==3,"Active custom package survives app restart");restarted.Close();}
        }
        static Button FindButton(Control parent,string text)
        {
            foreach(Control control in parent.Controls){var b=control as Button;if(b!=null&&b.Text==text)return b;var found=FindButton(control,text);if(found!=null)return found;}return null;
        }
    }
}
