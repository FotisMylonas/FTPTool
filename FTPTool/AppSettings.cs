using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.Web;
using FTPTool.CommandLineUtils;
using log4net;

namespace FTPTool.Settings
{
    public class AppSettings
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string LocalFolder { get; set; }
        public string RemoteFTPFolder { get; set; }
        public string FTPServer { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int FTPPort { get; set; }
        public string SshHostKeyFingerprint { get; set; }
        public bool RemoveFiles { get; set; }
        public bool Download { get; set; }
        public bool Upload { get; set; }
        public bool Unzip { get; set; }
        public string FTPDownloadRegEX { get; set; }

        public string DatePartRegEx { get; set; }
        public string Process { get; set; }
        public string UserID { get; set; }
        public string UnzipFolder { get; set; }
        public string DownloadDate { get; set; }
        public byte WindowSize { get; set; }
        public void WriteSettingsToConsole()
        {
            List<string> sb = new List<string>();
            sb.Add(string.Format("FTPServer:{0}", FTPServer));
            sb.Add(string.Format("FTPPort:{0}", FTPPort));
            sb.Add(string.Format("UserName:{0}", UserName));
            sb.Add(string.Format("LocalFolder:{0}", LocalFolder));
            sb.Add(string.Format("RemoteFTPFolder:{0}", RemoteFTPFolder));
            sb.Add(string.Format("SshHostKeyFingerprint:{0}", SshHostKeyFingerprint));
            sb.Add($"UnzipFolder:{UnzipFolder}");
            sb.Add($"Download:{Download}");
            sb.Add($"Upload:{Upload}");
            sb.Add($"Process:{Process}");
            sb.Add($"DownloadDate:{DownloadDate}");
            sb.Add($"DatePartRegEx:{DatePartRegEx}");
            foreach (string s in sb)
            {
                log.Info(s);
            }
        }
        public void LoadFromCommandLine(CommandLineOptions options)
        {
            RemoteFTPFolder = options.RemoteFTPFolder;
            LocalFolder = options.LocalFolder;
            Password = options.Password;
            UserName = options.UserName;
            UserID = options.UserID;
            UnzipFolder = options.UnzipFolder;
            Process = options.Process;
            DownloadDate = options.DownloadDate;
            Download = string.Equals(options.Action, "download", StringComparison.InvariantCultureIgnoreCase) || string.Equals(options.Action, "download_unzip", StringComparison.InvariantCultureIgnoreCase);
            Upload = string.Equals(options.Action, "upload", StringComparison.InvariantCultureIgnoreCase);
            Unzip = string.Equals(options.Action, "download_unzip", StringComparison.InvariantCultureIgnoreCase);
        }
        public AppSettings()
        {
            NameValueCollection nvc = ConfigurationManager.AppSettings;
            FTPServer = nvc["FTPServer"];
            FTPPort = Convert.ToInt32(nvc["FTPPort"]);
            WindowSize = Convert.ToByte(nvc["WindowSize"]);
            SshHostKeyFingerprint = nvc["SshHostKeyFingerprint"];
            FTPDownloadRegEX = nvc["FTPDownloadRegEX"];
            DatePartRegEx = nvc["DatePartRegEx"];
            string s = nvc["RemoveFiles"];
            bool removefilesval = false;
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (bool.TryParse(s, out bool tempval))
                {
                    removefilesval = tempval;
                }
            }
            RemoveFiles = removefilesval;
        }
    }

/*    public static class Convertors
    {
        public static AppSettings CommandLineAppSettings(CommandLineOptions options)
        {
            AppSettings res = null;
            if (options != null)
            {
                res = new AppSettings();
                res.RemoteFTPFolder = options.RemoteFTPFolder;
                res.LocalFolder = options.LocalFolder;
                res.Password = options.Password;
                res.UserName = options.UserName;
                res.RemoveFiles = options.RemoveFiles;
                res.Download = string.Equals(options.Action, "download", StringComparison.InvariantCultureIgnoreCase) || string.Equals(options.Action, "download_unzip", StringComparison.InvariantCultureIgnoreCase);
                res.Upload = string.Equals(options.Action, "upload", StringComparison.InvariantCultureIgnoreCase);
                res.Unzip = string.Equals(options.Action, "download_unzip", StringComparison.InvariantCultureIgnoreCase);
            }
            return res;
        }

        public static AppSettings ConfigFileToAppSettings()
        {
            AppSettings res = new AppSettings();
            {
                NameValueCollection nvc = ConfigurationManager.AppSettings;
                res.FTPServer = nvc["FTPServer"];
                res.FTPPort = Convert.ToInt32(nvc["FTPPort"]);
                res.SshHostKeyFingerprint = nvc["SshHostKeyFingerprint"];
                res.SshHostKeyFingerprint = nvc["SshHostKeyFingerprint"];
                string s = nvc["RemoveFiles"];
                bool removefilesval = false;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    if (bool.TryParse(s, out bool tempval))
                    {
                        removefilesval = tempval;
                    }
                }
                res.RemoveFiles = removefilesval;
            }
            return res;
        }
    }*/
}
