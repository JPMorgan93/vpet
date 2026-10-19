using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Vpet
{
    public enum FoodKind { Pudding }
    [DataContract]
    public sealed class PlatePreferences
    {
        [DataMember] public bool Visible;
        [DataMember] public float X=float.NaN,Y=float.NaN;
        [DataMember] public FoodKind DefaultFood=FoodKind.Pudding;
        [OnDeserializing] void Defaults(StreamingContext context){X=Y=float.NaN;}
        public void Validate(){if(!Enum.IsDefined(typeof(FoodKind),DefaultFood))DefaultFood=FoodKind.Pudding;}
    }

    internal sealed partial class ToyModel
    {
        public PointF PlatePosition {get;private set;}
        public string PlateDisplayId {get;private set;}
        public int FoodRemaining {get;private set;}
        public bool PlateDragging;
        public bool HasPlate {get{return pet.Settings.Plate.Visible;}}
        public PointF EatingPosition {get{return new PointF(PlatePosition.X,PlatePosition.Y-6*ItemArtwork.PixelScale(Scale));}}
        public RectangleF PlateBounds {get{var anchor=PlateArtwork.Anchor(Scale);return new RectangleF(new PointF(PlatePosition.X-anchor.X,PlatePosition.Y-anchor.Y),PlateArtwork.CanvasSize(Scale));}}
        void StorePlate(){pet.Settings.Plate.X=PlatePosition.X;pet.Settings.Plate.Y=PlatePosition.Y;}
        void PlacePlate(PointF requested)
        {
            DisplayArea best=pet.Displays[0];PointF result=requested;float score=float.MaxValue;
            foreach(var display in pet.Displays)
            {
                var work=display.Work;
                var size=PlateArtwork.CanvasSize(Scale);
                var anchor=PlateArtwork.Anchor(Scale);
                var allowed=RectangleF.FromLTRB(work.Left+anchor.X,work.Top+anchor.Y,
                    Math.Max(work.Left+anchor.X,work.Right-size.Width+anchor.X),Math.Max(work.Top+anchor.Y,work.Bottom-size.Height+anchor.Y));
                var point=Geometry.Clamp(requested,allowed);float distance=Geometry.Distance(point,requested);
                if(distance<score){best=display;result=point;score=distance;}
            }
            PlatePosition=result;PlateDisplayId=best.Id;StorePlate();
        }
        void RecoverPlate()
        {
            if(!ToyPreferences.Finite(PlatePosition.X)||!ToyPreferences.Finite(PlatePosition.Y))
                PlatePosition=new PointF(pet.Position.X+100*Scale,pet.Position.Y+20*Scale);
            PlacePlate(PlatePosition);
        }
        public void SetPlateVisible(bool visible,double now)
        {
            pet.Settings.Plate.Visible=visible;
            if(visible)RecoverPlate();
            else{PlateDragging=false;FoodRemaining=0;if(Target==PlayTarget.Plate)FinishFetch(now);}
        }
        public void DragPlate(PointF point)
        {
            PlacePlate(point);
            if(Target==PlayTarget.Plate&&Fetch!=FetchPhase.None&&Fetch!=FetchPhase.Returning)RestartApproach();
        }
        public void PressPlate(double now)
        {if(HasPlate&&FoodRemaining==0)ServeFood(pet.Settings.Plate.DefaultFood,now);}
        public void ServeFood(FoodKind food,double now)
        {
            if(!HasPlate||!Enum.IsDefined(typeof(FoodKind),food))return;
            pet.Settings.Plate.DefaultFood=food;FoodRemaining=3;
            ClearAnnouncement();tune.Clear();Target=PlayTarget.Plate;pet.BeginPlay();pet.ShakeUntil=0;
            Fetch=FetchPhase.Approaching;phaseTime=0;RouteToPlate();
            if(ReactionPlayed!=null)ReactionPlayed(Reactions.Hunger);
        }
        void RouteToPlate(){RouteTo(EatingPosition,PlateDisplayId);}
        bool FoodTick(double now,float dt)
        {
            if(Target!=PlayTarget.Plate||Fetch==FetchPhase.Returning)return false;
            if(PlateDragging)return true;
            if(Fetch==FetchPhase.Approaching)
            {
                if(Arrived){pet.CancelRoute();pet.FaceDownIdle();Fetch=FetchPhase.Shaking;phaseTime=0;pet.ShakeUntil=now+.125;}
                return true;
            }
            phaseTime+=Math.Max(0,Math.Min(.1f,dt));
            if(Fetch==FetchPhase.Shaking&&phaseTime>=.125f)
            {pet.ShakeUntil=0;phaseTime=0;Fetch=FetchPhase.Waiting;}
            else if(Fetch==FetchPhase.Waiting&&phaseTime>=.125f)
            {
                FoodRemaining=Math.Max(0,FoodRemaining-1);pet.ShakeUntil=0;phaseTime=0;
                if(FoodRemaining==0)FinishFetch(now);else Fetch=FetchPhase.Pausing;
            }
            else if(Fetch==FetchPhase.Pausing&&phaseTime>=.15f)
            {Fetch=FetchPhase.Shaking;phaseTime=0;pet.ShakeUntil=now+.125;}
            return true;
        }
    }

    internal sealed class PlateWindow : LayeredWindow
    {
        readonly ToyModel model;
        readonly PetModel pet;
        readonly LayeredWindow petWindow;
        readonly Action save;
        readonly Func<double> now;
        readonly Bitmap[] images=new Bitmap[4];
        internal new readonly ContextMenuStrip Menu=new ContextMenuStrip();
        readonly MenuDismissal dismissal;
        Point pointerStart;
        PointF plateStart;
        bool pressed,dragged;
        LayerMode? layer;
        int shownFood=-1;
        public PlateWindow(ToyModel model,PetModel pet,LayeredWindow petWindow,LayeredWindow crossingWindow,Action save,Func<double> now):base(false)
        {
            this.model=model;this.pet=pet;this.petWindow=petWindow;this.save=save;this.now=now;
            Text="Vpet plate";Cursor=Cursors.Hand;
            for(int i=0;i<images.Length;i++)images[i]=PlateArtwork.Draw(i,model.Scale);
            CompanionHandle=petWindow.Handle;OtherCompanionHandle=crossingWindow.Handle;
            AboveCompanions=true;
            IntPtr handle=Handle;Native.BackgroundAdornments.Add(handle);FormClosed+=delegate{Native.BackgroundAdornments.Remove(handle);};
            var pudding=new ToolStripMenuItem("Pudding"){Checked=true};Menu.Items.Add(pudding);
            pudding.Click+=delegate{model.ServeFood(FoodKind.Pudding,now());UpdatePlate();save();};
            Menu.Items.Add(new ToolStripSeparator());Menu.Items.Add("Remove Plate",null,delegate{SetVisible(false);});
            Menu.Opening+=delegate{pudding.Checked=pet.Settings.Plate.DefaultFood==FoodKind.Pudding;};
            ContextMenuStrip=Menu;dismissal=new MenuDismissal(Menu);
            MouseDown+=delegate(object sender,MouseEventArgs e)
            {if(e.Button!=MouseButtons.Left)return;pressed=true;dragged=false;pointerStart=Cursor.Position;plateStart=model.PlatePosition;Capture=true;};
            MouseMove+=delegate
            {
                if(!pressed)return;var point=Cursor.Position;var delta=new PointF(point.X-pointerStart.X,point.Y-pointerStart.Y);
                if(!dragged&&Geometry.Distance(delta,PointF.Empty)<4)return;
                dragged=true;model.PlateDragging=true;model.DragPlate(new PointF(plateStart.X+delta.X,plateStart.Y+delta.Y));UpdatePlate();
            };
            MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)EndGesture(true);};
            MouseCaptureChanged+=delegate{if(pressed&&!Capture)EndGesture(false);};
        }
        void EndGesture(bool click)
        {
            if(!pressed)return;pressed=false;Capture=false;model.PlateDragging=false;
            if(click&&!dragged)model.PressPlate(now());UpdatePlate();save();
        }
        public void SetVisible(bool visible)
        {EndGesture(false);Menu.Close();model.SetPlateVisible(visible,now());UpdatePlate();save();}
        public void UpdatePlate()
        {
            if(!model.HasPlate){Hide();return;}
            var location=Point.Round(model.PlateBounds.Location);
            if(!Visible||PresentedBounds.Location!=location||shownFood!=model.FoodRemaining)
            {Present(images[model.FoodRemaining],location);shownFood=model.FoodRemaining;}
            if(layer!=pet.Settings.Layer){SetLayer(pet.Settings.Layer);layer=pet.Settings.Layer;}
            if(!Visible)Show();
            // Stay above both sprite portions without changing the selected application-window layer.
            EnforceAboveCompanions();
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){dismissal.Dispose();Menu.Dispose();foreach(var image in images)image.Dispose();}
            base.Dispose(disposing);
        }
    }

    internal static class PlateArtwork
    {
        internal static PointF Anchor(float scale){float pixel=ItemArtwork.PixelScale(scale);return new PointF(16*pixel,25*pixel);}
        internal static Size CanvasSize(float scale){return ItemArtwork.CanvasSize(scale);}
        // Four states share exactly the same ground anchor; only the food is removed between bites.
        public static Bitmap Draw(int remaining,float scale)
        {
            var size=CanvasSize(scale);
            var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            var source=ItemArtwork.Canvas;
            var target=new RectangleF(0,0,size.Width,size.Height);
            using(var graphics=Graphics.FromImage(image))
            {
                ItemArtwork.Draw(graphics,"Plate.png",target,source);
                if(remaining>0)ItemArtwork.Draw(graphics,"Pudding - "+(4-Math.Min(3,remaining))+".png",target,source);
            }
            return image;
        }
    }
}
