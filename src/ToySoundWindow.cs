using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class ToySoundWindow : Form
    {
        internal readonly TrackBar Volume=new TrackBar{Name="TriangleVolume",Minimum=0,Maximum=100,TickFrequency=10,Dock=DockStyle.Fill};
        public ToySoundWindow(float volume,Action<float> test,Action<float> save)
        {
            Text="Triangle Sound Setting";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            ClientSize=new Size(480,205);MinimumSize=new Size(470,245);StartPosition=FormStartPosition.CenterScreen;MaximizeBox=false;MinimizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=4};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(root);
            var label=new Label{AutoSize=true,Dock=DockStyle.Fill};root.Controls.Add(label,0,0);
            var row=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Volume.Value=(int)Math.Round(volume*100);row.Controls.Add(Volume,0,0);row.Controls.Add(MakerUi.Button("Test sound",delegate{test(Volume.Value/100f);}),1,0);root.Controls.Add(row,0,1);
            root.Controls.Add(new Label{Text="Test the selected instrument sound. Save applies this volume to your taps and the pet's replies. 0% mutes it.",AutoSize=true,Dock=DockStyle.Fill},0,2);
            var buttons=MakerUi.Flow();buttons.FlowDirection=FlowDirection.RightToLeft;root.Controls.Add(buttons,0,3);
            var apply=MakerUi.Button("Save",delegate{save(Volume.Value/100f);DialogResult=DialogResult.OK;Close();});buttons.Controls.Add(apply);AcceptButton=apply;
            var cancel=MakerUi.Button("Cancel",delegate{Close();});buttons.Controls.Add(cancel);CancelButton=cancel;
            Action describe=()=>label.Text="Triangle volume: "+Volume.Value+"%";Volume.ValueChanged+=delegate{describe();};describe();
        }
    }
}
