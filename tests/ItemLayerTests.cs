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
                using(var app=new Form{Text="Item stacking test",Size=new Size(100,80),Location=new Point(20,20),StartPosition=FormStartPosition.Manual})
                using(var topmost=new Form{Text="Topmost stacking test",TopMost=true,Size=new Size(100,80),Location=new Point(130,20),StartPosition=FormStartPosition.Manual})
                using(var image=new Bitmap(32,36))
                {
                    pet.Show();MakerField<Timer>(pet,"timer").Stop();app.Show();topmost.Show();Application.DoEvents();Cursor.Position=new Point(30,30);
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
                    pet.Show();MakerField<Timer>(pet,"timer").Stop();app.Show();topmost.Show();Application.DoEvents();Cursor.Position=new Point(30,30);
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
                        if(!IsAbove(items[0].Handle,crossing.Handle))
                        {
                            var order=new System.Collections.Generic.List<IntPtr>();Native.EnumWindows(delegate(IntPtr h,IntPtr unused){order.Add(h);return true;},IntPtr.Zero);
                            Console.WriteLine("Layer diagnostics: "+string.Join(", ",new LayeredWindow[]{pet,crossing}.Concat(items).Select(w=>w.Text+"="+order.IndexOf(w.Handle)+" flags="+Native.GetWindowLongPtr(w.Handle,-20)+" companions="+w.CompanionHandle+"/"+w.OtherCompanionHandle)));
                        }
                        foreach(var item in items)Check(IsAbove(item.Handle,pet.Handle)&&IsAbove(item.Handle,crossing.Handle),item.Text+" remains above both crossing portions in "+mode);
                        Check(IsAbove(crossing.Handle,pet.Toys.Chest.Handle),"Chest stays below the second sprite portion");
                        Native.SetWindowPos(pet.Handle,IntPtr.Zero,0,0,0,0,0x213);RefreshItems(pet);
                        foreach(var item in items)Check(IsAbove(item.Handle,pet.Handle)&&IsAbove(item.Handle,crossing.Handle),item.Text+" follows a change of the higher crossing portion");
                        Check(IsAbove(pet.Handle,pet.Toys.Chest.Handle)&&IsAbove(crossing.Handle,pet.Toys.Chest.Handle),"Chest stays behind both sprite portions after reordering");
                        crossing.Hide();RefreshItems(pet);
                        if(mode==LayerMode.Dynamic)
                        {
                            app.Activate();Native.SetWindowPos(app.Handle,IntPtr.Zero,0,0,0,0,0x213);RefreshItems(pet);
                            Check(items.All(item=>IsAbove(app.Handle,item.Handle)),"Dynamic lets another ordinary application cover all items");
                            Check(Native.GetForegroundWindow()==app.Handle,"Item updates do not steal keyboard focus");
                        }
                    }
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
