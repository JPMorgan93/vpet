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
        readonly PreviewZoomBar zoom=new PreviewZoomBar();
        readonly SpriteSheetViewport viewport=new SpriteSheetViewport{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(220,216,229)};
        readonly Button tweak,undo;
        readonly Button[] cycles=new Button[SpriteProject.TotalCycles];
        readonly System.Collections.Generic.Stack<SpriteFrame[][]> history=new System.Collections.Generic.Stack<SpriteFrame[][]>();
        int cycle;
        int[] slots;
        bool tweaking;
        bool syncingSpeed;
        readonly TrackBar animationSpeed=new TrackBar{Name="AnimationSpeed",Minimum=25,Maximum=300,Value=100,TickFrequency=25,Width=260,SmallChange=5,LargeChange=25};
        readonly Label speedLabel=MakerUi.Label("Animation speed: 1x");
        public string ExportedPath {get;private set;}
        public SpriteTweakWindow(SpriteMakerWindow maker)
        {
            this.maker=maker;project=maker.Project;Text="Tweak and Complete";Font=new Font("Segoe UI",10);ClientSize=new Size(880,750);MinimumSize=new Size(740,770);
            BackColor=Color.FromArgb(248,247,252);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Padding=new Padding(16)};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,166));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Absolute,54));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
            for(int i=0;i<cycles.Length;i++)if(project.Enabled(i)&&project.Slots(i).Length>0){int row=i;cycles[i]=MakerUi.Button(SpriteProject.Cycles[i],delegate{SelectCycle(row);});}
            var animationScroll=new Panel{Name="AnimationChoicesScroll",Dock=DockStyle.Fill,AutoScroll=true};
            animationScroll.Controls.Add(MakerUi.AnimationChoices(cycles,"Choose an animation, then use its controls to adjust the sprite the way you like."));root.Controls.Add(animationScroll,0,0);
            root.Controls.Add(zoom,0,1);var speedControls=MakerUi.Flow();speedControls.Controls.Add(speedLabel);speedControls.Controls.Add(animationSpeed);
            speedControls.Controls.Add(MakerUi.Button("Reset to 1x",delegate{animationSpeed.Value=100;}));root.Controls.Add(speedControls,0,2);
            animationSpeed.ValueChanged+=delegate
            {
                if(syncingSpeed)return;project.SetSpeed(cycle,animationSpeed.Value/100f);maker.Dirty=true;
                if(tweaking)ToggleTweak();clock.Restart();speedLabel.Text="Animation speed: "+project.Speed(cycle).ToString("0.##")+"x";RefreshPreview();
            };
            preview=new TweakPreview(project);preview.BeforeNudge+=Remember;preview.Changed+=delegate{maker.Dirty=true;RefreshPreview();};viewport.Controls.Add(preview);root.Controls.Add(viewport,0,3);
            viewport.Resize+=delegate{preview.RefreshSize(viewport.ClientSize);};
            zoom.ZoomChanged+=delegate(int value,Point? anchor){viewport.ChangeZoom(preview,preview.Zoom,value/100f,preview.ImageOrigin,delegate{preview.Zoom=value/100f;preview.RefreshSize(viewport.ClientSize);},delegate{return preview.ImageOrigin;},anchor);};
            zoom.FitRequested+=FitPreview;Shown+=delegate{FitPreview();};
            preview.ZoomWheel+=delegate(int delta,Point point){zoom.Step(delta,viewport.PointToClient(preview.PointToScreen(point)));};
            root.Controls.Add(slider,0,4);slider.Enabled=false;slider.ValueChanged+=delegate{RefreshPreview();};
            var controls=MakerUi.Flow();root.Controls.Add(controls,0,5);tweak=MakerUi.Button("Tweak",delegate{ToggleTweak();});controls.Controls.Add(tweak);controls.Controls.Add(frameLabel);
            // Keep the preview's ground line still when controls/offset text change.
            tweak.MinimumSize=new Size(150,34);frameLabel.AutoSize=false;frameLabel.Size=new Size(380,28);
            root.Controls.Add(tweaks,0,6);tweaks.Enabled=false;
            tweaks.Controls.Add(MakerUi.Button("Magic Tweak",delegate{try{Remember();project.MagicTweak(cycle);maker.Dirty=true;RefreshPreview();}catch(Exception ex){history.Pop();MakerUi.Error(this,ex);}}));
            tweaks.Controls.Add(MakerUi.Button("Save Tweaks",delegate{if(maker.SaveProject(false))status.Text="Tweaks saved to your project.";}));
            undo=MakerUi.Button("Undo",delegate{if(history.Count>0){project.Data.Frames=history.Pop();maker.Dirty=true;RefreshPreview();}});tweaks.Controls.Add(undo);
            tweaks.Controls.Add(MakerUi.Button("Reset Cycle",delegate{Remember();foreach(int slot in slots){project.Data.Frames[cycle][slot].OffsetX=0;project.Data.Frames[cycle][slot].OffsetY=0;}maker.Dirty=true;RefreshPreview();}));
            root.Controls.Add(status,0,7);
            var bottom=MakerUi.Flow();bottom.FlowDirection=FlowDirection.RightToLeft;root.Controls.Add(bottom,0,8);
            bottom.Controls.Add(MakerUi.Button("Complete",Complete));
            bottom.Controls.Add(MakerUi.Button("How to Guide",delegate{MakerGuide.Show(this,true);}));
            foreach(Control control in root.Controls){control.Margin=Padding.Empty;var flow=control as FlowLayoutPanel;if(flow!=null)flow.Padding=Padding.Empty;}
            timer.Tick+=delegate{if(!tweaking)RefreshPreview();};timer.Start();
            FormClosed+=delegate{timer.Dispose();};SelectCycle(0);
        }
        void FitPreview()
        {zoom.SetPercent((int)Math.Floor(Math.Min((viewport.ClientSize.Width-54f)/project.Width(cycle),(viewport.ClientSize.Height-54f)/project.Height(cycle))*100),null);}
        void SelectCycle(int row)
        {
            cycle=row;slots=project.Slots(row);slider.Value=0;slider.Maximum=Math.Max(0,slots.Length-1);slider.Enabled=tweaking&&slots.Length>1;clock.Restart();
            syncingSpeed=true;animationSpeed.Value=(int)Math.Round(project.Speed(row)*100);syncingSpeed=false;speedLabel.Text="Animation speed: "+project.Speed(row).ToString("0.##")+"x";
            for(int i=0;i<cycles.Length;i++)if(cycles[i]!=null)cycles[i].BackColor=i==row?Color.FromArgb(221,211,241):Color.White;RefreshPreview();
        }
        void ToggleTweak()
        {
            tweaking=!tweaking;slider.Value=0;slider.Enabled=tweaking&&slots.Length>1;tweaks.Enabled=tweaking;tweak.Text=tweaking?"Resume Preview":"Tweak";preview.Editing=tweaking;clock.Restart();RefreshPreview();if(tweaking)preview.Focus();
        }
        void Remember()
        {
            var copy=new SpriteFrame[SpriteProject.TotalCycles][];for(int row=0;row<copy.Length;row++){copy[row]=new SpriteFrame[SpriteProject.MaximumFrames];for(int col=0;col<SpriteProject.MaximumFrames;col++)if(project.Data.Frames[row][col]!=null)copy[row][col]=project.Data.Frames[row][col].Copy();}history.Push(copy);
        }
        void RefreshPreview()
        {
            if(slots==null||slots.Length==0)return;
            int index=tweaking?Math.Min(slider.Value,slots.Length-1):(int)(clock.Elapsed.TotalSeconds*(cycle>=10?6:cycle<5?4:8)*project.Speed(cycle))%slots.Length;
            bool differentCycle=preview.Cycle!=cycle;preview.Cycle=cycle;preview.Slot=slots[index];if(differentCycle)preview.RefreshSize(viewport.ClientSize);preview.Invalidate();
            var frame=project.Data.Frames[cycle][slots[index]];frameLabel.Text="Frame "+(index+1)+" / "+slots.Length+" · slot "+(slots[index]+1)+(tweaking?" · offset "+frame.OffsetX+", "+frame.OffsetY:"");
            string problem=project.FrameProblem(cycle,frame,true);status.ForeColor=problem==null?Color.DarkGreen:Color.Firebrick;
            status.Text=problem==null?(tweaking?"Drag or use arrow keys to nudge (Shift = 5 px). Anything outside the frame is cut off in the preview and export. Magic Tweak aligns the lowest pixels at bottom-center.":"Previewing "+SpriteProject.Cycles[cycle]+". Anything outside this animation's frame is cut off. Choose Tweak to adjust frames."):"Frame "+(slots[index]+1)+": "+problem+".";
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
        public event Action<int,Point> ZoomWheel;
        public float Zoom=1;
        bool dragging;
        Point origin,offset;
        internal PointF ImageOrigin {get{return new PointF((Width-project.Width(Cycle)*Zoom)/2,(Height-project.Height(Cycle)*Zoom)/2);}}
        internal void RefreshSize(Size viewport)
        {Size=new Size(Math.Max(viewport.Width,(int)Math.Ceiling(project.Width(Cycle)*Zoom)+50),Math.Max(viewport.Height,(int)Math.Ceiling(project.Height(Cycle)*Zoom)+50));Invalidate();}
        protected override void ScaleControl(SizeF factor,BoundsSpecified specified)
        {base.ScaleControl(factor,specified&~BoundsSpecified.Size);if(Parent!=null)RefreshSize(Parent.ClientSize);}
        public TweakPreview(SpriteProject project){this.project=project;DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable,true);}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(!Editing||e.Button!=MouseButtons.Left)return;
            Focus();if(BeforeNudge!=null)BeforeNudge();var frame=project.Data.Frames[Cycle][Slot];offset=new Point(frame.OffsetX,frame.OffsetY);origin=e.Location;dragging=true;Capture=true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!dragging)return;var frame=project.Data.Frames[Cycle][Slot];
            frame.OffsetX=Math.Max(-4096,Math.Min(4096,offset.X+(int)Math.Round((e.X-origin.X)/Zoom)));
            frame.OffsetY=Math.Max(-4096,Math.Min(4096,offset.Y+(int)Math.Round((e.Y-origin.Y)/Zoom)));if(Changed!=null)Changed();
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if((ModifierKeys&Keys.Control)!=0)
            {if(!Capture&&e.Delta!=0&&ZoomWheel!=null)ZoomWheel(e.Delta,e.Location);var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;return;}
            base.OnMouseWheel(e);
        }
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
            float scale=Zoom,w=project.Width(Cycle)*scale,h=project.Height(Cycle)*scale,x=ImageOrigin.X,y=ImageOrigin.Y;
            using(var brush=new SolidBrush(Color.FromArgb(90,255,255,255)))e.Graphics.FillRectangle(brush,x,y,w,h);
            using(var image=project.RenderFrame(Cycle,Slot))
            {e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(image,new RectangleF(x,y,w,h),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);}
            using(var pen=new Pen(MakerUi.Purple,2))e.Graphics.DrawRectangle(pen,x,y,w,h);
            e.Graphics.DrawLine(Pens.Green,x,y+h,x+w,y+h);
            if(Editing)e.Graphics.DrawLine(Pens.Green,x+w/2,y+h-9,x+w/2,y+h+9);
        }
    }
}
