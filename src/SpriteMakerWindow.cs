using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static class MakerUi
    {
        public static readonly Color Purple=Color.FromArgb(66,46,105);
        public static FlowLayoutPanel Flow(){return new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Padding=new Padding(4)};}
        public static Button Button(string text,EventHandler click)
        {
            var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(90,34),Margin=new Padding(4),FlatStyle=FlatStyle.Flat,BackColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(190,179,208);if(click!=null)b.Click+=click;return b;
        }
        public static Label Label(string text){return new Label{Text=text,AutoSize=true,Margin=new Padding(8,11,4,4)};}
        public static TableLayoutPanel ChoiceSection(string name,string title,string description)
        {
            var section=new TableLayoutPanel{Name=name,Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,RowCount=2};
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.Controls.Add(new Label{Name=name+"Title",Text=title,AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Purple,Margin=new Padding(8,6,4,2)},0,0);
            var help=new Label{Name=name+"Description",Text=description,AutoSize=true,Margin=new Padding(8,0,4,4)};section.Controls.Add(help,0,1);
            section.SizeChanged+=delegate{var size=new Size(Math.Max(100,section.ClientSize.Width-16),0);if(help.MaximumSize!=size)help.MaximumSize=size;};
            return section;
        }
        public static void ChoiceRow(TableLayoutPanel section,string name,params Control[] controls)
        {
            var row=Flow();row.Name=name;
            foreach(var control in controls)if(control!=null)row.Controls.Add(control);
            row.Visible=row.Controls.Count>0;int index=section.RowCount++;section.RowStyles.Add(new RowStyle(SizeType.AutoSize));section.Controls.Add(row,0,index);
        }
        public static TableLayoutPanel AnimationChoices(Button[] buttons,string description)
        {
            var section=ChoiceSection("AnimationTypes","Animation types",description);
            ChoiceRow(section,"IdleAnimations",buttons.Take(5).ToArray());
            ChoiceRow(section,"WalkAnimations",buttons.Skip(5).Take(5).ToArray());
            ChoiceRow(section,"EmoteAnimations",buttons.Skip(10).ToArray());return section;
        }
        public static void Error(IWin32Window owner,Exception ex){MessageBox.Show(owner,ex.Message,"Sprite Maker",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        public static void Checker(Graphics g,Rectangle clip,int step)
        {
            g.FillRectangle(Brushes.White,clip);
            using(var brush=new SolidBrush(Color.FromArgb(227,223,235)))
                for(int y=clip.Top/step*step;y<clip.Bottom;y+=step)for(int x=clip.Left/step*step;x<clip.Right;x+=step)if(((x/step+y/step)&1)==0)g.FillRectangle(brush,x,y,step,step);
        }
    }
    internal sealed class SpriteMakerWindow : Form
    {
        internal SpriteProject Project {get;private set;}
        internal int Cycle,Slot;
        internal bool Dirty;
        string projectPath;
        readonly Preferences preferences;
        readonly Action savePreferences;
        readonly Button[] cycles=new Button[SpriteProject.TotalCycles],slots=new Button[SpriteProject.MaximumFrames];
        readonly CheckBox emotes=new CheckBox{Text="Emote Animations (optional)",AutoSize=true,Margin=new Padding(10)};
        readonly ComboBox facing=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=125,Margin=new Padding(4,8,4,4)};
        readonly CheckBox diagonal=new CheckBox{Text="Diagonal animations",AutoSize=true,Checked=true,Margin=new Padding(10)};
        readonly NumericUpDown frameWidth=new NumericUpDown{Minimum=1,Maximum=100,Value=32,Width=64,Margin=new Padding(4,8,4,4)};
        readonly NumericUpDown frameHeight=new NumericUpDown{Minimum=1,Maximum=150,Value=36,Width=64,Margin=new Padding(4,8,4,4)};
        readonly PreviewZoomBar zoom=new PreviewZoomBar();
        readonly SpriteSheetViewport viewport=new SpriteSheetViewport{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(220,216,229),BorderStyle=BorderStyle.FixedSingle};
        readonly TextBox status=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(248,247,252)};
        readonly Label selectionHelp=MakerUi.Label("Upload a transparent PNG to begin.");
        readonly TableLayoutPanel animationChoices;
        readonly Panel controlScroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
        readonly SpriteSheetView sheet=new SpriteSheetView();
        readonly SplitContainer editorSplit=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterWidth=8,FixedPanel=FixedPanel.Panel2,Size=new Size(960,350),Panel1MinSize=80,Panel2MinSize=45,SplitterDistance=242};
        readonly SplitContainer workspaceSplit=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterWidth=8,FixedPanel=FixedPanel.Panel1,Size=new Size(960,760),Panel1MinSize=100,Panel2MinSize=190,SplitterDistance=450};
        readonly Button complete,loadLast,updateSheet;
        bool syncing;
        public string ExportedPath {get;private set;}
        public SpriteMakerWindow(Preferences preferences=null,Action savePreferences=null)
        {
            this.preferences=preferences??new Preferences();this.savePreferences=savePreferences;
            Text="Vpet Sprite Maker";Font=new Font("Segoe UI",10);ClientSize=new Size(1000,800);MinimumSize=new Size(800,650);
            StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(248,247,252);AutoScaleMode=AutoScaleMode.Dpi;
            var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(12)};outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(outer);outer.Controls.Add(workspaceSplit,0,0);
            workspaceSplit.Panel1.Controls.Add(controlScroll);
            var root=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,RowCount=4};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            for(int i=0;i<4;i++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));controlScroll.Controls.Add(root);
            var commands=MakerUi.Flow();root.Controls.Add(commands,0,0);
            commands.Controls.Add(MakerUi.Button("Upload Sprite Sheet",Upload));
            updateSheet=MakerUi.Button("Update Sprite Sheet",UpdateSheet);commands.Controls.Add(updateSheet);
            commands.Controls.Add(MakerUi.Button("Save Project",delegate{SaveProject(false);}));
            commands.Controls.Add(MakerUi.Button("Save Project As…",delegate{SaveProject(true);}));
            commands.Controls.Add(MakerUi.Button("Load Project",LoadProject));
            loadLast=MakerUi.Button("Load Last Project",delegate{OpenProject(this.preferences.LastSpriteProject);});commands.Controls.Add(loadLast);
            var bottom=MakerUi.Flow();bottom.FlowDirection=FlowDirection.RightToLeft;outer.Controls.Add(bottom,0,1);
            complete=MakerUi.Button("Tweak and Complete",OpenTweak);bottom.Controls.Add(complete);
            bottom.Controls.Add(MakerUi.Button("How to Guide",delegate{MakerGuide.Show(this,false);}));
            var options=MakerUi.Flow();root.Controls.Add(options,0,1);
            options.Controls.Add(diagonal);options.Controls.Add(MakerUi.Label("Animation frame width"));options.Controls.Add(frameWidth);options.Controls.Add(MakerUi.Label("Height"));options.Controls.Add(frameHeight);
            options.Controls.Add(MakerUi.Label("Sheet faces"));facing.Items.AddRange(new object[]{"Left","Right"});facing.SelectedIndex=0;options.Controls.Add(facing);options.Controls.Add(emotes);
            for(int i=0;i<cycles.Length;i++){int index=i;cycles[i]=MakerUi.Button(SpriteProject.Cycles[i],delegate{ChooseCycle(index);});}
            animationChoices=MakerUi.AnimationChoices(cycles,"Choose the animation you want to edit.");root.Controls.Add(animationChoices,0,2);
            var frameChoices=MakerUi.ChoiceSection("AnimationFrames","Animation frames","Set each frame in the chosen animation's cycle, in playback order.");root.Controls.Add(frameChoices,0,3);
            for(int i=0;i<slots.Length;i++){int index=i;slots[i]=MakerUi.Button((i+1).ToString(),delegate{ChooseSlot(index);});slots[i].MinimumSize=new Size(48,34);}
            MakerUi.ChoiceRow(frameChoices,"FramesOneToFive",slots.Take(5).ToArray());MakerUi.ChoiceRow(frameChoices,"FramesSixToTen",slots.Skip(5).ToArray());
            MakerUi.ChoiceRow(frameChoices,"FrameActions",MakerUi.Button("Set",delegate{SetFrame();}),MakerUi.Button("Clear",delegate{ClearFrame();}));
            MakerUi.ChoiceRow(frameChoices,"FrameSelectionHelp",selectionHelp);
            var preview=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};preview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));preview.RowStyles.Add(new RowStyle(SizeType.AutoSize));preview.RowStyles.Add(new RowStyle(SizeType.Percent,100));workspaceSplit.Panel2.Controls.Add(preview);
            preview.Controls.Add(zoom,0,0);viewport.Controls.Add(sheet);editorSplit.Panel1.Controls.Add(viewport);editorSplit.Panel2.Controls.Add(status);preview.Controls.Add(editorSplit,0,1);
            workspaceSplit.BackColor=editorSplit.BackColor=Color.FromArgb(213,204,226);
            workspaceSplit.Panel1.BackColor=workspaceSplit.Panel2.BackColor=BackColor;editorSplit.Panel2.BackColor=BackColor;
            sheet.DimensionsChanged+=delegate(int w,int h){SetDimensions(w,h);};
            frameWidth.ValueChanged+=delegate{if(!syncing)SetDimensions((int)frameWidth.Value,(int)frameHeight.Value);};frameHeight.ValueChanged+=delegate{if(!syncing)SetDimensions((int)frameWidth.Value,(int)frameHeight.Value);};
            diagonal.CheckedChanged+=delegate{if(Project!=null&&!syncing){Project.Data.Diagonals=diagonal.Checked;Dirty=true;if(!Project.Enabled(Cycle))Cycle=0;ChooseCycle(Cycle);}};
            emotes.CheckedChanged+=delegate{if(Project!=null&&!syncing){Project.Data.EmoteAnimations=emotes.Checked;Dirty=true;if(!Project.Enabled(Cycle))Cycle=0;ChooseCycle(Cycle);}};
            facing.SelectedIndexChanged+=delegate{if(Project!=null&&!syncing){Project.Data.FacesRight=facing.SelectedIndex==1;Dirty=true;RefreshState();}};
            zoom.ZoomChanged+=delegate(int value,Point? anchor){viewport.ChangeZoom(sheet,sheet.Zoom,value/100f,PointF.Empty,delegate{sheet.Zoom=value/100f;},delegate{return PointF.Empty;},anchor);};
            zoom.FitRequested+=delegate{if(Project!=null)zoom.SetPercent((int)Math.Floor(Math.Min((viewport.ClientSize.Width-20f)/Project.Source.Width,(viewport.ClientSize.Height-20f)/Project.Source.Height)*100),null);};
            sheet.ZoomWheel+=delegate(int delta,Point point){zoom.Step(delta,viewport.PointToClient(sheet.PointToScreen(point)));};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!ConfirmDiscard())e.Cancel=true;};
            FormClosed+=delegate{if(Project!=null)Project.Dispose();};RefreshState();
        }
        internal void SetProject(SpriteProject project,string path)
        {
            if(Project!=null)Project.Dispose();Project=project;projectPath=path;Dirty=false;Cycle=Slot=0;sheet.Project=project;
            syncing=true;diagonal.Checked=project.Data.Diagonals;emotes.Checked=project.Data.EmoteAnimations;facing.SelectedIndex=project.Data.FacesRight?1:0;frameWidth.Value=project.Width(0);frameHeight.Value=project.Height(0);syncing=false;
            zoom.MaximumPercent=Math.Min(1600,3000000/Math.Max(project.Source.Width,project.Source.Height));
            RememberProject(path);ChooseCycle(0);sheet.RefreshSize();zoom.SetPercent(zoom.Percent,null);
        }
        void Upload(object sender,EventArgs e)
        {
            using(var dialog=new OpenFileDialog{Filter="Transparent PNG sheet|*.png",Title="Upload a sprite sheet (up to 4096 × 4096)"})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try{var project=SpriteProject.FromPng(dialog.FileName);if(!ConfirmDiscard()){project.Dispose();return;}SetProject(project,null);Dirty=true;}
                catch(Exception ex){MakerUi.Error(this,ex);}
            }
        }
        void LoadProject(object sender,EventArgs e)
        {
            using(var dialog=new OpenFileDialog{Filter="Vpet project|*.vpetproject"})if(dialog.ShowDialog(this)==DialogResult.OK)
                OpenProject(dialog.FileName);
        }
        void UpdateSheet(object sender,EventArgs e)
        {
            if(Project==null)return;
            using(var dialog=new OpenFileDialog{Filter="Transparent PNG sheet|*.png",Title="Update sprite sheet (keep animation frames)"})
                if(dialog.ShowDialog(this)==DialogResult.OK)UpdateSpriteSheet(dialog.FileName);
        }
        internal bool UpdateSpriteSheet(string path)
        {
            if(Project==null)return false;
            var sourceScroll=new PointF(-viewport.AutoScrollPosition.X/sheet.Zoom,-viewport.AutoScrollPosition.Y/sheet.Zoom);
            try
            {
                Project.ReplaceSource(path);Dirty=true;ExportedPath=null;
                zoom.MaximumPercent=Math.Min(1600,3000000/Math.Max(Project.Source.Width,Project.Source.Height));
                sheet.RefreshSize();zoom.SetPercent(zoom.Percent,null);RefreshState();
                viewport.AutoScrollPosition=new Point((int)Math.Round(sourceScroll.X*sheet.Zoom),(int)Math.Round(sourceScroll.Y*sheet.Zoom));
                status.Text="Sprite sheet updated. Frame mappings and tweaks were kept. Save Project to keep the new sheet."+Environment.NewLine+status.Text;
                return true;
            }
            catch(Exception ex)
            {
                status.ForeColor=Color.Firebrick;status.Text="Could not update the sprite sheet. "+ex.Message;return false;
            }
        }
        void RememberProject(string path)
        {
            if(string.IsNullOrWhiteSpace(path))return;
            preferences.LastSpriteProject=Path.GetFullPath(path);
            if(savePreferences!=null)savePreferences();
        }
        internal bool OpenProject(string path)
        {
            // Save/cancel first, so reopening the same file reads any changes just saved by the user.
            if(!ConfirmDiscard())return false;
            try
            {
                var project=SpriteProject.Load(path);SetProject(project,path);return true;
            }
            catch(Exception ex)
            {
                status.ForeColor=Color.Firebrick;
                status.Text="Could not open the project. "+(Project==null?"":"Your current work is still open. ")+"If the file was moved, use Load Project to find it."+Environment.NewLine+ex.Message;
                return false;
            }
        }
        internal bool SaveProject(bool choosePath)
        {
            if(Project==null)return false;
            string path=projectPath;
            if(choosePath||string.IsNullOrEmpty(path))using(var dialog=new SaveFileDialog{Filter="Vpet project|*.vpetproject",DefaultExt="vpetproject",FileName=string.IsNullOrEmpty(path)?"my-pet.vpetproject":Path.GetFileName(path)})
            {if(dialog.ShowDialog(this)!=DialogResult.OK)return false;path=dialog.FileName;}
            try{Project.Save(path);projectPath=path;Dirty=false;RememberProject(path);RefreshState();return true;}catch(Exception ex){MakerUi.Error(this,ex);return false;}
        }
        bool ConfirmDiscard()
        {
            if(!Dirty)return true;
            var choice=MessageBox.Show(this,"Save changes to your Sprite Maker project?","Sprite Maker",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
            return choice==DialogResult.No||(choice==DialogResult.Yes&&SaveProject(false));
        }
        internal void ChooseCycle(int row){Cycle=row;if(Project!=null){syncing=true;frameWidth.Value=Project.Width(row);frameHeight.Value=Project.Height(row);syncing=false;}ChooseSlot(0);}
        internal void ChooseSlot(int slot)
        {Slot=slot;sheet.Cycle=Cycle;sheet.Slot=Slot;sheet.Draft=Project==null?null:Project.Data.Frames[Cycle][Slot]==null?null:Project.Data.Frames[Cycle][Slot].Copy();RefreshState();}
        internal void SetDimensions(int w,int h)
        {
            if(Project==null)return;
            Project.SetSize(Cycle,w,h);
            syncing=true;frameWidth.Value=Project.Width(Cycle);frameHeight.Value=Project.Height(Cycle);syncing=false;Dirty=true;RefreshState();
        }
        internal void SetFrame()
        {
            if(Project==null)return;
            string problem=Project.FrameProblem(Cycle,sheet.Draft,false);
            if(problem!=null){MakerUi.Error(this,new InvalidDataException("Select a valid frame: "+problem));return;}
            Project.Data.Frames[Cycle][Slot]=sheet.Draft.Copy();Dirty=true;ChooseSlot(Math.Min(SpriteProject.MaximumFrames-1,Slot+1));
        }
        internal void ClearFrame(){if(Project==null)return;Project.Data.Frames[Cycle][Slot]=null;sheet.Draft=null;Dirty=true;RefreshState();}
        internal void RefreshState()
        {
            loadLast.Enabled=!string.IsNullOrWhiteSpace(preferences.LastSpriteProject);
            updateSheet.Enabled=Project!=null;
            for(int i=0;i<cycles.Length;i++){cycles[i].Visible=Project==null?i<10&&i%5<3:Project.Enabled(i);cycles[i].BackColor=i==Cycle?Color.FromArgb(221,211,241):Color.White;}
            animationChoices.Controls["EmoteAnimations"].Visible=Project!=null&&Project.Data.EmoteAnimations;
            diagonal.Enabled=emotes.Enabled=facing.Enabled=Project!=null;
            for(int i=0;i<slots.Length;i++){bool saved=Project!=null&&Project.Data.Frames[Cycle][i]!=null;slots[i].Text=(i+1)+(saved?" ✓":"");slots[i].ForeColor=saved?Color.DarkGreen:Color.Black;slots[i].BackColor=i==Slot?Color.FromArgb(221,211,241):Color.White;}
            selectionHelp.Text=Project==null?"Upload a transparent PNG to begin.":"Frame "+(Slot+1)+": drag border to move; corners resize all "+SpriteProject.Cycles[Cycle]+" frames.";
            complete.Enabled=false;
            if(Project==null)status.Text="Upload a sheet or load a saved project. Use the scrollbars to move around larger sheets."+Environment.NewLine+
                (loadLast.Enabled?"Load Last Project reopens your most recently opened or saved project.":"Open or save a project to enable Load Last Project.");
            else
            {
                var problems=Project.Problems(false);complete.Enabled=problems.Count==0;
                status.ForeColor=problems.Count==0?Color.DarkGreen:Color.Firebrick;
                status.Text=problems.Count==0?"✓ All required animations are saved. Open Tweak and Complete to preview, align, and export.":string.Join(Environment.NewLine,problems);
                if(Project.Slots(Cycle).Length>0&&!problems.Any(p=>p.StartsWith(SpriteProject.Cycles[Cycle]+":")||p.StartsWith(SpriteProject.Cycles[Cycle]+",")))status.Text="✓ "+SpriteProject.Cycles[Cycle]+" saved ("+Project.Slots(Cycle).Length+" frame(s))."+Environment.NewLine+status.Text;
            }
            Text="Vpet Sprite Maker"+(Dirty?" *":"");sheet.Invalidate();
        }
        void OpenTweak(object sender,EventArgs e)
        {
            if(Project==null||Project.Problems(false).Count>0)return;
            using(var window=new SpriteTweakWindow(this))
            {window.Icon=Icon;window.ShowDialog(this);if(window.ExportedPath!=null)ExportedPath=window.ExportedPath;}RefreshState();
        }
    }
    internal sealed class SpriteSheetViewport : Panel
    {
        // Focusing the large canvas must not scroll its top-left into view.
        // Scrollbars and the mouse wheel still control the viewport normally.
        protected override Point ScrollToControl(Control activeControl){return DisplayRectangle.Location;}
        internal void ChangeZoom(Control content,float oldZoom,float newZoom,PointF oldOrigin,Action resize,Func<PointF> newOrigin,Point? pointer)
        {
            var anchor=pointer??new Point(ClientSize.Width/2,ClientSize.Height/2);
            var pixel=new PointF((anchor.X-content.Left-oldOrigin.X)/oldZoom,(anchor.Y-content.Top-oldOrigin.Y)/oldZoom);
            resize();PerformLayout();var origin=newOrigin();
            AutoScrollPosition=new Point(Math.Max(0,(int)Math.Round(pixel.X*newZoom+origin.X-anchor.X)),Math.Max(0,(int)Math.Round(pixel.Y*newZoom+origin.Y-anchor.Y)));
        }
    }
    internal sealed class SpriteSheetView : Control
    {
        public SpriteProject Project;
        public SpriteFrame Draft;
        public int Cycle,Slot;
        public event Action<int,int> DimensionsChanged;
        public event Action<int,Point> ZoomWheel;
        float zoom=1;
        int corner=-1;
        bool moving;
        Point fixedCorner,dragOrigin,frameOrigin;
        public float Zoom {get{return zoom;}set{zoom=value;RefreshSize();}}
        public SpriteSheetView(){DoubleBuffered=true;Size=new Size(640,350);Cursor=Cursors.Cross;}
        protected override void ScaleControl(SizeF factor,BoundsSpecified specified)
        {
            // Zoom is measured in source pixels. DPI scaling must not independently
            // resize the image while selection coordinates still use the old zoom.
            base.ScaleControl(factor,specified&~BoundsSpecified.Size);RefreshSize();
        }
        public void RefreshSize()
        {
            var size=Project==null?new Size(640,350):new Size((int)Math.Ceiling(Project.Source.Width*zoom),(int)Math.Ceiling(Project.Source.Height*zoom));
            // A zoomed canvas can be much larger than the visible viewport.
            // Avoid allocating a multi-gigabyte WinForms backing bitmap.
            DoubleBuffered=(long)size.Width*size.Height<=16000000;Size=size;Invalidate();
        }
        Point ImagePoint(Point point){return new Point(Math.Max(0,(int)Math.Floor(point.X/zoom)),Math.Max(0,(int)Math.Floor(point.Y/zoom)));}
        int HitCorner(Point point)
        {
            if(Draft==null||Project==null)return -1;
            var r=Project.Selection(Cycle,Draft);Point[] points={r.Location,new Point(r.Right,r.Top),new Point(r.Left,r.Bottom),new Point(r.Right,r.Bottom)};
            // On small zoomed frames, only the visible handle counts as a corner.
            float reach=Math.Min(7,Math.Min(r.Width,r.Height)*zoom/4);
            for(int i=0;i<4;i++)if(Math.Abs(point.X-points[i].X*zoom)<=reach&&Math.Abs(point.Y-points[i].Y*zoom)<=reach)return i;
            return -1;
        }
        bool HitBorder(Point point)
        {
            if(Draft==null||Project==null)return false;
            var r=Project.Selection(Cycle,Draft);float left=r.Left*zoom,top=r.Top*zoom,right=r.Right*zoom,bottom=r.Bottom*zoom;
            return (point.X>=left-5&&point.X<=right+5&&(Math.Abs(point.Y-top)<=5||Math.Abs(point.Y-bottom)<=5))||
                (point.Y>=top-5&&point.Y<=bottom+5&&(Math.Abs(point.X-left)<=5||Math.Abs(point.X-right)<=5));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(Project==null||e.Button!=MouseButtons.Left)return;Focus();corner=HitCorner(e.Location);moving=false;
            if(Draft!=null)
            {
                var rect=Project.Selection(Cycle,Draft);Point[] points={rect.Location,new Point(rect.Right,rect.Top),new Point(rect.Left,rect.Bottom),new Point(rect.Right,rect.Bottom)};
                if(corner>=0){fixedCorner=points[3-corner];Capture=true;return;}
                if(HitBorder(e.Location)){moving=true;dragOrigin=e.Location;frameOrigin=new Point(Draft.X,Draft.Y);Capture=true;Cursor=Cursors.SizeAll;return;}
            }
            var p=ImagePoint(e.Location);Draft=new SpriteFrame{X=p.X,Y=p.Y};Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(Project==null)return;
            if(moving&&Capture)
            {
                Draft.X=Math.Max(0,Math.Min(Math.Max(0,Project.Source.Width-Project.Width(Cycle)),frameOrigin.X+(int)Math.Round((e.X-dragOrigin.X)/zoom)));
                Draft.Y=Math.Max(0,Math.Min(Math.Max(0,Project.Source.Height-Project.Height(Cycle)),frameOrigin.Y+(int)Math.Round((e.Y-dragOrigin.Y)/zoom)));
                Invalidate();return;
            }
            if(corner<0||!Capture){int hit=HitCorner(e.Location);Cursor=hit>=0?(hit==0||hit==3?Cursors.SizeNWSE:Cursors.SizeNESW):HitBorder(e.Location)?Cursors.SizeAll:Cursors.Cross;return;}
            var p=ImagePoint(e.Location);int w=Math.Max(1,Math.Min(100,Math.Abs(p.X-fixedCorner.X))),h=Math.Max(1,Math.Min(150,Math.Abs(p.Y-fixedCorner.Y)));
            Draft.X=(corner==0||corner==2)?Math.Max(0,fixedCorner.X-w):fixedCorner.X;
            Draft.Y=corner<2?Math.Max(0,fixedCorner.Y-h):fixedCorner.Y;
            if(DimensionsChanged!=null)DimensionsChanged(w,h);Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;corner=-1;moving=false;Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){corner=-1;moving=false;}}
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if((ModifierKeys&Keys.Control)!=0)
            {if(!Capture&&e.Delta!=0&&ZoomWheel!=null)ZoomWheel(e.Delta,e.Location);var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;return;}
            base.OnMouseWheel(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle clip=e.ClipRectangle;
            if(Parent!=null)clip=Rectangle.Intersect(clip,new Rectangle(-Left,-Top,Parent.ClientSize.Width,Parent.ClientSize.Height));
            if(clip.Width<=0||clip.Height<=0)return;
            e.Graphics.SetClip(clip,CombineMode.Intersect);MakerUi.Checker(e.Graphics,clip,16);if(Project==null)return;
            e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
            e.Graphics.DrawImage(Project.Source,new RectangleF(0,0,Project.Source.Width*zoom,Project.Source.Height*zoom),new RectangleF(0,0,Project.Source.Width,Project.Source.Height),GraphicsUnit.Pixel);
            using(var thin=new Pen(Color.FromArgb(140,Color.Red),1))for(int i=0;i<SpriteProject.MaximumFrames;i++)if(i!=Slot&&Project.Data.Frames[Cycle][i]!=null)DrawBox(e.Graphics,Project.Selection(Cycle,Project.Data.Frames[Cycle][i]),thin,false);
            if(Draft!=null)using(var pen=new Pen(Color.Red,2))DrawBox(e.Graphics,Project.Selection(Cycle,Draft),pen,true);
        }
        protected override void OnPaintBackground(PaintEventArgs e){} // OnPaint fills the visible checkerboard.
        void DrawBox(Graphics g,Rectangle rect,Pen pen,bool handles)
        {
            var r=new RectangleF(rect.X*zoom,rect.Y*zoom,rect.Width*zoom,rect.Height*zoom);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);
            if(handles)foreach(var p in new[]{new PointF(r.Left,r.Top),new PointF(r.Right,r.Top),new PointF(r.Left,r.Bottom),new PointF(r.Right,r.Bottom)})g.FillRectangle(Brushes.Red,p.X-4,p.Y-4,8,8);
        }
    }
}
