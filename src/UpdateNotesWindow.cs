using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class UpdateNotesWindow : Form
    {
        public UpdateNotesWindow(string version,string notes)
        {
            Text="Vpet update complete";
            Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            ClientSize=new Size(600,440);MinimumSize=new Size(460,340);StartPosition=FormStartPosition.CenterScreen;
            BackColor=Color.FromArgb(248,247,252);MinimizeBox=false;MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=5};Controls.Add(root);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var title=new Label{AutoSize=true,Dock=DockStyle.Fill,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,0,0,10),Text="Updated to Vpet "+version};root.Controls.Add(title,0,0);
            root.Controls.Add(new Label{AutoSize=true,Dock=DockStyle.Fill,MaximumSize=new Size(900,0),Margin=new Padding(0,0,0,12),Text="Your update is complete. Your settings, artwork, and shortcut choices have been kept."},0,1);
            root.Controls.Add(new Label{AutoSize=true,Text="What's changed",Margin=new Padding(0,0,0,8)},0,2);
            var description=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BackColor=Color.White,Text=notes.Replace("\r\n","\n").Replace("\n",Environment.NewLine),SelectionStart=0};root.Controls.Add(description,0,3);
            var buttons=MakerUi.Flow();buttons.FlowDirection=FlowDirection.RightToLeft;root.Controls.Add(buttons,0,4);
            var accept=MakerUi.Button("Close",null);accept.DialogResult=DialogResult.OK;buttons.Controls.Add(accept);AcceptButton=accept;CancelButton=accept;
            Shown+=delegate{description.Select(0,0);description.ScrollToCaret();accept.Focus();};
        }
    }
}
