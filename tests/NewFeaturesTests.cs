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
        static void OptionalAnimations()
        {
            using(var project=MakerFixture())
            {
                Check(!project.Data.FacesRight&&!project.Data.EmoteAnimations,"Old facing convention and optional emotes default safely");
                project.Data.EmoteAnimations=true;
                Check(project.Problems(true).Count==0,"Missing optional emote rows never block export");
                for(int row=10;row<18;row++)
                {project.SetSize(row,20,24);project.Data.Frames[row][0]=project.Data.Frames[0][0].Copy();project.Data.Frames[row][3]=project.Data.Frames[0][0].Copy();project.Data.Frames[row][3].OffsetX=3;}
                project.Data.Frames[17][0]=project.Data.Frames[17][3]=null;
                project.Data.FacesRight=true;
                string path=Path.Combine(artifacts,"emote-facing.vpetproject");project.Save(path);
                using(var loaded=SpriteProject.Load(path))using(var sprite=loaded.Build())
                {
                    Check(loaded.Data.FacesRight&&loaded.Data.EmoteAnimations&&loaded.Data.Frames.Length==18,"Facing and optional emote project rows survive saving");
                    Check(sprite.Counts.Length==18&&sprite.Cell==new Size(20,24),"Optional atlas rows preserve runtime cell dimensions");
                    int[] facings={6,2,0,7,1};
                    for(int row=0;row<10;row++)using(var source=project.RenderFrame(row,0))
                    {
                        var frame=sprite.Frame(row>=5,facings[row%5],0);bool exact=true;
                        for(int y=0;y<24;y++)for(int x=0;x<20;x++)exact&=frame.GetPixel(x,y).ToArgb()==source.GetPixel(x,y).ToArgb();
                        Check(exact,"Right-facing source moves right; up and down stay unmirrored: row "+row);
                    }
                    for(int reaction=0;reaction<7;reaction++)
                    {
                        Check(sprite.EmoteFrame(reaction,0)!=null&&sprite.EmoteFrame(reaction,2)==sprite.EmoteFrame(reaction,0),"Optional "+Reactions.Names[reaction]+" animation loops its actual selected frames");
                        Check(sprite.EmoteFrame(reaction,0).GetPixel(2,2).A>0,"Emote animations are not mirrored by directional sheet option");
                    }
                    Check(sprite.EmoteFrame(7,0)==null&&sprite.EmoteFrame(8,0)==null&&sprite.EmoteFrame(-1,0)==null,"Missing, custom and pause reactions fall back without indexing optional frames");
                    string output=Path.Combine(artifacts,"emote-facing.vpetsprite");sprite.SavePackage(output);
                    using(var imported=SpriteSet.Import(output))Check(imported.Counts.SequenceEqual(sprite.Counts)&&imported.EmoteFrame(1,0).GetPixel(2,2).A>0,"Optional runtime animations survive export/import");
                    loaded.Data.EmoteAnimations=false;
                    using(var normal=loaded.Build())Check(normal.Counts.Length==10&&normal.EmoteFrame(1,0)==null,"Turning off optional emotes exports a legacy-compatible movement atlas");
                    Check(loaded.Data.Frames[11][0]!=null,"Turning off optional emotes retains project selections");
                }
                project.Data.Frames[11][0].X=4096;Check(project.Problems(true).Any(p=>p.StartsWith("Love,")),"Invalid selected optional artwork is explained before export");
                // A true v2 file has ten size entries and ten frame rows, unlike new projects.
                var legacy=new SpriteManifest{Version=2,Kind="project",Width=20,Height=24,Frames=project.Data.Frames.Take(10).ToArray(),CycleWidths=Enumerable.Repeat(20,10).ToArray(),CycleHeights=Enumerable.Repeat(24,10).ToArray()};
                path=Path.Combine(artifacts,"v2-migration.vpetproject");SpritePackage.Write(path,legacy,project.Source);
                using(var migrated=SpriteProject.Load(path))
                {Check(migrated.Data.Frames.Length==18&&migrated.Slots(11).Length==0&&migrated.Width(11)==20,"V2 projects gain empty optional slots and valid dimensions");migrated.Save(path);using(var sprite=migrated.Build())Check(sprite.EmoteFrame(1,0)==null,"Migrated project keeps ordinary runtime behavior");}
            }
            using(var pause=Artwork.Bubble(-1,null,1,false))
            {Check(pause.GetPixel(25,25).R<150&&pause.GetPixel(34,25).R==255&&pause.GetPixel(40,25).R<150,"Pause bubble draws two separated bars");pause.Save(Path.Combine(artifacts,"pause-bubble.png"));}
            Check(Reactions.Names.Length==8&&!Reactions.Names.Contains("Pause"),"Pause never becomes a replaceable personality emote");
        }
        static void TrianglePlay()
        {
            foreach(MovementMode mode in Enum.GetValues(typeof(MovementMode)))
            {
                var pet=Pet(mode);pet.Settings.Speed=0;if(mode==MovementMode.Restricted)pet.SetRadius(30);
                var toys=Toys(pet);toys.MoveZone(new PointF(750,400));toys.SpawnTriangle();
                int sounds=0;double clock=0;var played=new List<double>();toys.ChimePlayed+=delegate{sounds++;played.Add(clock);};
                toys.PressTriangle(0);clock=.22;toys.PressTriangle(clock);clock=.59;toys.PressTriangle(clock);
                Check(sounds==3&&toys.RememberedNotes==3&&toys.Target==PlayTarget.Triangle&&pet.Playing,"Each user tap chimes immediately and redirects "+mode+" pet to the instrument");
                for(int i=0;i<18000&&pet.Playing;i++){clock=.6+i*.01;ToyStep(toys,pet,clock,.01f);}
                Check(sounds==6&&!pet.Playing,"Pet repeats exactly three notes and resumes "+mode+" settings");
                Near((float)(played[4]-played[3]),.22f,.021f,"Playback remembers the first tap interval");
                Near((float)(played[5]-played[4]),.37f,.021f,"Playback remembers the second tap interval");
                Check(toys.NextPlayAt-clock>=59&&toys.NextPlayAt-clock<=120,"Completed toy action schedules a randomized later visit");
                if(mode==MovementMode.Restricted)Check(Geometry.Distance(pet.Position,pet.Anchor)<=30,"Instrument visit returns pet to its unchanged restricted circle");
            }
            var p=Pet(MovementMode.Static);var t=Toys(p);t.RemoveBall(0);t.SpawnTriangle();int chimes=0;t.ChimePlayed+=delegate{chimes++;};
            p.Place(t.Triangle);t.PressTriangle(0);t.PressTriangle(.001);t.PressTriangle(.002);
            for(int i=0;i<100;i++)ToyStep(t,p,i*.01,.01f);
            Check(chimes==3,"Instrument waits for a pause and arrival before playing the phrase");
            p.Paused=true;for(int i=0;i<300;i++)ToyStep(t,p,1+i*.01,.01f);Check(chimes==3,"Settings pause suppresses instrument playback");p.Paused=false;
            for(int i=0;i<250;i++)ToyStep(t,p,4+i*.01,.01f);Check(chimes==6,"Rapid taps remain distinct during playback after unpausing");
            t.PressTriangle(7);t.LaunchPull(new PointF(10,10),7); // Missing ball cannot interrupt a phrase.
            Check(t.Target==PlayTarget.Triangle,"Launching a removed ball cannot interrupt instrument play");
            t.SpawnBall(7);t.LaunchPull(new PointF(20,10),7);Check(t.Target==PlayTarget.Ball&&t.RememberedNotes==0,"New user ball launch overrides the triangle phrase");
            t.PressTriangle(8);Check(t.Target==PlayTarget.Triangle&&t.RememberedNotes==1,"New triangle press overrides a ball fetch");
            t.RemoveTriangle(8);int before=chimes;for(int i=0;i<500;i++)ToyStep(t,p,8+i*.01,.01f);Check(chimes==before&&!p.Playing,"Removing triangle cancels pending playback");
            t.SpawnTriangle();t.DragTriangle(new PointF(-10000,10000));Check(ToyModel.ContainsInclusive(t.TriangleBounds,t.Triangle),"Triangle dragging keeps the whole instrument in the fence");
            t.MoveZone(new PointF(850,160));Check(ToyModel.ContainsInclusive(t.TriangleBounds,t.Triangle),"Moving zone recovers an excluded triangle");
            t.PressTriangle(14);t.CancelFetchForPetDrag(14);Check(t.Fetch==FetchPhase.None&&t.RememberedNotes==0,"Picking up pet cancels instrument memory");
            t.SetVisible(false,15);Check(!t.HasTriangle&&!t.HasBall&&!t.Settings.DisplayChest,"Closing chest removes all toys");
            p=Pet(MovementMode.Static);t=Toys(p);t.BeforePetTick(0,.01f);double due=t.NextPlayAt;Check(due>=60&&due<=120,"Spontaneous play starts on a 60–120 second timer");
            p.Paused=true;ToyStep(t,p,due+1,.01f);Check(t.Fetch==FetchPhase.None,"Menu or settings pause prevents spontaneous play");p.Paused=false;
            ToyStep(t,p,due+2,.01f);Check(t.Fetch!=FetchPhase.None&&t.Target==PlayTarget.Ball,"Available ball is visited spontaneously after random interval");
            t.CancelFetchForPetDrag(due+3);t.RemoveBall(due+3);t.SpawnTriangle();due=t.NextPlayAt;ToyStep(t,p,due+.01,.01f);
            Check(t.Fetch!=FetchPhase.None&&t.Target==PlayTarget.Triangle&&t.RememberedNotes>=1&&t.RememberedNotes<=3,"Pet can spontaneously visit the triangle and play a short phrase");
            t.SetVisible(false,due+1);ToyStep(t,p,due+500,.01f);Check(!p.Playing,"Closed chest never restarts spontaneous play");
            byte[] wave=ToyChime.CreateWave();Check(wave.Length==22094&&System.Text.Encoding.ASCII.GetString(wave,0,4)=="RIFF"&&BitConverter.ToInt32(wave,24)==22050,"Chime is a valid half-second PCM wave");
            int peak=0;for(int i=44;i<wave.Length;i+=2)peak=Math.Max(peak,Math.Abs((int)BitConverter.ToInt16(wave,i)));Check(peak>1000&&peak<16000,"Generated chime is audible with headroom against clipping");
            using(var image=ToyArtwork.Triangle(1)){Check(image.GetPixel(0,0).A==0&&SpriteProject.VisibleBounds(image).Height>30,"Triangle drawing retains transparent corners and a visible instrument");image.Save(Path.Combine(artifacts,"triangle.png"));}
            p=Pet(MovementMode.Static);t=Toys(p);t.RemoveBall(0);t.SpawnTriangle();p.Place(t.Triangle);chimes=0;t.ChimePlayed+=delegate{chimes++;};
            t.PressTriangle(0);t.PressTriangle(.2);t.PressTriangle(.4);double now=0;
            while(chimes<4&&now<10){now+=.01;ToyStep(t,p,now,.01f);}
            Check(chimes==4&&t.Fetch==FetchPhase.Repeating,"First repeat note begins after the user's three taps");
            t.DragTriangle(new PointF(t.Triangle.X-80,t.Triangle.Y));
            while(p.Playing&&now<30){now+=.01;ToyStep(t,p,now,.01f);}
            Check(chimes==6&&!p.Playing,"Moving instrument during playback reroutes pet and plays only the remaining notes");
            p=Pet(MovementMode.Static);p.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1),new DisplayArea("right",new Rectangle(200,0,1000,760),1.5f)});
            p.Place(new PointF(-500,400));t=Toys(p);t.RemoveBall(0);t.MoveZone(new PointF(800,400));t.SpawnTriangle();t.PressTriangle(0);now=0;
            while(p.Crossing==null&&now<25){now+=.01;ToyStep(t,p,now,.01f);}
            var crossing=p.Crossing;Check(crossing!=null,"Triangle visit uses seamless cross-display routing");
            t.PressTriangle(now);Check(p.Crossing==crossing&&t.RememberedNotes==2,"Further taps preserve the ongoing display transition");
            p.SetDisplays(new List<DisplayArea>{new DisplayArea("left",new Rectangle(-1000,100,900,650),1)});t.RecoverDisplays();
            Check(t.DisplayId=="left"&&ToyModel.ContainsInclusive(t.TriangleBounds,t.Triangle),"Disconnected display recovers the triangle into its play zone");
        }
        static void NewMakerWindows()
        {
            using(var maker=new SpriteMakerWindow())
            {
                maker.SetProject(MakerFixture(),null);maker.Show();Application.DoEvents();
                Check(FindButton(maker,"How to Guide")!=null,"Sheet editor provides an in-app how-to guide");
                MakerField<ComboBox>(maker,"facing").SelectedIndex=1;MakerField<CheckBox>(maker,"emotes").Checked=true;Application.DoEvents();
                Check(maker.Project.Data.FacesRight&&maker.Project.Data.EmoteAnimations,"Facing and optional emote controls update the project");
                var cycles=MakerField<Button[]>(maker,"cycles");Check(cycles.Skip(10).All(b=>b.Visible)&&MakerField<SpriteSheetViewport>(maker,"viewport").Height>=100,"Optional buttons are visible and leave usable sheet space");
                maker.ChooseCycle(11);maker.SetDimensions(25,27);maker.Project.Data.Frames[11][0]=new SpriteFrame();
                using(var tweak=new SpriteTweakWindow(maker))
                {
                    tweak.Show();Application.DoEvents();Check(FindButton(tweak,"How to Guide")!=null&&FindButton(tweak,"Love")!=null,"Tweak offers its guide and populated reaction animations");
                    FindButton(tweak,"Love").PerformClick();FindButton(tweak,"Tweak").PerformClick();FindButton(tweak,"Magic Tweak").PerformClick();FindButton(tweak,"Undo").PerformClick();
                    Check(maker.Project.Data.Frames.Length==18&&maker.Project.Data.Frames[11][0].OffsetY==0,"Undo preserves all optional rows and restores their offsets");
                    using(var shot=new Bitmap(tweak.Width,tweak.Height)){tweak.DrawToBitmap(shot,new Rectangle(Point.Empty,shot.Size));shot.Save(Path.Combine(artifacts,"emote-tweak.png"));}tweak.Close();
                }
                using(var shot=new Bitmap(maker.Width,maker.Height)){maker.DrawToBitmap(shot,new Rectangle(Point.Empty,shot.Size));shot.Save(Path.Combine(artifacts,"emote-maker.png"));}
                foreach(bool tweak in new[]{false,true})using(var guide=MakerGuide.Create(tweak))
                {guide.Show();Application.DoEvents();Check(guide.Controls.OfType<RichTextBox>().Single().Text.Contains(tweak?"Magic Tweak":"Step 4"),"Guide displays complete selectable instructions");guide.Close();}
                maker.Dirty=false;maker.Close();
            }
            string root=AppDomain.CurrentDomain.BaseDirectory;
            using(var pet=new PetWindow(Path.Combine(artifacts,"pause-"+Guid.NewGuid().ToString("N")),Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),true,Path.Combine(artifacts,"pause-smoke")))
            {
                pet.Show();MakerField<Timer>(pet,"timer").Stop();pet.Model.Paused=true;
                Check(pet.ShowPause,"Paused pet shows the reserved pause symbol");pet.PreviewReaction(1);Check(!pet.ShowPause&&pet.ActiveReaction==1,"Try a reaction temporarily replaces pause indicator");
                typeof(PetWindow).GetField("explicitPreviewUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,0d);
                Check(pet.ShowPause,"Pause symbol returns after explicit preview");pet.Model.Paused=false;Check(!pet.ShowPause,"Pause clears when pet resumes");pet.Close();
            }
        }
    }
}
