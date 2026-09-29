using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class SettingsWindow : Form
    {
        readonly PetWindow pet;
        readonly TabControl tabs=new TabControl();
        readonly Timer previewTimer=new Timer{Interval=40};
        readonly Stopwatch previewClock=Stopwatch.StartNew();
        readonly Label assetStatus=new Label();
        readonly Label importStatus=new Label();
        readonly Button useButton=new Button();
        SpriteSet pending;
        Panel animationPreview;
        ComboBox previewDirection,previewActivity;
        PictureBox sheetPreview;
        PictureBox emotePreview;
        ComboBox emoteChoice;
        Label replacementStatus;
        CheckBox restrictedAreaCheckbox;
        TableLayoutPanel customEmoteList;
        readonly ToolTip customEmoteNames=new ToolTip();

        public SettingsWindow(PetWindow pet)
        {
            this.pet=pet;Text="Vpet "+ReleaseInfo.Version+" · Settings";Icon=pet.Icon;StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(650,720);MinimumSize=new Size(550,520);Font=new Font("Segoe UI",10);
            BackColor=Color.FromArgb(248,247,252);AutoScaleMode=AutoScaleMode.Dpi;
            var header=Stack();header.Dock=DockStyle.Top;header.Padding=new Padding(20,12,20,12);header.BackColor=Color.FromArgb(66,46,105);
            var heading=Copy("A little company for your desktop",true);heading.Font=new Font("Segoe UI",17,FontStyle.Bold);heading.ForeColor=Color.White;Add(header,heading);
            var hint=Copy("Drag to move · right-click for controls · your changes save automatically");hint.ForeColor=Color.FromArgb(223,212,244);Add(header,hint);
            tabs.Dock=DockStyle.Fill;tabs.Padding=new Point(16,10);
            Controls.Add(tabs);Controls.Add(header);
            AddMovement();AddPersonality();AddArtwork();AddAdvanced();
            previewTimer.Tick+=delegate{if(animationPreview!=null&&tabs.SelectedIndex==2)animationPreview.Invalidate();};previewTimer.Start();
            pet.AssetsChanged+=AssetsChanged;
            FormClosed+=delegate{pet.AssetsChanged-=AssetsChanged;previewTimer.Dispose();customEmoteNames.Dispose();if(pending!=null)pending.Dispose();if(sheetPreview.Image!=null)sheetPreview.Image.Dispose();if(emotePreview.Image!=null)emotePreview.Image.Dispose();};
        }
        public void SelectTab(int index){tabs.SelectedIndex=index;}
        public void SyncRestrictedAreaVisibility()
        {if(restrictedAreaCheckbox!=null)restrictedAreaCheckbox.Checked=pet.Model.RestrictedAreaVisible;}
        TabPage Page(string name)
        {
            var page=new TabPage(name){BackColor=BackColor,AutoScroll=true,Padding=new Padding(24)};tabs.TabPages.Add(page);return page;
        }
        static Label LabelAt(Control parent,string text,int x,int y,int width,int height,bool bold)
        {
            var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(width,height),ForeColor=Color.FromArgb(54,45,70)};
            if(bold)label.Font=new Font("Segoe UI",11,FontStyle.Bold);parent.Controls.Add(label);return label;
        }
        static Button ButtonAt(Control parent,string text,int x,int y,int width,EventHandler click)
        {
            var button=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,36),FlatStyle=FlatStyle.Flat,BackColor=Color.White};
            button.FlatAppearance.BorderColor=Color.FromArgb(205,194,222);button.Click+=click;parent.Controls.Add(button);return button;
        }
        static ComboBox ComboAt(Control parent,string[] items,int x,int y,int width,int selected)
        {
            var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(x,y),Width=width};combo.Items.AddRange(items);combo.SelectedIndex=selected;parent.Controls.Add(combo);return combo;
        }
        void ChooseSheet(object sender,EventArgs e)
        {
            using(var dialog=new OpenFileDialog{Filter="Vpet sprite package|*.vpetsprite",Title="Upload Custom Sprite"})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try
                {
                    LoadSprite(dialog.FileName);
                }
                catch(Exception ex){ShowError(ex.Message);}
            }
        }
        void LoadSprite(string path)
        {
            var candidate=SpriteSet.Import(path);if(pending!=null)pending.Dispose();pending=candidate;
            useButton.Enabled=true;importStatus.Text="Previewing "+Path.GetFileName(path)+". Choose “Use this pet” to apply.";UpdateSheetPreview();
        }
        void UsePending(object sender,EventArgs e)
        {
            if(pending==null)return;
            try{var candidate=pending;pet.UseCustom(candidate);pending=null;useButton.Enabled=false;importStatus.Text="Your custom pet is saved and active.";UpdateSheetPreview();}
            catch(Exception ex){ShowError(ex.Message);}
        }
        void UpdateSheetPreview()
        {
            if(sheetPreview==null)return;var old=sheetPreview.Image;sheetPreview.Image=new Bitmap((pending??pet.Sprites).Sheet);if(old!=null)old.Dispose();
        }
        void RefreshEmotePreview()
        {
            if(emoteChoice==null||emotePreview==null||replacementStatus==null)return;
            int index=emoteChoice.SelectedIndex;var old=emotePreview.Image;emotePreview.Image=Artwork.Bubble(index,pet.Replacements.Get(index),1,false);if(old!=null)old.Dispose();
            replacementStatus.Text=(pet.Replacements.Get(index)==null?"Using the original image.":"Using your saved image.")+" Replacements apply to greetings, pickups, and random reactions. PNG only, up to 512 × 512 pixels.";
        }
        void RefreshCustomEmotes()
        {
            if(customEmoteList==null)return;
            customEmoteList.SuspendLayout();
            try
            {
                customEmoteNames.RemoveAll();
                while(customEmoteList.Controls.Count>0)customEmoteList.Controls[0].Dispose();
                customEmoteList.RowStyles.Clear();customEmoteList.RowCount=0;
                for(int i=0;i<Reactions.Names.Length;i++)if(pet.Replacements.Get(i)!=null)
                {int reaction=i;AddCustomEmoteRow(Reactions.Names[i]+" (custom replacement)",delegate{pet.PreviewReaction(reaction);});}
                foreach(var emote in pet.CustomEmotes)
                {string name=emote.Name;AddCustomEmoteRow(name,delegate{pet.PreviewCustomEmote(name);});}
                if(customEmoteList.RowCount==0)
                {
                    customEmoteList.RowCount=1;
                    var empty=new Label{Text="No custom emotes yet. Add images to the emote folder or replace a default emote above.",AutoSize=true,MaximumSize=new Size(customEmoteList.Width,0),Margin=new Padding(0,4,0,12)};
                    customEmoteList.Controls.Add(empty,0,0);customEmoteList.SetColumnSpan(empty,2);
                }
            }
            finally{customEmoteList.ResumeLayout(true);}
        }
        void AddCustomEmoteRow(string name,EventHandler preview)
        {
            int row=customEmoteList.RowCount++;
            int height=Math.Max(36,Font.Height+16);
            customEmoteList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label=new Label{Text=name,UseMnemonic=false,AutoSize=true,AutoEllipsis=false,TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill,Margin=new Padding(0,4,8,4)};
            var button=new Button{Text="Try It Out",AccessibleName="Try "+name,Height=height,Dock=DockStyle.Fill,Margin=new Padding(0,4,0,4),FlatStyle=FlatStyle.Flat,BackColor=Color.White};
            button.FlatAppearance.BorderColor=Color.FromArgb(205,194,222);button.Click+=preview;
            customEmoteList.Controls.Add(label,0,row);customEmoteList.Controls.Add(button,1,row);customEmoteNames.SetToolTip(label,name);
        }
        void AssetsChanged(){assetStatus.Text=pet.EmoteStatus??"No custom emotes yet.";RefreshEmotePreview();RefreshCustomEmotes();customEmoteNames.SetToolTip(assetStatus,assetStatus.Text);}
        void ShowError(string message){MessageBox.Show(this,message,"Could not load artwork",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    internal sealed class DoubleBufferedPanel : Panel {public DoubleBufferedPanel(){DoubleBuffered=true;}}
}
