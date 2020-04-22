using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;
using System.IO;
using System.IO.Compression;
using log4net;
using System.Text.RegularExpressions;
using FTPTool.Settings;
using CommandLine;
using FTPTool.CommandLineUtils;
using System.Globalization;
using Blogica.Interfaces.DB;

namespace FTPTool.WinSCP
{

    public static class Helpers
    {
        public static DateTime? ExtractDate(string value, string regex)
        {
            DateTime? res = null;
            Match datematch = Regex.Match(value, regex, RegexOptions.IgnoreCase);
            if (datematch.Success)
            {
                string filedate = datematch.Value;
                DateTime filenamedate;
                DateTime.TryParseExact(filedate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out filenamedate);
                res = filenamedate;
            }
            return res;
        }
    }

    public class FileDesc
    {
        public string FileName { get; set; }
        public string RemotePathFileName { get; set; }
        public string LocalPathFileName { get; set; }
        public DateTime FileNameDate { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
    public class WinSCPWrapper
    {
        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected AppSettings settings;
        protected FileInfo[] files;

        public WinSCPWrapper(string[] args)
        {
            AppSettings settings = new AppSettings();
            var parserresult = CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args)
              .WithParsed(options =>
              {
                  settings.LoadFromCommandLine(options);
              })
              .WithNotParsed(
                errors =>
                {
                    foreach (var er in errors)
                    { Console.WriteLine(er); }
                }
              );
        }

        protected void PrepareUploadList()
        {
            DirectoryInfo di = new DirectoryInfo(settings.LocalFolder);
            this.files = di.GetFiles("*.txt", SearchOption.TopDirectoryOnly);
            int i = 0;
            while (i <= this.files.Length - 1)
            {
                i++;
            }
        }

        protected SessionOptions getEurobankSessionOptions()
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                PortNumber = settings.FTPPort,
                Protocol = Protocol.Sftp,
                HostName = settings.FTPServer,
                UserName = settings.UserName,
                Password = settings.Password,
                SshHostKeyFingerprint = settings.SshHostKeyFingerprint
            };
            return sessionOptions;
        }

        protected TransferOptions GetTransferOptions()
        {
            TransferOptions transferOptions = new TransferOptions();
            transferOptions.TransferMode = TransferMode.Binary;
            transferOptions.PreserveTimestamp = true;
            transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;
            return transferOptions;
        }
        protected Session GetOpenSession()
        {
            Session session = new Session();
            session.FileTransferProgress += Session_FileTransferProgress;
            session.FileTransferred += Session_FileTransferred;
            session.Failed += Session_Failed;
            SessionOptions sessionOptions = getEurobankSessionOptions();
            session.Open(sessionOptions);
            return session;
        }

