using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static T MakerField<T>(object window,string name)
        {return (T)window.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);}
        static void MakerZoomWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                var project=new SpriteProject(new Bitmap(1400,1800,PixelFormat.Format32bppArgb));project.Data.Width=60;project.Data.Height=70;
                using(var g=Graphics.FromImage(project.Source))g.FillRectangle(Brushes.Purple,400,950,40,50);
                project.Data.Frames[0][0]=new SpriteFrame{X=390,Y=940,OffsetX=3,OffsetY=-2};
                maker.SetProject(project,null);maker.Show();Application.DoEvents();
                var sheet=MakerField<SpriteSheetView>(maker,"sheet");var zoom=MakerField<PreviewZoomBar>(maker,"zoom");var viewport=(SpriteSheetViewport)sheet.Parent;
                viewport.AutoScrollPosition=new Point(300,900);Application.DoEvents();
                var anchor=new Point(175,90);var before=new PointF((anchor.X-sheet.Left)/sheet.Zoom,(anchor.Y-sheet.Top)/sheet.Zoom);
                zoom.SetPercent(250,anchor);Application.DoEvents();
                Near((anchor.X-sheet.Left)/sheet.Zoom,before.X,.5f,"Sheet zoom keeps the pointed source pixel horizontally fixed");
                Near((anchor.Y-sheet.Top)/sheet.Zoom,before.Y,.5f,"Sheet zoom keeps the pointed source pixel vertically fixed");
                Check(sheet.Size==new Size(3500,4500)&&zoom.Percent==250,"Custom percentage resizes the sheet preview exactly");
                Check(!maker.Dirty&&project.Data.Width==60&&project.Data.Height==70&&project.Data.Frames[0][0].OffsetX==3,"Zoom does not edit dimensions, offsets, or project dirty state");
                Check(sheet.Draft.X==390&&sheet.Draft.Y==940,"Zoom preserves the red selection in source pixels");
                foreach(Control control in zoom.Controls)if(control is NumericUpDown)((NumericUpDown)control).Value=300;
                Check(sheet.Zoom==3,"Typing a percentage updates the preview");
                FindButton(zoom,"+").PerformClick();Check(zoom.Percent==375&&sheet.Zoom==3.75f,"Zoom in button magnifies the sheet");
                FindButton(zoom,"−").PerformClick();Check(zoom.Percent==300,"Zoom out button reduces the sheet");
                var scroll=viewport.AutoScrollPosition;maker.SetFrame();Application.DoEvents();Check(viewport.AutoScrollPosition==scroll,"Setting a frame after zoom keeps scroll position");
                var model=project.Data.Frames[0][0];Check(model.X==390&&model.Y==940,"Setting a zoomed frame saves original source coordinates");
                FindButton(zoom,"Fit").PerformClick();Application.DoEvents();Check(sheet.Width<=viewport.ClientSize.Width&&sheet.Height<=viewport.ClientSize.Height,"Fit shows the whole sprite sheet");
                FindButton(zoom,"100%").PerformClick();Check(sheet.Zoom==1&&sheet.Size==project.Source.Size,"100% restores one preview pixel per source pixel");
                zoom.SetPercent(0,null);Check(zoom.Percent==10,"Zoom is bounded above zero");
                zoom.SetPercent(2000,null);Check(zoom.Percent==1600,"Zoom is bounded at 1600 percent");
                using(var large=new Bitmap(4096,4096,PixelFormat.Format32bppArgb))
                {
                    maker.SetProject(new SpriteProject((Bitmap)large.Clone()),null);zoom.SetPercent(1600,null);
                    Check(sheet.Width<=30000&&sheet.Height<=30000,"Largest supported PNG stays within native control dimensions at maximum zoom");
                }
                maker.Dirty=false;maker.Close();
            }
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);
                using(var window=new SpriteTweakWindow(maker))
                {
                    window.Show();Application.DoEvents();var preview=MakerField<TweakPreview>(window,"preview");var zoom=MakerField<PreviewZoomBar>(window,"zoom");var viewport=(SpriteSheetViewport)preview.Parent;
                    Check(preview.Zoom>1,"Animation preview opens fitted to the available space");
                    var frame=maker.Project.Data.Frames[0][0];int x=frame.OffsetX,y=frame.OffsetY;
                    zoom.SetPercent(1600,null);Application.DoEvents();
                    Check(preview.Zoom==16&&viewport.VerticalScroll.Visible,"Large animation preview exposes scrollbars");
                    var anchor=new Point(viewport.ClientSize.Width/2,viewport.ClientSize.Height/2);
                    var before=new PointF((anchor.X-preview.Left-preview.ImageOrigin.X)/preview.Zoom,(anchor.Y-preview.Top-preview.ImageOrigin.Y)/preview.Zoom);
                    zoom.SetPercent(1400,anchor);Application.DoEvents();
                    Near((anchor.X-preview.Left-preview.ImageOrigin.X)/preview.Zoom,before.X,.1f,"Animation zoom preserves the pointed artwork horizontally");
                    Near((anchor.Y-preview.Top-preview.ImageOrigin.Y)/preview.Zoom,before.Y,.1f,"Animation zoom preserves the pointed artwork vertically");
                    Check(frame.OffsetX==x&&frame.OffsetY==y&&!maker.Dirty,"Animation zoom never changes alignment");
                    FindButton(window,"Tweak").PerformClick();zoom.SetPercent(400,null);Application.DoEvents();
                    typeof(TweakPreview).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(preview,new object[]{new MouseEventArgs(MouseButtons.Left,1,100,100,0)});
                    typeof(TweakPreview).GetMethod("OnMouseMove",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(preview,new object[]{new MouseEventArgs(MouseButtons.Left,1,112,108,0)});
                    typeof(TweakPreview).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(preview,new object[]{new MouseEventArgs(MouseButtons.Left,1,112,108,0)});
                    Check(frame.OffsetX==x+3&&frame.OffsetY==y+2,"Dragging at 400 percent nudges by correct source pixels");
                    FindButton(zoom,"100%").PerformClick();Check(preview.Zoom==1,"Animation 100% restores actual pixel size");
                    FindButton(zoom,"Fit").PerformClick();Application.DoEvents();
                    Check(maker.Project.Data.Width*preview.Zoom+50<=viewport.ClientSize.Width&&maker.Project.Data.Height*preview.Zoom+50<=viewport.ClientSize.Height,"Animation Fit reveals the complete cell and baseline");
                    var bounds=preview.Bounds;FindButton(window,"Resume Preview").PerformClick();Application.DoEvents();Check(preview.Bounds==bounds,"Playback changes leave the chosen zoom stable");
                    using(var bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(Point.Empty,window.Size));bitmap.Save(Path.Combine(artifacts,"sprite-tweak-zoom.png"));}
                    window.Close();
                }
                maker.Dirty=false;
            }
        }
    }
}
