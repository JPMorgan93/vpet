using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static string MappingJson(SpriteProject project)
        {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(SpriteManifest)).WriteObject(stream,project.Data);return System.Text.Encoding.UTF8.GetString(stream.ToArray());}}
        static void SheetReplacement()
        {
            string sheet=Path.Combine(artifacts,"replacement.png"),saved=Path.Combine(artifacts,"replaced.vpetproject");
            using(var project=MakerFixture())
            {
                project.Data.FacesRight=true;project.Data.Diagonals=false;project.Data.EmoteAnimations=true;
                project.SetSize(2,22,26);project.Data.Frames[0][0].OffsetX=1;project.Data.Frames[0][0].OffsetY=2;
                project.Data.Frames[11][4]=project.Data.Frames[0][0].Copy();
                string mappings=MappingJson(project);var data=project.Data;
                using(var image=new Bitmap(320,300,PixelFormat.Format32bppArgb))
                {using(var g=Graphics.FromImage(image))SpritePackage.CopyPixels(g,project.Source,0,0);image.SetPixel(2,2,Color.Lime);image.SetResolution(144,144);image.Save(sheet,ImageFormat.Png);}
                project.ReplaceSource(sheet);
                Check(project.Source.Size==new Size(320,300)&&project.Source.GetPixel(2,2).ToArgb()==Color.Lime.ToArgb(),"Updated sheet adopts new pixels and dimensions without DPI resampling");
                Check(project.Data==data&&MappingJson(project)==mappings,"Replacing sheet preserves every mapping, size, offset, optional row and facing option");
                using(var frame=project.RenderFrame(0,0))Check(frame.GetPixel(3,4).ToArgb()==Color.Lime.ToArgb(),"Existing crop and offsets immediately render revised artwork");
                project.Save(saved);
                using(var loaded=SpriteProject.Load(saved))using(var sprite=loaded.Build())
                {Check(MappingJson(loaded)==mappings&&loaded.Source.Size==new Size(320,300),"Saving and reopening embeds revised PNG with identical mappings");Check(HasLime(sprite.Frame(false,6,0)),"Final sprite export uses revised artwork at the retained selection");}
                using(var image=new Bitmap(16,16,PixelFormat.Format32bppArgb)){image.SetPixel(2,2,Color.Lime);image.Save(sheet,ImageFormat.Png);}
                project.ReplaceSource(sheet);
                Check(MappingJson(project)==mappings&&project.Problems(false).Any(p=>p.Contains("outside the sheet")),"Smaller replacement retains mappings and flags their out-of-bounds selections");
                project.Save(saved);using(var loaded=SpriteProject.Load(saved))Check(MappingJson(loaded)==mappings&&loaded.Problems(false).Count>0,"Incomplete mappings on a smaller sheet remain editable after saving");
                Reject(delegate{using(var built=project.Build()){ }},"Invalid preserved selections cannot silently export wrong sprites");
                var current=project.Source;
                using(var image=new Bitmap(20,20)){using(var g=Graphics.FromImage(image))g.Clear(Color.White);image.Save(sheet,ImageFormat.Png);}
                Reject(delegate{project.ReplaceSource(sheet);},"Opaque replacement is rejected");
                File.WriteAllText(sheet,"invalid PNG");Reject(delegate{project.ReplaceSource(sheet);},"Broken replacement is rejected");
                using(var image=new Bitmap(4097,1)){image.SetPixel(0,0,Color.Red);image.Save(sheet,ImageFormat.Png);}
                Reject(delegate{project.ReplaceSource(sheet);},"Replacement respects source-image size limit");
                Check(project.Source==current&&MappingJson(project)==mappings,"Invalid replacement leaves source image and all mapping data untouched");
            }
        }
        static int Crossings(byte[] wave)
        {
            int result=0;short previous=0;
            for(int i=44;i<Math.Min(wave.Length,8044);i+=2){short sample=BitConverter.ToInt16(wave,i);if(sample*previous<0)result++;if(sample!=0)previous=sample;}
            return result;
        }
        static void InstrumentSounds()
        {
            string settings=Path.Combine(artifacts,"instrument-settings.json");File.WriteAllText(settings,"{\"Toys\":{}}");
            Check(Preferences.Load(settings).Toys.Sound==TriangleSound.Chime,"Existing toy preferences default to Chime");
            byte[][] waves=new byte[3][];
            foreach(TriangleSound sound in Enum.GetValues(typeof(TriangleSound)))
            {
                var prefs=new Preferences();prefs.Toys.Sound=sound;prefs.Save(settings);Check(Preferences.Load(settings).Toys.Sound==sound,"Sound choice persists: "+sound);
                byte[] wave=ToyChime.CreateWave(sound);waves[(int)sound]=wave;
                Check(wave.Length==22094&&BitConverter.ToInt32(wave,24)==22050&&BitConverter.ToInt32(wave,40)==22050,"Instrument wave is valid half-second PCM: "+sound);
                int peak=0;double firstEnergy=0,lastEnergy=0;
                for(int i=44;i<wave.Length;i+=2){int sample=BitConverter.ToInt16(wave,i);peak=Math.Max(peak,Math.Abs(sample));if(i<2244)firstEnergy+=(double)sample*sample;if(i>wave.Length-2200)lastEnergy+=(double)sample*sample;}
                Check(peak>1000&&peak<short.MaxValue&&BitConverter.ToInt16(wave,44)==0&&Math.Abs((int)BitConverter.ToInt16(wave,wave.Length-2))<5,"Sound is audible, unclipped, and starts/ends smoothly: "+sound);
                Check(firstEnergy>lastEnergy*10,"Sound decays before its end: "+sound);
                File.WriteAllBytes(Path.Combine(artifacts,sound+".wav"),wave);
                var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.SpawnTriangle();pet.Place(toys.Triangle);toys.Settings.Sound=sound;
                var heard=new System.Collections.Generic.List<TriangleSound>();toys.ChimePlayed+=delegate{heard.Add(toys.Settings.Sound);};
                toys.PressTriangle(0);toys.PressTriangle(.2);
                for(int i=0;i<1000&&pet.Playing;i++)ToyStep(toys,pet,.2+i*.01,.01f);
                Check(heard.Count==4&&heard.All(s=>s==sound),"User and pet playback share sound choice and retain exact tap count: "+sound);
            }
            Check(!waves[0].SequenceEqual(waves[1])&&!waves[0].SequenceEqual(waves[2])&&!waves[1].SequenceEqual(waves[2]),"All three instrument sounds have distinct waveforms");
            Check(Crossings(waves[1])<Crossings(waves[0])&&Crossings(waves[2])>Crossings(waves[1])*2,"Honk has a lower pitch than chime; snare contains a broadband rattle");
            File.WriteAllText(settings,"{\"Toys\":{\"Sound\":999}}");Check(Preferences.Load(settings).Toys.Sound==TriangleSound.Chime,"Invalid saved sound safely returns to Chime");
        }
        static void SheetReplacementWindows()
        {
            string path=Path.Combine(artifacts,"update-sheet-ui.png"),projectPath=Path.Combine(artifacts,"update-sheet-ui.vpetproject");
            using(var maker=new SpriteMakerWindow())
            {
                maker.Show();Application.DoEvents();Check(!FindButton(maker,"Update Sprite Sheet").Enabled,"Updating artwork is unavailable until a project is open");
                var project=MakerFixture();project.Save(projectPath);maker.SetProject(project,projectPath);maker.ChooseCycle(2);maker.ChooseSlot(1);
                var sheet=MakerField<SpriteSheetView>(maker,"sheet");var draft=sheet.Draft;var zoom=MakerField<PreviewZoomBar>(maker,"zoom");zoom.SetPercent(300,null);
                using(var image=new Bitmap(project.Source)){image.SetPixel(26,53,Color.Lime);image.Save(path,ImageFormat.Png);}
                string mappings=MappingJson(project);
                Check(FindButton(maker,"Update Sprite Sheet").Enabled&&maker.UpdateSpriteSheet(path),"Open project accepts the updated sheet");
                Check(maker.Project==project&&maker.Dirty&&MappingJson(project)==mappings&&maker.Cycle==2&&maker.Slot==1&&sheet.Draft==draft&&zoom.Percent==300,"Sheet update preserves project, current selection, zoom and mappings while marking edits unsaved");
                Check(MakerField<Button>(maker,"complete").Enabled&&maker.SaveProject(false),"Valid revised artwork can still be tweaked and saves into the same project");
                using(var loaded=SpriteProject.Load(projectPath))Check(loaded.Source.GetPixel(26,53).ToArgb()==Color.Lime.ToArgb(),"Saved project contains revised artwork");
                Check(!maker.UpdateSpriteSheet(Path.Combine(artifacts,"missing-sheet.png"))&&!maker.Dirty&&maker.Project==project,"Failed sheet update preserves saved project and dirty state");
                using(var image=new Bitmap(16,16,PixelFormat.Format32bppArgb)){image.SetPixel(2,2,Color.Lime);image.Save(path,ImageFormat.Png);}
                Check(maker.UpdateSpriteSheet(path)&&!MakerField<Button>(maker,"complete").Enabled&&MakerField<TextBox>(maker,"status").Text.Contains("outside the sheet"),"Smaller sheet displays mapping errors and disables completion until repaired");
                Check(maker.SaveProject(false),"An incomplete updated project can be saved for later mapping repairs");
                using(var image=new Bitmap(maker.Width,maker.Height)){maker.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(artifacts,"update-sprite-sheet.png"));}
                maker.Close();
            }
        }
    }
}
