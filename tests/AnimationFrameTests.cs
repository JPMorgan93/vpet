using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void AnimationFrames()
        {
            using(var project=new SpriteProject(new Bitmap(200,1500,PixelFormat.Format32bppArgb)))
            {
                for(int row=0;row<10;row++)
                {
                    project.SetSize(row,13+3*row,17+4*row);
                    for(int slot=0;slot<2;slot++)
                    {
                        var frame=new SpriteFrame{X=slot*100,Y=row*150};project.Data.Frames[row][slot]=frame;
                        using(var g=Graphics.FromImage(project.Source))g.FillRectangle(Brushes.Magenta,frame.X+2,frame.Y+2,project.Width(row)-4,project.Height(row)-6);
                        project.Source.SetPixel(frame.X+(project.Width(row)-1)/2,frame.Y+project.Height(row)-2,Color.Blue);
                    }
                }
                Check(project.Selection(0,project.Data.Frames[0][0]).Size==project.Selection(0,project.Data.Frames[0][1]).Size,"All frames in a cycle share a selection size");
                Check(project.Width(0)!=project.Width(1)&&project.Height(0)!=project.Height(1),"Different animation types have independent dimensions");
                for(int row=0;row<10;row++)project.MagicTweak(row);
                using(var built=project.Build())
                {
                    Check(built.Cell==new Size(40,53),"Different-sized cycles export into the maximum cell without stretching");
                    int[] facing={6,2,4,5,3};
                    for(int row=0;row<10;row++)for(int slot=0;slot<2;slot++)
                        Check(SpriteProject.GroundPoint(built.Frame(row>=5,facing[row%5],slot))==new Point(19,52),"Odd/even cycle dimensions retain the same bottom-center ground pixel");
                }
                // Both signs and both axes: only pixels inside the cycle's output rectangle survive.
                foreach(var offset in new[]{new Point(-7,5),new Point(6,-8),new Point(4096,0)})
                {
                    var frame=project.Data.Frames[2][0];frame.OffsetX=offset.X;frame.OffsetY=offset.Y;
                    using(var rendered=project.RenderFrame(2,0))
                    {
                        bool exact=true;
                        for(int y=0;y<rendered.Height;y++)for(int x=0;x<rendered.Width;x++)
                        {
                            int sx=x-offset.X,sy=y-offset.Y;
                            int expected=sx<0||sy<0||sx>=rendered.Width||sy>=rendered.Height?0:project.Source.GetPixel(frame.X+sx,frame.Y+sy).ToArgb();
                            if(rendered.GetPixel(x,y).ToArgb()!=expected)exact=false;
                        }
                        Check(exact,"Offsets crop exact pixels without leaking outside the frame: "+offset);
                        using(var built=project.Build())
                        {
                            var runtime=built.Frame(false,4,0);int padX=(runtime.Width-1)/2-(rendered.Width-1)/2,padY=runtime.Height-rendered.Height;bool padding=true;
                            for(int y=0;y<runtime.Height;y++)for(int x=0;x<runtime.Width;x++)
                            {int expected=x>=padX&&x<padX+rendered.Width&&y>=padY?rendered.GetPixel(x-padX,y-padY).ToArgb():0;if(runtime.GetPixel(x,y).ToArgb()!=expected)padding=false;}
                            Check(padding,"Runtime padding cannot restore clipped pixels");
                        }
                    }
                }
                string path=Path.Combine(artifacts,"cycle-sizes.vpetproject");project.Save(path);
                using(var loaded=SpriteProject.Load(path))
                {
                    for(int row=0;row<10;row++)Check(loaded.Width(row)==project.Width(row)&&loaded.Height(row)==project.Height(row),"Cycle dimensions survive project save/load");
                    Check(loaded.Data.Frames[2][0].OffsetX==4096,"Fully clipped alignment survives project save");
                    using(var built=loaded.Build())
                    {
                        string sprite=Path.Combine(artifacts,"clipped.vpetsprite");built.SavePackage(sprite);
                        using(var imported=SpriteSet.Import(sprite))Check(SpriteProject.VisibleBounds(imported.Frame(false,4,0)).IsEmpty,"A fully clipped animation frame remains blank in saved runtime playback");
                    }
                    loaded.Data.CycleWidths[0]=101;Reject(delegate{loaded.Save(path);},"Oversized animation width is rejected before overwriting a project");
                }
            }
            using(var legacy=MakerFixture())
            {
                string path=Path.Combine(artifacts,"legacy-size.vpetproject");
                var legacyData=new SpriteManifest{Kind="project",Width=legacy.Data.Width,Height=legacy.Data.Height,Frames=legacy.Data.Frames.Take(10).Select(row=>row.Take(5).ToArray()).ToArray()};
                SpritePackage.Write(path,legacyData,legacy.Source);
                using(var loaded=SpriteProject.Load(path))
                {
                    Check(loaded.Data.Version==1&&Enumerable.Range(0,10).All(row=>loaded.Width(row)==20&&loaded.Height(row)==24),"Version 1 projects retain their original shared size on every cycle");
                    loaded.SetSize(2,25,26);loaded.Save(path);
                }
                using(var loaded=SpriteProject.Load(path))Check(loaded.Data.Version==SpritePackage.CurrentVersion&&loaded.Width(2)==25&&loaded.Width(0)==20,"Legacy project upgrades without changing other cycles");
                using(var built=legacy.Build())
                {
                    path=Path.Combine(artifacts,"legacy-sprite.vpetsprite");using(var oldAtlas=built.Sheet.Clone(new Rectangle(0,0,built.Cell.Width*5,built.Sheet.Height),System.Drawing.Imaging.PixelFormat.Format32bppArgb))SpritePackage.Write(path,new SpriteManifest{Kind="sprite",Width=built.Cell.Width,Height=built.Cell.Height,Counts=built.Counts},oldAtlas);
                    using(var loaded=SpriteSet.Import(path))Check(loaded.Cell==built.Cell,"Version 1 runtime sprites remain supported");
                }
            }
        }
        static void AnimationFrameWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();
                maker.ChooseCycle(0);maker.SetDimensions(18,22);maker.ChooseCycle(2);maker.SetDimensions(21,25);
                Check(maker.Project.Width(0)==18&&maker.Project.Height(0)==22&&maker.Project.Width(2)==21,"Editing side animation dimensions leaves Idle Up unchanged");
                maker.ChooseCycle(0);Check(MakerField<NumericUpDown>(maker,"frameWidth").Value==18&&MakerField<NumericUpDown>(maker,"frameHeight").Value==22,"Cycle selection restores its own number fields");
                maker.ChooseSlot(0);maker.SetDimensions(19,23);Check(maker.Project.Width(2)==21,"Editing another slot still leaves other animation sizes intact");
                maker.Project.Data.Frames[0][0].OffsetX=25;
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();FindButton(tweak,"Tweak").PerformClick();
                    var preview=MakerField<TweakPreview>(tweak,"preview");var zoom=MakerField<PreviewZoomBar>(tweak,"zoom");zoom.SetPercent(400,null);
                    using(var image=new Bitmap(preview.Width,preview.Height))
                    {
                        preview.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));int colored=0;
                        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,y);if(c.R==70&&c.G==50&&c.B==150)colored++;}
                        Check(colored==0,"Tweak preview cuts fully shifted source art out of the visible animation");
                    }
                    typeof(SpriteTweakWindow).GetMethod("SelectCycle",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(tweak,new object[]{2});
                    Check(preview.Cycle==2&&preview.ImageOrigin.X==(preview.Width-21*preview.Zoom)/2,"Tweak preview switches to the selected cycle's own frame dimensions");
                    using(var image=new Bitmap(tweak.Width,tweak.Height)){tweak.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(artifacts,"animation-sizes.png"));}
                    tweak.Close();
                }
                maker.Dirty=false;maker.Close();
            }
        }
    }
}
