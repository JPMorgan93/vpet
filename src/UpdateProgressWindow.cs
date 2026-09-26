using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class UpdateProgressWindow : Form
    {
        readonly Label status=new Label{AutoSize=false,Location=new Point(24,24),Size=new Size(390,45)};
        readonly ProgressBar progress=new ProgressBar{Location=new Point(24,80),Size=new Size(390,24)};
        public UpdateProgressWindow(string version)
        {
            Text="Updating Vpet";ClientSize=new Size(438,132);StartPosition=FormStartPosition.CenterScreen;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ControlBox=false;
            Font=new Font("Segoe UI",10);Controls.Add(status);Controls.Add(progress);
            status.Text="Downloading Vpet "+version+"…";
        }
        public void Report(int percent)
        {
            if(IsDisposed||!IsHandleCreated)return;
            if(InvokeRequired){BeginInvoke(new Action<int>(Report),percent);return;}
            progress.Value=Math.Max(0,Math.Min(100,percent));
            if(percent==100)status.Text="Verifying update…";
        }
    }
}
