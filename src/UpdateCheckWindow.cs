using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class UpdateCheckWindow : Form
    {
        readonly Label status=new Label{AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0,0,0,12)};
        readonly Button update;
        AvailableUpdate available;

        public UpdateCheckWindow(Action<AvailableUpdate> install)
        {
            Text="Vpet updates";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            ClientSize=new Size(460,220);MinimumSize=new Size(420,255);StartPosition=FormStartPosition.CenterScreen;
            BackColor=Color.FromArgb(248,247,252);MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};Controls.Add(root);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(new Label{AutoSize=true,Dock=DockStyle.Fill,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,0,0,14),Text="Your version: Vpet "+ReleaseInfo.Version},0,0);
            root.Controls.Add(status,0,1);
            var buttons=MakerUi.Flow();buttons.FlowDirection=FlowDirection.RightToLeft;root.Controls.Add(buttons,0,2);
            update=MakerUi.Button("Update",delegate
            {
                if(available==null)return;
                Console.WriteLine("Update clicked");var chosen=available;available=null;update.Enabled=false;Console.WriteLine("Closing update window");Close();Console.WriteLine("Invoking install callback");install(chosen);Console.WriteLine("Callback done");
            });buttons.Controls.Add(update);
            var close=MakerUi.Button("Close",delegate{Close();});buttons.Controls.Add(close);CancelButton=close;
            ShowChecking();
        }
        public void ShowChecking()
        {available=null;status.Text="Checking for the newest public release…";update.Visible=false;update.Enabled=false;AcceptButton=null;}
        public void ShowResult(AvailableUpdate result)
        {
            available=result;
            status.Text=result==null?"You're running the most recent version of Vpet.":"Vpet "+result.Version+" is available.\r\n\r\nChoose Update to download and install it. What's changed will appear when the update is complete.";
            update.Visible=result!=null;update.Enabled=result!=null;AcceptButton=result==null?null:update;
        }
        public void ShowError(string message)
        {available=null;status.Text="Could not check for updates. Your pet will keep running.\r\n\r\n"+message;update.Visible=false;update.Enabled=false;AcceptButton=null;}
    }
}
