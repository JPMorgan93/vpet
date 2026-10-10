using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void RefreshItems(PetWindow pet)
        {pet.Toys.Update();pet.Plate.UpdatePlate();pet.Joystick.UpdateJoystick();Application.DoEvents();}
        static void ItemLayerWindows()
        {
            Point cursor=Cursor.Position;IntPtr foreground=Native.GetForegroundWindow();
            string directory=Path.Combine(artifacts,"item-layer-"+Guid.NewGuid().ToString("N"));
            string reference=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Blue Dragon.png");
            try
            {
                using(var pet=new PetWindow(directory,reference,true,directory))
                {
                    typeof(PetWindow).GetField("smokeStep",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,99);
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();Cursor.Position=new Point(30,30);
                    pet.Model.Settings.Movement=MovementMode.Static;pet.Model.Settings.SyncPlayZone=false;
                    Check(!pet.Model.Settings.CustomPet&&pet.Sprites.Speeds.All(speed=>speed==2),"New profiles use twice-speed default animations");
                    var speeds=Enumerable.Repeat(1f,10).ToArray();speeds[5]=.5f;
                    pet.UseCustom(new SpriteSet((Bitmap)pet.Sprites.Sheet.Clone(),true,pet.Sprites.Counts,speeds,10));
                    Check(pet.Sprites.Speed(false,2)==1&&pet.Sprites.Speed(true,6)==.5f,"Choosing a custom sprite retains its own independent animation speeds");
                    pet.Close();
                }
                using(var pet=new PetWindow(directory,reference,true,directory))
                using(var app=new Form{Text="Item stacking test",Size=new Size(100,80),Location=new Point(20,20),StartPosition=FormStartPosition.Manual})
                using(var topmost=new Form{Text="Topmost stacking test",TopMost=true,Size=new Size(100,80),Location=new Point(130,20),StartPosition=FormStartPosition.Manual})
                using(var image=new Bitmap(32,36))
                {
                    typeof(PetWindow).GetField("smokeStep",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,99);
                    pet.Show();Application.DoEvents();MakerField<Timer>(pet,"timer").Stop();app.Show();topmost.Show();Application.DoEvents();Cursor.Position=new Point(30,30);
                    Check(pet.Model.Settings.CustomPet&&pet.Sprites.Speed(false,2)==1&&pet.Sprites.Speed(true,6)==.5f,"Restarting retains custom speeds rather than applying the default multiplier");
                    pet.RestoreDefault();Check(!pet.Model.Settings.CustomPet&&pet.Sprites.Speeds.All(speed=>speed==2),"Restore Default reapplies twice-speed Blue Dragon animations");
                    pet.Model.Settings.Movement=MovementMode.Static;pet.Model.Settings.SyncPlayZone=false;
                    var toys=pet.Toys.Model;pet.Toys.SetVisible(true);toys.SpawnBall(pet.Now);toys.SpawnTriangle();toys.SpawnCoin();toys.SpawnCard();toys.SpawnDie();pet.Plate.SetVisible(true);pet.Joystick.SetVisible(true);
                    var crossing=MakerField<LayeredWindow>(pet,"crossingWindow");
                    using(var g=Graphics.FromImage(image))g.Clear(Color.MediumPurple);
                    var items=new LayeredWindow[]{pet.Toys.Ball,pet.Toys.Triangle,pet.Toys.Coin,pet.Toys.Card,pet.Toys.Die,pet.Plate,pet.Joystick};
                    foreach(LayerMode mode in new[]{LayerMode.OverEverything,LayerMode.Dynamic,LayerMode.UnderAll,LayerMode.OverEverything})
                    {
                        pet.Model.Settings.Layer=mode;pet.ApplyLayer();
                        typeof(PetWindow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);
                        RefreshItems(pet);
                        foreach(var item in items)
                        {
                            Check(item.Visible&&IsAbove(item.Handle,pet.Handle),item.Text+" renders above pet in "+mode);
                            Check(((Native.GetWindowLongPtr(item.Handle,-20).ToInt64()&8)!=0)==(mode==LayerMode.OverEverything),item.Text+" retains the selected topmost band in "+mode);
                            if(mode==LayerMode.UnderAll)Check(IsAbove(app.Handle,item.Handle),item.Text+" stays under ordinary applications");
                            if(mode==LayerMode.Dynamic)Check(IsAbove(topmost.Handle,item.Handle),item.Text+" stays under a topmost application");
                            Native.SetWindowPos(item.Handle,new IntPtr(1),0,0,0,0,0x213);
                            Check(IsAbove(item.Handle,pet.Handle),item.Text+" cannot be reordered beneath the sprite");
                        }
                        Check(IsAbove(pet.Handle,pet.Toys.Chest.Handle)&&IsAbove(pet.Handle,pet.Toys.Fence.Handle),"Chest and play fence remain below the sprite in "+mode);
                        // Simulate both visible native sprite portions and alternate which portion is higher.
                        crossing.Present(image,new Point(pet.PresentedBounds.Right+10,pet.PresentedBounds.Top));crossing.Show();crossing.SetLayer(mode);RefreshItems(pet);
                        Check(crossing.Visible&&Native.IsWindowVisible(crossing.Handle),"Both sprite portions remain visible during the isolated crossing check");
                        foreach(var item in items)Check(IsAbove(item.Handle,pet.Handle)&&IsAbove(item.Handle,crossing.Handle),item.Text+" remains above both crossing portions in "+mode+"; pet="+IsAbove(item.Handle,pet.Handle)+", crossing="+IsAbove(item.Handle,crossing.Handle)+", target="+Native.AboveCompanionTarget(item.Handle,pet.Handle,crossing.Handle,mode,true)+", order="+ItemOrder(items)+", handles="+string.Join(",",items.Select(w=>w.Text+":"+w.Handle.ToInt64())));
                        Check(IsAbove(crossing.Handle,pet.Toys.Chest.Handle),"Chest stays below the second sprite portion");
                        Native.SetWindowPos(pet.Handle,IntPtr.Zero,0,0,0,0,0x213);RefreshItems(pet);
                        foreach(var item in items)Check(IsAbove(item.Handle,pet.Handle)&&IsAbove(item.Handle,crossing.Handle),item.Text+" follows a change of the higher crossing portion");
                        Check(IsAbove(pet.Handle,pet.Toys.Chest.Handle)&&IsAbove(crossing.Handle,pet.Toys.Chest.Handle),"Chest stays behind both sprite portions after reordering");
                        crossing.Hide();RefreshItems(pet);
                        if(mode==LayerMode.Dynamic)
                        {
                            app.Activate();Native.SetWindowPos(app.Handle,IntPtr.Zero,0,0,0,0,0x213);Application.DoEvents();IntPtr active=Native.GetForegroundWindow();RefreshItems(pet);
                            Check(items.All(item=>IsAbove(app.Handle,item.Handle)),"Dynamic lets another ordinary application cover all items");
                            Check(Native.GetForegroundWindow()==active,"Item updates do not steal keyboard focus");
                        }
                    }
                    OverlappingItems(pet,items);
                    pet.Model.Place(new PointF(pet.Model.Current.Work.Left+70,pet.Model.Current.Work.Top+140));
                    typeof(PetWindow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);RefreshItems(pet);
                    var rendered=MakerField<Bitmap>(pet,"rendered");Point ink=Point.Empty;int nearest=int.MaxValue;
                    for(int y=0;y<rendered.Height;y++)for(int x=0;x<rendered.Width;x++)if(rendered.GetPixel(x,y).A==255)
                    {int distance=(x-rendered.Width/2)*(x-rendered.Width/2)+(y-rendered.Height/2)*(y-rendered.Height/2);if(distance<nearest){nearest=distance;ink=new Point(x,y);}}
                    Cursor.Position=new Point(pet.PresentedBounds.Left+ink.X,pet.PresentedBounds.Top+ink.Y);Application.DoEvents();PetFrame(pet);
                    Check(pet.Model.Hovered,"An unobstructed visible pet still detects hover");
                    var petSize=pet.Model.Current.PetSize(pet.Sprites.Cell);pet.Model.Place(new PointF(toys.Ball.X,toys.Ball.Y+petSize.Height/2));
                    typeof(PetWindow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);RefreshItems(pet);
                    var ballCenter=Point.Round(toys.Ball);Cursor.Position=ballCenter;
                    Check(Native.WindowFromPoint(new Native.POINT(ballCenter.X,ballCenter.Y))==pet.Toys.Ball.Handle,"An overlapping ball receives pointer input in front of the pet");PetFrame(pet);
                    Check(!pet.Model.Hovered&&!pet.Model.Paused,"Hovering a foreground toy does not pause the pet underneath");
                    ToyMouse(pet.Toys.Ball,0x201,ballCenter);ToyMouse(pet.Toys.Ball,0x202,ballCenter);toys.AdvanceBall(.1f);
                    Check(toys.BounceHeight>0&&!pet.Model.Shaking(pet.Now),"Clicking an overlapping ball bounces it without triggering the pet's click shake");
                    Cursor.Position=new Point(30,30);
                    pet.Model.Settings.Movement=MovementMode.Restricted;pet.Model.Settings.DisplayRestrictedArea=true;PetFrame(pet);
                    var restricted=MakerField<RestrictedAreaOverlay>(pet,"restrictedOverlay");
                    Check(IsAbove(pet.Handle,restricted.HandleWindow)&&restricted.RingWindows.All(handle=>IsAbove(pet.Handle,handle)),"Separate restricted fence remains below the sprite");
                    pet.Toys.Menu.Show(pet.Toys.Chest,new Point(10,10));RefreshItems(pet);Check(items.All(item=>IsAbove(item.Handle,pet.Handle)),"Opening the toy menu preserves foreground item ordering");pet.Toys.Menu.Close();
                    pet.Toys.SetVisible(false);RefreshItems(pet);Check(!pet.Toys.Chest.Visible&&pet.Plate.Visible&&pet.Joystick.Visible&&IsAbove(pet.Plate.Handle,pet.Handle)&&IsAbove(pet.Joystick.Handle,pet.Handle),"Plate and joystick remain above pet when chest is hidden");
                    pet.Close();
                }
            }
            finally{Cursor.Position=cursor;Native.SetForegroundWindow(foreground);}
        }
    }
}
