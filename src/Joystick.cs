using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Vpet
{
    [DataContract] public sealed class JoystickPreferences
    {
        [DataMember] public bool Visible;
        [DataMember] public float X=float.NaN,Y=float.NaN;
        [OnDeserializing] void Defaults(StreamingContext context){X=Y=float.NaN;}
    }
    internal sealed partial class ToyModel
    {
        public PointF JoystickPosition {get;private set;}
        public string JoystickDisplayId {get;private set;}
        public bool JoystickDragging;
        public bool HasJoystick {get{return pet.Settings.Joystick.Visible;}}
        public PointF JoystickApproach {get{return new PointF(JoystickPosition.X,JoystickPosition.Y-16*Scale);}}
        public event Action ArcadeRequested;
        public event Action ArcadeClosed;
        void RecoverJoystick()
        {
            if(!ToyPreferences.Finite(JoystickPosition.X)||!ToyPreferences.Finite(JoystickPosition.Y))
                JoystickPosition=new PointF(pet.Position.X-110*Scale,pet.Position.Y+15*Scale);
            PlaceJoystick(JoystickPosition);
        }
        void PlaceJoystick(PointF requested)
        {
            float best=float.MaxValue;PointF chosen=requested;string displayId=null;
            foreach(var display in pet.Displays)
            {
                var work=display.Work;var allowed=RectangleF.FromLTRB(work.Left+40*Scale,work.Top+60*Scale,
                    Math.Max(work.Left+40*Scale,work.Right-40*Scale),Math.Max(work.Top+60*Scale,work.Bottom-12*Scale));
                var candidate=Geometry.Clamp(requested,allowed);float distance=Geometry.Distance(requested,candidate);
                if(distance<best){best=distance;chosen=candidate;displayId=display.Id;}
            }
            JoystickPosition=chosen;JoystickDisplayId=displayId;pet.Settings.Joystick.X=chosen.X;pet.Settings.Joystick.Y=chosen.Y;
        }
        public void SetJoystickVisible(bool visible,double now)
        {
            pet.Settings.Joystick.Visible=visible;
            if(visible)RecoverJoystick();
            else {JoystickDragging=false;if(ArcadeClosed!=null)ArcadeClosed();if(Target==PlayTarget.Joystick)FinishFetch(now);}
        }
        public void DragJoystick(PointF point)
        {PlaceJoystick(point);if(Target==PlayTarget.Joystick&&Fetch==FetchPhase.Approaching)RestartApproach();}
        public void PressJoystick(double now)
        {
            if(!HasJoystick)return;
            if(Fetch==FetchPhase.Arcade){if(ArcadeRequested!=null)ArcadeRequested();return;}
            ClearAnnouncement();tune.Clear();Target=PlayTarget.Joystick;pet.BeginPlay();Fetch=FetchPhase.Approaching;phaseTime=0;RouteToJoystick();
        }
        void RouteToJoystick(){RouteTo(JoystickApproach,JoystickDisplayId);}
        bool JoystickTick()
        {
            if(Target!=PlayTarget.Joystick||Fetch==FetchPhase.Returning)return false;
            if(Fetch==FetchPhase.Approaching&&Arrived&&!JoystickDragging)
            {pet.CancelRoute();pet.FaceDownIdle();Fetch=FetchPhase.Arcade;if(ArcadeRequested!=null)ArcadeRequested();}
            return true;
        }
        public void LeaveArcade(double now){if(Target==PlayTarget.Joystick&&Fetch==FetchPhase.Arcade)FinishFetch(now);}
    }
    internal sealed class JoystickWindow : LayeredWindow
    {
        readonly ToyModel model;readonly PetModel pet;readonly LayeredWindow petWindow;
        readonly Action save;readonly Func<double> now;readonly Bitmap image;
        internal new readonly ContextMenuStrip Menu=new ContextMenuStrip();readonly MenuDismissal dismissal;
        bool pressed,dragged;Point pointerStart;PointF origin;
        public JoystickWindow(ToyModel model,PetModel pet,LayeredWindow petWindow,LayeredWindow crossingWindow,Action save,Func<double> now):base(false)
        {
            this.model=model;this.pet=pet;this.petWindow=petWindow;this.save=save;this.now=now;
            image=JoystickArtwork.Draw(model.Scale);Text="Vpet joystick";Cursor=Cursors.Hand;
            CompanionHandle=petWindow.Handle;OtherCompanionHandle=crossingWindow.Handle;
            IntPtr handle=Handle;Native.BackgroundAdornments.Add(handle);FormClosed+=delegate{Native.BackgroundAdornments.Remove(handle);};
            Menu.Items.Add("Remove Joystick",null,delegate{SetVisible(false);});ContextMenuStrip=Menu;dismissal=new MenuDismissal(Menu);
            MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;pressed=true;dragged=false;pointerStart=Cursor.Position;origin=model.JoystickPosition;Capture=true;};
            MouseMove+=delegate
            {
                if(!pressed)return;var delta=new PointF(Cursor.Position.X-pointerStart.X,Cursor.Position.Y-pointerStart.Y);
                if(!dragged&&Geometry.Distance(delta,PointF.Empty)<4)return;
                dragged=true;model.JoystickDragging=true;model.DragJoystick(new PointF(origin.X+delta.X,origin.Y+delta.Y));UpdateJoystick();
            };
            MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left||!pressed)return;bool click=!dragged;EndDrag();if(click)model.PressJoystick(now());};
            MouseCaptureChanged+=delegate{if(pressed&&!Capture)EndDrag();};
        }
        void EndDrag(){pressed=false;Capture=false;model.JoystickDragging=false;save();}
        public void SetVisible(bool visible){if(!visible){Menu.Close();EndDrag();}model.SetJoystickVisible(visible,now());UpdateJoystick();save();}
        public void UpdateJoystick()
        {
            if(!model.HasJoystick){if(Visible)Hide();return;}
            BehindWindow=pet.Settings.Layer==LayerMode.UnderAll?IntPtr.Zero:petWindow.Handle;SetLayer(pet.Settings.Layer);
            Present(image,new Point((int)Math.Round(model.JoystickPosition.X-40*model.Scale),(int)Math.Round(model.JoystickPosition.Y-60*model.Scale)));
            if(!Visible)Show();EnforceUnderAll();
        }
        protected override void Dispose(bool disposing){if(disposing){dismissal.Dispose();Menu.Dispose();image.Dispose();}base.Dispose(disposing);}
    }
    internal static class JoystickArtwork
    {
        public static Bitmap Draw(float scale)
        {
            var image=new Bitmap((int)Math.Ceiling(80*scale),(int)Math.Ceiling(72*scale),PixelFormat.Format32bppArgb);
            using(var source=new Bitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference","Joystick.png")))
            using(var g=Graphics.FromImage(image))using(var attributes=new ImageAttributes())
            {
                float factor=Math.Min(image.Width/(float)source.Width,image.Height/(float)source.Height);
                int width=(int)Math.Round(source.Width*factor),height=(int)Math.Round(source.Height*factor);
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source,new Rectangle((image.Width-width)/2,(image.Height-height)/2,width,height),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);
            }
            return image;
        }
    }
}
