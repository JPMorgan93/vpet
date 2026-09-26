using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Vpet
{
    [DataContract] internal sealed class GitHubAsset
    {
        [DataMember(Name="name")] public string Name { get; set; }
        [DataMember(Name="browser_download_url")] public string Url { get; set; }
        [DataMember(Name="size")] public long Size { get; set; }
    }
    [DataContract] internal sealed class GitHubRelease
    {
        [DataMember(Name="tag_name")] public string Tag { get; set; }
        [DataMember(Name="draft")] public bool Draft { get; set; }
        [DataMember(Name="prerelease")] public bool Prerelease { get; set; }
        [DataMember(Name="assets")] public GitHubAsset[] Assets { get; set; }
    }
    internal sealed class AvailableUpdate
    {
        public string Version,FileName,DownloadUrl,ChecksumUrl;
        public long Size;
    }
    internal static class Updates
    {
        internal const int MaximumInstallerSize=64*1024*1024;
        public static AvailableUpdate Parse(string json,string currentVersion,string repository)
        {
            GitHubRelease release;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                release=(GitHubRelease)new DataContractJsonSerializer(typeof(GitHubRelease)).ReadObject(stream);
            if(release==null||release.Draft||release.Prerelease||!Regex.IsMatch(release.Tag??"","^v[0-9]+\\.[0-9]+\\.[0-9]+$"))return null;
            Version version;
            if(!Version.TryParse(release.Tag.Substring(1),out version)||version<=new Version(currentVersion))return null;
            string name="Vpet-Setup-"+version+"-Windows-x64.exe";
            GitHubAsset installer=null,checksum=null;
            foreach(var asset in release.Assets??new GitHubAsset[0])
            {
                if(asset.Name==name){if(installer!=null)throw new InvalidDataException("Duplicate installer asset.");installer=asset;}
                if(asset.Name=="SHA256SUMS.txt"){if(checksum!=null)throw new InvalidDataException("Duplicate checksum asset.");checksum=asset;}
            }
            if(installer==null||checksum==null)throw new InvalidDataException("The public release is missing its installer or checksum.");
            if(installer.Size<=0||installer.Size>MaximumInstallerSize)throw new InvalidDataException("Unexpected update size.");
            string prefix="https://github.com/"+repository+"/releases/download/"+release.Tag+"/";
            if(installer.Url!=prefix+name||checksum.Url!=prefix+"SHA256SUMS.txt")throw new InvalidDataException("The update assets do not belong to this repository and release.");
            return new AvailableUpdate{Version=version.ToString(),FileName=name,DownloadUrl=installer.Url,ChecksumUrl=checksum.Url,Size=installer.Size};
        }
        public static AvailableUpdate Check()
        {
            try
            {
                return Parse(Encoding.UTF8.GetString(Fetch("https://api.github.com/repos/"+ReleaseInfo.Repository+"/releases/latest",1024*1024)),ReleaseInfo.Version,ReleaseInfo.Repository);
            }
            catch(WebException ex)
            {
                var response=ex.Response as HttpWebResponse;
                if(response!=null&&response.StatusCode==HttpStatusCode.NotFound){response.Dispose();return null;}
                if(response!=null)response.Dispose();throw;
            }
        }
        static byte[] Fetch(string url,int maximum)
        {
            ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
            var request=(HttpWebRequest)WebRequest.Create(url);
            request.UserAgent="Vpet/"+ReleaseInfo.Version;request.Accept="application/vnd.github+json";
            request.Headers["X-GitHub-Api-Version"]="2022-11-28";
            request.Timeout=30000;request.ReadWriteTimeout=30000;
            using(var response=(HttpWebResponse)request.GetResponse())
            {
                if(response.ResponseUri.Scheme!="https"||response.ContentLength>maximum)throw new InvalidDataException("Invalid update response.");
                using(var input=response.GetResponseStream())using(var output=new MemoryStream())
                {
                    var buffer=new byte[16384];int count;
                    while((count=input.Read(buffer,0,buffer.Length))>0)
                    {if(output.Length+count>maximum)throw new InvalidDataException("Update response is too large.");output.Write(buffer,0,count);}
                    return output.ToArray();
                }
            }
        }
        public static string ExpectedHash(string checksums,string fileName)
        {
            string result=null;
            foreach(string raw in checksums.Split('\n'))
            {
                var match=Regex.Match(raw.Trim(),"^([a-fA-F0-9]{64}) [ *](.+)$");
                if(match.Success&&match.Groups[2].Value==fileName)
                {if(result!=null)throw new InvalidDataException("Duplicate installer checksum.");result=match.Groups[1].Value.ToLowerInvariant();}
            }
            if(result==null)throw new InvalidDataException("Installer checksum is missing.");return result;
        }
        public static string Hash(string file)
        {
            using(var stream=File.OpenRead(file))using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        public static string Download(AvailableUpdate update,string dataDirectory,out string hash)
        {
            hash=ExpectedHash(Encoding.UTF8.GetString(Fetch(update.ChecksumUrl,16384)),update.FileName);
            byte[] bytes=Fetch(update.DownloadUrl,MaximumInstallerSize);
            if(bytes.LongLength!=update.Size)throw new InvalidDataException("The downloaded installer size does not match the release.");
            using(var sha=SHA256.Create())
                if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=hash)
                    throw new InvalidDataException("The installer checksum did not match. Nothing was installed.");
            string folder=Path.Combine(dataDirectory,"Updates",update.Version);Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,update.FileName);File.WriteAllBytes(path,bytes);return path;
        }
        public static void StartInstallerAfterExit(string installer,string hash)
        {
            string helper=Path.Combine(Path.GetDirectoryName(installer),"Vpet.UpdateHelper.exe");
            File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location,helper,true);
            var info=new ProcessStartInfo(helper,"--apply-update "+Process.GetCurrentProcess().Id+" \""+installer+"\" "+hash){UseShellExecute=false};
            using(var process=Process.Start(info))if(process==null)throw new IOException("Could not start the update installer helper.");
        }
        public static void ApplyUpdate(string[] args)
        {
            if(args.Length!=4)throw new InvalidDataException("Invalid updater arguments.");
            int id;if(!int.TryParse(args[1],out id))throw new InvalidDataException("Invalid process ID.");
            try { using(var parent=Process.GetProcessById(id))if(!parent.WaitForExit(30000))throw new IOException("Close Vpet before installing its update."); }
            catch(ArgumentException){} // The original process already exited.
            string path=Path.GetFullPath(args[2]);
            string expectedRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VpetPrototype","Updates")+Path.DirectorySeparatorChar;
            if(!path.StartsWith(expectedRoot,StringComparison.OrdinalIgnoreCase)||!Regex.IsMatch(Path.GetFileName(path),"^Vpet-Setup-[0-9]+\\.[0-9]+\\.[0-9]+-Windows-x64\\.exe$")||!Regex.IsMatch(args[3],"^[a-f0-9]{64}$")||Hash(path)!=args[3])throw new InvalidDataException("Installer verification failed.");
            Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
        }
    }
}