        public bool UploadFiles()
        {
            bool res = false;
            if (settings.Upload)
            {
                PrepareUploadList();
                try
                {
                    // Setup session options
                    SessionOptions sessionOptions = getEurobankSessionOptions();
                    using (Session session = GetOpenSession())
                    {
                        TransferOptions transferOptions = GetTransferOptions();
                        TransferOperationResult transferResult;
                        foreach (FileInfo f in this.files)
                        {
                            Console.WriteLine("Uploading file {0}", f.FullName);
                            transferResult = session.PutFiles(f.FullName, settings.LocalFolder, true, transferOptions);
                            transferResult.Check();
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                log.InfoFormat("Upload of {0} succeeded", transfer.FileName);
                            }
                        }
                        res = true;
                    }
                }
                catch (Exception e)
                {
                    log.Error($"{e.Message} {e.StackTrace}");
                    if (e.InnerException != null)
                    {
                        log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                    }
                }
            }
            return res;
        }
        /*public List<FileDesc> DonloadFilesByPattern(string filepattern)
        {
            List<FileDesc> fileslist = new List<FileDesc>();
            try
            {
                // Setup session options
                SessionOptions sessionOptions = getEurobankSessionOptions();
                using (Session session = GetOpenSession())
                {
                    RemoteDirectoryInfo rinfo = session.ListDirectory(settings.RemoteFTPFolder);
                    if (rinfo != null)
                    {
                        string remotefolder = settings.RemoteFTPFolder;
                        if (!remotefolder.Last().Equals('/'))
                        {
                            remotefolder = remotefolder + "/";
                        }
                        string localfolder = settings.LocalFolder;
                        if (!localfolder.Last().Equals('\\'))
                        {
                            localfolder = localfolder + "\\";
                        }
                        RemoteFileInfoCollection rfiles = rinfo.Files;
                        foreach (RemoteFileInfo f in rfiles)
                        {
                            if (!f.IsDirectory)
                            {
                                Match match = Regex.Match(f.Name, filepattern, RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    string fname = remotefolder + f.Name;
                                    string destfname = localfolder + f.Name;
                                    fileslist.Add(new FileDesc { FileName = f.Name, LocalPathFileName = destfname, RemotePathFileName = fname });
                                }
                            }
                        }
                        DonloadFiles(fileslist);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"{e.Message} {e.StackTrace}");
                if (e.InnerException != null)
                {
                    log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                }
            }
            return fileslist;
        }

        public DateTime? GetMostRecentFiles()
        {
            DateTime? res = null;
            List<FileDesc> l = GetFileList();
            if (l != null)
            {
                res = l.OrderByDescending(x => x.FileNameDate).First().FileNameDate;
            }
            return res;
        }*/

        public List<FileDesc> GetFileList(byte windowSize)
        {
            log.Debug("GetFileList in");
            List<FileDesc> fileslist = new List<FileDesc>();
            try
            {
                // Setup session options
                SessionOptions sessionOptions = getEurobankSessionOptions();
                using (Session session = GetOpenSession())
                {
                    RemoteDirectoryInfo rinfo = session.ListDirectory(settings.RemoteFTPFolder);
                    if (rinfo != null)
                    {
                        string remotefolder = settings.RemoteFTPFolder;
                        if (!remotefolder.Last().Equals('/'))
                        {
                            remotefolder = remotefolder + "/";
                        }
                        string localfolder = settings.LocalFolder;
                        if (!localfolder.Last().Equals('\\'))
                        {
                            localfolder = localfolder + "\\";
                        }
                        RemoteFileInfoCollection rfiles = rinfo.Files;
                        log.Info($"files found:{rfiles.Count}");
                        List<FileDesc> tempfilelist = new List<FileDesc>();
                        foreach (RemoteFileInfo f in rfiles)
                        {
                            if (!f.IsDirectory)
                            {
                                Match match = Regex.Match(f.Name, settings.FTPDownloadRegEX, RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    string fname = remotefolder + f.Name;
                                    string destfname = localfolder + f.Name;
                                    //string datedigits = new String(fname.Where(Char.IsDigit).ToArray());
                                    string datePartRegEx = settings.DatePartRegEx;
                                    string filedate = "";
                                    Match datematch = Regex.Match(f.Name, datePartRegEx, RegexOptions.IgnoreCase);
                                    if (datematch.Success)
                                    {
                                        filedate = datematch.Value;
                                        DateTime filenamedate;
                                        if (DateTime.TryParseExact(filedate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out filenamedate))
                                        {
                                            //log.Info($"extracted date:{filenamedate} from filename:{f.Name}");
                                            tempfilelist.Add(new FileDesc { FileName = f.Name, LocalPathFileName = destfname, RemotePathFileName = fname, FileNameDate = filenamedate, LastWriteTime = f.LastWriteTime });
                                        }
                                    }
                                }
                            }
                        }
                        tempfilelist.Sort((x, y) => x.FileNameDate.CompareTo(y.FileNameDate));
                        if (windowSize > 0)
                        {
                            fileslist.AddRange(tempfilelist.OrderByDescending(x => x.FileNameDate).Take(windowSize));
                        }
                        else
                        {
                            fileslist.AddRange(tempfilelist);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"{e.Message} {e.StackTrace}");
                if (e.InnerException != null)
                {
                    log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                }
            }
            log.Debug("GetFileList out");
            return fileslist;
        }

        /*public IEnumerable<FileDesc> DownloadFiles(IEnumerable<FileDesc> remotefiles)
        {
            if ((remotefiles == null) && (remotefiles.Count() <= 0)) return null;
            List<FileDesc> resultlist = new List<FileDesc>();
            if (!settings.Download)
            {
                try
                {
                    using (Session session = GetOpenSession())
                    {
                        TransferOptions transferOptions = GetTransferOptions();
                        foreach (FileDesc f in remotefiles)
                        {
                            try
                            {
                                TransferOperationResult transferResult = session.GetFiles(f.RemotePathFileName, f.LocalPathFileName, settings.RemoveFiles, transferOptions);
                                transferResult.Check();
                                // Print results
                                foreach (TransferEventArgs transfer in transferResult.Transfers)
                                {
                                    log.InfoFormat("download of {0} succeeded", transfer.FileName);
                                    //fileslist.Add(destfname);
                                    resultlist.Add(new FileDesc { FileName = f.FileName, LocalPathFileName = f.LocalPathFileName, RemotePathFileName = f.RemotePathFileName });
                                }
                            }
                            catch (Exception e)
                            {
                                log.Error($"{e.Message} {e.StackTrace}");
                                if (e.InnerException != null)
                                {
                                    log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error($"{e.Message} {e.StackTrace}");
                    if (e.InnerException != null)
                    {
                        log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                    }
                }
            }
            return resultlist;
        }*/

        protected bool DonloadIfAllFilesExist(IEnumerable<FileDesc> remotefiles)
        {
            log.Debug("DonloadIfAllFilesExist in");
            bool res = false;
            if ((remotefiles == null) && (remotefiles.Count() <= 0)) return res;
            if (settings.Download)
            {
                List<FileDesc> trasferedfileslist = new List<FileDesc>();
                try
                {
                    using (Session session = GetOpenSession())
                    {
                        bool candownload = false;
                        try
                        {
                            candownload = remotefiles.All(file => session.FileExists(file.RemotePathFileName));
                        }
                        catch (Exception e)
                        {
                            candownload = false;
                            log.Error($"{e.Message} {e.StackTrace}");
                            if (e.InnerException != null)
                            {
                                log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                            }
                        }
                        if (candownload)
                        {
                            log.Warn("all files present, will proceed to download");
                            TransferOptions transferOptions = GetTransferOptions();
                            foreach (FileDesc f in remotefiles)
                            {
                                try
                                {
                                    TransferOperationResult transferResult = session.GetFiles(f.RemotePathFileName, f.LocalPathFileName, settings.RemoveFiles, transferOptions);
                                    transferResult.Check();
                                    // Print results
                                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                                    {
                                        log.InfoFormat("download of {0} succeeded", transfer.FileName);
                                        trasferedfileslist.Add(new FileDesc { FileName = f.FileName, LocalPathFileName = f.LocalPathFileName, RemotePathFileName = f.RemotePathFileName });
                                    }
                                }
                                catch (Exception e)
                                {
                                    log.Error($"{e.Message} {e.StackTrace}");
                                    if (e.InnerException != null)
                                    {
                                        log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                                    }
                                }
                            }
                            if (remotefiles.Count() == trasferedfileslist.Count())
                            {
                                res = trasferedfileslist.All(file => File.Exists(file.LocalPathFileName));
                            }
                        }
                        else
                        {
                            log.Warn("not all files present");
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error($"{e.Message} {e.StackTrace}");
                    if (e.InnerException != null)
                    {
                        log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                    }
                }
            }
            log.Debug("DonloadIfAllFilesExist out");
            return res;
        }

        protected void Session_Failed(object sender, FailedEventArgs e)
        {
            log.Error($"session failed {e.ToString()}");
        }

        protected void Session_FileTransferred(object sender, TransferEventArgs e)
        {
            log.Info($"file transfered {e.FileName}");
        }

        protected void Session_FileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            log.Info($"FileProgress:{e.FileProgress:P1}, OverallProgress:{e.OverallProgress:P1}");
        }

        protected void UploadFile(string filename, string destinationdir)
        {
            try
            {
                // Setup session options
                SessionOptions sessionOptions = getEurobankSessionOptions();
                using (Session session = new Session())
                {
                    // Connect
                    session.Open(sessionOptions);

                    // Upload files
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;

                    TransferOperationResult transferResult;
                    transferResult = session.PutFiles(filename, destinationdir, false, transferOptions);

                    // Throw on any error
                    transferResult.Check();

                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        log.InfoFormat("Upload of {0} succeeded", transfer.FileName);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"{e.Message} {e.StackTrace}");
                if (e.InnerException != null)
                {
                    log.Error($"{e.InnerException.Message} {e.InnerException.StackTrace}");
                }
            }
        }
        public IEnumerable<string> UnzipFiles(IEnumerable<string> zipfiles)
        {
            log.Debug("UnzipFiles in");
            List<string> l = new List<string>();
            if (settings.Unzip)
            {
                string unzipfolder = settings.UnzipFolder;
                if (!unzipfolder.EndsWith(@"\"))
                {
                    unzipfolder = unzipfolder + "\\";
                }
                foreach (string zipfile in zipfiles)
                {
                    ZipArchive archive = ZipFile.OpenRead(zipfile);
                    foreach (ZipArchiveEntry en in archive.Entries)
                    {
                        string fname = unzipfolder + en.Name;
                        en.ExtractToFile(fname, true);
                        l.Add(fname);
                    }
                }
            }
            log.Info($"[COUNTERS:UNZIPPEDFILES]{l.Count()}");
            log.Debug("UnzipFiles out");
            return l;
        }
    }
    public class EurobankFTPDownloader : WinSCPWrapper
    {
        public EurobankFTPDownloader(string[] args) : base(args)
        {

        }
        protected bool PackageAlreadyDownloadedForDate(DateTime date)
        {
            DBContext db = new DBContext();
            return db.PackageAlreadyDownloadedForDate(settings.UserID, date);
        }

        public IEnumerable<FileDesc> DownloadFilesForToday()
        {
            return DownloadFilesForDate(DateTime.Now);
        }
        public IEnumerable<FileDesc> DownloadMostRecentFiles()
        {
            log.Debug("DownloadMostRecentFiles in");
            List<FileDesc> res = new List<FileDesc>();
            IEnumerable<FileDesc> downloadlist = GetFileList(settings.WindowSize);
            if (downloadlist != null)
            {
                log.Debug($"downloadlist count:{downloadlist.Count()}");
                IEnumerable<DateTime> datelist = downloadlist.Select(x => x.FileNameDate).Distinct().OrderBy(d => d);
                if (datelist != null)
                {
                    log.Debug($"datelist count:{datelist.Count()}");
                    foreach (DateTime date in datelist)
                    {
                        log.Debug($"going to download files for date:{date}");
                        IEnumerable<FileDesc> fd = DownloadFilesForDate(date);
                        if (fd != null)
                        {
                            res.AddRange(fd);
                        }
                        else
                        {
                            log.Info($"no results for date:{date}");
                        }
                    }
                }
            }
            log.Debug("DownloadMostRecentFiles out");
            return res;
        }
        public IEnumerable<FileDesc> DownloadFilesForDate(DateTime date)
        {
            log.Debug("DownloadFilesForDate in");
            List<FileDesc> res = null;
            if (!PackageAlreadyDownloadedForDate(date))
            {
                log.Info($"package {date} was never fully downloaded");
                string remotefolder = settings.RemoteFTPFolder;
                string file1 = $"{settings.UserID}pay_{date:yyyyMMdd}.zip";
                string file2 = $"{settings.UserID}_{date:yyyyMMdd}.zip";
                if (!remotefolder.Last().Equals('/'))
                {
                    remotefolder = remotefolder + "/";
                }
                string localfolder = settings.LocalFolder;
                if (!localfolder.Last().Equals('\\'))
                {
                    localfolder = localfolder + "\\";
                }
                List<FileDesc> l = new List<FileDesc>();
                l.Add(new FileDesc { FileName = file1, RemotePathFileName = remotefolder + file1, LocalPathFileName = localfolder + file1, FileNameDate = date });
                l.Add(new FileDesc { FileName = file2, RemotePathFileName = remotefolder + file2, LocalPathFileName = localfolder + file2, FileNameDate = date });
                if (DonloadIfAllFilesExist(l))
                {
                    res = l;
                }
            }
            else
            {
                log.Warn($"package already donloaded for date:{date}");
            }
            log.Debug("DownloadFilesForDate out");
            return res;
        }
        public IEnumerable<string> UnzipFiles(DateTime date, IEnumerable<FileDesc> files)
        {
            log.Debug($"UnzipFiles in");
            IEnumerable<string> res = null;
            res = UnzipFiles(files.Where(w => w.FileNameDate == date).Select(x => x.LocalPathFileName));
            log.Debug($"UnzipFiles out");
            return res;
        }
        public IEnumerable<FileDesc> MarkFiles(DateTime date, IEnumerable<FileDesc> files, IEnumerable<string> unzippedFiles)
        {
            log.Debug($"MarkFiles in");
            DBContext db = new DBContext();
            Package p = new Package
            {
                ID = -1,
                ExternalID = settings.UserID,
                PackageDate = date,
                PackageState = "DOWNLOADED",
                PackageName = $"{date:yyyyMMdd}",
                Process = settings.Process
            };
            foreach (FileDesc f in files.Where(w => w.FileNameDate == date))
            {
                log.Debug($"downloaded file:{f.FileName}");
                p.PackageFiles.Add(new PackageFile { FileName = f.FileName, ExternalFileName = f.RemotePathFileName, LocalFileName = f.LocalPathFileName, FileState = "DOWNLOADED", PackageID = -1 });
            }
            foreach (string f in unzippedFiles)
            {
                log.Debug($"unzipped file:{f}");
                p.PackageFiles.Add(new PackageFile { FileName = f, LocalFileName = f, FileState = "QUEUED", PackageID = -1 });
            }
            db.NewPackage(p);
            log.Debug($"MarkFiles out");
            return files;
        }
    }
}
