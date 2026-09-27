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
        public static FlowLayoutPanel Flow(){return new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Padding=new Padding(4)};}
        public static Button Button(string text,EventHandler click)
        {
            var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(90,34),Margin=new Padding(4),FlatStyle=FlatStyle.Flat,BackColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(190,179,208);if(click!=null)b.Click+=click;return b;
        }
        public static Label Label(string text){return new Label{Text=text,AutoSize=true,Margin=new Padding(8,11,4,4)};}
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
        readonly Button[] cycles=new Button[10],slots=new Button[5];
        readonly CheckBox diagonal=new CheckBox{Text="Diagonal animations",AutoSize=true,Checked=true,Margin=new Padding(10)};
        readonly NumericUpDown frameWidth=new NumericUpDown{Minimum=1,Maximum=100,Value=32,Width=64,Margin=new Padding(4,8,4,4)};
        readonly NumericUpDown frameHeight=new NumericUpDown{Minimum=1,Maximum=150,Value=36,Width=64,Margin=new Padding(4,8,4,4)};
        readonly ComboBox zoom=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=82,Margin=new Padding(4,8,4,4)};
        readonly TextBox status=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(248,247,252)};
        readonly Label selectionHelp=MakerUi.Label("Upload a transparent PNG to begin.");
        readonly SpriteSheetView sheet=new SpriteSheetView();
        readonly Button complete;
        bool syncing;
        public string ExportedPath {get;private set;}
        public SpriteMakerWindow()
        {
            Text="Vpet Sprite Maker";Font=new Font("Segoe UI",10);ClientSize=new Size(1000,800);MinimumSize=new Size(800,650);
            StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(248,247,252);AutoScaleMode=AutoScaleMode.Dpi;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(12)};
            for(int i=0;i<4;i++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,100));Controls.Add(root);
            var commands=MakerUi.Flow();root.Controls.Add(commands,0,0);
            commands.Controls.Add(MakerUi.Button("Upload Sprite Sheet",Upload));
            commands.Controls.Add(MakerUi.Button("Save Project",delegate{SaveProject(false);}));
            commands.Controls.Add(MakerUi.Button("Save Project As…",delegate{SaveProject(true);}));
            commands.Controls.Add(MakerUi.Button("Load Project",LoadProject));
            complete=MakerUi.Button("Tweak and Complete",OpenTweak);commands.Controls.Add(complete);
            var options=MakerUi.Flow();root.Controls.Add(options,0,1);
            options.Controls.Add(diagonal);options.Controls.Add(MakerUi.Label("Frame width"));options.Controls.Add(frameWidth);options.Controls.Add(MakerUi.Label("Height"));options.Controls.Add(frameHeight);
            options.Controls.Add(MakerUi.Label("Zoom"));options.Controls.Add(zoom);zoom.Items.AddRange(new object[]{"25%","50%","100%","200%","400%"});zoom.SelectedIndex=2;
            var choices=MakerUi.Flow();root.Controls.Add(choices,0,2);
            for(int i=0;i<10;i++){int index=i;cycles[i]=MakerUi.Button(SpriteProject.Cycles[i],delegate{ChooseCycle(index);});choices.Controls.Add(cycles[i]);}
            var frameChoices=MakerUi.Flow();root.Controls.Add(frameChoices,0,3);
            for(int i=0;i<5;i++){int index=i;slots[i]=MakerUi.Button((i+1).ToString(),delegate{ChooseSlot(index);});slots[i].MinimumSize=new Size(48,34);frameChoices.Controls.Add(slots[i]);}
            frameChoices.Controls.Add(MakerUi.Button("Set",delegate{SetFrame();}));frameChoices.Controls.Add(MakerUi.Button("Clear",delegate{ClearFrame();}));frameChoices.Controls.Add(selectionHelp);
            var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(220,216,229),BorderStyle=BorderStyle.FixedSingle};viewport.Controls.Add(sheet);root.Controls.Add(viewport,0,4);root.Controls.Add(status,0,5);
            sheet.DimensionsChanged+=delegate(int w,int h){SetDimensions(w,h);};
            frameWidth.ValueChanged+=delegate{if(!syncing)SetDimensions((int)frameWidth.Value,(int)frameHeight.Value);};frameHeight.ValueChanged+=delegate{if(!syncing)SetDimensions((int)frameWidth.Value,(int)frameHeight.Value);};
            diagonal.CheckedChanged+=delegate{if(Project!=null&&!syncing){Project.Data.Diagonals=diagonal.Checked;Dirty=true;if(!Project.Enabled(Cycle))Cycle=0;ChooseCycle(Cycle);}};
            zoom.SelectedIndexChanged+=delegate{sheet.Zoom=new[]{.25f,.5f,1f,2f,4f}[zoom.SelectedIndex];};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!ConfirmDiscard())e.Cancel=true;};
            FormClosed+=delegate{if(Project!=null)Project.Dispose();};RefreshState();
        }
        internal void SetProject(SpriteProject project,string path)
        {
            if(Project!=null)Project.Dispose();Project=project;projectPath=path;Dirty=false;Cycle=Slot=0;sheet.Project=project;
            syncing=true;diagonal.Checked=project.Data.Diagonals;frameWidth.Value=project.Data.Width;frameHeight.Value=project.Data.Height;syncing=false;
            ChooseCycle(0);sheet.RefreshSize();
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
                try{var project=SpriteProject.Load(dialog.FileName);if(!ConfirmDiscard()){project.Dispose();return;}SetProject(project,dialog.FileName);}
                catch(Exception ex){MakerUi.Error(this,ex);}
        }
        internal bool SaveProject(bool choosePath)
        {
            if(Project==null)return false;
            string path=projectPath;
            if(choosePath||string.IsNullOrEmpty(path))using(var dialog=new SaveFileDialog{Filter="Vpet project|*.vpetproject",DefaultExt="vpetproject",FileName=string.IsNullOrEmpty(path)?"my-pet.vpetproject":Path.GetFileName(path)})
            {if(dialog.ShowDialog(this)!=DialogResult.OK)return false;path=dialog.FileName;}
            try{Project.Save(path);projectPath=path;Dirty=false;RefreshState();return true;}catch(Exception ex){MakerUi.Error(this,ex);return false;}
        }
        bool ConfirmDiscard()
        {
            if(!Dirty)return true;
            var choice=MessageBox.Show(this,"Save changes to your Sprite Maker project?","Sprite Maker",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
            return choice==DialogResult.No||(choice==DialogResult.Yes&&SaveProject(false));
        }
        internal void ChooseCycle(int row){Cycle=row;ChooseSlot(0);}
        internal void ChooseSlot(int slot)
        {Slot=slot;sheet.Cycle=Cycle;sheet.Slot=Slot;sheet.Draft=Project==null?null:Project.Data.Frames[Cycle][Slot]==null?null:Project.Data.Frames[Cycle][Slot].Copy();RefreshState();}
        internal void SetDimensions(int w,int h)
        {
            if(Project==null)return;
            Project.Data.Width=Math.Max(1,Math.Min(100,w));Project.Data.Height=Math.Max(1,Math.Min(150,h));
            syncing=true;frameWidth.Value=Project.Data.Width;frameHeight.Value=Project.Data.Height;syncing=false;Dirty=true;RefreshState();
        }
        internal void SetFrame()
        {
            if(Project==null)return;
            string problem=Project.FrameProblem(sheet.Draft,false);
            if(problem!=null){MakerUi.Error(this,new InvalidDataException("Select a valid frame: "+problem));return;}
            Project.Data.Frames[Cycle][Slot]=sheet.Draft.Copy();Dirty=true;ChooseSlot(Math.Min(4,Slot+1));
        }
        internal void ClearFrame(){if(Project==null)return;Project.Data.Frames[Cycle][Slot]=null;sheet.Draft=null;Dirty=true;RefreshState();}
        internal void RefreshState()
        {
            for(int i=0;i<10;i++){cycles[i].Visible=Project==null?i%5<3:Project.Enabled(i);cycles[i].BackColor=i==Cycle?Color.FromArgb(221,211,241):Color.White;}
            for(int i=0;i<5;i++){bool saved=Project!=null&&Project.Data.Frames[Cycle][i]!=null;slots[i].Text=(i+1)+(saved?" ✓":"");slots[i].ForeColor=saved?Color.DarkGreen:Color.Black;slots[i].BackColor=i==Slot?Color.FromArgb(221,211,241):Color.White;}
            selectionHelp.Text=Project==null?"Upload a transparent PNG to begin.":"Click to place frame "+(Slot+1)+"; drag a red corner to resize all frames.";
            complete.Enabled=false;
            if(Project==null)status.Text="Upload a sheet or load a saved project. Use the scrollbars to move around larger sheets.";
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
    internal sealed class SpriteSheetView : Control
    {
        public SpriteProject Project;
        public SpriteFrame Draft;
        public int Cycle,Slot;
        public event Action<int,int> DimensionsChanged;
        float zoom=1;
        int corner=-1;
        Point fixedCorner;
        public float Zoom {get{return zoom;}set{zoom=value;RefreshSize();}}
        public SpriteSheetView(){DoubleBuffered=true;Size=new Size(640,350);Cursor=Cursors.Cross;}
        public void RefreshSize(){Size=Project==null?new Size(640,350):new Size((int)Math.Ceiling(Project.Source.Width*zoom),(int)Math.Ceiling(Project.Source.Height*zoom));Invalidate();}
        Point ImagePoint(Point point){return new Point(Math.Max(0,(int)Math.Floor(point.X/zoom)),Math.Max(0,(int)Math.Floor(point.Y/zoom)));}
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(Project==null||e.Button!=MouseButtons.Left)return;Focus();corner=-1;
            if(Draft!=null)
            {
                var rect=Project.Selection(Draft);Point[] points={rect.Location,new Point(rect.Right,rect.Top),new Point(rect.Left,rect.Bottom),new Point(rect.Right,rect.Bottom)};
                for(int i=0;i<4;i++)if(Math.Abs(e.X-points[i].X*zoom)<=7&&Math.Abs(e.Y-points[i].Y*zoom)<=7){corner=i;fixedCorner=points[3-i];Capture=true;return;}
            }
            var p=ImagePoint(e.Location);Draft=new SpriteFrame{X=p.X,Y=p.Y};Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(corner<0||!Capture)return;
            var p=ImagePoint(e.Location);int w=Math.Max(1,Math.Min(100,Math.Abs(p.X-fixedCorner.X))),h=Math.Max(1,Math.Min(150,Math.Abs(p.Y-fixedCorner.Y)));
            Draft.X=(corner==0||corner==2)?Math.Max(0,fixedCorner.X-w):fixedCorner.X;
            Draft.Y=corner<2?Math.Max(0,fixedCorner.Y-h):fixedCorner.Y;
            if(DimensionsChanged!=null)DimensionsChanged(w,h);Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);corner=-1;Capture=false;}
        protected override void OnPaint(PaintEventArgs e)
        {
            MakerUi.Checker(e.Graphics,e.ClipRectangle,16);if(Project==null)return;
            e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
            e.Graphics.DrawImage(Project.Source,new Rectangle(0,0,Width,Height));
            using(var thin=new Pen(Color.FromArgb(140,Color.Red),1))for(int i=0;i<5;i++)if(i!=Slot&&Project.Data.Frames[Cycle][i]!=null)DrawBox(e.Graphics,Project.Selection(Project.Data.Frames[Cycle][i]),thin,false);
            if(Draft!=null)using(var pen=new Pen(Color.Red,2))DrawBox(e.Graphics,Project.Selection(Draft),pen,true);
        }
        void DrawBox(Graphics g,Rectangle rect,Pen pen,bool handles)
        {
            var r=new RectangleF(rect.X*zoom,rect.Y*zoom,rect.Width*zoom,rect.Height*zoom);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);
            if(handles)foreach(var p in new[]{new PointF(r.Left,r.Top),new PointF(r.Right,r.Top),new PointF(r.Left,r.Bottom),new PointF(r.Right,r.Bottom)})g.FillRectangle(Brushes.Red,p.X-4,p.Y-4,8,8);
        }
    }
}
