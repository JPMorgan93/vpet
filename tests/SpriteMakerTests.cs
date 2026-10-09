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
            GroundAlignment();
            AnimationFrames();
            TenFrameProjects();
            BlueDragonFrames();
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
                        Check(loaded.Frame(false,3,4).GetPixel(10,23).R==190,"Fifth idle frame renders its own pixels");
                        Check(loaded.Frame(false,3,5)==loaded.Frame(false,3,0),"Five-frame idle wraps after its actual last frame");
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
                        Check(built.Frame(false,4,2).GetPixel(7,10).R==190,"Nonconsecutive fifth slot exports after the earlier populated slots");
                        Check(!built.HasDiagonals&&built.Frame(false,1,0)==built.Frame(false,0,0),"Missing diagonal uses cardinal artwork");
                        Check(built.ResolveFacing(1,new PointF(10,2),2)==0,"Cardinal fallback follows actual shallow angle");
                        Check(built.ResolveFacing(1,new PointF(2,10),0)==2,"Cardinal fallback follows actual steep angle");
                        Check(built.ResolveFacing(1,new PointF(10,11),0)==0,"Direction hysteresis avoids flicker around 45 degrees");
                        Check(built.ResolveFacing(1,PointF.Empty,2)==2,"Idle retains last cardinal direction");
                        Check(built.ResolveFacing(2,PointF.Empty,0)==2,"Hover/pickup faces down");
                    }
                    loaded.Data.Frames[0][0].OffsetY=30;using(var clipped=loaded.Build())Check(SpriteProject.VisibleBounds(clipped.Frame(false,6,0)).IsEmpty,"Export permits entirely clipped frames");
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
                {blank.Data.Width=20;blank.Data.Height=20;Check(blank.FrameProblem(0,new SpriteFrame(),false)=="frame is transparent","Empty selection rejected");}
            }
        }
        static void GroundAlignment()
        {
            using(var project=new SpriteProject(new Bitmap(200,36,PixelFormat.Format32bppArgb)))
            {
                project.Source.SetResolution(144,144);project.Data.Width=40;project.Data.Height=36;
                int[] feet={12,22,16,21,17},left={3,5,11,4,0},right={28,28,34,24,29};
                for(int slot=0;slot<5;slot++)
                {
                    int origin=slot*40;
                    using(var g=Graphics.FromImage(project.Source))
                    {g.FillRectangle(Brushes.Purple,origin+feet[slot]-3,9+slot,7,7);g.FillRectangle(Brushes.Purple,origin+left[slot],6,right[slot]-left[slot],2);}
                    project.Source.SetPixel(origin+feet[slot],22+slot*2,Color.FromArgb(slot==4?1:255,100,30,130));
                    for(int row=0;row<10;row++)project.Data.Frames[row][slot]=new SpriteFrame{X=origin,Y=0};
                }
                for(int row=0;row<10;row++)project.MagicTweak(row);
                for(int slot=0;slot<5;slot++)using(var result=project.RenderFrame(0,slot))
                {
                    Check(SpriteProject.GroundPoint(result)==new Point(19,35),"Asymmetric poses share the same lowest-pixel ground point, including faint alpha and nonstandard DPI (slot "+slot+", actual "+SpriteProject.GroundPoint(result)+")");
                    Check(result.GetPixel(19,35).A==(slot==4?1:255),"Lowest pixel alpha survives alignment");
                    Check(SpriteProject.VisibleBounds(result).Width==Math.Max(right[slot],feet[slot]+4)-left[slot],"Ground alignment preserves the complete pose without stretching or clipping");
                }
                string png=Path.Combine(artifacts,"high-dpi-source.png");project.Source.Save(png,ImageFormat.Png);
                using(var imported=SpriteProject.FromPng(png))for(int slot=0;slot<5;slot++)
                {
                    using(var crop=imported.Source.Clone(new Rectangle(slot*40,0,40,36),PixelFormat.Format32bppArgb))Check(SpriteProject.GroundPoint(crop)==new Point(feet[slot],22+slot*2),"PNG DPI cannot move source pixels during upload");
                }
                string path=Path.Combine(artifacts,"ground-project.vpetproject");project.Save(path);
                using(var loaded=SpriteProject.Load(path))using(var built=loaded.Build())
                {
                    string exported=Path.Combine(artifacts,"ground-pet.vpetsprite");built.SavePackage(exported);
                    using(var runtime=SpriteSet.Import(exported))for(int slot=0;slot<5;slot++)Check(SpriteProject.GroundPoint(runtime.Frame(false,2,slot))==new Point(19,35),"Ground point survives project save, export and runtime playback");
                }
                int offset=project.Data.Frames[0][0].OffsetX;project.MagicTweak(0);Check(project.Data.Frames[0][0].OffsetX==offset,"Repeated Magic Tweak does not accumulate offsets");
            }
            using(var project=new SpriteProject(new Bitmap(40,20,PixelFormat.Format32bppArgb)))
            {
                project.Data.Width=20;project.Data.Height=20;
                for(int x=0;x<40;x++)project.Source.SetPixel(x,2,Color.Purple);
                project.Source.SetPixel(0,10,Color.Purple);project.Source.SetPixel(39,10,Color.Purple);
                project.Data.Frames[0][0]=new SpriteFrame{OffsetY=3};project.Data.Frames[0][1]=new SpriteFrame{X=20};
                project.MagicTweak(0);using(var frame=project.RenderFrame(0,0))Check(SpriteProject.GroundPoint(frame)==new Point(9,19),"Magic Tweak anchors wide poses while clipping their excess artwork");
                using(var frame=project.RenderFrame(0,1))Check(SpriteProject.GroundPoint(frame)==new Point(9,19),"Opposite full-width pose shares the same ground point");
            }
        }
        static void SheetMouse(SpriteSheetView sheet,string method,int x,int y)
        {typeof(SpriteSheetView).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
        static void MakerScrollAndDrag()
        {
            using(var maker=new SpriteMakerWindow())
            {
                var project=new SpriteProject(new Bitmap(1400,1800,PixelFormat.Format32bppArgb));project.Data.Width=60;project.Data.Height=70;
                using(var g=Graphics.FromImage(project.Source))g.FillRectangle(Brushes.Purple,400,950,40,50);
                maker.SetProject(project,null);maker.Show();Application.DoEvents();
                var sheet=(SpriteSheetView)typeof(SpriteMakerWindow).GetField("sheet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(maker);
                var viewport=(Panel)sheet.Parent;var set=FindButton(maker,"Set");set.Focus();
                viewport.AutoScrollPosition=new Point(300,900);Application.DoEvents();var scrolled=viewport.AutoScrollPosition;
                Check(scrolled.X<0&&scrolled.Y<0,"Large sprite sheet is scrolled in both directions");
                SheetMouse(sheet,"OnMouseDown",390,940);SheetMouse(sheet,"OnMouseUp",390,940);Application.DoEvents();
                Check(viewport.AutoScrollPosition==scrolled,"Focusing and placing the red frame preserves scroll");
                set.Focus();set.PerformClick();Application.DoEvents();Check(viewport.AutoScrollPosition==scrolled,"Setting and advancing a frame preserves scroll");
                maker.ChooseSlot(0);Application.DoEvents();Check(viewport.AutoScrollPosition==scrolled,"Restoring a saved frame preserves scroll");
                foreach(float zoom in new[]{.25f,.5f,1f,2f,4f})
                {
                    sheet.Zoom=zoom;sheet.Draft=new SpriteFrame{X=390,Y=940,OffsetX=2,OffsetY=-3};
                    int x=(int)(420*zoom),y=(int)(940*zoom);
                    SheetMouse(sheet,"OnMouseDown",x,y);SheetMouse(sheet,"OnMouseMove",x+(int)(20*zoom),y+(int)(12*zoom));SheetMouse(sheet,"OnMouseUp",x+(int)(20*zoom),y+(int)(12*zoom));
                    Check(sheet.Draft.X==410&&sheet.Draft.Y==952,"Dragging the border follows the pointer at zoom "+zoom);
                    Check(project.Data.Width==60&&project.Data.Height==70&&sheet.Draft.OffsetX==2&&sheet.Draft.OffsetY==-3,"Moving the border preserves dimensions and tweak offsets");
                    Check(project.Data.Frames[0][0].X==390,"Border drag edits the draft until Set is chosen");
                }
                sheet.Zoom=1;sheet.Draft=new SpriteFrame{X=400,Y=950};
                SheetMouse(sheet,"OnMouseDown",430,950);SheetMouse(sheet,"OnMouseMove",-200,-100);SheetMouse(sheet,"OnMouseUp",-200,-100);
                Check(sheet.Draft.X==0&&sheet.Draft.Y==0,"Border drag clamps to the top-left sheet edge");
                SheetMouse(sheet,"OnMouseDown",30,0);SheetMouse(sheet,"OnMouseMove",3000,3000);SheetMouse(sheet,"OnMouseUp",3000,3000);
                Check(sheet.Draft.X==1340&&sheet.Draft.Y==1730,"Border drag clamps the whole frame to the bottom-right edge");
                sheet.Draft=new SpriteFrame{X=400,Y=950};SheetMouse(sheet,"OnMouseDown",430,950);sheet.Capture=false;SheetMouse(sheet,"OnMouseMove",800,1200);
                Check(sheet.Draft.X==400&&sheet.Draft.Y==950,"Lost capture ends a border drag");
                maker.Dirty=false;maker.Close();
            }
        }
        static void MakerWindows()
        {
            MakerZoomWindows();
            MakerScrollAndDrag();
            TenFrameWindows();
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();
                Check(maker.Project.Problems(false).Count==0,"Complete project opens in editor");
                maker.ChooseCycle(4);maker.ChooseSlot(4);
                var sheet=(SpriteSheetView)typeof(SpriteMakerWindow).GetField("sheet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(maker);
                Check(sheet.Draft!=null&&sheet.Draft.X==96,"Selecting numbered frame restores its rectangle");
                maker.ClearFrame();Check(maker.Project.Data.Frames[4][4]==null,"Clear removes selected frame only");
                sheet.Draft=new SpriteFrame{X=96,Y=100};maker.SetFrame();Check(maker.Slot==5&&maker.Project.Data.Frames[4][4]!=null,"Set frame five advances to the sixth slot");
                maker.ChooseSlot(0);sheet.Draft=new SpriteFrame{X=0,Y=100};maker.SetFrame();Check(maker.Slot==1,"Set advances to the next slot");
                maker.SetDimensions(21,24);Check(maker.Project.Width(4)==21&&maker.Project.Width(0)==20,"Resizing an animation leaves other animation types unchanged");
                sheet.Zoom=2;sheet.Draft=new SpriteFrame{X=0,Y=100};
                sheet.Scale(new SizeF(1.5f,1.5f));Check(sheet.Size==new Size(maker.Project.Source.Width*2,maker.Project.Source.Height*2),"DPI changes preserve source-pixel zoom and hit coordinates");
                typeof(SpriteSheetView).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,1,42,248,0)});
                typeof(SpriteSheetView).GetMethod("OnMouseMove",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,0,44,250,0)});
                typeof(SpriteSheetView).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sheet,new object[]{new MouseEventArgs(MouseButtons.Left,1,44,250,0)});
                Check(maker.Project.Width(4)==22&&maker.Project.Height(4)==25,"Corner dragging converts zoomed coordinates to animation dimensions");
                maker.SetDimensions(21,24);MakerField<PreviewZoomBar>(maker,"zoom").SetPercent(200,null);maker.ChooseSlot(0);
                using(var bitmap=new Bitmap(maker.Width,maker.Height)){maker.DrawToBitmap(bitmap,new Rectangle(Point.Empty,maker.Size));bitmap.Save(Path.Combine(artifacts,"sprite-maker.png"));}
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();
                    var preview=(TweakPreview)typeof(SpriteTweakWindow).GetField("preview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tweak);
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{4});
                    Application.DoEvents();var previewBounds=preview.Bounds;
                    typeof(SpriteTweakWindow).GetMethod("ToggleTweak",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,null);Application.DoEvents();
                    Check(preview.Bounds==previewBounds,"Entering Tweak keeps the preview and ground line in the same position");
                    var slider=(TrackBar)typeof(SpriteTweakWindow).GetField("slider",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tweak);
                    Check(slider.Visible&&slider.Maximum==4&&slider.Value==0,"Tweak reveals a zero-based frame slider");
                    var magic=FindButton(tweak,"Magic Tweak");magic.PerformClick();Application.DoEvents();
                    foreach(int slot in maker.Project.Slots(4))using(var image=maker.Project.RenderFrame(4,slot))Check(SpriteProject.VisibleBounds(image).Bottom==24,"Magic Tweak UI aligns bottoms");
                    foreach(int slot in maker.Project.Slots(4)){slider.Value=slot;Application.DoEvents();Check(preview.Bounds==previewBounds,"Scrubbing frames does not shift the preview baseline");}
                    FindButton(tweak,"Undo").PerformClick();Check(maker.Project.Data.Frames[4][0].OffsetY==0,"Undo restores alignment offsets");
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{0});
                    Check(slider.Maximum==0&&!slider.Enabled,"One-frame animation has a fixed zero slider");
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,new object[]{4});
                    using(var bitmap=new Bitmap(tweak.Width,tweak.Height)){tweak.DrawToBitmap(bitmap,new Rectangle(Point.Empty,tweak.Size));bitmap.Save(Path.Combine(artifacts,"sprite-tweak.png"));}
                    typeof(SpriteTweakWindow).GetMethod("ToggleTweak",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tweak,null);Application.DoEvents();Check(preview.Bounds==previewBounds,"Resuming animation keeps the preview ground line fixed");
                    tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
            string dataDirectory=Path.Combine(artifacts,"maker-pet-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dataDirectory);
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Blue Dragon.png");
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
