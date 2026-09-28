using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class SettingsWindow : Form
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
            ClientSize=new Size(650,720);MinimumSize=new Size(650,720);Font=new Font("Segoe UI",10);
            BackColor=Color.FromArgb(248,247,252);AutoScaleMode=AutoScaleMode.Dpi;
            var header=new Panel{Dock=DockStyle.Top,Height=88,BackColor=Color.FromArgb(66,46,105)};
            header.Controls.Add(new Label{Text="A little company for your desktop",ForeColor=Color.White,Font=new Font("Segoe UI",17,FontStyle.Bold),AutoSize=true,Location=new Point(24,18)});
            header.Controls.Add(new Label{Text="Drag to move · right-click for controls · your changes save automatically",ForeColor=Color.FromArgb(223,212,244),AutoSize=true,Location=new Point(25,54)});
            tabs.Dock=DockStyle.Fill;tabs.Padding=new Point(16,10);
            Controls.Add(tabs);Controls.Add(header);
            AddMovement();AddPersonality();AddArtwork();
            previewTimer.Tick+=delegate{if(animationPreview!=null&&tabs.SelectedIndex==2)animationPreview.Invalidate();};previewTimer.Start();
            pet.AssetsChanged+=AssetsChanged;
            FormClosed+=delegate{pet.AssetsChanged-=AssetsChanged;previewTimer.Dispose();customEmoteNames.Dispose();if(pending!=null)pending.Dispose();if(sheetPreview.Image!=null)sheetPreview.Image.Dispose();if(emotePreview.Image!=null)emotePreview.Image.Dispose();};
        }
        public void SelectTab(int index){tabs.SelectedIndex=index;}
        public void SyncRestrictedAreaVisibility()
        {if(restrictedAreaCheckbox!=null)restrictedAreaCheckbox.Checked=pet.Model.Settings.DisplayRestrictedArea;}
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
        void AddMovement()
        {
            var page=Page("Movement");
            LabelAt(page,"Where should your pet wander?",24,22,560,28,true);
            var movement=ComboAt(page,new[]{"Free Roam","Restricted","Static"},24,56,250,(int)pet.Model.Settings.Movement);
            var restricted=new Panel{Location=new Point(24,174),Size=new Size(550,207)};page.Controls.Add(restricted);
            var showArea=new CheckBox{Text="Display restricted area",Location=Point.Empty,Size=new Size(350,28),Checked=pet.Model.Settings.DisplayRestrictedArea};restricted.Controls.Add(showArea);
            restrictedAreaCheckbox=showArea;
            LabelAt(restricted,"Restricted radius (pixels)",0,37,330,26,true);
            var radiusNumber=new NumericUpDown{Minimum=30,Maximum=1000,Value=pet.Model.Settings.Radius,Location=new Point(420,35),Width=120};restricted.Controls.Add(radiusNumber);
            var radius=new TrackBar{Minimum=30,Maximum=1000,Value=pet.Model.Settings.Radius,TickStyle=TickStyle.None,Location=new Point(-6,70),Width=550};restricted.Controls.Add(radius);
            var fenceHelp=new RichTextBox{ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=BackColor,Font=Font,Location=new Point(0,116),Size=new Size(550,88),TabStop=false,ScrollBars=RichTextBoxScrollBars.None};
            fenceHelp.Text="Turn on Display restricted area to see the fence. Drag its center handle to move it. Use the slider or enter a radius to resize it. The fence stays fixed; if it excludes your pet, the pet moves to its center.";
            fenceHelp.Select(fenceHelp.Text.IndexOf("Display restricted area"),"Display restricted area".Length);fenceHelp.SelectionFont=new Font(Font,FontStyle.Bold);fenceHelp.Select(0,0);restricted.Controls.Add(fenceHelp);
            var description=LabelAt(page,"",24,100,550,64,false);
            var controls=new Panel{Size=new Size(565,315)};page.Controls.Add(controls);
            var speedLabel=LabelAt(controls,"",0,0,555,28,true);
            var speed=new TrackBar{Minimum=0,Maximum=100,Value=pet.Model.Settings.Speed,TickFrequency=10,Location=new Point(-6,34),Width=550};controls.Controls.Add(speed);
            LabelAt(controls,"0",4,78,40,24,false);LabelAt(controls,"50",258,78,40,24,false);LabelAt(controls,"100",513,78,42,24,false);
            LabelAt(controls,"Left-click for Love and a half-second shake.\nDragging uses down-facing idle; release rests for five seconds.",0,117,550,48,false);
            LabelAt(controls,"Window location",0,183,530,28,true);
            var layer=ComboAt(controls,new[]{"Over Everything","Under All","Dynamic"},0,219,250,(int)pet.Model.Settings.Layer);
            var layerDescription=LabelAt(controls,"",0,259,550,56,false);
            Action update=delegate
            {
                bool enabled=pet.Model.Settings.Movement==MovementMode.Restricted;
                restricted.Visible=enabled;controls.Location=new Point(24,(enabled?restricted.Bottom:description.Bottom)+14);
                description.Text=Names.MovementDescription(pet.Model.Settings.Movement);
                radiusNumber.Value=radius.Value;
                layerDescription.Text=Names.LayerDescription(pet.Model.Settings.Layer);
                speedLabel.Text="Walking speed · "+speed.Value+" / 100  ("+(speed.Value*2)+" px/sec at 100% scaling)";
                showArea.Checked=pet.Model.Settings.DisplayRestrictedArea;
            };
            movement.SelectedIndexChanged+=delegate{pet.Model.ChangeMode((MovementMode)movement.SelectedIndex,pet.Now);update();pet.SettingsChanged(false);};
            showArea.CheckedChanged+=delegate{if(pet.Model.Settings.DisplayRestrictedArea==showArea.Checked)return;pet.Model.Settings.DisplayRestrictedArea=showArea.Checked;pet.SettingsChanged(false);};
            radius.ValueChanged+=delegate{pet.Model.SetRadius(radius.Value);update();pet.SettingsChanged(false);};
            radiusNumber.ValueChanged+=delegate{radius.Value=(int)radiusNumber.Value;};
            speed.ValueChanged+=delegate{pet.Model.Settings.Speed=speed.Value;update();pet.SettingsChanged(false);};
            layer.SelectedIndexChanged+=delegate{pet.Model.Settings.Layer=(LayerMode)layer.SelectedIndex;update();pet.ApplyLayer();pet.SettingsChanged(false);};
            update();
        }
        void AddPersonality()
        {
            var page=Page("Personality");
            LabelAt(page,"A personality of their own",24,22,555,30,true);
            var personality=ComboAt(page,new[]{"Sweet","Sassy","Bashful"},24,64,250,(int)pet.Model.Settings.Personality);
            var description=LabelAt(page,"",24,108,555,60,false);
            Action describe=delegate
            {
                var selected=pet.Model.Settings.Personality;
                string favorites=selected==Personality.Sweet?"Music, Love, and Question":selected==Personality.Sassy?"Anger, Fear, and Proud":"Question, Sad, and Disgust";
                description.Text="Hover: "+Reactions.Names[Reactions.Hover(selected)]+". Pick up and drag: "+Reactions.Names[Reactions.Pickup(selected)]+". Click: Love.\nRandom reactions favor "+favorites+".\nAll eight default emotes can appear randomly.";
            };describe();
            personality.SelectedIndexChanged+=delegate{pet.Model.Settings.Personality=(Personality)personality.SelectedIndex;describe();pet.SettingsChanged(false);};
            LabelAt(page,"Random reactions",24,181,550,28,true);
            var frequency=ComboAt(page,new[]{"Rarely · 3–5 minutes","Sometimes · 60–120 seconds","Often · 30–60 seconds","Off"},24,219,350,(int)pet.Model.Settings.Frequency);
            frequency.SelectedIndexChanged+=delegate{pet.Model.Settings.Frequency=(Frequency)frequency.SelectedIndex;pet.SettingsChanged(true);};
            LabelAt(page,"Try a reaction",24,274,540,28,true);
            for(int i=0;i<8;i++)
            {
                int index=i;ButtonAt(page,Reactions.Names[i],24+(i%4)*140,311+(i/4)*44,130,delegate{pet.PreviewReaction(index);});
            }
            LabelAt(page,"Replace a default emote",24,410,550,28,true);
            emoteChoice=ComboAt(page,Reactions.Names,24,450,225,0);
            emotePreview=new PictureBox{Location=new Point(490,438),Size=new Size(68,62)};page.Controls.Add(emotePreview);
            ButtonAt(page,"Choose image…",24,498,190,delegate
            {
                using(var dialog=new OpenFileDialog{Filter="PNG image|*.png",Title="Replace "+Reactions.Names[emoteChoice.SelectedIndex]+" (up to 512 × 512)"})
                {if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{pet.ReplaceEmote(emoteChoice.SelectedIndex,dialog.FileName);}catch(Exception ex){ShowError(ex.Message);}}
            });
            ButtonAt(page,"Restore original",229,498,190,delegate{try{pet.RestoreEmote(emoteChoice.SelectedIndex);}catch(Exception ex){ShowError(ex.Message);}});
            replacementStatus=LabelAt(page,"",24,547,550,60,false);
            emoteChoice.SelectedIndexChanged+=delegate{RefreshEmotePreview();};RefreshEmotePreview();
            LabelAt(page,"Additional random emotes",24,610,550,28,true);
            LabelAt(page,"Add PNG images up to 512 × 512 pixels. Images appear on white inside the speech bubble and refresh automatically.",24,651,550,47,false);
            ButtonAt(page,"Open emote folder",24,708,195,delegate{pet.OpenEmoteFolder();});
            assetStatus.Location=new Point(24,758);assetStatus.Size=new Size(550,28);assetStatus.AutoEllipsis=true;assetStatus.Text=pet.EmoteStatus??"No custom emotes yet.";page.Controls.Add(assetStatus);
            LabelAt(page,"Your custom emotes",24,792,550,28,true);
            customEmoteList=new TableLayoutPanel{Name="CustomEmoteList",Location=new Point(24,826),Width=550,MinimumSize=new Size(550,0),AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
            customEmoteList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            customEmoteList.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));
            page.Controls.Add(customEmoteList);RefreshCustomEmotes();
            // Leave the existing personality controls below the name options.
            foreach(Control control in page.Controls)control.Top+=152;
            LabelAt(page,"Pet name (optional)",24,20,550,28,true);
            var petName=new TextBox{Text=pet.Model.Settings.PetName,MaxLength=40,Location=new Point(24,54),Width=350};page.Controls.Add(petName);
            var nameDisplay=ComboAt(page,new[]{"Hide name","Show on hover","Always display"},390,54,180,(int)pet.Model.Settings.NameDisplay);
            nameDisplay.Enabled=!string.IsNullOrWhiteSpace(petName.Text);
            LabelAt(page,"Leave blank for no name. Names appear below your pet with white-outlined letters and no background box.",24,96,550,48,false);
            petName.TextChanged+=delegate{pet.Model.Settings.PetName=Preferences.CleanName(petName.Text);nameDisplay.Enabled=pet.Model.Settings.PetName.Length>0;pet.NameChanged();};
            nameDisplay.SelectedIndexChanged+=delegate{pet.Model.Settings.NameDisplay=(NameVisibility)nameDisplay.SelectedIndex;pet.NameChanged();};
            petName.TabIndex=0;nameDisplay.TabIndex=1;personality.TabIndex=2;
        }
        void AddArtwork()
        {
            var page=Page("Sprite");
            LabelAt(page,"Make this pet your own",24,20,550,30,true);
            LabelAt(page,"Create a sprite in Sprite Maker or upload a .vpetsprite file.\nLegacy 5 × 10 PNG sheets are supported. Cells: up to 100 × 150 px.",24,59,570,50,false);
            ButtonAt(page,"Upload Custom Sprite",24,121,185,ChooseSheet);
            useButton.Text="Use this pet";useButton.Location=new Point(224,121);useButton.Size=new Size(155,36);useButton.Enabled=false;useButton.Click+=UsePending;page.Controls.Add(useButton);
            ButtonAt(page,"Restore default",394,121,175,delegate
            {
                try{pet.RestoreDefault();if(pending!=null){pending.Dispose();pending=null;}useButton.Enabled=false;importStatus.Text="Default pet restored.";UpdateSheetPreview();}
                catch(Exception ex){ShowError(ex.Message);}
            });
            importStatus.Location=new Point(24,170);importStatus.Size=new Size(550,40);importStatus.Text="Current pet: "+(pet.Model.Settings.CustomPet?"custom artwork":"original purple companion");page.Controls.Add(importStatus);
            previewActivity=ComboAt(page,new[]{"Idle","Walk"},24,219,155,1);
            previewDirection=ComboAt(page,new[]{"Right","Down-right","Down","Down-left","Left","Up-left","Up","Up-right"},194,219,185,2);
            animationPreview=new DoubleBufferedPanel{Location=new Point(24,265),Size=new Size(545,140),BackColor=Color.FromArgb(233,228,242)};page.Controls.Add(animationPreview);
            animationPreview.Paint+=delegate(object sender,PaintEventArgs e)
            {
                var sprites=pending??pet.Sprites;bool walk=previewActivity.SelectedIndex==1;
                int frame=(int)(previewClock.Elapsed.TotalSeconds*(walk?8:4));
                float scale=Math.Min(3,Math.Min(130f/sprites.Cell.Width,120f/sprites.Cell.Height));
                int w=(int)(sprites.Cell.Width*scale),h=(int)(sprites.Cell.Height*scale);
                e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
                e.Graphics.DrawImage(sprites.Frame(walk,previewDirection.SelectedIndex,frame),new Rectangle((545-w)/2,(140-h)/2,w,h));
            };
            LabelAt(page,"Runtime sheet preview",24,421,290,28,true);
            sheetPreview=new PictureBox{Location=new Point(24,458),Size=new Size(200,300),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(233,228,242)};page.Controls.Add(sheetPreview);UpdateSheetPreview();
            LabelAt(page,"Rows, from top to bottom\n\nIdle: up, down, side, up-diagonal, down-diagonal\n\nWalk: up, down, side, up-diagonal, down-diagonal\n\nOptional emotes: Music, Love, Question, Anger, Sad, Fear, Disgust, Proud.\nDownloaded directional artwork faces left.",247,458,310,200,false);
            ButtonAt(page,"Save default template…",247,660,285,delegate
            {
                using(var dialog=new SaveFileDialog{Filter="PNG image|*.png",FileName="vpet-template.png"})if(dialog.ShowDialog(this)==DialogResult.OK)
                {try{using(var original=SpriteSet.FromReference(pet.ReferencePath))original.Sheet.Save(dialog.FileName,ImageFormat.Png);}catch(Exception ex){ShowError(ex.Message);}}
            });
            ButtonAt(page,"Download current sprite sheet…",247,706,285,delegate
            {
                using(var dialog=new SaveFileDialog{Filter="PNG image|*.png",FileName="vpet-current-sheet.png"})if(dialog.ShowDialog(this)==DialogResult.OK)
                {try{pet.Sprites.Sheet.Save(dialog.FileName,ImageFormat.Png);}catch(Exception ex){ShowError(ex.Message);}}
            });
            LabelAt(page,"Download the active pet's transparent PNG sheet to edit in your art program, then upload it to Sprite Maker. Rows follow the animation buttons; optional emote rows follow walking rows. Use the How to Guide to select frames and export a usable sprite.",247,756,310,115,false);
            foreach(Control control in page.Controls)if(control.Top>=170)control.Top+=44;
            ButtonAt(page,"Open Sprite Maker",24,165,185,delegate
            {
                using(var maker=new SpriteMakerWindow{Icon=Icon})
                {
                    maker.ShowDialog(this);
                    if(maker.ExportedPath!=null)try{LoadSprite(maker.ExportedPath);}catch(Exception ex){ShowError(ex.Message);}
                }
            });
            foreach(Control control in page.Controls)control.Top+=272;
            LabelAt(page,"Load Vpet on PC startup",24,20,550,28,true);
            var startup=ComboAt(page,new[]{"No (Default)","Yes"},24,56,250,pet.Model.Settings.LaunchOnStartup?1:0);
            LabelAt(page,"Yes opens your pet automatically when you sign in to Windows. Choose No to turn this off.",24,94,550,42,false);
            startup.TabIndex=0;
            bool resetting=false;
            startup.SelectedIndexChanged+=delegate
            {
                if(resetting)return;
                try{pet.SetLaunchOnStartup(startup.SelectedIndex==1);}
                catch(Exception ex)
                {
                    resetting=true;startup.SelectedIndex=pet.Model.Settings.LaunchOnStartup?1:0;resetting=false;
                    MessageBox.Show(this,ex.Message,"Could not change startup setting",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            };
            LabelAt(page,"Auto-update on app startup",24,156,550,28,true);
            var autoUpdate=ComboAt(page,new[]{"No (Default)","Yes"},24,192,250,pet.Model.Settings.AutoUpdate?1:0);
            autoUpdate.Name="AutoUpdate";autoUpdate.TabIndex=1;
            LabelAt(page,"Yes installs new releases automatically when Vpet starts. Check for updates always installs directly. Changes appear after the update completes.",24,230,550,55,false);
            autoUpdate.SelectedIndexChanged+=delegate{pet.Model.Settings.AutoUpdate=autoUpdate.SelectedIndex==1;pet.Save();};
        }
        void ChooseSheet(object sender,EventArgs e)
        {
            using(var dialog=new OpenFileDialog{Filter="Vpet sprites|*.vpetsprite;*.png|Vpet sprite package|*.vpetsprite|Legacy PNG sheet|*.png",Title="Upload Custom Sprite"})
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
            var label=new Label{Text=name,UseMnemonic=false,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill,Margin=new Padding(0,4,8,4)};
            var button=new Button{Text="Try It Out",AccessibleName="Try "+name,Height=height,Dock=DockStyle.Fill,Margin=new Padding(0,4,0,4),FlatStyle=FlatStyle.Flat,BackColor=Color.White};
            button.FlatAppearance.BorderColor=Color.FromArgb(205,194,222);button.Click+=preview;
            customEmoteList.Controls.Add(label,0,row);customEmoteList.Controls.Add(button,1,row);customEmoteNames.SetToolTip(label,name);
        }
        void AssetsChanged(){assetStatus.Text=pet.EmoteStatus??"No custom emotes yet.";RefreshEmotePreview();RefreshCustomEmotes();customEmoteNames.SetToolTip(assetStatus,assetStatus.Text);}
        void ShowError(string message){MessageBox.Show(this,message,"Could not load artwork",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    internal sealed class DoubleBufferedPanel : Panel {public DoubleBufferedPanel(){DoubleBuffered=true;}}
}
