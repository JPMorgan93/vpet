using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static class ReminderUi
    {
        internal static readonly Color Background=Color.FromArgb(248,247,252);
        internal static TableLayoutPanel Stack()
        {var panel=new TableLayoutPanel{ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,Margin=new Padding(0)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));return panel;}
        internal static void Add(TableLayoutPanel panel,Control control)
        {int row=panel.RowCount++;panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));control.Dock=DockStyle.Top;panel.Controls.Add(control,0,row);}
        internal static Label Label(string text,bool bold=false)
        {return new Label{Text=text,AutoSize=true,Margin=new Padding(4,7,4,7),Font=new Font("Segoe UI",10,bold?FontStyle.Bold:FontStyle.Regular)};}
        internal static RichTextBox MessageBox(bool editable)
        {
            var box=new RichTextBox{ReadOnly=!editable,DetectUrls=true,MaxLength=200,Multiline=true,WordWrap=true,ScrollBars=RichTextBoxScrollBars.Vertical,Height=100,BorderStyle=editable?BorderStyle.FixedSingle:BorderStyle.None,BackColor=Color.White,Font=new Font("Segoe UI",10),Margin=new Padding(4)};
            box.LinkClicked+=delegate(object sender,LinkClickedEventArgs e){OpenLink(box,e.LinkText);};return box;
        }
        internal static bool WebLink(string text,out Uri uri)
        {return Uri.TryCreate(text,UriKind.Absolute,out uri)&&(uri.Scheme==Uri.UriSchemeHttp||uri.Scheme==Uri.UriSchemeHttps);}
        internal static void OpenLink(IWin32Window owner,string text)
        {
            Uri uri;if(!WebLink(text,out uri))return;
            try{Process.Start(new ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});}
            catch(Exception ex){System.Windows.Forms.MessageBox.Show(owner,"The link could not be opened. "+ex.Message,"Reminder link",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        }
    }

    internal sealed class ReminderWindow : Form
    {
        readonly ReminderStore store;
        readonly TableLayoutPanel content=ReminderUi.Stack(),list=ReminderUi.Stack();
        readonly Panel scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(14)};
        readonly GroupBox editor=new GroupBox{Text="Edit Reminder",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(12),Visible=false};
        readonly ComboBox kind=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=160};
        readonly DateTimePicker date=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="ddd, MMM d, yyyy",Width=225};
        readonly NumericUpDown hour=new NumericUpDown{Minimum=1,Maximum=12,Value=12,Width=62};
        readonly NumericUpDown minute=new NumericUpDown{Minimum=0,Maximum=59,Width=62};
        readonly ComboBox period=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=72};
        readonly CheckBox[] days=new CheckBox[7];
        readonly ComboBox[] frequencies=new ComboBox[7];
        readonly TableLayoutPanel recurring=ReminderUi.Stack(),frequencyRows=ReminderUi.Stack();
        readonly FlowLayoutPanel dateRow=MakerUi.Flow();
        readonly RichTextBox message=ReminderUi.MessageBox(true);
        readonly CheckBox active=new CheckBox{Text="Active",AutoSize=true,Checked=true,Margin=new Padding(4,10,4,10)};
        readonly Label count=ReminderUi.Label("0 / 200 characters"),error=ReminderUi.Label("");
        readonly Button add;
        Reminder draft;bool loading;
        internal Func<DateTime> LocalNow=()=>DateTime.Now;
        public ReminderWindow(ReminderStore store)
        {
            this.store=store;Text="Reminder Window";ClientSize=new Size(660,740);MinimumSize=new Size(480,400);
            Font=new Font("Segoe UI",10);BackColor=ReminderUi.Background;AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;
            Controls.Add(scroll);scroll.Controls.Add(content);
            var actions=MakerUi.Flow();add=MakerUi.Button("Add Reminder",delegate{Edit(Reminder.New(LocalNow()));});actions.Controls.Add(add);ReminderUi.Add(content,actions);
            ReminderUi.Add(content,ReminderUi.Label("Reminders use your PC's local time. Vpet must be running; missed reminders appear when it next opens. Select Edit to change a saved reminder."));
            ReminderUi.Add(content,editor);ReminderUi.Add(content,list);
            var fields=ReminderUi.Stack();editor.Controls.Add(fields);
            var typeRow=MakerUi.Flow();typeRow.Controls.Add(MakerUi.Label("Type"));kind.Items.AddRange(new object[]{"One-Time","Recurring"});typeRow.Controls.Add(kind);ReminderUi.Add(fields,typeRow);
            dateRow.Controls.Add(MakerUi.Label("Date"));dateRow.Controls.Add(date);ReminderUi.Add(fields,dateRow);
            var dayRow=MakerUi.Flow();string[] names={"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};
            for(int i=0;i<7;i++)
            {
                int day=i;days[i]=new CheckBox{Text=names[i],Appearance=Appearance.Button,AutoSize=true,MinimumSize=new Size(48,32),TextAlign=ContentAlignment.MiddleCenter,Margin=new Padding(3)};
                days[i].CheckedChanged+=delegate{if(!loading)UpdateDays(day);};dayRow.Controls.Add(days[i]);
                frequencies[i]=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=290};
                foreach(ReminderFrequency frequency in Enum.GetValues(typeof(ReminderFrequency)))frequencies[i].Items.Add(new ReminderDay{Day=(DayOfWeek)i,Frequency=frequency}.Description);
                frequencies[i].SelectedIndex=0;
            }
            ReminderUi.Add(recurring,dayRow);ReminderUi.Add(recurring,frequencyRows);
            ReminderUi.Add(recurring,ReminderUi.Label("Every other day-of-week starts with the week when that day is selected (Sunday–Saturday). Each selected day has its own frequency."));ReminderUi.Add(fields,recurring);
            var time=MakerUi.Flow();time.Controls.Add(MakerUi.Label("Time"));time.Controls.Add(hour);time.Controls.Add(MakerUi.Label(":"));time.Controls.Add(minute);period.Items.AddRange(new object[]{"AM","PM"});period.SelectedIndex=0;time.Controls.Add(period);ReminderUi.Add(fields,time);
            ReminderUi.Add(fields,ReminderUi.Label("Reminder message",true));ReminderUi.Add(fields,message);ReminderUi.Add(fields,count);ReminderUi.Add(fields,active);
            error.ForeColor=Color.Firebrick;ReminderUi.Add(fields,error);
            var buttons=MakerUi.Flow();buttons.Controls.Add(MakerUi.Button("Save",delegate{SaveDraft();}));buttons.Controls.Add(MakerUi.Button("Delete",delegate{DeleteDraft();}));buttons.Controls.Add(MakerUi.Button("Cancel",delegate{CloseEditor();}));ReminderUi.Add(fields,buttons);
            message.TextChanged+=delegate{count.Text=message.TextLength+" / 200 characters";};
            kind.SelectedIndexChanged+=delegate{dateRow.Visible=kind.SelectedIndex==0;recurring.Visible=kind.SelectedIndex==1;};
            store.Changed+=RefreshList;FormClosed+=delegate{store.Changed-=RefreshList;};RefreshList();
        }
        internal void Edit(Reminder item)
        {
            draft=item.Copy();loading=true;kind.SelectedIndex=(int)item.Kind;date.Value=Reminder.ReadLocal(item.Date);
            hour.Value=item.MinuteOfDay/60%12==0?12:item.MinuteOfDay/60%12;minute.Value=item.MinuteOfDay%60;period.SelectedIndex=item.MinuteOfDay>=720?1:0;
            for(int i=0;i<7;i++){var rule=item.Days.Find(d=>(int)d.Day==i);days[i].Checked=rule!=null;frequencies[i].SelectedIndex=rule==null?0:(int)rule.Frequency;}
            message.Text=item.Message;active.Checked=item.Active;error.Text="";loading=false;RebuildFrequencies();editor.Visible=true;scroll.AutoScrollPosition=Point.Empty;
        }
        void UpdateDays(int day)
        {
            if(draft==null)return;
            if(days[day].Checked){draft.Days.Add(new ReminderDay{Day=(DayOfWeek)day,AnchorDate=Reminder.Local(LocalNow().Date)});frequencies[day].SelectedIndex=0;}
            else draft.Days.RemoveAll(d=>(int)d.Day==day);
            RebuildFrequencies();
        }
        void RebuildFrequencies()
        {
            // Detach persistent picklists before disposing their previous row containers.
            foreach(var combo in frequencies)if(combo.Parent!=null)combo.Parent.Controls.Remove(combo);
            foreach(Control row in frequencyRows.Controls.Cast<Control>().ToArray())row.Dispose();frequencyRows.Controls.Clear();frequencyRows.RowStyles.Clear();frequencyRows.RowCount=0;
            for(int i=0;i<7;i++)if(days[i].Checked)
            {var row=MakerUi.Flow();row.Controls.Add(MakerUi.Label(((DayOfWeek)i).ToString()));row.Controls.Add(frequencies[i]);ReminderUi.Add(frequencyRows,row);}
        }
        internal void SaveDraft()
        {
            if(draft==null)return;
            try
            {
                var item=draft.Copy();item.Kind=(ReminderKind)kind.SelectedIndex;item.Date=Reminder.Local(date.Value.Date);
                item.MinuteOfDay=(int)hour.Value%12*60+(int)minute.Value+period.SelectedIndex*720;item.Message=message.Text;item.Active=active.Checked;
                foreach(var rule in item.Days)rule.Frequency=(ReminderFrequency)frequencies[(int)rule.Day].SelectedIndex;
                store.Save(item,LocalNow());CloseEditor();
            }
            catch(Exception ex){error.Text=ex.Message;}
        }
        void DeleteDraft(){if(draft==null)return;try{store.Delete(draft.Id);CloseEditor();}catch(Exception ex){error.Text=ex.Message;}}
        void CloseEditor(){editor.Visible=false;draft=null;scroll.AutoScrollPosition=Point.Empty;}
        void RefreshList()
        {
            var position=scroll.AutoScrollPosition;list.SuspendLayout();
            foreach(Control child in list.Controls.Cast<Control>().ToArray())child.Dispose();list.Controls.Clear();list.RowStyles.Clear();list.RowCount=0;
            add.Enabled=store.LoadError==null;
            if(store.LoadError!=null)ReminderUi.Add(list,ReminderUi.Label(store.LoadError));
            else if(!store.Items.Any())ReminderUi.Add(list,ReminderUi.Label("No reminders yet. Choose Add Reminder to create one."));
            foreach(var item in store.Items)
            {
                var row=new TableLayoutPanel{ColumnCount=2,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,BackColor=Color.White,Padding=new Padding(8),Margin=new Padding(0,6,0,6)};
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                row.Controls.Add(MakerUi.Button("Edit",delegate{Edit(item);}),0,0);
                var body=ReminderUi.Stack();ReminderUi.Add(body,ReminderUi.Label((item.Kind==ReminderKind.OneTime?"One-Time":"Recurring")+" · "+(item.Active?"Active":item.LastFired!=null&&item.Kind==ReminderKind.OneTime?"Completed":"Inactive"),true));ReminderUi.Add(body,ReminderUi.Label(item.Schedule));
                var text=ReminderUi.MessageBox(false);text.Text=item.Message;ReminderUi.Add(body,text);row.Controls.Add(body,1,0);ReminderUi.Add(list,row);
            }
            list.ResumeLayout(true);scroll.AutoScrollPosition=new Point(-position.X,-position.Y);
        }
    }
}
