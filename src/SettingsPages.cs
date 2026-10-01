using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class SettingsWindow
    {
        static TableLayoutPanel Stack()
        {
            var panel=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,Margin=Padding.Empty};
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));return panel;
        }
        static void Add(TableLayoutPanel stack,Control control)
        {int row=stack.RowCount++;stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));control.Dock=DockStyle.Fill;control.Margin=new Padding(0,4,0,10);stack.Controls.Add(control,0,row);}
        static Label Copy(string text,bool title=false)
        {return new Label{Text=text,AutoSize=true,Dock=DockStyle.Fill,Font=new Font("Segoe UI",title?11:10,title?FontStyle.Bold:FontStyle.Regular),ForeColor=Color.FromArgb(54,45,70)};}
        static ComboBox Choice(string[] items,int selected,int width=260)
        {var box=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=width,Margin=new Padding(0,4,12,4)};box.Items.AddRange(items);box.SelectedIndex=selected;return box;}
        static FlowLayoutPanel Row(params Control[] controls)
        {var row=MakerUi.Flow();row.Padding=Padding.Empty;row.Margin=Padding.Empty;row.Controls.AddRange(controls);return row;}
        TableLayoutPanel PageStack(string name)
        {var page=Page(name);page.Padding=new Padding(20);var stack=Stack();page.Controls.Add(stack);return stack;}
        void AddMovement()
        {
            var page=PageStack("Movement");Add(page,Copy("Where should your pet wander?",true));
            var movement=Choice(new[]{"Free Roam","Restricted","Static"},(int)pet.Model.Settings.Movement);Add(page,Row(movement));
            var description=Copy(Names.MovementDescription(pet.Model.Settings.Movement));Add(page,description);
            var restricted=Stack();Add(page,restricted);
            restrictedAreaCheckbox=new CheckBox{Text="Display restricted area",AutoSize=true,Checked=pet.Model.RestrictedAreaVisible,Font=new Font(Font,FontStyle.Bold)};Add(restricted,restrictedAreaCheckbox);
            Add(restricted,Copy("Turn on Display restricted area to see the fence. Drag the + to move it; drag an edge or corner to resize it. If the fence excludes your pet, the pet moves to its center. Advanced settings control whether this uses the play-zone fence."));
            var speedLabel=Copy("",true);Add(page,speedLabel);
            var speed=new TrackBar{Name="WalkingSpeed",Minimum=0,Maximum=100,Value=pet.Model.Settings.Speed,TickFrequency=10,AutoSize=true};Add(page,speed);
            var ticks=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3};for(int i=0;i<3;i++)ticks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
            ticks.Controls.Add(Copy("0"),0,0);var middle=Copy("50");middle.TextAlign=ContentAlignment.MiddleCenter;ticks.Controls.Add(middle,1,0);var last=Copy("100");last.TextAlign=ContentAlignment.MiddleRight;ticks.Controls.Add(last,2,0);Add(page,ticks);
            Add(page,Copy("Left-click for Love and a half-second shake. Dragging uses down-facing idle; release rests for five seconds."));
            Add(page,Copy("Window location",true));var layer=Choice(new[]{"Over Everything","Under All","Dynamic"},(int)pet.Model.Settings.Layer);Add(page,Row(layer));
            var layerDescription=Copy(Names.LayerDescription(pet.Model.Settings.Layer));Add(page,layerDescription);
            Action describe=delegate{description.Text=Names.MovementDescription(pet.Model.Settings.Movement);restricted.Visible=pet.Model.Settings.Movement==MovementMode.Restricted;speedLabel.Text="Walking speed: "+speed.Value+" / 100 ("+(speed.Value*2)+" px/sec at 100% scaling)";};
            movement.SelectedIndexChanged+=delegate{pet.Model.ChangeMode((MovementMode)movement.SelectedIndex,pet.Now);describe();SyncRestrictedAreaVisibility();pet.SettingsChanged(false);};
            restrictedAreaCheckbox.CheckedChanged+=delegate{if(pet.Model.RestrictedAreaVisible!=restrictedAreaCheckbox.Checked){pet.Model.SetRestrictedAreaVisible(restrictedAreaCheckbox.Checked);pet.SettingsChanged(false);}};
            speed.ValueChanged+=delegate{pet.Model.Settings.Speed=speed.Value;describe();pet.SettingsChanged(false);};
            layer.SelectedIndexChanged+=delegate{pet.Model.Settings.Layer=(LayerMode)layer.SelectedIndex;layerDescription.Text=Names.LayerDescription(pet.Model.Settings.Layer);pet.ApplyLayer();pet.SettingsChanged(false);};describe();
        }
        void AddPersonality()
        {
            var page=PageStack("Personality");Add(page,Copy("Pet name (optional)",true));
            var name=new TextBox{Text=pet.Model.Settings.PetName,MaxLength=40,Width=285,Margin=new Padding(0,4,12,4)};
            var display=Choice(new[]{"Hide name","Show on hover","Always display"},(int)pet.Model.Settings.NameDisplay,180);display.Enabled=!string.IsNullOrWhiteSpace(name.Text);Add(page,Row(name,display));
            Add(page,Copy("Leave blank for no name. Names appear below your pet with white-outlined letters and no background box."));
            name.TextChanged+=delegate{pet.Model.Settings.PetName=Preferences.CleanName(name.Text);display.Enabled=pet.Model.Settings.PetName.Length>0;pet.NameChanged();};
            display.SelectedIndexChanged+=delegate{pet.Model.Settings.NameDisplay=(NameVisibility)display.SelectedIndex;pet.NameChanged();};
            Add(page,Copy("A personality of their own",true));var personality=Choice(new[]{"Sweet","Sassy","Bashful"},(int)pet.Model.Settings.Personality);Add(page,Row(personality));var description=Copy("");Add(page,description);
            Action describe=delegate
            {
                var selected=pet.Model.Settings.Personality;string favorites=selected==Personality.Sweet?"Music, Love, and Question":selected==Personality.Sassy?"Anger, Fear, and Proud":"Question, Sad, and Disgust";
                description.Text="Hover: "+Reactions.Names[Reactions.Hover(selected)]+". Pick up and drag: "+Reactions.Names[Reactions.Pickup(selected)]+". Click: Love.\nRandom reactions favor "+favorites+". All nine default emotes can appear randomly.";
            };describe();personality.SelectedIndexChanged+=delegate{pet.Model.Settings.Personality=(Personality)personality.SelectedIndex;describe();pet.SettingsChanged(false);};
            Add(page,Copy("Random reactions",true));var frequency=Choice(new[]{"Rarely · 90–120 seconds","Sometimes · 30–60 seconds","Often · 15–30 seconds","Off"},(int)pet.Model.Settings.Frequency,330);Add(page,Row(frequency));
            frequency.SelectedIndexChanged+=delegate{pet.Model.Settings.Frequency=(Frequency)frequency.SelectedIndex;pet.SettingsChanged(true);};
            Add(page,Copy("Try a reaction",true));var reactions=Row();for(int i=0;i<Reactions.Names.Length;i++){int index=i;reactions.Controls.Add(MakerUi.Button(Reactions.Names[i],delegate{pet.PreviewReaction(index);}));}Add(page,reactions);
            Add(page,Copy("Replace a default emote",true));emoteChoice=Choice(Reactions.Names,0,225);emotePreview=new PictureBox{Size=new Size(68,62),Margin=new Padding(12,0,0,0)};Add(page,Row(emoteChoice,emotePreview));
            Add(page,Row(MakerUi.Button("Choose image…",delegate
            {
                using(var dialog=new OpenFileDialog{Filter="PNG image|*.png",Title="Replace "+Reactions.Names[emoteChoice.SelectedIndex]+" (up to 512 × 512)"})
                {if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{pet.ReplaceEmote(emoteChoice.SelectedIndex,dialog.FileName);}catch(Exception ex){ShowError(ex.Message);}}
            }),MakerUi.Button("Restore original",delegate{try{pet.RestoreEmote(emoteChoice.SelectedIndex);}catch(Exception ex){ShowError(ex.Message);}})));
            replacementStatus=Copy("");Add(page,replacementStatus);emoteChoice.SelectedIndexChanged+=delegate{RefreshEmotePreview();};RefreshEmotePreview();
            Add(page,Copy("Additional random emotes",true));Add(page,Copy("Add PNG images up to 512 × 512 pixels. Images appear on white inside the speech bubble and refresh automatically."));
            Add(page,Row(MakerUi.Button("Open emote folder",delegate{pet.OpenEmoteFolder();})));
            assetStatus.AutoSize=true;assetStatus.AutoEllipsis=false;assetStatus.Text=pet.EmoteStatus??"No custom emotes yet.";Add(page,assetStatus);
            Add(page,Copy("Your custom emotes",true));customEmoteList=new TableLayoutPanel{Name="CustomEmoteList",AutoSize=true,ColumnCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
            customEmoteList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));customEmoteList.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));Add(page,customEmoteList);RefreshCustomEmotes();
        }
        void AddArtwork()
        {
            var page=PageStack("Sprite");Add(page,Copy("Load Vpet on PC startup",true));
            var startup=Choice(new[]{"No (Default)","Yes"},pet.Model.Settings.LaunchOnStartup?1:0);Add(page,Row(startup));
            Add(page,Copy("Yes opens your pet automatically when you sign in to Windows. Choose No to turn this off."));bool resetting=false;
            startup.SelectedIndexChanged+=delegate
            {
                if(resetting)return;try{pet.SetLaunchOnStartup(startup.SelectedIndex==1);}
                catch(Exception ex){resetting=true;startup.SelectedIndex=pet.Model.Settings.LaunchOnStartup?1:0;resetting=false;MessageBox.Show(this,ex.Message,"Could not change startup setting");}
            };
            Add(page,Copy("Auto-update on app startup",true));var autoUpdate=Choice(new[]{"No (Default)","Yes"},pet.Model.Settings.AutoUpdate?1:0);autoUpdate.Name="AutoUpdate";Add(page,Row(autoUpdate));
            Add(page,Copy("Yes installs new releases automatically when Vpet starts. Check for updates shows the version and an Update button. Changes appear after installation."));autoUpdate.SelectedIndexChanged+=delegate{pet.Model.Settings.AutoUpdate=autoUpdate.SelectedIndex==1;pet.Save();};
            Add(page,Copy("Make this pet your own",true));Add(page,Copy("Create a sprite in Sprite Maker or upload a .vpetsprite file. Each animation can use frames up to 100 × 150 pixels."));
            useButton.Text="Use this pet";useButton.AutoSize=true;useButton.MinimumSize=new Size(125,36);useButton.Enabled=false;useButton.Click+=UsePending;
            Add(page,Row(MakerUi.Button("Upload Custom Sprite",ChooseSheet),useButton,MakerUi.Button("Restore default",delegate
            {try{pet.RestoreDefault();if(pending!=null){pending.Dispose();pending=null;}useButton.Enabled=false;importStatus.Text="Default pet restored.";UpdateSheetPreview();}catch(Exception ex){ShowError(ex.Message);}})));
            Add(page,Row(MakerUi.Button("Open Sprite Maker",delegate
            {
                using(var maker=new SpriteMakerWindow(pet.Model.Settings,pet.Save){Icon=Icon})
                {maker.ShowDialog(this);if(maker.ExportedPath!=null)try{LoadSprite(maker.ExportedPath);}catch(Exception ex){ShowError(ex.Message);}}
            })));
            importStatus.AutoSize=true;importStatus.Text="Current pet: "+(pet.Model.Settings.CustomPet?"custom artwork":"original purple companion");Add(page,importStatus);
            previewActivity=Choice(new[]{"Idle","Walk"},1,155);previewDirection=Choice(new[]{"Right","Down-right","Down","Down-left","Left","Up-left","Up","Up-right"},2,185);Add(page,Row(previewActivity,previewDirection));
            animationPreview=new DoubleBufferedPanel{Height=160,BackColor=Color.FromArgb(233,228,242)};Add(page,animationPreview);
            animationPreview.Paint+=delegate(object sender,PaintEventArgs e)
            {
                var sprites=pending??pet.Sprites;bool walk=previewActivity.SelectedIndex==1;double phase=previewClock.Elapsed.TotalSeconds*(walk?8:4);
                float scale=Math.Min(3,Math.Min((animationPreview.ClientSize.Width-20f)/sprites.Cell.Width,140f/sprites.Cell.Height));int w=(int)(sprites.Cell.Width*scale),h=(int)(sprites.Cell.Height*scale);
                e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
                e.Graphics.DrawImage(sprites.FrameAtPhase(walk,previewDirection.SelectedIndex,phase),new Rectangle((animationPreview.ClientSize.Width-w)/2,(animationPreview.ClientSize.Height-h)/2,w,h));
            };
            Add(page,Copy("Runtime sheet preview",true));sheetPreview=new PictureBox{Height=300,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(233,228,242)};Add(page,sheetPreview);UpdateSheetPreview();
            Add(page,Copy("Rows: idle up, down, side, up-diagonal, down-diagonal; then the five walking animations. Optional emotes follow: Music, Love, Question, Anger, Sad, Fear, Disgust, Proud, Hunger. Downloaded directional artwork faces left."));
            Add(page,Row(MakerUi.Button("Save default template…",delegate
            {
                using(var dialog=new SaveFileDialog{Filter="PNG image|*.png",FileName="vpet-template.png"})if(dialog.ShowDialog(this)==DialogResult.OK)
                {try{using(var original=SpriteSet.FromReference(pet.ReferencePath))original.Sheet.Save(dialog.FileName,ImageFormat.Png);}catch(Exception ex){ShowError(ex.Message);}}
            })));
            Add(page,Row(MakerUi.Button("Download current sprite sheet…",delegate
            {
                using(var dialog=new SaveFileDialog{Filter="PNG image|*.png",FileName="vpet-current-sheet.png"})if(dialog.ShowDialog(this)==DialogResult.OK)
                {try{pet.Sprites.Sheet.Save(dialog.FileName,ImageFormat.Png);}catch(Exception ex){ShowError(ex.Message);}}
            })));
            Add(page,Copy("Download the active pet's transparent PNG sheet to edit in your art program, then upload it to Sprite Maker. Use the How to Guide to select frames and export a usable sprite."));
        }
        void AddAdvanced()
        {
            var page=PageStack("Advanced");Add(page,Copy("Movement and toy play space",true));
            var sync=new CheckBox{Name="SyncPlayZone",Text="Sync Play Zone with Restricted Area",AutoSize=true,Checked=pet.Model.Settings.SyncPlayZone};Add(page,sync);
            Add(page,Copy("On by default. In Restricted movement, your pet uses the play-zone fence and the separate restricted fence is hidden. Display restricted area and Display Play Zone control the same fence. You can move or resize it even with the toy chest hidden.\n\nTurn this off to keep the pet's restricted fence and toy play zone independent. Free Roam and Static still work normally."));
            sync.CheckedChanged+=delegate{pet.Model.Settings.SyncPlayZone=sync.Checked;pet.Model.CancelRoute();pet.Model.EnsureInsideRestrictedArea();SyncRestrictedAreaVisibility();pet.SettingsChanged(false);};
        }
    }
}
