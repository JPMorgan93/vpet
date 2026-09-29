using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ToyUpdates()
        {
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                var pet=Pet(mode);pet.Settings.Speed=0;var toys=Toys(pet);toys.SpawnCard();double now=0;
                StartUserCard(toys,pet,ref now);toys.ChooseCard(true);Until(toys,pet,ref now,()=>toys.CardRevealed);Until(toys,pet,ref now,()=>!pet.Playing);
                var position=pet.Position;toys.PressCard(now);
                Check(toys.CardTurningDown&&!pet.Playing&&!pet.Destination.HasValue&&toys.Announcement==null,"Clicking a revealed card only starts its back flip in "+mode);
                bool sawFront=false,sawBack=false;for(int i=0;i<70;i++){sawFront|=toys.CardShowsFace;sawBack|=!toys.CardShowsFace;now+=.01;ToyStep(toys,pet,now,.01f);}
                Check(sawFront&&sawBack&&!toys.CardRevealed&&!toys.CardTurningDown&&pet.Position==position&&toys.Fetch==FetchPhase.None,"Manual back flip animates both sides without a new game or pet movement");
                StartUserCard(toys,pet,ref now);var waiting=pet.Position;
                for(int i=0;i<295;i++){now+=.1;ToyStep(toys,pet,now,.1f);}Check(toys.WaitingForCardChoice,"User can still choose before 30 seconds");
                pet.Paused=true;for(int i=0;i<50;i++){now+=.1;ToyStep(toys,pet,now,.1f);}pet.Paused=false;
                Check(toys.WaitingForCardChoice,"Settings pause suspends the card decision timer");
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Leaving,2);
                Check(toys.Announcement==null&&pet.Destination.HasValue&&!toys.CardRevealed,"Thirty-second timeout clears the called card and starts departure");
                Until(toys,pet,ref now,()=>!pet.Playing);
                Check(Geometry.Distance(waiting,pet.Position)>3&&pet.Settings.Movement==mode&&pet.Settings.Speed==0,"Timeout moves elsewhere even in Static or at speed zero, then restores preferences");
                StartUserCard(toys,pet,ref now);toys.ChooseCard(false);Until(toys,pet,ref now,()=>toys.CardRevealed);Until(toys,pet,ref now,()=>!pet.Playing);
                pet.Place(new PointF(100,150));toys.PressCard(now,true);
                Check(toys.CardRevealed&&!toys.CardTurningDown&&toys.Fetch==FetchPhase.Approaching,"Autonomous play leaves the revealed card up until arrival");
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.TurningDown);
                Check(toys.CardTurningDown&&toys.Announcement==null,"Pet flips the card down after arriving, before announcing its next card");
                Until(toys,pet,ref now,()=>toys.WaitingForCardChoice);Check(!toys.CardRevealed&&!toys.CardTurningDown&&toys.Announcement!=null,"Autonomous game begins after the back flip finishes");
                Until(toys,pet,ref now,()=>toys.Fetch==FetchPhase.Result);Until(toys,pet,ref now,()=>!pet.Playing);
            }
            var model=Pet(MovementMode.Static);var games=Toys(model);games.SpawnDie();games.SpawnCoin();games.SpawnCard();games.SpawnTriangle();double time=0;
            games.LaunchPull(new PointF(40,20),time);games.DragBall(new PointF(-10000,10000),time);
            Check(!games.Rolling&&games.Launcher==BallLauncher.None&&!model.Playing&&ToyModel.ContainsInclusive(games.BallBounds,games.Ball),"Moving the ball clamps it and cancels its active launch/fetch");
            games.RollDie(time);games.DragDie(new PointF(10000,-10000),time);
            Check(!games.DieRolling&&!model.Playing&&ToyModel.ContainsInclusive(games.DieBounds,games.Die),"Moving the D20 clamps it and ends its roll without launching");
            games.MoveZone(new PointF(240,160));games.DragGame(PlayTarget.Coin,new PointF(games.Coin.X,games.CoinBounds.Top));games.PressCoin(time);
            Until(games,model,ref time,()=>games.Fetch==FetchPhase.Flipping);for(int i=0;i<55;i++){time+=.01;ToyStep(games,model,time,.01f);}
            Check(games.CoinDrawPosition.Y<games.Zone.Top&&games.CoinDrawPosition.Y<model.Current.Work.Top&&games.CoinDrawPosition.X==games.Coin.X,"Coin flip rises upward beyond fence and screen edges");
            games.CleanUp(time);Check(games.Settings.DisplayChest&&games.Settings.DisplayZone&&!games.HasBall&&!games.HasTriangle&&!games.HasCoin&&!games.HasCard&&!games.HasDie&&games.Announcement==null&&!model.Playing,"Cleanup removes all toys and active play while retaining chest and fence");
            using(var red=ToyArtwork.Fence(new Size(480,320),1,Color.Red))using(var blue=ToyArtwork.Fence(new Size(480,320),1))
            {
                Check(red.GetPixel(246,160).R>red.GetPixel(246,160).B&&blue.GetPixel(246,160).B>blue.GetPixel(246,160).R,"Independent restriction is red and play zone stays blue");
                using(var sheet=new Bitmap(500,720))using(var g=Graphics.FromImage(sheet))
                using(var r=ToyArtwork.FenceTitle("Restricted Area",1,Color.Red))using(var b=ToyArtwork.FenceTitle("Play Zone",1,Color.FromArgb(32,115,201)))
                {g.Clear(Color.LightGray);g.DrawImageUnscaled(r,10,0);g.DrawImageUnscaled(red,10,32);g.DrawImageUnscaled(b,10,365);g.DrawImageUnscaled(blue,10,397);sheet.Save(Path.Combine(artifacts,"named-fences.png"));}
            }
        }
        static void ToyUpdateWindowChecks(PetWindow pet)
        {
            var windows=pet.Toys;var toys=windows.Model;pet.Model.Settings.Movement=MovementMode.Static;pet.Model.Settings.Layer=LayerMode.OverEverything;pet.ApplyLayer();windows.SetVisible(true);
            Check(windows.Menu.Items[1].Text=="Clean Up Toys","Cleanup is directly below Display Play Zone");
            var menu=MakerField<ContextMenuStrip>(pet,"menu");Check(menu.Items[menu.Items.Count-3] is ToolStripSeparator&&menu.Items[menu.Items.Count-2].Text=="Check for Updates…"&&menu.Items[menu.Items.Count-1].Text=="Close Vpet","Update check sits below the separator and directly above Close Vpet");
            foreach(string name in new[]{"Ball","D20","Coin","Card","Triangle"})if(!((ToolStripMenuItem)Item(windows.Menu,name)).Checked)Item(windows.Menu,name).PerformClick();windows.Update();Application.DoEvents();
            Check(windows.FenceLabel.Visible&&windows.FenceLabel.Text=="Play Zone"&&windows.FenceLabel.Bottom<=windows.Fence.Top,"Play Zone label is visible above its top-left corner");
            foreach(bool die in new[]{false,true})
            {
                var window=die?windows.Die:windows.Ball;var original=die?toys.Die:toys.Ball;var point=Point.Round(original);
                ToyMouse(window,0x201,point);ToyMouse(window,0x200,new Point(point.X+20,point.Y-15));Check(!windows.Arrow.Visible,"Left drag has no launch arrow");ToyMouse(window,0x202,Cursor.Position);
                Check((die?toys.Die:toys.Ball)!=original&&!(die?toys.DieRolling:toys.Rolling),"Left drag repositions without launching: "+window.Text);
                point=Point.Round(die?toys.Die:toys.Ball);ToyMouse(window,0x204,point);ToyMouse(window,0x200,new Point(point.X+50,point.Y+25));Check(windows.Arrow.Visible,"Right drag shows the launch arrow");ToyMouse(window,0x205,Cursor.Position);
                Check((die?toys.DieVelocity:toys.Velocity).X<0&&(die?toys.DieVelocity:toys.Velocity).Y<0,"Right drag launches opposite the pull: "+window.Text);
            }
            toys.DragDie(toys.Die,pet.Now);windows.Update();pet.Model.Place(new PointF(toys.Zone.Left+60,toys.Zone.Bottom-10));
            var render=typeof(PetWindow).GetMethod("Render",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);render.Invoke(pet,null);
            Cursor.Position=Point.Round(toys.Die);Application.DoEvents();render.Invoke(pet,null);
            Check(windows.CurrentAnnouncement!=null&&windows.CurrentAnnouncement.Kind==SpecialEmoteKind.Number&&windows.CurrentAnnouncement.Value==toys.DieValue&&toys.Announcement==null,"Hovering the stopped D20 previews its value without changing game state");
            Check(MakerField<LayeredWindow>(pet,"bubble").Visible&&MakerField<string>(pet,"gameReactionKey").StartsWith("Number:"+toys.DieValue+":"),"Die hover renders the number in the pet's speech bubble");
            Cursor.Position=new Point(pet.Model.Current.Work.Left+1,pet.Model.Current.Work.Top+1);Check(windows.CurrentAnnouncement==null,"Moving off the die clears its transient value preview");
            double now=0;StartUserCard(toys,pet.Model,ref now);toys.ChooseCard(true);Until(toys,pet.Model,ref now,()=>toys.CardRevealed);windows.Update();var card=Point.Round(toys.Card);
            ToyMouse(windows.Card,0x201,card);ToyMouse(windows.Card,0x202,card);Check(toys.CardTurningDown&&!pet.Model.Playing&&!pet.Model.Destination.HasValue,"Native face-up click only turns the card over");
            Until(toys,pet.Model,ref now,()=>!toys.CardTurningDown);ToyMouse(windows.Card,0x201,card);ToyMouse(windows.Card,0x202,card);Check(toys.Fetch==FetchPhase.Approaching,"Native face-down click starts the next game");
            Item(windows.Menu,"Clean Up Toys").PerformClick();
            Check(windows.Chest.Visible&&windows.Fence.Visible&&!windows.Ball.Visible&&!windows.Die.Visible&&!windows.Card.Visible&&!windows.Coin.Visible&&!windows.Triangle.Visible&&!pet.Model.Playing,"Native cleanup removes all toys but leaves the chest and fence");
            Check(windows.Menu.Items.OfType<ToolStripMenuItem>().Where(i=>i.Tag is PlayTarget).All(i=>!i.Checked),"Cleanup unchecks every toy option");
        }
    }
}
