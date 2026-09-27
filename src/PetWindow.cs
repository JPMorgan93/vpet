using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class PetWindow : LayeredWindow
    {
        public readonly PetModel Model;
        public readonly string DataDirectory;
        public readonly string ReferencePath;
        public SpriteSet Sprites { get; private set; }
        public readonly List<CustomEmote> CustomEmotes=new List<CustomEmote>();
        public readonly EmoteReplacements Replacements;
        public string EmoteStatus { get; private set; }
        public double Now { get {return clock.Elapsed.TotalSeconds;} }
        internal int ActiveReaction {get{return reaction;}}
        public event Action AssetsChanged;
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly Random random=new Random();
        readonly Timer timer=new Timer{Interval=16};
        readonly LayeredWindow bubble=new LayeredWindow(true);
        readonly LayeredWindow crossingWindow=new LayeredWindow(false);
        readonly RestrictedAreaOverlay restrictedOverlay;
        readonly NotifyIcon tray=new NotifyIcon();
        readonly ContextMenuStrip menu=new ContextMenuStrip();
        readonly MenuDismissal menuDismissal;
        SettingsWindow settingsWindow;
        Bitmap rendered,crossingRendered,customReaction;
        Bitmap reactionPreview;
        float reactionPreviewScale;
        bool reactionPreviewBelow;
        Form dragWindow;
        double previousTime,phase,lastHover=-10,bubbleUntil,nextRandom,nextDisplayCheck,nextAssetCheck,nextSave;
        bool lastWalk,buttonDown,moved,menuOpen,closing;
        Point mouseStart;
        PointF dragStart;
        int reaction=-1;
        string displaySignature="",assetSignature="";
        readonly bool smoke;
        readonly string smokeOutput;
        int smokeStep;
        double nextUpdateCheck=10;
        bool checkingUpdate,installingUpdate;
        AvailableUpdate availableUpdate;
        string notifiedVersion;
        ToolStripMenuItem installUpdate;

        public PetWindow(string dataDirectory,string referencePath,bool smoke,string smokeOutput) : base(false)
        {
            this.smoke=smoke;this.smokeOutput=smokeOutput;DataDirectory=dataDirectory;ReferencePath=referencePath;
            Directory.CreateDirectory(DataDirectory);Directory.CreateDirectory(EmoteDirectory);
            Replacements=new EmoteReplacements(Path.Combine(DataDirectory,"DefaultEmotes"));
            var prefs=Preferences.Load(Path.Combine(DataDirectory,"settings.json"));
            if(float.IsNaN(prefs.X)||float.IsNaN(prefs.Y))
            {
                var launchArea=Screen.FromPoint(Cursor.Position).WorkingArea;
                prefs.X=launchArea.Left+launchArea.Width*.6f;prefs.Y=launchArea.Top+launchArea.Height*.7f;
            }
            Sprites=SpriteSet.FromReference(referencePath);
            if(prefs.CustomPet)
            {
                try {var custom=SpriteSet.Import(Path.Combine(DataDirectory,"pet.png"));Sprites.Dispose();Sprites=custom;}
                catch(Exception ex){prefs.CustomPet=false;File.WriteAllText(Path.Combine(DataDirectory,"asset-error.txt"),ex.Message);}
            }
            Model=new PetModel(prefs,random);Model.FrameSize=Sprites.Cell;
            RefreshDisplays();Model.IdleUntil=Now+2;
            restrictedOverlay=new RestrictedAreaOverlay(Model,Save,this,crossingWindow);
            Text="Vpet";bubble.Text="Vpet reaction";bubble.Owner=this;
            BuildMenu();ContextMenuStrip=menu;
            menuDismissal=new MenuDismissal(menu);
            using(var icon=new Icon(Path.Combine(Path.GetDirectoryName(referencePath),"Vpet.ico")))
            {tray.Icon=(Icon)icon.Clone();Icon=(Icon)icon.Clone();}
            tray.Text="Vpet · right-click for controls";tray.ContextMenuStrip=menu;tray.Visible=!smoke;
            if(!smoke&&prefs.LaunchOnStartup)
            {try{StartupRegistration.SetEnabled(true,Application.ExecutablePath);}catch(Exception ex){Notify("Startup could not be enabled",ex.Message);}}
            tray.DoubleClick+=delegate{OpenSettings(0);};
            tray.BalloonTipClicked+=delegate{if(availableUpdate!=null)OfferUpdate();};
            MouseDown+=BeginDrag;MouseMove+=ContinueDrag;MouseUp+=EndDrag;
            MouseCaptureChanged+=delegate {if(buttonDown&&dragWindow==this&&!Capture)FinishDrag(false);};
            crossingWindow.ContextMenuStrip=menu;crossingWindow.MouseDown+=BeginDrag;crossingWindow.MouseMove+=ContinueDrag;crossingWindow.MouseUp+=EndDrag;
            crossingWindow.MouseCaptureChanged+=delegate{if(buttonDown&&dragWindow==crossingWindow&&!crossingWindow.Capture)FinishDrag(false);};
            timer.Tick+=Tick;
            Shown+=delegate{ApplyLayer();RefreshEmotes();ResetReactionTimer();timer.Start();};
            FormClosing+=OnClosing;
        }
        protected override void WndProc(ref Message message)
        {
            // Refresh on display/work-area notifications; polling also covers shell and DPI changes.
            if(message.Msg==0x007E||message.Msg==0x001A)nextDisplayCheck=0;
            base.WndProc(ref message);
        }
        public string EmoteDirectory {get{return Path.Combine(DataDirectory,"Emotes");}}
        public bool SettingsOpen {get{return settingsWindow!=null&&!settingsWindow.IsDisposed;}}
        void BuildMenu()
        {
            var movement=new ToolStripMenuItem("Movement Controls");
            var type=new ToolStripMenuItem("Type");
            foreach(MovementMode value in Enum.GetValues(typeof(MovementMode)))
            {
                var captured=value;var item=new ToolStripMenuItem(Names.Movement(value)){Tag=value};
                item.Click+=delegate{Model.ChangeMode(captured,Now);Save();};type.DropDownItems.Add(item);
            }
            movement.DropDownItems.Add(type);movement.DropDownItems.Add("Speed and radius…",null,delegate{OpenSettings(0);});
            var displayArea=new ToolStripMenuItem("Display restricted area"){CheckOnClick=true};
            displayArea.Click+=delegate
            {
                Model.Settings.DisplayRestrictedArea=displayArea.Checked;
                if(SettingsOpen)settingsWindow.SyncRestrictedAreaVisibility();
                SettingsChanged(false);
            };
            movement.DropDownItems.Add(displayArea);
            var layer=new ToolStripMenuItem("Location");
            foreach(LayerMode value in Enum.GetValues(typeof(LayerMode)))
            {var captured=value;var item=new ToolStripMenuItem(Names.Layer(value)){Tag=value};item.Click+=delegate{Model.Settings.Layer=captured;ApplyLayer();Save();};layer.DropDownItems.Add(item);}
            movement.DropDownItems.Add(layer);menu.Items.Add(movement);
            var personality=new ToolStripMenuItem("Personality");
            foreach(Personality value in Enum.GetValues(typeof(Personality)))
            {var captured=value;var item=new ToolStripMenuItem(value.ToString()){Tag=value};item.Click+=delegate{Model.Settings.Personality=captured;Save();};personality.DropDownItems.Add(item);}
            personality.DropDownItems.Add(new ToolStripSeparator());personality.DropDownItems.Add("Emote frequency…",null,delegate{OpenSettings(1);});
            personality.DropDownItems.Add("Custom emote folder…",null,delegate{OpenEmoteFolder();});menu.Items.Add(personality);
            menu.Items.Add("Upload Vpet…",null,delegate{OpenSettings(2);});
            menu.Items.Add("Settings…",null,delegate{OpenSettings(0);});
            menu.Items.Add("Check for updates…",null,delegate{CheckForUpdates(true);});
            installUpdate=new ToolStripMenuItem("Install update…"){Visible=false};installUpdate.Click+=delegate{OfferUpdate();};menu.Items.Add(installUpdate);
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Close Vpet",null,delegate{Close();});
            menu.Opening+=delegate
            {
                menuOpen=true;
                displayArea.Checked=Model.Settings.DisplayRestrictedArea;
                displayArea.Enabled=Model.Settings.Movement==MovementMode.Restricted;
                foreach(ToolStripMenuItem item in type.DropDownItems)item.Checked=(MovementMode)item.Tag==Model.Settings.Movement;
                foreach(ToolStripMenuItem item in layer.DropDownItems)item.Checked=(LayerMode)item.Tag==Model.Settings.Layer;
                foreach(ToolStripItem raw in personality.DropDownItems){var item=raw as ToolStripMenuItem;if(item!=null&&item.Tag is Personality)item.Checked=(Personality)item.Tag==Model.Settings.Personality;}
            };
            menu.Closed+=delegate{menuOpen=false;};
        }
        public void OpenSettings(int tab)
        {
            if(!SettingsOpen){settingsWindow=new SettingsWindow(this);settingsWindow.FormClosed+=delegate{settingsWindow=null;Save();};settingsWindow.Show();}
            settingsWindow.SelectTab(tab);settingsWindow.Activate();
        }
        public void OpenEmoteFolder(){Process.Start(new ProcessStartInfo(EmoteDirectory){UseShellExecute=true});}
        public void ApplyLayer()
        {
            // An owned popup can promote its owner. Under All locks the two windows independently.
            bubble.Owner=Model.Settings.Layer==LayerMode.UnderAll?null:this;
            crossingWindow.Owner=Model.Settings.Layer==LayerMode.UnderAll?null:this;
            CompanionHandle=bubble.Handle;OtherCompanionHandle=crossingWindow.Handle;
            bubble.CompanionHandle=Handle;bubble.OtherCompanionHandle=crossingWindow.Handle;
            crossingWindow.CompanionHandle=Handle;crossingWindow.OtherCompanionHandle=bubble.Handle;
            SetLayer(Model.Settings.Layer);bubble.SetLayer(Model.Settings.Layer);crossingWindow.SetLayer(Model.Settings.Layer);
            restrictedOverlay.Update();
        }
        public void SettingsChanged(bool resetReaction)
        {
            if(resetReaction)ResetReactionTimer();restrictedOverlay.Update();Save();
        }
        public void NameChanged()
        {
            Model.UpdateNameFootroom();Model.Place(Model.Position);Model.CancelRoute();Save();
        }
        public void SetLaunchOnStartup(bool enabled)
        {
            if(!smoke)StartupRegistration.SetEnabled(enabled,Application.ExecutablePath);
            Model.Settings.LaunchOnStartup=enabled;Save();
        }
        public void ResetReactionTimer(){nextRandom=Now+Reactions.Interval(Model.Settings.Frequency,random);}
        public void PreviewReaction(int index){ShowReaction(index);}
        public void PreviewCustomEmote(string name)
        {
            // Resolve the current name after a refresh, since adding/removing files changes indices.
            RefreshEmotes();
            int index=CustomEmotes.FindIndex(emote=>string.Equals(emote.Name,name,StringComparison.OrdinalIgnoreCase));
            if(index>=0)ShowReaction(Reactions.Names.Length+index);
        }
        public void ReplaceEmote(int index,string path){Replacements.Replace(index,path);ShowReaction(index);if(AssetsChanged!=null)AssetsChanged();}
        public void RestoreEmote(int index){Replacements.Restore(index);ShowReaction(index);if(AssetsChanged!=null)AssetsChanged();}
        void ShowReaction(int index)
        {
            if(reactionPreview!=null){reactionPreview.Dispose();reactionPreview=null;}
            if(customReaction!=null){customReaction.Dispose();customReaction=null;}
            reaction=index;
            if(index>=8)
            {if(index-8>=CustomEmotes.Count){reaction=-1;return;}customReaction=(Bitmap)CustomEmotes[index-8].Image.Clone();}
            else if(Replacements.Get(index)!=null)customReaction=(Bitmap)Replacements.Get(index).Clone();
            bubbleUntil=Now+3;nextRandom=bubbleUntil+Reactions.Interval(Model.Settings.Frequency,random);
        }
        Bitmap ReactionImage(float scale,bool below)
        {
            // Resample a high-resolution emote once, not on every animation tick.
            if(reactionPreview==null||reactionPreviewScale!=scale||reactionPreviewBelow!=below)
            {
                if(reactionPreview!=null)reactionPreview.Dispose();
                reactionPreview=Artwork.Bubble(reaction,customReaction,scale,below);
                reactionPreviewScale=scale;reactionPreviewBelow=below;
            }
            return (Bitmap)reactionPreview.Clone();
        }
        void BeginDrag(object sender,MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;
            buttonDown=true;moved=false;mouseStart=Cursor.Position;dragStart=Model.Position;dragWindow=(Form)sender;
            Model.FaceDownIdle();phase=0;lastHover=Now;
            if(Model.Crossing!=null)dragStart=dragWindow==crossingWindow?Model.Crossing.DestinationAnchor:Model.Crossing.SourceAnchor;
            dragWindow.Capture=true;
            if(Model.Settings.Layer==LayerMode.Dynamic)Native.SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x13);
        }
        void ContinueDrag(object sender,MouseEventArgs e)
        {
            if(!buttonDown)return;
            Point cursor=Cursor.Position;int dx=cursor.X-mouseStart.X,dy=cursor.Y-mouseStart.Y;
            if(!moved&&Math.Abs(dx)+Math.Abs(dy)<4)return;
            if(!moved){moved=true;Model.Dragging=true;Model.FaceDownIdle();Model.CancelRoute();Model.ShakeUntil=0;ShowReaction(Reactions.Pickup(Model.Settings.Personality));}
            Model.Place(new PointF(dragStart.X+dx,dragStart.Y+dy));
        }
        void EndDrag(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)FinishDrag();}
        void FinishDrag(bool clickAllowed=true)
        {
            if(!buttonDown)return;buttonDown=false;if(dragWindow!=null)dragWindow.Capture=false;dragWindow=null;
            if(moved){Model.Release(Now);Save();}
            else if(clickAllowed){ShowReaction(Model.Click(Now));phase=0;lastHover=Now;}
            moved=false;
        }
        bool IsHovered()
        {
            return Hit(this,rendered)||(crossingWindow.Visible&&Hit(crossingWindow,crossingRendered));
        }
        static bool Hit(Form window,Bitmap image)
        {if(image==null)return false;Point p=window.PointToClient(Cursor.Position);return p.X>=0&&p.Y>=0&&p.X<image.Width&&p.Y<image.Height&&image.GetPixel(p.X,p.Y).A>0;}
        void Tick(object sender,EventArgs e)
        {
            double now=Now;float dt=(float)Math.Min(.1,now-previousTime);previousTime=now;
            if(now>=nextDisplayCheck){RefreshDisplays();nextDisplayCheck=now+2;}
            if(now>=nextAssetCheck){RefreshEmotes();nextAssetCheck=now+3;}
            bool hovering=IsHovered();
            if(hovering&&!Model.Hovered&&!buttonDown&&!menuOpen&&!SettingsOpen&&now-lastHover>=5)
            {ShowReaction(Reactions.Hover(Model.Settings.Personality));lastHover=now;}
            Model.Hovered=hovering;Model.Paused=menuOpen||SettingsOpen||buttonDown||restrictedOverlay.Dragging;
            Model.Tick(now,dt);
            if(Model.Walking!=lastWalk){phase=0;lastWalk=Model.Walking;}
            else phase+=dt*(Model.Walking?8*Model.ActualSpeed/(100*Model.Current.Scale):4);
            if(now>=nextRandom&&!Model.Dragging&&!Model.Shaking(now)&&!Model.Paused&&now>=bubbleUntil)
                ShowReaction(Reactions.Choose(Model.Settings.Personality,8+CustomEmotes.Count,random));
            Render();restrictedOverlay.Update();
            if(now>=nextSave){Save();nextSave=now+15;}
            if(smoke)SmokeStep(now);
            else if(now>=nextUpdateCheck&&!checkingUpdate&&!installingUpdate)CheckForUpdates(false);
        }
        async void CheckForUpdates(bool manual)
        {
            if(checkingUpdate||installingUpdate)return;checkingUpdate=true;nextUpdateCheck=Now+6*60*60;
            try
            {
                availableUpdate=await Task.Factory.StartNew<AvailableUpdate>(Updates.Check);
                if(closing)return;
                installUpdate.Visible=availableUpdate!=null;
                if(availableUpdate!=null)
                {
                    installUpdate.Text="Install Vpet "+availableUpdate.Version+"…";
                    if(manual)OfferUpdate();
                    else if(notifiedVersion!=availableUpdate.Version){notifiedVersion=availableUpdate.Version;Notify("Vpet update available","Version "+availableUpdate.Version+" is ready. Click here or use the pet menu to install it.");}
                }
                else if(manual)MessageBox.Show("You have the latest public release ("+ReleaseInfo.Version+").","Vpet updates",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            catch(Exception ex)
            {
                nextUpdateCheck=Now+30*60;
                if(manual&&!closing)MessageBox.Show("Could not check for updates. Your pet will keep running.\n\n"+ex.Message,"Vpet updates",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            finally{checkingUpdate=false;}
        }
        async void OfferUpdate()
        {
            if(availableUpdate==null||installingUpdate||closing)return;
            var update=availableUpdate;
            if(MessageBox.Show("Update to Vpet "+update.Version+"? Your pet will briefly close during installation. Your name, settings, artwork, and shortcut choices will be kept.","Vpet update",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
            installingUpdate=true;installUpdate.Enabled=false;
            using(var progress=new UpdateProgressWindow(update.Version))
            {
            progress.Show();
            try
            {
                string hash=null;
                string path=await Task.Factory.StartNew(delegate{return Updates.Download(update,DataDirectory,out hash,progress.Report);});
                if(closing)return;
                Updates.StartInstallerAfterExit(path,hash);Close();
            }
            catch(Exception ex){if(!closing)MessageBox.Show("The update could not be installed. Your current pet is unchanged.\n\n"+ex.Message,"Vpet update",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{installingUpdate=false;if(!closing)installUpdate.Enabled=true;}
            }
        }
        void SmokeStep(double now)
        {
            Directory.CreateDirectory(smokeOutput);
            if(smokeStep==0&&now>1)
            {
                using(var preview=Artwork.Scale(Sprites.Frame(false,2,0),new Size(128,144)))preview.Save(Path.Combine(smokeOutput,"pet.png"));
                Model.ChangeMode(MovementMode.Restricted,now);restrictedOverlay.Update();
                Model.Settings.PetName="Mochi";Model.Settings.NameDisplay=NameVisibility.Always;NameChanged();
                ShowReaction(1);OpenSettings(0);smokeStep++;
            }
            else if(smokeStep>=1&&smokeStep<=3&&now>smokeStep+1)
            {
                using(var screenshot=new Bitmap(settingsWindow.Width,settingsWindow.Height))
                {settingsWindow.DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(Path.Combine(smokeOutput,"settings-"+(smokeStep-1)+".png"));}
                if(smokeStep<3)settingsWindow.SelectTab(smokeStep);
                else {settingsWindow.Close();Model.Settings.Layer=LayerMode.Dynamic;ApplyLayer();}
                smokeStep++;
            }
            else if(smokeStep==4&&now>5)
            {
                Model.Settings.Layer=LayerMode.UnderAll;ApplyLayer();smokeStep++;
                File.WriteAllText(Path.Combine(smokeOutput,"under-all-layer.txt"),"Requested UnderAll; actual "+Model.Settings.Layer+"; no desktop parenting required.");
            }
            else if(smokeStep==5&&now>6)
            {Model.Settings.Layer=LayerMode.OverEverything;ApplyLayer();ShowReaction(7);smokeStep++;}
            else if(smokeStep==6&&now>7)
            {
                File.WriteAllText(Path.Combine(smokeOutput,"smoke-result.txt"),"PASS: layered pet and reaction windows rendered; three settings tabs opened and captured; layer switches completed; "+Model.Displays.Count+" display(s); cell "+Sprites.Cell+".");
                Close();
            }
        }
        void Render()
        {
            var display=Model.Current;Size size=display.PetSize(Sprites.Cell);
            var frame=Sprites.Frame(Model.Walking,Model.Facing,(int)phase);
            float offset=Model.Shaking(Now)?(float)(Math.Sin(Now*65)*3*display.Scale):0;
            PointF anchor=Geometry.Clamp(new PointF(Model.Position.X+offset,Model.Position.Y),display.Allowed(Sprites.Cell,false));
            var location=new Point((int)Math.Round(anchor.X-size.Width/2f),(int)Math.Round(anchor.Y-size.Height));
            if(Model.Crossing!=null)
            {
                var crossing=Model.Crossing;
                var from=new Point((int)Math.Round(crossing.SourceAnchor.X-crossing.SourceSize.Width/2f),(int)Math.Round(crossing.SourceAnchor.Y-crossing.SourceSize.Height));
                var to=new Point((int)Math.Round(crossing.DestinationAnchor.X-crossing.DestinationSize.Width/2f),(int)Math.Round(crossing.DestinationAnchor.Y-crossing.DestinationSize.Height));
                var first=Artwork.DisplayFragment(frame,crossing.SourceSize,from,crossing.From.Work);
                var second=Artwork.DisplayFragment(frame,crossing.DestinationSize,to,crossing.To.Work);
                if(rendered!=null)rendered.Dispose();rendered=first;if(crossingRendered!=null)crossingRendered.Dispose();crossingRendered=second;
                Present(rendered,from);if(!crossingWindow.Visible)crossingWindow.Show();crossingWindow.Present(crossingRendered,to);
                bool arriving=crossing.Progress>=.5f;anchor=arriving?crossing.DestinationAnchor:crossing.SourceAnchor;location=arriving?to:from;
            }
            else
            {
                var next=Artwork.Scale(frame,size);if(rendered!=null)rendered.Dispose();rendered=next;Present(rendered,location);
                if(crossingWindow.Visible)crossingWindow.Hide();
            }
            bool showName=Model.Settings.ShowName(Model.Hovered||buttonDown);
            bool showReaction=reaction>=0&&Now<bubbleUntil;
            if(showName)
            {
                bool below=location.Y-(int)Math.Ceiling(62*display.Scale)<display.Work.Top;
                using(var reactionImage=showReaction?ReactionImage(display.Scale,below):null)
                using(var caption=PetCaption.Draw(Model.Settings.PetName,display.Scale,reactionImage,display.Work.Width,size.Height,below))
                {
                    int x=Math.Max(display.Work.Left,Math.Min(display.Work.Right-caption.Width,(int)anchor.X-caption.Width/2));
                    int y=location.Y-(reactionImage!=null&&!below?reactionImage.Height:0);
                    // Transparent space over the sprite keeps its input and rendering independent.
                    if(!bubble.Visible)bubble.Show();bubble.Present(caption,new Point(x,y));
                }
            }
            else if(showReaction)
            {
                int bh=(int)(62*display.Scale);bool below=location.Y-bh<display.Work.Top;
                using(var image=ReactionImage(display.Scale,below))
                {
                    int bx=Math.Max(display.Work.Left,Math.Min(display.Work.Right-image.Width,(int)anchor.X-image.Width/2));
                    int by=below?location.Y+size.Height:location.Y-image.Height;
                    by=Math.Max(display.Work.Top,Math.Min(display.Work.Bottom-image.Height,by));
                    if(!bubble.Visible)bubble.Show();bubble.Present(image,new Point(bx,by));
                }
            }
            else if(bubble.Visible)bubble.Hide();
            // Keep the lock even when other applications open, restore, or reorder themselves.
            EnforceUnderAll();if(bubble.Visible)bubble.EnforceUnderAll();if(crossingWindow.Visible)crossingWindow.EnforceUnderAll();
        }
        void RefreshDisplays()
        {
            var displays=Native.Displays();string signature=string.Join("|",displays.Select(d=>d.Id+d.Work.ToString()+d.Scale));
            if(signature!=displaySignature){displaySignature=signature;Model.SetDisplays(displays);}
        }
        void RefreshEmotes()
        {
            try
            {
                var files=Directory.GetFiles(EmoteDirectory).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray();
                string signature=string.Join("|",files.Select(p=>p+File.GetLastWriteTimeUtc(p).Ticks+new FileInfo(p).Length));
                if(signature==assetSignature)return;
                foreach(var item in CustomEmotes)item.Dispose();CustomEmotes.Clear();var errors=new List<string>();
                foreach(var file in files)
                {
                    try{CustomEmotes.Add(new CustomEmote{Name=Path.GetFileNameWithoutExtension(file),Image=SpriteSet.ReadPng(file,Artwork.MaximumEmoteSize,Artwork.MaximumEmoteSize)});}
                    catch(Exception ex){errors.Add(Path.GetFileName(file)+": "+ex.Message);}
                }
                assetSignature=signature;EmoteStatus=CustomEmotes.Count+" custom emote(s) loaded.";
                if(errors.Count>0){EmoteStatus+="\n"+string.Join("\n",errors);Notify("Some custom emotes could not load",errors[0]);}
                if(AssetsChanged!=null)AssetsChanged();
            }
            catch(IOException){assetSignature="";} // Retry files being copied or edited on the next refresh.
            catch(UnauthorizedAccessException){EmoteStatus="The custom emote folder is not accessible.";}
        }
        public void UseCustom(SpriteSet candidate)
        {
            string pending=Path.Combine(DataDirectory,"pet.pending.png"),destination=Path.Combine(DataDirectory,"pet.png");
            candidate.Sheet.Save(pending,System.Drawing.Imaging.ImageFormat.Png);
            if(File.Exists(destination))File.Replace(pending,destination,null);else File.Move(pending,destination);
            var old=Sprites;Sprites=candidate;Model.FrameSize=Sprites.Cell;Model.Place(Model.Position);Model.CancelRoute();Model.EnsureInsideRestrictedArea();phase=0;
            Model.Settings.CustomPet=true;old.Dispose();Save();if(AssetsChanged!=null)AssetsChanged();
        }
        public void RestoreDefault()
        {
            var replacement=SpriteSet.FromReference(ReferencePath);var old=Sprites;Sprites=replacement;old.Dispose();
            Model.FrameSize=Sprites.Cell;Model.Place(Model.Position);Model.CancelRoute();Model.EnsureInsideRestrictedArea();phase=0;
            Model.Settings.CustomPet=false;Save();if(AssetsChanged!=null)AssetsChanged();
        }
        void Notify(string title,string message){if(!smoke){tray.BalloonTipTitle=title;tray.BalloonTipText=message;tray.ShowBalloonTip(5000);}}
        public void Save()
        {
            try{Model.Store();Model.Settings.Save(Path.Combine(DataDirectory,"settings.json"));}
            catch(Exception ex){Notify("Settings could not be saved",ex.Message);}
        }
        void OnClosing(object sender,FormClosingEventArgs e)
        {
            if(closing)return;closing=true;timer.Stop();menuDismissal.Dispose();Save();
            if(SettingsOpen)settingsWindow.Close();restrictedOverlay.Dispose();crossingWindow.Close();bubble.Close();tray.Visible=false;tray.Dispose();
            if(rendered!=null)rendered.Dispose();if(crossingRendered!=null)crossingRendered.Dispose();if(customReaction!=null)customReaction.Dispose();Replacements.Dispose();
            if(reactionPreview!=null)reactionPreview.Dispose();
            foreach(var item in CustomEmotes)item.Dispose();Sprites.Dispose();timer.Dispose();
        }
    }

    internal static class Names
    {
        public static string Movement(MovementMode mode){return mode==MovementMode.FreeRoam?"Free Roam":mode.ToString();}
        public static string MovementDescription(MovementMode mode)
        {
            if(mode==MovementMode.Restricted)return "Your pet wanders inside a fixed circular fence. Set its position and radius using the controls below.";
            if(mode==MovementMode.Static)return "Your pet stays where you place it and plays its idle animation. You can still drag it, interact with it, and show reactions.";
            return "Your pet wanders freely across connected displays, choosing random destinations and resting between walks. It stays clear of taskbars.";
        }
        public static string Layer(LayerMode mode){return mode==LayerMode.OverEverything?"Over Everything":mode==LayerMode.UnderAll?"Under All": "Dynamic";}
        public static string LayerDescription(LayerMode mode)
        {
            if(mode==LayerMode.OverEverything)return "Over Everything keeps your pet and its reactions above other application windows.";
            if(mode==LayerMode.UnderAll)return "Under All locks your pet and its reactions beneath all application windows, above the desktop background.";
            return "Dynamic uses normal window stacking. Other application windows can cover your pet; wandering and reactions never take keyboard focus.";
        }
    }
}
