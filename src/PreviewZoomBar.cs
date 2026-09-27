using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class PreviewZoomBar : FlowLayoutPanel
    {
        readonly NumericUpDown percent=new NumericUpDown{Minimum=1,Maximum=1600,Value=100,Increment=25,Width=76,Margin=new Padding(4,8,0,4),AccessibleName="Preview zoom percentage"};
        bool syncing;
        public event Action<int,Point?> ZoomChanged;
        public event Action FitRequested;
        public int Percent {get{return (int)percent.Value;}}
        public int MaximumPercent {get{return (int)percent.Maximum;}set{percent.Maximum=Math.Max(1,value);}}
        public PreviewZoomBar()
        {
            Dock=DockStyle.Fill;AutoSize=true;WrapContents=true;Padding=new Padding(4);
            Controls.Add(MakerUi.Label("Preview zoom"));
            var minus=MakerUi.Button("−",delegate{Step(-1,null);});minus.AutoSize=false;minus.MinimumSize=minus.Size=new Size(34,34);minus.AccessibleName="Zoom out";Controls.Add(minus);
            Controls.Add(percent);Controls.Add(MakerUi.Label("%"));
            var plus=MakerUi.Button("+",delegate{Step(1,null);});plus.AutoSize=false;plus.MinimumSize=plus.Size=new Size(34,34);plus.AccessibleName="Zoom in";Controls.Add(plus);
            Controls.Add(MakerUi.Button("100%",delegate{SetPercent(100,null);}));
            Controls.Add(MakerUi.Button("Fit",delegate{if(FitRequested!=null)FitRequested();}));
            Controls.Add(MakerUi.Label("Ctrl + mouse wheel to zoom"));
            percent.ValueChanged+=delegate{if(!syncing&&ZoomChanged!=null)ZoomChanged(Percent,null);};
        }
        public void SetPercent(int value,Point? anchor)
        {
            syncing=true;percent.Value=Math.Max((int)percent.Minimum,Math.Min(MaximumPercent,value));syncing=false;
            if(ZoomChanged!=null)ZoomChanged(Percent,anchor);
        }
        public void Step(int direction,Point? anchor)
        {int next=(int)Math.Round(Percent*Math.Pow(1.25,Math.Sign(direction)));if(next==Percent)next+=Math.Sign(direction);SetPercent(next,anchor);}
    }
}
