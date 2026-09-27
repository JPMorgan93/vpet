using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class SpriteTweakWindow : Form
    {
        readonly SpriteMakerWindow maker;
        readonly SpriteProject project;
        readonly Timer timer=new Timer{Interval=40};
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly FlowLayoutPanel tweaks=MakerUi.Flow();
        readonly TrackBar slider=new TrackBar{Dock=DockStyle.Fill,Minimum=0,Maximum=0,TickFrequency=1,SmallChange=1,LargeChange=1};
        readonly Label status=new Label{Dock=DockStyle.Fill,AutoSize=false};
        readonly Label frameLabel=MakerUi.Label("");
        readonly TweakPreview preview;
        readonly Button tweak,undo;
        readonly Button[] cycles=new Button[10];
        readonly System.Collections.Generic.Stack<SpriteFrame[][]> history=new System.Collections.Generic.Stack<SpriteFrame[][]>();
        int cycle;
        int[] slots;
        bool tweaking;
        public string ExportedPath {get;private set;}
        public SpriteTweakWindow(SpriteMakerWindow maker)
        {
            this.maker=maker;project=maker.Project;Text="Tweak and Complete";Font=new Font("Segoe UI",10);ClientSize=new Size(880,750);MinimumSize=new Size(740,640);
            BackColor=Color.FromArgb(248,247,252);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Padding=new Padding(16)};
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Absolute,60));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
            var choices=MakerUi.Flow();root.Controls.Add(choices,0,0);
            for(int i=0;i<10;i++)if(project.Enabled(i)){int row=i;cycles[i]=MakerUi.Button(SpriteProject.Cycles[i],delegate{SelectCycle(row);});choices.Controls.Add(cycles[i]);}
            preview=new TweakPreview(project);preview.Dock=DockStyle.Fill;preview.BeforeNudge+=Remember;preview.Changed+=delegate{maker.Dirty=true;RefreshPreview();};root.Controls.Add(preview,0,1);
            root.Controls.Add(slider,0,2);slider.Enabled=false;slider.ValueChanged+=delegate{RefreshPreview();};
            var controls=MakerUi.Flow();root.Controls.Add(controls,0,3);tweak=MakerUi.Button("Tweak",delegate{ToggleTweak();});controls.Controls.Add(tweak);controls.Controls.Add(frameLabel);
            // Keep the preview's ground line still when controls/offset text change.
            tweak.MinimumSize=new Size(150,34);frameLabel.AutoSize=false;frameLabel.Size=new Size(380,28);
            root.Controls.Add(tweaks,0,4);tweaks.Enabled=false;
            tweaks.Controls.Add(MakerUi.Button("Magic Tweak",delegate{try{Remember();project.MagicTweak(cycle);maker.Dirty=true;RefreshPreview();}catch(Exception ex){history.Pop();MakerUi.Error(this,ex);}}));
            tweaks.Controls.Add(MakerUi.Button("Save Tweaks",delegate{if(maker.SaveProject(false))status.Text="Tweaks saved to your project.";}));
            undo=MakerUi.Button("Undo",delegate{if(history.Count>0){project.Data.Frames=history.Pop();maker.Dirty=true;RefreshPreview();}});tweaks.Controls.Add(undo);
            tweaks.Controls.Add(MakerUi.Button("Reset Cycle",delegate{Remember();foreach(int slot in slots){project.Data.Frames[cycle][slot].OffsetX=0;project.Data.Frames[cycle][slot].OffsetY=0;}maker.Dirty=true;RefreshPreview();}));
            root.Controls.Add(status,0,5);
            var bottom=MakerUi.Flow();bottom.FlowDirection=FlowDirection.RightToLeft;root.Controls.Add(bottom,0,6);
            bottom.Controls.Add(MakerUi.Button("Complete",Complete));bottom.Controls.Add(MakerUi.Button("Back to Sprite Maker",delegate{Close();}));
            timer.Tick+=delegate{if(!tweaking)RefreshPreview();};timer.Start();
            FormClosed+=delegate{timer.Dispose();};SelectCycle(0);
        }
        void SelectCycle(int row)
        {
            cycle=row;slots=project.Slots(row);slider.Value=0;slider.Maximum=Math.Max(0,slots.Length-1);slider.Enabled=tweaking&&slots.Length>1;clock.Restart();
            for(int i=0;i<10;i++)if(cycles[i]!=null)cycles[i].BackColor=i==row?Color.FromArgb(221,211,241):Color.White;RefreshPreview();
        }
        void ToggleTweak()
        {
            tweaking=!tweaking;slider.Value=0;slider.Enabled=tweaking&&slots.Length>1;tweaks.Enabled=tweaking;tweak.Text=tweaking?"Resume Preview":"Tweak";preview.Editing=tweaking;clock.Restart();RefreshPreview();if(tweaking)preview.Focus();
        }
        void Remember()
        {
            var copy=new SpriteFrame[10][];for(int row=0;row<10;row++){copy[row]=new SpriteFrame[5];for(int col=0;col<5;col++)if(project.Data.Frames[row][col]!=null)copy[row][col]=project.Data.Frames[row][col].Copy();}history.Push(copy);
        }
        void RefreshPreview()
        {
            if(slots==null||slots.Length==0)return;
            int index=tweaking?Math.Min(slider.Value,slots.Length-1):(int)(clock.Elapsed.TotalSeconds*(cycle<5?4:8))%slots.Length;
            preview.Cycle=cycle;preview.Slot=slots[index];preview.Invalidate();
            var frame=project.Data.Frames[cycle][slots[index]];frameLabel.Text="Frame "+(index+1)+" / "+slots.Length+" · slot "+(slots[index]+1)+(tweaking?" · offset "+frame.OffsetX+", "+frame.OffsetY:"");
            string problem=project.FrameProblem(frame,true);status.ForeColor=problem==null?Color.DarkGreen:Color.Firebrick;
            status.Text=problem==null?(tweaking?"Magic Tweak anchors each pose by its lowest visible pixels to the same ground point. Drag or use arrow keys to nudge (Shift = 5 px). Save Tweaks saves the project.":"Previewing "+SpriteProject.Cycles[cycle]+". Choose Tweak to adjust individual frames."):"Frame "+(slots[index]+1)+": "+problem+". Nudge it inside the frame, Undo, or use Magic Tweak.";
            undo.Enabled=history.Count>0;
        }
        void Complete(object sender,EventArgs e)
        {
            try
            {
                using(var sprite=project.Build())
                {
                    if(!maker.SaveProject(false))return;
                    using(var dialog=new SaveFileDialog{Filter="Vpet sprite|*.vpetsprite",DefaultExt="vpetsprite",FileName="my-pet.vpetsprite"})
                    {
                        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                        sprite.SavePackage(dialog.FileName);ExportedPath=dialog.FileName;
                        MessageBox.Show(this,"Project saved and sprite exported. Close Sprite Maker to preview it in Sprite settings, then choose Use this pet.","Sprite complete",MessageBoxButtons.OK,MessageBoxIcon.Information);Close();
                    }
                }
            }
            catch(Exception ex){MakerUi.Error(this,ex);}
        }
    }
    internal sealed class TweakPreview : Control
    {
        readonly SpriteProject project;
        public int Cycle,Slot;
        public bool Editing;
        public event Action BeforeNudge,Changed;
        bool dragging;
        Point origin,offset;
        float PreviewScale {get{return Math.Max(.25f,Math.Min(8,Math.Min((Width-50f)/project.Data.Width,(Height-50f)/project.Data.Height)));}}
        public TweakPreview(SpriteProject project){this.project=project;DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable,true);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(!Editing||e.Button!=MouseButtons.Left)return;
            Focus();if(BeforeNudge!=null)BeforeNudge();var frame=project.Data.Frames[Cycle][Slot];offset=new Point(frame.OffsetX,frame.OffsetY);origin=e.Location;dragging=true;Capture=true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!dragging)return;var frame=project.Data.Frames[Cycle][Slot];
            frame.OffsetX=Math.Max(-4096,Math.Min(4096,offset.X+(int)Math.Round((e.X-origin.X)/PreviewScale)));
            frame.OffsetY=Math.Max(-4096,Math.Min(4096,offset.Y+(int)Math.Round((e.Y-origin.Y)/PreviewScale)));if(Changed!=null)Changed();
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            Keys key=keyData&Keys.KeyCode;
            if(Editing&&(key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down))
            {
                if(BeforeNudge!=null)BeforeNudge();int step=(keyData&Keys.Shift)!=0?5:1;var frame=project.Data.Frames[Cycle][Slot];
                frame.OffsetX=Math.Max(-4096,Math.Min(4096,frame.OffsetX+(key==Keys.Left?-step:key==Keys.Right?step:0)));
                frame.OffsetY=Math.Max(-4096,Math.Min(4096,frame.OffsetY+(key==Keys.Up?-step:key==Keys.Down?step:0)));if(Changed!=null)Changed();return true;
            }
            return base.ProcessCmdKey(ref msg,keyData);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            MakerUi.Checker(e.Graphics,e.ClipRectangle,16);var frame=project.Data.Frames[Cycle][Slot];if(frame==null)return;
            float scale=PreviewScale,w=project.Data.Width*scale,h=project.Data.Height*scale,x=(Width-w)/2,y=(Height-h)/2;
            using(var brush=new SolidBrush(Color.FromArgb(90,255,255,255)))e.Graphics.FillRectangle(brush,x,y,w,h);
            using(var image=project.Source.Clone(project.Selection(frame),PixelFormat.Format32bppArgb))
            {e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(image,new RectangleF(x+frame.OffsetX*scale,y+frame.OffsetY*scale,w,h),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);}
            using(var pen=new Pen(project.FrameProblem(frame,true)==null?MakerUi.Purple:Color.Red,2))e.Graphics.DrawRectangle(pen,x,y,w,h);
            e.Graphics.DrawLine(Pens.Green,x,y+h,x+w,y+h);
            if(Editing)e.Graphics.DrawLine(Pens.Green,x+w/2,y+h-9,x+w/2,y+h+9);
        }
    }
}
