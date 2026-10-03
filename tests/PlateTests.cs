using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void PlateFeatures()
        {
            HungerProjects();CoinDuringDrag();
            string file=Path.Combine(artifacts,"plate-settings.json");File.WriteAllText(file,"{}");
            var prefs=Preferences.Load(file);Check(!prefs.Plate.Visible&&prefs.Plate.DefaultFood==FoodKind.Pudding,"Older preferences gain a hidden plate with default pudding");
            prefs.Plate.Visible=true;prefs.Plate.X=333;prefs.Plate.Y=444;prefs.Save(file);var restored=Preferences.Load(file);
            Check(restored.Plate.Visible&&restored.Plate.X==333&&restored.Plate.Y==444&&restored.Plate.DefaultFood==FoodKind.Pudding,"Plate position, visibility and default food survive restart");
            prefs.Plate.DefaultFood=(FoodKind)100;prefs.Validate();Check(prefs.Plate.DefaultFood==FoodKind.Pudding,"Unknown food falls back to pudding");
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))foreach(bool sync in new[]{false,true})
            {
                var pet=Pet(mode);pet.Settings.SyncPlayZone=sync;pet.Settings.Speed=0;
                var toys=Toys(pet);toys.ResizeZone(toys.Zone,ZoneEdge.Right|ZoneEdge.Bottom,new PointF(-200,-120));
                toys.SetPlateVisible(true,0);toys.DragPlate(new PointF(900,600));
                var zone=toys.Zone;var restriction=pet.OwnRestrictedArea;
                Check(!zone.Contains(toys.PlatePosition)&&toys.FoodRemaining==0,"Empty plate can sit outside play zone: "+mode+" sync "+sync);
                toys.SpawnCard();toys.PressCard(0);int emote=-1;toys.ReactionPlayed+=i=>emote=i;
                toys.PressPlate(0);
                Check(toys.Target==PlayTarget.Plate&&toys.FoodRemaining==3&&pet.Playing&&emote==Reactions.Hunger,"Serving food interrupts card and shows Hunger with restriction override");
                toys.CleanUp(0);Check(toys.HasPlate&&toys.FoodRemaining==3&&toys.Target==PlayTarget.Plate,"Cleaning up toys leaves plate and active meal intact");
                toys.SetVisible(false,0);Check(pet.Playing&&toys.HasPlate,"Closing toy chest leaves feeding active");
                double time=0;int previous=3,bites=0,shakes=0;FetchPhase before=toys.Fetch;double shakeStarted=0;
                for(int i=0;i<24000&&pet.Playing;i++)
                {
                    time+=.005;ToyStep(toys,pet,time,.005f);
                    if(toys.Fetch==FetchPhase.Shaking&&before!=FetchPhase.Shaking){shakes++;shakeStarted=time;}
                    if(toys.FoodRemaining!=previous)
                    {
                        Check(toys.FoodRemaining==previous-1,"Every bite removes exactly a third");
                        Near((float)(time-shakeStarted),.25f,.011f,"Food disappears after the eighth-second shake and eighth-second pause");
                        Near(pet.Position.X,toys.PlatePosition.X,1,"Eating pet centers behind the food");
                        Near(pet.Position.Y,toys.EatingPosition.Y,1,"Eating pet stands above the plate");
                        previous=toys.FoodRemaining;bites++;
                    }
                    before=toys.Fetch;
                }
                Check(bites==3&&shakes==3&&toys.FoodRemaining==0&&!pet.Playing,"Three shakes finish the pudding and resume "+mode);
                Check(zone==toys.Zone&&restriction==pet.OwnRestrictedArea,"Feeding never relocates either fence");
                if(mode==MovementMode.Restricted)Check(pet.InsideRestriction(pet.Position),"Pet returns inside its active restriction after eating");
                toys.PressPlate(time);Check(toys.FoodRemaining==3,"Clicking an emptied plate serves the default food again");
                toys.SetPlateVisible(false,time);Check(toys.FoodRemaining==0&&!toys.HasPlate,"Hiding plate removes food and cancels meal");
            }
            var p=Pet(MovementMode.Static);var t=Toys(p);t.SetPlateVisible(true,0);t.DragPlate(new PointF(600,500));t.ServeFood(FoodKind.Pudding,0);p.Place(t.EatingPosition);
            double now=0;while(t.FoodRemaining==3&&now<2){now+=.005;ToyStep(t,p,now,.005f);}
            Check(t.FoodRemaining==2,"One bite leaves the two-thirds render");
            t.PressPlate(now);Check(t.FoodRemaining==2,"Clicking nonempty plate does not refill or restart the meal");
            t.PlateDragging=true;t.DragPlate(new PointF(800,500));
            for(int i=0;i<800;i++){now+=.01;ToyStep(t,p,now,.01f);}
            Check(t.FoodRemaining==2&&t.Fetch==FetchPhase.Approaching,"Dragging plate redirects pet and defers bites until release");
            t.PlateDragging=false;t.Editing=true;
            while(t.FoodRemaining>0&&now<30){now+=.01;ToyStep(t,p,now,.01f);}
            Check(t.FoodRemaining==0,"Remaining bites complete even while a different item is being moved");
            t.Editing=false;t.ServeFood(FoodKind.Pudding,now);t.CancelFetchForPetDrag(now);Check(!p.Playing&&t.FoodRemaining==3,"Picking up pet cancels its route without deleting food");
            t.ServeFood(FoodKind.Pudding,now);t.Launch(new PointF(100,20),BallLauncher.User,now);Check(t.Target==PlayTarget.Ball&&t.FoodRemaining==3,"New toy action can interrupt meal without competing controllers");
            p.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1),new DisplayArea("right",new Rectangle(200,0,1000,760),1.5f)});
            t.RecoverDisplays();p.Place(new PointF(-500,400));t.DragPlate(new PointF(800,500));t.ServeFood(FoodKind.Pudding,now);bool crossing=false;
            for(int i=0;i<15000&&p.Playing;i++){now+=.01;ToyStep(t,p,now,.01f);crossing|=p.Crossing!=null;}
            Check(crossing&&p.CurrentDisplay=="right"&&t.FoodRemaining==0,"Pet crosses separated displays to eat from an independent plate");
            p.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1)});t.RecoverDisplays();
            Check(t.PlateDisplayId=="left"&&p.Current.Work.Contains(Rectangle.Ceiling(t.PlateBounds)),"Disconnected plate monitor recovers to a connected work area");
            t.DragPlate(new PointF(-100000,100000));Check(p.Current.Work.Contains(Rectangle.Ceiling(t.PlateBounds)),"Whole plate and toppings stay within desktop work area");
            PlateArt();
        }
        static void CoinDuringDrag()
        {
            foreach(PlayTarget other in new[]{PlayTarget.Ball,PlayTarget.Triangle,PlayTarget.Card,PlayTarget.D20})
            {
                var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.SpawnCoin();toys.SpawnTriangle();toys.SpawnCard();toys.SpawnDie();toys.PressCoin(0);
                double now=0;while(toys.Fetch!=FetchPhase.Flipping&&now<20){now+=.01;ToyStep(toys,pet,now,.01f);}
                Check(toys.Fetch==FetchPhase.Flipping,"Coin reaches airborne flip before moving "+other);toys.Editing=true;pet.Hovered=true;
                float last=toys.CoinFlip;
                for(int i=0;i<130;i++)
                {
                    if(other==PlayTarget.Ball)toys.DragBall(new PointF(toys.Ball.X+1,toys.Ball.Y),now);
                    else if(other==PlayTarget.Triangle)toys.DragTriangle(new PointF(toys.Triangle.X+1,toys.Triangle.Y));
                    else if(other==PlayTarget.Card)toys.DragGame(other,new PointF(toys.Card.X+1,toys.Card.Y));
                    else toys.DragDie(new PointF(toys.Die.X+1,toys.Die.Y),now);
                    now+=.01;ToyStep(toys,pet,now,.01f);
                    if(i==10)Check(toys.CoinFlip>last,"Coin animation advances during another toy's drag");
                }
                Check(toys.Fetch==FetchPhase.Result&&toys.Announcement!=null,"Coin lands and announces while "+other+" is still held");
            }
        }
        static void HungerProjects()
        {
            Check(Reactions.Names[Reactions.Hunger]=="Hunger"&&Reactions.CodePoints[Reactions.Hunger]==0x1F37D,"Hunger is the ninth default reaction with U+1F37D");
            Check(SystemEmoji.Image(Reactions.Hunger)!=null,"Windows renders the Hunger emoji without exhausting the defaults cache");
            foreach(Personality personality in Enum.GetValues(typeof(Personality)))Check(Reactions.Weight(personality,Reactions.Hunger)==3,"Hunger has ordinary random reaction weight for "+personality);
            string replacement=Path.Combine(artifacts,"hunger-emotes");
            using(var art=new Bitmap(50,50)){using(var g=Graphics.FromImage(art))g.Clear(Color.Magenta);art.Save(Path.Combine(artifacts,"hunger.png"));}
            using(var emotes=new EmoteReplacements(replacement))emotes.Replace(Reactions.Hunger,Path.Combine(artifacts,"hunger.png"));
            using(var emotes=new EmoteReplacements(replacement)){Check(emotes.Get(Reactions.Hunger).GetPixel(10,10).ToArgb()==Color.Magenta.ToArgb(),"Hunger custom replacement survives reopening");emotes.Restore(Reactions.Hunger);Check(emotes.Get(Reactions.Hunger)==null,"Hunger restores to system emoji");}
            using(var project=MakerFixture())
            {
                project.Data.EmoteAnimations=true;project.SetSize(18,20,24);project.Data.Frames[18][0]=project.Data.Frames[0][0].Copy();project.SetSpeed(18,1.5f);
                string path=Path.Combine(artifacts,"hunger.vpetproject");project.Save(path);
                using(var reopened=SpriteProject.Load(path))using(var sprite=reopened.Build())
                {
                    Check(reopened.Slots(18).Length==1&&reopened.Speed(18)==1.5f,"Hunger mapping and speed survive project roundtrip");
                    string output=Path.Combine(artifacts,"hunger.vpetsprite");sprite.SavePackage(output);
                    using(var imported=SpriteSet.Import(output))Check(imported.EmoteFrame(Reactions.Hunger,0)!=null&&imported.Speeds[18]==1.5f&&imported.EmoteFrame(9,0)==null,"Hunger exports a usable optional animation without treating custom images as default poses");
                }
                for(int row=10;row<18;row++){project.SetSize(row,20+row%3,24);project.Data.Frames[row][2]=new SpriteFrame{X=0,Y=0,OffsetX=row,OffsetY=-row};project.SetSpeed(row,1+row/20f);}
                foreach(int version in new[]{3,4})
                {
                    var legacy=new SpriteManifest{Version=version,Kind="project",Width=20,Height=24,EmoteAnimations=true,Frames=project.Data.Frames.Take(18).ToArray(),CycleWidths=project.Data.CycleWidths.Take(18).ToArray(),CycleHeights=project.Data.CycleHeights.Take(18).ToArray(),CycleSpeeds=version==4?project.Data.CycleSpeeds.Take(18).ToArray():null};
                    string old=Path.Combine(artifacts,"old-emotes-v"+version+".vpetproject");SpritePackage.Write(old,legacy,project.Source);
                    using(var loaded=SpriteProject.Load(old))
                    {
                        for(int row=10;row<18;row++)Check(loaded.Data.Frames[row][2].OffsetX==row&&loaded.Data.Frames[row][2].OffsetY==-row&&loaded.Width(row)==20+row%3&&loaded.Speed(row)==(version==4?1+row/20f:1),"V"+version+" migration preserves existing optional mapping "+row);
                        Check(loaded.Slots(18).Length==0&&loaded.Speed(18)==1,"Legacy migration adds empty Hunger with normal speed");loaded.Save(old);
                        using(var again=SpriteProject.Load(old))Check(again.Data.Frames[17][2].OffsetX==17&&again.Slots(18).Length==0,"Migrated optional mappings remain intact after saving");
                    }
                }
            }
        }
        static void PlateArt()
        {
            using(var preview=new Bitmap(440,135))using(var g=Graphics.FromImage(preview))
            {
                g.Clear(Color.FromArgb(236,230,246));
                foreach(int remaining in new[]{3,2,1,0})using(var image=PlateArtwork.Draw(remaining,1))
                {
                    Check(image.Size==new Size(72,63)&&image.GetPixel(0,0).A==0&&image.GetPixel(36,55).A==255,"Plate state "+remaining+" is 75% size with transparent surroundings and an opaque white rim");
                    Check(image.GetPixel(45,30).A==(remaining==3?255:0),"First bite visibly removes the right food section");
                    int x=(3-remaining)*110+7;g.DrawImageUnscaled(image,x,10);g.DrawString(remaining==0?"Empty":remaining+"/3 remaining",SystemFonts.DefaultFont,Brushes.Black,x,106);
                }
                preview.Save(Path.Combine(artifacts,"pudding-states.png"));
            }
        }
        static void PlateWindows()
        {
            Point original=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();string root=AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                using(var pet=new PetWindow(Path.Combine(artifacts,"plate-ui-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"plate-smoke")))
                using(var app=new LayeredWindow(false))using(var solid=new Bitmap(30,30))
                {
                    pet.Show();MakerField<Timer>(pet,"timer").Stop();pet.Model.Settings.Movement=MovementMode.Static;
                    using(var g=Graphics.FromImage(solid))g.Clear(Color.White);app.Present(solid,new Point(40,40));app.Show();
                    var menu=pet.ContextMenuStrip;var display=(ToolStripMenuItem)menu.Items[0];
                    Check(display.Text=="Display Items"&&display.DropDownItems.Cast<ToolStripItem>().Select(i=>i.Text).SequenceEqual(new[]{"Toy Chest","Plate"}),"Display Items is first and contains Toy Chest and Plate");
                    var toggle=(ToolStripMenuItem)display.DropDownItems[1];toggle.PerformClick();Application.DoEvents();
                    var plate=pet.Plate;var model=pet.Toys.Model;
                    Check(plate.Visible&&!model.Settings.DisplayChest&&model.HasPlate&&model.FoodRemaining==0,"Plate toggle works with chest hidden and starts empty");
                    model.DragPlate(new PointF(pet.Model.Current.Work.Left+250,pet.Model.Current.Work.Top+350));plate.UpdatePlate();
                    var center=Point.Round(model.PlatePosition);ToyMouse(plate,0x201,center);ToyMouse(plate,0x202,center);
                    Check(model.FoodRemaining==3&&pet.ActiveReaction==Reactions.Hunger,"Native plate click serves pudding and previews Hunger");
                    model.CancelFetchForPetDrag(pet.Now);var before=model.PlatePosition;
                    ToyMouse(plate,0x201,center);ToyMouse(plate,0x200,new Point(center.X+50,center.Y+30));ToyMouse(plate,0x202,Cursor.Position);
                    Check(model.PlatePosition.X==before.X+50&&model.PlatePosition.Y==before.Y+30&&model.Fetch==FetchPhase.None,"Native left drag moves plate without serving or restarting meal");
                    Check(plate.PresentedBounds.Location==Point.Round(model.PlateBounds.Location),"Presented plate follows native drag coordinates");
                    plate.Menu.Show(plate,new Point(20,20));Application.DoEvents();Check(((ToolStripMenuItem)plate.Menu.Items[0]).Checked&&!pet.ShowPause,"Pudding is selected and plate menu does not show settings pause");
                    plate.Menu.Items[0].PerformClick();plate.Menu.Close();Check(model.Fetch==FetchPhase.Approaching&&model.FoodRemaining==3,"Food menu selection serves and restarts eating");
                    model.CancelFetchForPetDrag(pet.Now);pet.Model.Place(model.EatingPosition);
                    foreach(LayerMode mode in Enum.GetValues(typeof(LayerMode)))
                    {
                        pet.Model.Settings.Layer=mode;pet.ApplyLayer();typeof(PetWindow).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pet,null);plate.UpdatePlate();Application.DoEvents();
                        Check(((Native.GetWindowLongPtr(plate.Handle,-20).ToInt64()&8)!=0)==(mode==LayerMode.OverEverything),"Plate topmost flag follows "+mode);
                        var order=new List<IntPtr>();Native.EnumWindows(delegate(IntPtr h,IntPtr unused){order.Add(h);return true;},IntPtr.Zero);
                        Check(order.IndexOf(plate.Handle)<order.IndexOf(pet.Handle),"Food renders above pet in "+mode);
                        if(mode==LayerMode.UnderAll)Check(order.IndexOf(plate.Handle)>order.IndexOf(app.Handle),"Under All places food below application windows");
                    }
                    pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();plate.UpdatePlate();
                    var rect=plate.PresentedBounds;Check(Native.WindowFromPoint(new Native.POINT(rect.Left,rect.Top))!=plate.Handle,"Transparent plate corners are click-through");
                    pet.OpenSettings(1);Application.DoEvents();var settings=MakerField<SettingsWindow>(pet,"settingsWindow");
                    Check(Descendants(settings).OfType<Button>().Any(b=>b.Text=="Hunger"),"Settings includes Hunger Try a reaction button");
                    var choice=MakerField<ComboBox>(settings,"emoteChoice");Check(choice.Items.Contains("Hunger"),"Hunger has replace and restore options");settings.Close();
                    Item(plate.Menu,"Remove Plate").PerformClick();Check(!plate.Visible&&!model.HasPlate&&model.FoodRemaining==0,"Remove Plate hides the window and clears food");pet.Close();
                }
            }
            finally{Cursor.Position=original;Native.SetForegroundWindow(foreground);}
        }
    }
}
