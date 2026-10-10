using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static string ItemOrder(LayeredWindow[] items)
        {
            var handles=new HashSet<IntPtr>(items.Select(item=>item.Handle));var ordered=new List<IntPtr>();
            Native.EnumWindows(delegate(IntPtr handle,IntPtr unused){if(handles.Contains(handle))ordered.Add(handle);return true;},IntPtr.Zero);
            return string.Join(",",ordered.Select(handle=>handle.ToInt64().ToString()));
        }
        sealed class ItemOrderWatch : NativeWindow,IDisposable
        {
            readonly Action changed;
            public ItemOrderWatch(IntPtr handle,Action changed){this.changed=changed;AssignHandle(handle);}
            protected override void WndProc(ref Message message)
            {base.WndProc(ref message);if(message.Msg==0x47)changed();}
            public void Dispose(){ReleaseHandle();}
        }
        static void OverlappingItems(PetWindow pet,LayeredWindow[] items)
        {
            var toys=pet.Toys.Model;var center=toys.Center;var modeBefore=pet.Model.Settings.Layer;var movementBefore=pet.Model.Settings.Movement;pet.Model.Settings.Movement=MovementMode.Static;
            PointF ball=toys.Ball,triangle=toys.Triangle,coin=toys.Coin,card=toys.Card,die=toys.Die,plate=toys.PlatePosition,joystick=toys.JoystickPosition;
            toys.DragBall(center,pet.Now);toys.DragTriangle(center);toys.DragGame(PlayTarget.Coin,center);toys.DragGame(PlayTarget.Card,center);toys.DragDie(center,pet.Now);
            toys.DragPlate(center);toys.DragJoystick(new PointF(center.X,center.Y+22*toys.Scale));
            foreach(LayerMode mode in new[]{LayerMode.Dynamic,LayerMode.OverEverything,LayerMode.UnderAll})
            {
                pet.Model.Settings.Layer=mode;pet.ApplyLayer();RefreshItems(pet);
                string order=ItemOrder(items);int changes=0;var observers=new List<ItemOrderWatch>();
                try
                {
                    foreach(var item in items)observers.Add(new ItemOrderWatch(item.Handle,delegate{if(ItemOrder(items)!=order)changes++;}));
                    for(int frame=0;frame<20;frame++)
                    {
                        var shifted=new PointF(center.X+frame%2,center.Y);
                        toys.DragBall(shifted,pet.Now);toys.DragTriangle(shifted);toys.DragGame(PlayTarget.Coin,shifted);toys.DragGame(PlayTarget.Card,shifted);toys.DragDie(shifted,pet.Now);toys.DragPlate(shifted);toys.DragJoystick(new PointF(shifted.X,shifted.Y+22*toys.Scale));
                        RefreshItems(pet);PetFrame(pet);
                    }
                    Check(changes==0&&ItemOrder(items)==order,"Overlapping toys, plate, and joystick never exchange layers during intermediate redraws in "+mode+" (changes: "+changes+")");
                }
                finally{foreach(var observer in observers)observer.Dispose();}
                RefreshItems(pet);
            }
            toys.DragBall(ball,pet.Now);toys.DragTriangle(triangle);toys.DragGame(PlayTarget.Coin,coin);toys.DragGame(PlayTarget.Card,card);toys.DragDie(die,pet.Now);toys.DragPlate(plate);toys.DragJoystick(joystick);
            pet.Model.Settings.Layer=modeBefore;pet.Model.Settings.Movement=movementBefore;pet.ApplyLayer();RefreshItems(pet);
        }
    }
}
