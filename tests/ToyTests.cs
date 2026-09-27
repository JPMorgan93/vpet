using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static ToyModel Toys(PetModel pet)
        {var toys=new ToyModel(pet,new Random(19));toys.SetVisible(true,0);toys.SpawnBall(0);return toys;}
        static void ToyStep(ToyModel toys,PetModel pet,double now,float dt)
        {toys.BeforePetTick(now,dt);pet.Tick(now,dt);toys.AfterPetTick(now,dt);}
        static void ToyBehavior()
        {
            ToyRefinements();
            string file=Path.Combine(artifacts,"toy-settings.json");File.WriteAllText(file,"{}");
            var prefs=Preferences.Load(file);Check(!prefs.Toys.DisplayChest&&prefs.Toys.DisplayZone,"Existing users start with chest off and fence on");
            var pet=Pet(MovementMode.FreeRoam);var toys=Toys(pet);
            var zone=toys.Zone;var center=toys.Center;
            toys.DragChest(new PointF(-10000,10000));
            Check(toys.Chest.X-toys.ChestSize.Width/2>=zone.Left&&toys.Chest.Y+toys.ChestSize.Height/2<=zone.Bottom,"Whole chest stays inside fence when dragged past edges");
            toys.DragChest(center);toys.MoveZone(new PointF(center.X+10,center.Y));Check(toys.Chest==center,"Moving fence around contained chest leaves chest in place");
            toys.MoveZone(new PointF(950,150));Check(toys.Chest==toys.Center,"Moving fence away relocates chest to center");
            Check(pet.Current.Work.Contains(Rectangle.Ceiling(toys.Zone)),"Zone cannot enter taskbar or leave connected work area");
            toys.ResizeZone(toys.Zone,ZoneEdge.Left|ZoneEdge.Top,new PointF(10000,10000));
            Near(toys.Zone.Width,160,.01f,"Zone minimum width");Near(toys.Zone.Height,140,.01f,"Zone minimum height");
            Check(ToyModel.ContainsInclusive(toys.BallBounds,toys.Ball),"Shrinking the zone recovers the entire ball");
            toys.ResizeZone(toys.Zone,ZoneEdge.Left|ZoneEdge.Bottom,new PointF(-10000,10000));
            Check(pet.Current.Work.Contains(Rectangle.Ceiling(toys.Zone)),"Corner resize stops at working-area edges");
            toys.Settings.DisplayZone=false;toys.Store();pet.Settings.Save(file);prefs=Preferences.Load(file);
            Check(prefs.Toys.DisplayChest&&!prefs.Toys.DisplayZone&&prefs.Toys.X==toys.Zone.X&&prefs.Toys.ChestX==toys.Chest.X,"Chest visibility, hidden fence, rectangle and chest position survive restart");
            File.WriteAllText(file,"{\"Toys\":null}");Check(Preferences.Load(file).Toys!=null,"Null toy preferences migrate safely");
            toys.BeginAim();var ball=toys.Ball;toys.AdvanceBall(.1f);Check(toys.Ball==ball,"Aiming holds ball in place");
            toys.Bounce();toys.AdvanceBall(.1f);Check(toys.BounceHeight>0&&toys.Launcher==BallLauncher.None,"Click bounces without starting a fetch");
            for(int i=0;i<9;i++)toys.AdvanceBall(.1f);Check(toys.BounceHeight==0,"Three bounces finish and stay landed");
            var velocity=toys.PullVelocity(new PointF(80,-40));Check(velocity.X<0&&velocity.Y>0,"Launch arrow points opposite mouse pull");
            Near(Geometry.Distance(toys.PullVelocity(new PointF(10000,0)),PointF.Empty),720,.001f,"Pull power is capped");

            // The prediction and actual simulation must agree after repeated wall/corner ricochets, at varied frame times.
            var rng=new Random(88);
            foreach(float scale in new[]{1f,1.25f,2f})
            for(int shot=0;shot<24;shot++)
            {
                pet=Pet(MovementMode.Static);pet.SetDisplays(new List<DisplayArea>{new DisplayArea("primary",new Rectangle(-1600,-200,1600,1100),scale)});toys=Toys(pet);
                toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(-1000,-1000));toys.Settings.DisplayZone=shot%2==0;
                double angle=shot*Math.PI/12;float power=(100+(float)rng.NextDouble()*1500)*scale;
                toys.Launch(new PointF((float)Math.Cos(angle)*power,(float)Math.Sin(angle)*power),BallLauncher.Pet,0);
                var predicted=toys.RestingPoint;bool inside=true;int frames=0;
                while(toys.Rolling&&frames++<3000)
                {toys.AdvanceBall(.005f+(float)rng.NextDouble()*.095f);inside&=ToyModel.ContainsInclusive(toys.BallBounds,toys.Ball);}
                Check(inside&&!toys.Rolling,"Ball stays fully contained and stops after ricochets, scale "+scale+", shot "+shot);
                Near(Geometry.Distance(predicted,toys.Ball),0,.04f,"Predicted resting point matches actual bounce trajectory");
            }
            pet=Pet(MovementMode.FreeRoam);toys=Toys(pet);toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(-1000,-1000));
            toys.Launch(new PointF(500,500),BallLauncher.Pet,0);bool xHit=false,yHit=false;float oldX=toys.Velocity.X,oldY=toys.Velocity.Y;
            while(toys.Rolling)
            {toys.AdvanceBall(.03f);xHit|=oldX*toys.Velocity.X<0;yHit|=oldY*toys.Velocity.Y<0;oldX=toys.Velocity.X;oldY=toys.Velocity.Y;}
            Check(xHit&&yHit,"Both axes reflect at the fence rather than clamp or wrap");

            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                pet=Pet(mode);pet.Settings.Speed=0;if(mode==MovementMode.Restricted)pet.SetRadius(30);
                var anchor=pet.Anchor;toys=Toys(pet);toys.MoveZone(new PointF(750,400));toys.SpawnBall(0);
                pet.IdleUntil=500;toys.LaunchPull(new PointF(-50,-20),0);
                Check(pet.Playing&&pet.Destination.HasValue&&pet.IdleUntil==0,"User launch immediately overrides wandering/rest/static/zero speed for "+mode);
                bool paused=false,shook=false;double pauseStart=-1,shakeStart=-1,kick=-1;
                for(int i=0;i<18000;i++)
                {
                    double now=i*.01;ToyStep(toys,pet,now,.01f);
                    if(toys.Fetch==FetchPhase.Pausing&&!paused){paused=true;pauseStart=now;}
                    if(toys.Fetch==FetchPhase.Shaking&&!shook){shook=true;shakeStart=now;}
                    if(toys.Launcher==BallLauncher.Pet){kick=now;break;}
                }
                Check(paused&&shook&&kick>0,"Pet reaches settled ball, pauses, shakes and returns it for "+mode);
                Near((float)(shakeStart-pauseStart),.25f,.011f,"Pause lasts a quarter second");
                Near((float)(kick-shakeStart),.5f,.011f,"Return shake lasts half a second");
                Check(pet.Settings.Movement==mode&&pet.Settings.Speed==0&&pet.Anchor==anchor,"Fetching preserves saved mode, speed and circle");
                for(int i=0;i<18000&&pet.Playing;i++)ToyStep(toys,pet,kick+i*.01,.01f);
                Check(!pet.Playing&&toys.Fetch==FetchPhase.None,"Pet returns to ordinary movement after kicking "+mode);
                if(mode==MovementMode.Restricted)Check(Geometry.Distance(pet.Position,anchor)<=30,"Restricted pet walks back inside its unchanged circle");
                for(int i=0;i<300;i++)ToyStep(toys,pet,kick+200+i*.01,.01f);
                Check(!pet.Playing&&toys.Launcher==BallLauncher.Pet,"Pet launch never starts another chase");
            }

            pet=Pet(MovementMode.Static);toys=Toys(pet);toys.LaunchPull(new PointF(30,0),0);pet.Hovered=true;var stopped=pet.Position;
            for(int i=0;i<400;i++)ToyStep(toys,pet,i*.01,.01f);
            Check(pet.Position==stopped&&toys.Launcher==BallLauncher.User,"Hover pauses fetch while ball physics settles");pet.Hovered=false;
            toys.LaunchPull(new PointF(-50,-30),4);Check(Geometry.Distance(pet.Destination.Value,toys.RestingPoint)<.1f,"New user launch replaces previous fetch destination");
            toys.BeginAim();var held=toys.Ball;ToyStep(toys,pet,4.1,.1f);Check(held==toys.Ball,"Aim freezes rolling ball without changing launch ownership");toys.CancelAim();
            toys.CancelFetchForPetDrag(5);Check(!pet.Playing&&toys.Fetch==FetchPhase.None,"Dragging the pet cancels fetch");
            toys.LaunchPull(new PointF(30,0),6);toys.SetVisible(false,6);Check(!toys.HasBall&&!pet.Playing&&!toys.Aiming,"Hiding chest removes ball and cancels fetch");

            pet=Pet(MovementMode.Restricted);pet.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1),new DisplayArea("right",new Rectangle(200,0,1000,760),1.5f)});
            pet.MoveRestrictedArea(new PointF(-500,400));
            toys=Toys(pet);toys.MoveZone(new PointF(800,400));Check(toys.DisplayId=="right","Zone center can move to a separated display");
            toys.SpawnBall(0);toys.LaunchPull(new PointF(20,30),0);
            bool crossed=false;for(int i=0;i<16000&&toys.Launcher!=BallLauncher.Pet;i++){ToyStep(toys,pet,i*.01,.01f);crossed|=pet.Crossing!=null;}
            Check(crossed&&toys.Launcher==BallLauncher.Pet,"Fetching follows seamless routes across separated and scaled displays");
            pet.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1)});toys.RecoverDisplays();
            Check(toys.DisplayId=="left"&&pet.Current.Work.Contains(Rectangle.Ceiling(toys.Zone)),"Disconnected toy display recovers to a connected working area");
            Check(ToyModel.ContainsInclusive(toys.BallBounds,toys.Ball),"Disconnected display recovery keeps ball in recovered zone");
            ToyArt();
        }
        static void ToyArt()
        {
            using(var preview=new Bitmap(560,380))using(var g=Graphics.FromImage(preview))
            using(var fence=ToyArtwork.Fence(new Size(480,320),1))using(var chest=ToyArtwork.Chest(1))using(var ball=ToyArtwork.Ball(1))
            {
                g.Clear(Color.FromArgb(239,234,247));g.DrawImageUnscaled(fence,40,30);g.DrawImageUnscaled(chest,150,175);g.DrawImageUnscaled(ball,350,165);
                g.DrawString("Toy chest & play zone",SystemFonts.MessageBoxFont,Brushes.Black,42,355);
                preview.Save(Path.Combine(artifacts,"toy-chest-preview.png"));
                Check(fence.GetPixel(80,80).A==0,"Empty fence interior is completely click-through");
                Check(fence.GetPixel(240,160).A>0,"Center movement control is clickable");
                Check(chest.GetPixel(0,0).A==0&&ball.GetPixel(0,0).A==0,"Toy window corners are transparent");
            }
        }
        static void ToyWindowsTest()
        {
            Point original=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();
            string root=AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using(var pet=new PetWindow(Path.Combine(artifacts,"toys-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"toy-smoke")))
                using(var application=new LayeredWindow(false))using(var image=new Bitmap(30,30))
                {
                    pet.Show();MakerField<Timer>(pet,"timer").Stop();pet.Model.Settings.Movement=MovementMode.Static;
                    using(var g=Graphics.FromImage(image))g.Clear(Color.White);application.Present(image,new Point(50,50));application.Show();
                    var windows=pet.Toys;var toys=windows.Model;
                    var toggle=pet.ContextMenuStrip.Items.Cast<ToolStripItem>().First(i=>i.Text=="Display Toy Chest") as ToolStripMenuItem;
                    Check(toggle!=null&&!toys.Settings.DisplayChest,"Pet menu provides Display Toy Chest, default off");toggle.PerformClick();Application.DoEvents();
                    Check(windows.Chest.Visible&&windows.Fence.Visible,"Toggling chest shows chest and fence");
                    Check(windows.Menu.Items[0].Text=="Display Play Zone"&&windows.Menu.Items[1].Text=="Ball","Chest menu begins with fence toggle followed by Ball");
                    Check(windows.Menu.Items.Count==3&&windows.Menu.Items[2].Text=="Help Messages","Chest menu contains toggles only, without old help messages");
                    windows.Menu.Items[1].PerformClick();Check(windows.Ball.Visible,"Ball menu creates the red ball");
                    foreach(LayerMode mode in Enum.GetValues(typeof(LayerMode)))
                    {
                        pet.Model.Settings.Layer=mode;pet.ApplyLayer();windows.Update();Application.DoEvents();
                        foreach(var window in windows.Windows.Where(w=>w.Visible))
                        {
                            Check(IsAbove(pet.Handle,window.Handle),"Toy stays below pet in "+mode);
                            Check(((Native.GetWindowLongPtr(window.Handle,-20).ToInt64()&8)!=0)==(mode==LayerMode.OverEverything),window.Text+" topmost flag follows "+mode+" ("+Native.GetWindowLongPtr(window.Handle,-20)+", pet "+Native.GetWindowLongPtr(pet.Handle,-20)+")");
                            if(mode==LayerMode.UnderAll)Check(IsAbove(application.Handle,window.Handle),"Under All locks toys below application");
                        }
                    }
                    pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();
                    var chest=toys.Chest;ToyMouse(windows.Chest,0x201,Point.Round(chest));ToyMouse(windows.Chest,0x200,new Point((int)chest.X+45,(int)chest.Y+25));ToyMouse(windows.Chest,0x202,Cursor.Position);
                    Check(toys.Chest!=chest&&toys.Zone.Contains(toys.Chest),"Native chest drag moves inside zone");
                    var zone=toys.Zone;var center=Point.Round(toys.Center);ToyMouse(windows.Fence,0x201,center);ToyMouse(windows.Fence,0x200,new Point(center.X-30,center.Y-30));ToyMouse(windows.Fence,0x202,Cursor.Position);
                    Check(toys.Zone!=zone,"Native middle handle moves play zone");
                    zone=toys.Zone;var corner=new Point((int)zone.Right-3,(int)zone.Bottom-3);ToyMouse(windows.Fence,0x201,corner);ToyMouse(windows.Fence,0x200,new Point(corner.X-35,corner.Y-25));ToyMouse(windows.Fence,0x202,Cursor.Position);
                    Near(toys.Zone.Width,zone.Width-35,.1f,"Native corner drag resizes width");Near(toys.Zone.Height,zone.Height-25,.1f,"Native corner drag resizes height");
                    windows.Menu.Show(windows.Chest,new Point(10,10));Application.DoEvents();windows.Menu.Items[0].PerformClick();windows.Menu.Close();
                    Check(!windows.Fence.Visible&&windows.Chest.Visible&&toys.HasBall,"Fence can be hidden independently of active toys");
                    var ball=Point.Round(toys.Ball);ToyMouse(windows.Ball,0x201,ball);ToyMouse(windows.Ball,0x202,ball);toys.AdvanceBall(.1f);
                    Check(toys.BounceHeight>0&&toys.Launcher==BallLauncher.None,"Native click triggers bounce only");
                    ToyMouse(windows.Ball,0x201,ball);ToyMouse(windows.Ball,0x200,new Point(ball.X+50,ball.Y-25));
                    Check(windows.Arrow.Visible&&toys.Aiming,"Pull gesture shows launch arrow");ToyMouse(windows.Ball,0x202,Cursor.Position);
                    Check(!windows.Arrow.Visible&&toys.Velocity.X<0&&toys.Velocity.Y>0&&toys.Launcher==BallLauncher.User&&pet.Model.Playing,"Release launches opposite pull and triggers fetch");
                    ToyMouse(windows.Ball,0x201,ball);ToyMouse(windows.Ball,0x200,new Point(ball.X+70,ball.Y));windows.Ball.Capture=false;Application.DoEvents();
                    Check(!toys.Aiming&&!windows.Arrow.Visible,"Lost capture cancels aim and removes arrow");
                    windows.Menu.Show(windows.Chest,new Point(10,10));Application.DoEvents();
                    MakerField<MenuDismissal>(windows,"dismissal").MouseDownAt(new Point(windows.Menu.Right+50,windows.Menu.Bottom+50));Application.DoEvents();Check(!windows.Menu.Visible,"Clicking away dismisses toy chest menu");
                    windows.Menu.Items[1].PerformClick();Check(!toys.HasBall&&!windows.Ball.Visible&&!pet.Model.Playing,"Unchecked Ball removes it and cancels the pending fetch");
                    windows.Menu.Items[1].PerformClick();Check(toys.HasBall&&windows.Ball.Visible&&toys.Launcher==BallLauncher.None,"Checking Ball again adds a fresh unlaunched ball");
                    toys.Settings.DisplayZone=true;windows.Update();var hover=Point.Round(toys.Ball);
                    Check(windows.HelpAt(hover,windows.Ball.Handle).Contains("three bounces"),"Ball hover supplies ball instructions");
                    Check(windows.HelpAt(Point.Round(toys.Center),windows.Fence.Handle).Contains("resize"),"Center hover supplies fence instructions");
                    Check(windows.HelpAt(new Point((int)toys.Zone.Left+3,(int)toys.Zone.Top+40),windows.Fence.Handle)!=null,"Border hover supplies fence instructions");
                    Check(windows.HelpAt(new Point((int)toys.Zone.Left+45,(int)toys.Zone.Top+45),windows.Fence.Handle)==null,"Empty play-space interior never shows fence help");
                    Check(windows.HelpAt(hover,application.Handle)==null,"A covering application prevents toy hover help");
                    pet.Present(image,new Point(pet.Model.Current.Work.Left+10,pet.Model.Current.Work.Top+10));
                    Cursor.Position=hover;windows.Update();Application.DoEvents();
                    IntPtr hoverWindow=Native.WindowFromPoint(new Native.POINT(hover.X,hover.Y));
                    Check(windows.Help.Visible,"Hovering the actual ball displays help (hit "+hoverWindow+", ball "+windows.Ball.Handle+", pet "+pet.Handle+", fence "+windows.Fence.Handle+", help "+windows.Help.Handle+")");
                    Native.RECT helpBounds,chestBounds;Native.GetWindowRect(windows.Help.Handle,out helpBounds);Native.GetWindowRect(windows.Chest.Handle,out chestBounds);
                    Check(helpBounds.Bottom<=chestBounds.Top,"Help message is positioned above the chest (help bottom "+helpBounds.Bottom+", chest top "+chestBounds.Top+")");
                    Cursor.Position=new Point((int)toys.Zone.Left+50,(int)toys.Zone.Top+50);windows.Update();Check(!windows.Help.Visible,"Moving away hides the message");
                    windows.Menu.Items[2].PerformClick();Check(!toys.Settings.HelpMessages&&windows.HelpAt(hover,windows.Ball.Handle)==null,"Help Messages toggle disables hover messages");
                    toggle.PerformClick();Check(!windows.Chest.Visible&&!windows.Ball.Visible&&!windows.Fence.Visible&&!windows.Arrow.Visible,"Turning chest off hides every toy window");
                    Check(!pet.Model.Playing,"Turning chest off cancels pending fetch");
                    pet.Close();application.Close();
                }
            }
            finally{Cursor.Position=original;if(foreground!=IntPtr.Zero)Native.SetForegroundWindow(foreground);}
        }
        static void ToyMouse(LayeredWindow window,int message,Point screen)
        {Cursor.Position=screen;var local=window.PointToClient(screen);Native.SendMessage(window.Handle,(uint)message,new IntPtr(message==0x202?0:1),new IntPtr((local.Y<<16)|(local.X&0xffff)));}
    }
}
