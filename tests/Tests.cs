using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vpet
{
    internal static class Tests
    {
        static int count;
        static string artifacts;
        static void Check(bool condition,string message){count++;if(!condition)throw new Exception("FAIL: "+message);}
        static void Near(float actual,float expected,float tolerance,string message){Check(Math.Abs(actual-expected)<=tolerance,message+" ("+actual+" vs "+expected+")");}
        static void Reject(Action action,string message)
        {bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,message);}
        static PetModel Pet(MovementMode mode)
        {
            var pet=new PetModel(new Preferences{Movement=mode,X=400,Y=400},new Random(7));
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("primary",new Rectangle(0,0,1000,760),1)});return pet;
        }
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                if(Array.IndexOf(args,"--window-tests")>=0)
                {
                    Native.EnableDpi();Application.EnableVisualStyles();WindowLayers();
                    Console.WriteLine("PASS: "+count+" native window-layer assertions.");return 0;
                }
                artifacts=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-artifacts");Directory.CreateDirectory(artifacts);
                DirectionAndMotion();Interaction();Displays();ContinuousCrossings();ReactionsAndSettings();SpritesAndImages();EmoteOverrides();BubbleBorders();PetNames();UpdateReleases();
                Console.WriteLine("PASS: "+count+" assertions across movement, interaction, displays, reactions, persistence, and artwork.");return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
        static void PetNames()
        {
            var prefs=new Preferences();
            Check(!prefs.ShowName(false)&&!prefs.ShowName(true),"Blank name has no overlay");
            prefs.PetName="Mochi";
            Check(prefs.ShowName(false),"Names default to always visible");
            prefs.NameDisplay=NameVisibility.OnHover;
            Check(!prefs.ShowName(false)&&prefs.ShowName(true),"Hover-only visibility");
            prefs.NameDisplay=NameVisibility.Hidden;Check(!prefs.ShowName(true),"Hidden name stays hidden on hover");
            prefs.PetName=" \r\n ";prefs.NameDisplay=NameVisibility.Always;prefs.Validate();Check(!prefs.HasName,"Whitespace-only name stays absent");
            prefs.PetName="  Mochi\n  ";prefs.Validate();Check(prefs.PetName=="Mochi","Name is sanitized");
            string file=Path.Combine(artifacts,"named-settings.json");prefs.NameDisplay=NameVisibility.OnHover;prefs.Save(file);
            var loaded=Preferences.Load(file);Check(loaded.PetName=="Mochi"&&loaded.NameDisplay==NameVisibility.OnHover,"Name preferences survive restart");
            File.WriteAllText(file,"{}");loaded=Preferences.Load(file);Check(loaded.PetName==""&&loaded.NameDisplay==NameVisibility.Always,"Old preferences migrate without a name");
            foreach(float scale in new[]{1f,1.25f,1.5f,2f})
            {
                var pet=Pet(MovementMode.Restricted);pet.Settings.PetName="Mochi";pet.Settings.NameDisplay=NameVisibility.Always;
                pet.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1500,-100,1500,1080),scale)});
                var anchor=pet.Anchor;pet.UpdateNameHeadroom();pet.Place(new PointF(-1500,-100));
                using(var reaction=Artwork.Bubble(1,null,scale,false))using(var caption=PetCaption.Draw("Mochi",scale,reaction,1500))
                {
                    float petTop=pet.Position.Y-pet.Current.PetSize(pet.FrameSize).Height;
                    Check(petTop-caption.Height>=pet.Current.Work.Top,"Caption fits above pet at top edge, scale "+scale);
                    Check(caption.Height>reaction.Height,"Name and bubble occupy separate vertical space");
                    caption.Save(Path.Combine(artifacts,"name-bubble-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
                }
                Check(pet.Anchor==anchor,"Name headroom does not move restricted fence");
                pet.Settings.NameDisplay=NameVisibility.Hidden;pet.UpdateNameHeadroom();Check(pet.Current.NameHeadroom==0,"Hidden names reserve no headroom");
            }
        }
        static string ReleaseJson(GitHubRelease release)
        {
            using(var stream=new MemoryStream())
            {new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(GitHubRelease)).WriteObject(stream,release);return System.Text.Encoding.UTF8.GetString(stream.ToArray());}
        }
        static void UpdateReleases()
        {
            const string repo="JPMorgan93/vpet";
            const string prefix="https://github.com/JPMorgan93/vpet/releases/download/v1.10.0/";
            var installer=new GitHubAsset{Name="Vpet-Setup-1.10.0-Windows-x64.exe",Url=prefix+"Vpet-Setup-1.10.0-Windows-x64.exe",Size=2000};
            var checksum=new GitHubAsset{Name="SHA256SUMS.txt",Url=prefix+"SHA256SUMS.txt",Size=100};
            var release=new GitHubRelease{Tag="v1.10.0",Assets=new[]{installer,checksum}};
            Check(Updates.Parse(ReleaseJson(release),"1.9.0",repo).Version=="1.10.0","Update comparison is numeric");
            Check(Updates.Parse(ReleaseJson(release),"1.10.0",repo)==null,"Same version is ignored");
            Check(Updates.Parse(ReleaseJson(release),"2.0.0",repo)==null,"Downgrades are ignored");
            release.Draft=true;Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo)==null,"Drafts never reach users");release.Draft=false;
            release.Prerelease=true;Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo)==null,"Test prereleases never reach users");release.Prerelease=false;
            release.Tag="v1.10.0-rc1";Check(Updates.Parse(ReleaseJson(release),"1.0.0",repo)==null,"Nonstable tags are ignored");release.Tag="v1.10.0";
            installer.Url="https://example.com/update.exe";Reject(delegate{Updates.Parse(ReleaseJson(release),"1.0.0",repo);},"Foreign installer URLs rejected");installer.Url=prefix+installer.Name;
            installer.Size=Updates.MaximumInstallerSize+1L;Reject(delegate{Updates.Parse(ReleaseJson(release),"1.0.0",repo);},"Oversized installers rejected");installer.Size=2000;
            release.Assets=new[]{installer};Reject(delegate{Updates.Parse(ReleaseJson(release),"1.0.0",repo);},"Missing checksum blocks update");
            release.Assets=new[]{installer,checksum,installer};Reject(delegate{Updates.Parse(ReleaseJson(release),"1.0.0",repo);},"Ambiguous assets rejected");
            string hash=new string('a',64),line=hash+"  "+installer.Name;
            Check(Updates.ExpectedHash(line+"\r\n",installer.Name)==hash,"Release checksum accepted");
            Reject(delegate{Updates.ExpectedHash(line,"different.exe");},"Checksum must match exact asset name");
            Reject(delegate{Updates.ExpectedHash(line+"\n"+line,installer.Name);},"Duplicate checksum rejected");
            string path=Path.Combine(artifacts,"checksum-fixture.txt");File.WriteAllText(path,"before");string first=Updates.Hash(path);File.WriteAllText(path,"after");Check(first!=Updates.Hash(path),"Changed installer content changes checksum");
        }
        static bool IsAbove(IntPtr above,IntPtr below)
        {
            IntPtr cursor=below;
            for(int i=0;i<10000;i++){cursor=Native.GetWindow(cursor,3);if(cursor==above)return true;if(cursor==IntPtr.Zero)return false;}
            return false;
        }
        static void WindowLayers()
        {
            using(var pet=new LayeredWindow(false))using(var bubble=new LayeredWindow(true))using(var application=new LayeredWindow(false))
            using(var image=new Bitmap(40,40))
            {
                using(var graphics=Graphics.FromImage(image))graphics.Clear(Color.MediumPurple);
                Point origin=Screen.PrimaryScreen.WorkingArea.Location;origin.Offset(80,80);
                application.Text="Vpet stacking test";pet.CompanionHandle=bubble.Handle;bubble.CompanionHandle=pet.Handle;
                application.Show();application.Present(image,origin);application.SetLayer(LayerMode.Dynamic);
                pet.Show();pet.Present(image,origin);bubble.Show();bubble.Present(image,new Point(origin.X,origin.Y-40));
                pet.SetLayer(LayerMode.OverEverything);bubble.SetLayer(LayerMode.OverEverything);
                Check((Native.GetWindowLongPtr(pet.Handle,-20).ToInt64()&8)!=0,"Over Everything is topmost");
                pet.SetLayer(LayerMode.UnderAll);bubble.SetLayer(LayerMode.UnderAll);Application.DoEvents();
                Check(IsAbove(application.Handle,pet.Handle)&&IsAbove(application.Handle,bubble.Handle),"Under All places pet and bubble below an application");
                Check((Native.GetWindowLongPtr(pet.Handle,-20).ToInt64()&8)==0,"Under All clears topmost state");
                Check(Native.GetParent(pet.Handle)==IntPtr.Zero,"Under All does not require a desktop parent");
                Native.SetWindowPos(pet.Handle,new IntPtr(-1),0,0,0,0,0x13);
                Native.SetWindowPos(bubble.Handle,IntPtr.Zero,0,0,0,0,0x13);
                Check(IsAbove(application.Handle,pet.Handle)&&IsAbove(application.Handle,bubble.Handle),"Lock blocks attempts to raise either pet window");
                Check((Native.GetWindowLongPtr(pet.Handle,-20).ToInt64()&8)==0,"Lock blocks topmost promotion");
                Native.SetWindowPos(pet.Handle,IntPtr.Zero,origin.X+30,origin.Y+20,0,0,0x15);
                Check(IsAbove(application.Handle,pet.Handle),"Movement does not raise Under All pet");
                bubble.Hide();bubble.Show();bubble.EnforceUnderAll();Check(IsAbove(application.Handle,bubble.Handle),"Newly shown reaction remains beneath application");
                Native.SetWindowPos(application.Handle,new IntPtr(1),0,0,0,0,0x13);
                pet.EnforceUnderAll();bubble.EnforceUnderAll();
                Check(IsAbove(application.Handle,pet.Handle)&&IsAbove(application.Handle,bubble.Handle),"Lock recovers after another application lowers itself");
                pet.SetLayer(LayerMode.Dynamic);Native.SetWindowPos(pet.Handle,IntPtr.Zero,0,0,0,0,0x13);
                Check(IsAbove(pet.Handle,application.Handle),"Dynamic releases the Under All lock");
                pet.SetLayer(LayerMode.OverEverything);Check((Native.GetWindowLongPtr(pet.Handle,-20).ToInt64()&8)!=0,"Over Everything still works after Under All");
                var fenceModel=Pet(MovementMode.Restricted);
                using(var fence=new RestrictedAreaOverlay(fenceModel,delegate{},pet))
                {
                    foreach(LayerMode mode in Enum.GetValues(typeof(LayerMode)))
                    {
                        pet.SetLayer(mode);fence.Update();pet.EnforceUnderAll();fence.Update();
                        Check(IsAbove(pet.Handle,fence.HandleWindow),"Center handle stays below sprite in "+mode);
                        foreach(var ring in fence.RingWindows)Check(IsAbove(pet.Handle,ring),"Fence stays below sprite in "+mode);
                        Native.SetWindowPos(fence.HandleWindow,new IntPtr(-1),0,0,0,0,0x13);
                        Check(IsAbove(pet.Handle,fence.HandleWindow),"Center handle cannot promote itself above sprite in "+mode);
                        if(mode==LayerMode.UnderAll)Check(IsAbove(application.Handle,pet.Handle),"Fence does not interfere with Under All");
                    }
                }
                application.Close();bubble.Close();pet.Close();
            }
            var model=Pet(MovementMode.FreeRoam);model.ChangeMode(MovementMode.Restricted,0);
            using(var fence=new RestrictedAreaOverlay(model,delegate{}))
            {
                fence.Update();Check(fence.IsDisplayed,"Restricted mode displays an on-screen fence handle");
                var bounds=fence.HandleBounds;model.SetDestination(new PointF(550,500),"primary");model.Tick(2,.1f);fence.Update();
                Check(fence.HandleBounds==bounds,"On-screen fence remains stationary while pet moves");
                model.MoveRestrictedArea(new PointF(800,600));fence.Update();
                Check(fence.HandleBounds.X==785&&fence.HandleBounds.Y==585,"Fence handle follows an explicitly moved center: "+fence.HandleBounds);
                Check(model.Position==model.Anchor,"Moving fence past pet relocates pet to center");
                model.Settings.DisplayRestrictedArea=false;fence.Update();Check(!fence.IsDisplayed,"Checkbox hides the on-screen fence");
                model.Settings.DisplayRestrictedArea=true;fence.Update();Check(fence.IsDisplayed,"Checkbox shows the fence again");
                model.ChangeMode(MovementMode.Static,3);fence.Update();Check(!fence.IsDisplayed,"Leaving Restricted removes the fence UI");
            }
            string root=AppDomain.CurrentDomain.BaseDirectory;
            using(var pet=new PetWindow(Path.Combine(root,"test-artifacts","click-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(root,"test-artifacts","click-output")))
            {
                pet.Model.Settings.Movement=MovementMode.Static;pet.Show();Application.DoEvents();
                foreach(Personality personality in Enum.GetValues(typeof(Personality)))
                {
                    pet.Model.Settings.Personality=personality;pet.Model.Facing=6;
                    var point=new IntPtr((36<<16)|32);
                    Native.SendMessage(pet.Handle,0x0201,new IntPtr(1),point);
                    Check(pet.Model.Facing==2&&!pet.Model.Walking,"Mouse press selects down idle");
                    Native.SendMessage(pet.Handle,0x0202,IntPtr.Zero,point);
                    Check(pet.ActiveReaction==Reactions.Love,"Actual left click shows Love for "+personality);
                    Check(pet.Model.Shaking(pet.Now)&&pet.Model.ShakeUntil-pet.Now<=.5,"Actual left click starts half-second shake");
                }
                pet.Close();
            }
        }
        static void DirectionAndMotion()
        {
            PointF[] directions={new PointF(1,0),new PointF(1,1),new PointF(0,1),new PointF(-1,1),new PointF(-1,0),new PointF(-1,-1),new PointF(0,-1),new PointF(1,-1)};
            for(int i=0;i<8;i++)Check(Geometry.Direction(directions[i],2)==i,"8-way facing "+i);
            double angle=24*Math.PI/180;Check(Geometry.Direction(new PointF((float)Math.Cos(angle),(float)Math.Sin(angle)),0)==0,"Direction hysteresis retains facing at 24 degrees");
            angle=29*Math.PI/180;Check(Geometry.Direction(new PointF((float)Math.Cos(angle),(float)Math.Sin(angle)),0)==1,"Direction changes past 27.5 degrees");
            var straight=Pet(MovementMode.FreeRoam);straight.SetDestination(new PointF(800,400),"primary");straight.Tick(1,.1f);
            var diagonal=Pet(MovementMode.FreeRoam);diagonal.SetDestination(new PointF(800,700),"primary");diagonal.Tick(1,.1f);
            Near(Geometry.Distance(straight.Position,new PointF(400,400)),10,.001f,"Default speed is 100 px/sec");
            Near(Geometry.Distance(diagonal.Position,new PointF(400,400)),10,.001f,"Diagonal travel uses the same total speed");
            var near=Pet(MovementMode.FreeRoam);near.SetDestination(new PointF(430,400),"primary");near.Tick(1,.1f);Near(near.Position.X,405,.001f,"Half speed near destination");
            near.Place(new PointF(420,400));near.Tick(2,.1f);Near(near.Position.X,422.5f,.001f,"Quarter speed closer to destination");
            for(int i=0;i<50&&near.Destination.HasValue;i++)near.Tick(3+i*.1,.1f);
            Near(near.Position.X,430,.001f,"Arrival lands exactly at destination");Check(!near.Destination.HasValue,"Arrival clears destination");
            Check(near.IdleUntil>=13&&near.IdleUntil<=38,"Arrival pause is 10–30 seconds");
        }
        static void Interaction()
        {
            var pet=Pet(MovementMode.FreeRoam);pet.SetDestination(new PointF(700,500),"primary");var destination=pet.Destination;
            pet.Hovered=true;pet.Tick(1,.1f);Check(pet.Position==new PointF(400,400)&&!pet.Walking,"Hover pauses walking");
            Check(pet.Destination==destination,"Hover preserves destination");pet.Hovered=false;pet.Tick(2,.1f);Check(pet.Walking,"Pointer exit resumes walking");
            pet.Release(10);Check(!pet.Shaking(10)&&!pet.Shaking(11),"Drag release does not shake");
            var released=pet.Position;pet.Tick(14.999,.1f);Check(pet.Position==released,"Drag release rests for five seconds");
            pet.Hovered=true;pet.Tick(16,.1f);Check(pet.Position==released&&pet.Facing==2,"Hover extends pause and selects down idle");
            pet.Hovered=false;pet.Tick(17,.1f);Check(pet.Destination.HasValue,"Post-drag movement resumes after pointer leaves");
            pet.Dragging=true;pet.Facing=6;pet.Tick(18,.1f);Check(pet.Facing==2&&!pet.Walking,"Dragging selects down idle");pet.Dragging=false;
            foreach(Personality personality in Enum.GetValues(typeof(Personality)))
            {
                pet.Settings.Personality=personality;Check(pet.Click(20)==Reactions.Love,"Click always requests Love");
                Check(pet.Shaking(20.499)&&!pet.Shaking(20.5),"Click shake lasts exactly half a second");
                var still=pet.Position;pet.Tick(20.2,.1f);Check(pet.Position==still&&pet.Facing==2&&!pet.Walking,"Click shake uses down idle without travel");
            }
            pet.Release(20.3);Check(!pet.Shaking(20.3),"Dragging cancels any preceding click shake");
            var stationary=Pet(MovementMode.Static);stationary.Release(0);stationary.Tick(40,.1f);Check(!stationary.Walking&&!stationary.Destination.HasValue,"Static stays still after release");
            var restricted=Pet(MovementMode.Restricted);var fixedAnchor=restricted.Anchor;restricted.Place(new PointF(700,500));restricted.Release(1);Check(restricted.Anchor==fixedAnchor&&restricted.Position==fixedAnchor,"Pet drag never moves the fence; outside release returns to center");
            Check(!restricted.SetDestination(new PointF(100,100),"primary"),"Restricted destination excludes outside radius");
            restricted.Tick(33,.1f);Check(restricted.Destination.HasValue&&Geometry.Distance(restricted.Destination.Value,restricted.Anchor)<=restricted.Settings.Radius,"Restricted random selection stays in circle");
            restricted.MoveRestrictedArea(new PointF(800,600));Check(restricted.Position==restricted.Anchor,"Moving fence beyond pet relocates pet to center");
            var standing=restricted.Position;restricted.MoveRestrictedArea(new PointF(750,600));Check(restricted.Position==standing,"Moving fence while pet remains inside does not move pet");
            restricted.SetRadius(30);Check(restricted.Position==restricted.Anchor,"Shrinking fence beyond pet relocates pet to center");
            var anchor=restricted.Anchor;restricted.ChangeMode(MovementMode.FreeRoam,34);restricted.Settings.DisplayRestrictedArea=false;restricted.ChangeMode(MovementMode.Restricted,35);
            Check(restricted.Anchor==anchor&&restricted.Settings.DisplayRestrictedArea,"Re-enabling Restricted preserves the fence and checks Display restricted area");
            var zero=Pet(MovementMode.FreeRoam);zero.Settings.Speed=0;zero.Tick(1,.1f);Check(!zero.Walking&&zero.Position==new PointF(400,400),"Zero speed stops movement");
            var first=Pet(MovementMode.FreeRoam);first.Place(new PointF(700,500));first.ChangeMode(MovementMode.Restricted,1);Check(first.Anchor==first.Position,"First restricted area is created around current pet position");
            first.SetRadius(5000);Check(first.Settings.Radius==1000,"Radius cannot exceed 1000 pixels");
        }
        static void Displays()
        {
            var pet=Pet(MovementMode.FreeRoam);
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("primary",new Rectangle(0,0,1000,760),1),new DisplayArea("offset",new Rectangle(1400,-400,1000,700),1.5f)});
            pet.Place(new PointF(999,799));Near(pet.Position.X,968,.01f,"Full sprite stays within right edge");Near(pet.Position.Y,760,.01f,"Pet excludes taskbar area");
            pet.Place(new PointF(500,400));Check(pet.SetDestination(new PointF(1900,0),"offset"),"Can route to a non-touching monitor");
            bool crossed=false;for(int i=0;i<2500&&pet.Destination.HasValue;i++)
            {
                pet.Tick(1+i*.1,.1f);var allowed=pet.Current.Allowed(pet.FrameSize);
                if(pet.Crossing==null)Check(pet.Position.X>=allowed.Left-.01&&pet.Position.X<=allowed.Right+.01&&pet.Position.Y>=allowed.Top-.01&&pet.Position.Y<=allowed.Bottom+.01,"Outside a crossing, the full pet fits in the work area");
                else Check(pet.Crossing.Progress>=0&&pet.Crossing.Progress<1,"Crossing advances through a visible split animation");
                if(pet.Current.Id=="offset")crossed=true;
            }
            Check(crossed&&pet.Current.Id=="offset","Pet reaches offset monitor");Near(pet.Position.X,1900,.01f,"Cross-display final destination");
            Check(pet.Current.PetSize(pet.FrameSize)==new Size(96,108),"Sprite uses destination display DPI");
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("primary",new Rectangle(0,0,1000,760),1)});
            Check(pet.Current.Id=="primary"&&!pet.Destination.HasValue,"Disconnected display relocates pet and invalidates route");
            Check(!pet.SetDestination(new PointF(1900,0),"offset"),"Disconnected display cannot become a destination");
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1200,-200,1200,900),1)});
            pet.Place(new PointF(-1300,-300));Near(pet.Position.X,-1168,.01f,"Negative desktop coordinates are supported");Near(pet.Position.Y,-128,.01f,"Full sprite fits at top edge");
        }
        static void ContinuousCrossings()
        {
            var a=new DisplayArea("a",new Rectangle(0,0,1000,760),1);
            var neighbors=new[]{new DisplayArea("right",new Rectangle(1000,0,1000,760),1),new DisplayArea("left",new Rectangle(-1000,0,1000,760),1),
                new DisplayArea("down",new Rectangle(0,760,1000,760),1),new DisplayArea("up",new Rectangle(0,-760,1000,760),1)};
            using(var frame=new Bitmap(32,36))
            {
                using(var graphics=Graphics.FromImage(frame))graphics.Clear(Color.MediumPurple);
                foreach(var b in neighbors)
                {
                    var crossing=DisplayCrossing.Plan(a,b,frame.Size,new PointF(b.Work.Left+400,b.Work.Top+400));
                    for(int i=0;i<=10;i++)
                    {
                        crossing.Progress=i/10f;
                        Near(Geometry.Distance(crossing.SourceAnchor,crossing.DestinationAnchor),0,.001f,"Adjoining display fragments share an exact screen position");
                        var p=new Point((int)Math.Round(crossing.SourceAnchor.X-32),(int)Math.Round(crossing.SourceAnchor.Y-72));
                        using(var source=Artwork.DisplayFragment(frame,new Size(64,72),p,a.Work))using(var target=Artwork.DisplayFragment(frame,new Size(64,72),p,b.Work))
                        {
                            int pixels=0;for(int y=0;y<72;y++)for(int x=0;x<64;x++)pixels+=(source.GetPixel(x,y).A>0?1:0)+(target.GetPixel(x,y).A>0?1:0);
                            Check(pixels==64*72,"Display crossing has no missing or duplicated pixels");
                        }
                    }
                }
            }
            var pet=Pet(MovementMode.FreeRoam);var right=neighbors[0];pet.SetDisplays(new List<DisplayArea>{a,right});pet.Place(new PointF(950,400));pet.SetDestination(new PointF(1400,400),right.Id);
            bool split=false;for(int i=0;i<1000&&pet.Destination.HasValue;i++)
            {
                var previous=pet.Position;pet.Tick(1+i*.05,.05f);
                Check(Geometry.Distance(previous,pet.Position)<=5.01f,"Adjoining displays have no position jumps");
                if(pet.Crossing!=null)split=true;
            }
            Check(split&&pet.Current.Id==right.Id&&!pet.Destination.HasValue,"Pet completes seamless display crossing");
            var far=new DisplayArea("far",new Rectangle(2000,0,1000,760),1);
            Check(DisplayCrossing.NextDisplay(a,far,new List<DisplayArea>{a,right,far},new Size(32,36)).Id==right.Id,"Travel to a far display routes through the middle display");
            var offset=new DisplayArea("gap",new Rectangle(1400,-200,1000,760),1);
            var bridge=DisplayCrossing.Plan(a,offset,new Size(32,36),new PointF(1800,300));bridge.Progress=.5f;
            Check(bridge.SourceAnchor.X>a.Work.Right-32&&bridge.DestinationAnchor.X<offset.Work.Left+32,"Nonaligned displays share progressive edge fragments instead of teleporting");
        }
        static void EmoteOverrides()
        {
            string directory=Path.Combine(artifacts,"replacement-"+Guid.NewGuid().ToString("N"));
            string imagePath=Path.Combine(artifacts,"replacement-image.png"),invalid=Path.Combine(artifacts,"replacement-invalid.png");
            using(var image=new Bitmap(24,24)){using(var g=Graphics.FromImage(image))g.Clear(Color.HotPink);image.Save(imagePath,ImageFormat.Png);}
            using(var image=new Bitmap(51,50))image.Save(invalid,ImageFormat.Png);
            using(var replacements=new EmoteReplacements(directory))
            {
                replacements.Replace(0,imagePath);Check(replacements.Get(0).GetPixel(0,0).ToArgb()==Color.HotPink.ToArgb(),"Built-in Music image can be replaced");
                Check(replacements.Get(1)==null,"Replacing one emote leaves other defaults alone");
                Reject(delegate{replacements.Replace(0,invalid);},"Oversized replacement is rejected");Check(replacements.Get(0)!=null,"Failed import preserves the previous replacement");
                using(var bubble=Artwork.Bubble(0,replacements.Get(0),1,false))Check(bubble.GetPixel(34,26).R>200,"Replacement is rendered in the normal reaction bubble");
            }
            using(var restored=new EmoteReplacements(directory))
            {Check(restored.Get(0)!=null,"Emote replacement survives restart");restored.Restore(0);Check(restored.Get(0)==null,"Restore original removes replacement");}
            using(var restored=new EmoteReplacements(directory))Check(restored.Get(0)==null,"Restored original persists across restart");
        }
        static void ReactionsAndSettings()
        {
            var random=new Random(3);
            for(int i=0;i<100;i++){double t=Reactions.Interval(Frequency.Sometimes,random);Check(t>=60&&t<=120,"Sometimes interval is 60–120 seconds");}
            Check(double.IsPositiveInfinity(Reactions.Interval(Frequency.Off,random)),"Off disables random scheduler");
            Check(Reactions.Weight(Personality.Sweet,0)==6&&Reactions.Weight(Personality.Sweet,4)==3&&Reactions.Weight(Personality.Sweet,3)==1,"Personality priorities have 6:3:1 weights");
            Check(Reactions.Weight(Personality.Sassy,8)==3,"Custom emotes have priority two");
            var prefs=new Preferences{Movement=MovementMode.Restricted,Speed=75,Radius=300,X=-100,Y=400,AnchorX=-100,AnchorY=400,Facing=7,CustomPet=true};
            string file=Path.Combine(artifacts,"settings.json");prefs.Save(file);prefs.Speed=76;prefs.Save(file);
            var restored=Preferences.Load(file);Check(restored.Speed==76&&restored.X==-100&&restored.CustomPet&&restored.Facing==7,"Settings round-trip and atomic overwrite");
            File.WriteAllText(file,"broken JSON");Check(Preferences.Load(file).Speed==50,"Corrupt preferences fall back to defaults");
            File.WriteAllText(file,"{\"Layer\":1,\"Speed\":50,\"Radius\":250}");
            Check(Preferences.Load(file).Layer==LayerMode.UnderAll,"Saved Desktop Only numeric preference migrates to Under All");
            Check(Preferences.Load(file).DisplayRestrictedArea,"Existing settings default restricted area display on");
            var underAll=new Preferences{Layer=LayerMode.UnderAll,X=400,Y=400,AnchorX=400,AnchorY=400};underAll.Save(file);
            Check(Preferences.Load(file).Layer==LayerMode.UnderAll,"Under All persists across restart");
            underAll.Speed=0;underAll.DisplayRestrictedArea=false;underAll.RestrictedAreaCreated=true;underAll.Save(file);
            var saved=Preferences.Load(file);Check(saved.Speed==0&&!saved.DisplayRestrictedArea&&saved.RestrictedAreaCreated,"Zero speed and fence preferences survive restart");
            File.WriteAllText(file,"{\"ShakeSeconds\":30,\"Radius\":5000}");var legacy=Preferences.Load(file);
            Check(legacy.Radius==1000,"Saved oversized radius migrates to 1000 pixels");legacy.Save(file);
            Check(!File.ReadAllText(file).Contains("ShakeSeconds"),"Obsolete shake setting is dropped from saved preferences");
        }
        static void BubbleBorders()
        {
            using(var white=new Bitmap(50,50))
            {
                using(var graphics=Graphics.FromImage(white))graphics.Clear(Color.White);
                foreach(float scale in new[]{1f,1.25f,1.5f,2f})foreach(bool below in new[]{false,true})
                using(var bubble=Artwork.Bubble(0,white,scale,below))
                {
                    float top=below?8:0;
                    var points=new[]{new PointF(1,top+26),new PointF(66,top+26),new PointF(15,top+1),new PointF(15,top+51),new PointF(4.5f,top+4.5f),new PointF(62.5f,top+47.5f),
                        below?new PointF(29.5f,5):new PointF(29.5f,55.5f),below?new PointF(36.5f,5):new PointF(36.5f,55.5f)};
                    foreach(var point in points)
                    {
                        bool ink=false;int cx=(int)(point.X*scale),cy=(int)(point.Y*scale),radius=(int)Math.Ceiling(scale);
                        for(int y=Math.Max(0,cy-radius);y<=Math.Min(bubble.Height-1,cy+radius);y++)for(int x=Math.Max(0,cx-radius);x<=Math.Min(bubble.Width-1,cx+radius);x++)
                        {var pixel=bubble.GetPixel(x,y);if(pixel.A>100&&pixel.R<230&&pixel.G<230&&pixel.B>pixel.G)ink=true;}
                        Check(ink,"Bubble body and tail retain outline at "+point+", scale "+scale+", below="+below);
                    }
                    bubble.Save(Path.Combine(artifacts,"bubble-"+scale+"-"+below+".png"),ImageFormat.Png);
                }
            }
        }
        static void SpritesAndImages()
        {
            for(int i=0;i<8;i++)
            {
                var emoji=SystemEmoji.Image(i);int coloredPixels=0;
                for(int y=0;y<emoji.Height;y++)for(int x=0;x<emoji.Width;x++)
                {var pixel=emoji.GetPixel(x,y);if(Math.Max(pixel.R,Math.Max(pixel.G,pixel.B))-Math.Min(pixel.R,Math.Min(pixel.G,pixel.B))>25)coloredPixels++;}
                Check(coloredPixels>50,"Windows renders "+Reactions.Names[i]+" as a visible color emoji");
            }
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Base Vpet Sprite Sheet.png");
            using(var sprites=SpriteSet.FromReference(reference))
            {
                Check(sprites.Cell==new Size(32,36),"Reference produces aligned 32 × 36 cells");
                Check(sprites.Sheet.Width==160&&sprites.Sheet.Height==360,"Runtime grid is 5 × 10");
                for(int row=0;row<5;row++)for(int y=row*36;y<(row+1)*36;y++)for(int x=128;x<160;x++)
                    if(sprites.Sheet.GetPixel(x,y).A!=0)throw new Exception("Fifth idle frame is not transparent");
                Check(true,"Unused idle cells stay fully transparent");
                string valid=Path.Combine(artifacts,"default-runtime.png");sprites.Sheet.Save(valid,ImageFormat.Png);
                using(var imported=SpriteSet.Import(valid))Check(imported.Cell==sprites.Cell,"Default runtime sheet passes custom import validation");
                for(int state=0;state<2;state++)for(int facing=0;facing<8;facing++)for(int frame=0;frame<(state==0?4:5);frame++)
                {
                    var image=sprites.Frame(state==1,facing,frame);bool visible=false,transparent=false;
                    for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){if(image.GetPixel(x,y).A>0)visible=true;else transparent=true;}
                    Check(visible&&transparent,"Every runtime animation frame has art and transparency");
                }
                Bitmap left=sprites.Frame(true,4,2),right=sprites.Frame(true,0,2);
                for(int y=0;y<left.Height;y++)for(int x=0;x<left.Width;x++)if(left.GetPixel(x,y)!=right.GetPixel(left.Width-1-x,y))throw new Exception("Mirrored direction differs");
                Check(true,"Opposite-facing sprite is an exact horizontal mirror");
                using(var broken=new Bitmap(sprites.Sheet)){broken.SetPixel(128,0,Color.Red);string p=Path.Combine(artifacts,"invalid-idle.png");broken.Save(p,ImageFormat.Png);Reject(delegate{using(var unused=SpriteSet.Import(p)){}},"Invalid idle column rejected");}
                using(var preview=new Bitmap(640,400))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(235,229,245));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    string[] names={"Right","Down-right","Down","Down-left","Left","Up-left","Up","Up-right"};
                    for(int i=0;i<8;i++)
                    {
                        int x=(i%4)*160,y=(i/4)*200;g.DrawString(names[i],SystemFonts.DefaultFont,Brushes.Black,x+15,y+12);
                        g.DrawImage(sprites.Frame(false,i,0),new Rectangle(x+15,y+45,64,72));g.DrawImage(sprites.Frame(true,i,2),new Rectangle(x+85,y+45,64,72));
                        using(var bubble=Artwork.Bubble(i,null,1,false))g.DrawImageUnscaled(bubble,x+45,y+127);
                    }
                    preview.Save(Path.Combine(artifacts,"animation-preview.png"),ImageFormat.Png);
                }
            }
            Reject(delegate{using(var unused=SpriteSet.Import(reference)){}},"Annotated reference cannot be imported as a runtime sheet");
            using(var image=new Bitmap(51,50)){string p=Path.Combine(artifacts,"large-emote.png");image.Save(p,ImageFormat.Png);Reject(delegate{using(var unused=SpriteSet.ReadPng(p,50,50)){}},"Oversized custom emote rejected");}
            string fake=Path.Combine(artifacts,"fake.png");File.WriteAllText(fake,"This is not a PNG image.");Reject(delegate{using(var unused=SpriteSet.ReadPng(fake,50,50)){}},"Non-PNG contents rejected");
            using(var image=new Bitmap(50,50)){string p=Path.Combine(artifacts,"emote.png");image.Save(p,ImageFormat.Png);using(var loaded=SpriteSet.ReadPng(p,50,50))Check(loaded.Width==50,"50 × 50 custom PNG accepted");}
        }
    }
}
