using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTPTool.Settings;
using FTPTool.WinSCP;
using FTPTool.CommandLineUtils;
using CommandLine;
using log4net;
using System.Globalization;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace FTPTool
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            log.Debug("Main in");
            try
            {
                AppSettings settings = new AppSettings();
                string dateString = "";
                var parserresult = CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args)
                  .WithParsed(options =>
                  {
                      dateString = options.DownloadDate;
                      settings.LoadFromCommandLine(options);
                  })
                  .WithNotParsed(
                    errors =>
                    {
                        foreach (var er in errors)
                        { log.Error(er.ToString()); }
                    }
                  );
                EurobankFTPDownloader ftp = new EurobankFTPDownloader(args);
                DateTime? filesdate = null;
                if (!string.IsNullOrWhiteSpace(dateString))
                {
                    CultureInfo provider = CultureInfo.InvariantCulture;
                    string format = "yyyyMMdd";
                    try
                    {
                        filesdate = DateTime.ParseExact(dateString, format, provider);
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
                IEnumerable<FileDesc> files = null;
                if (filesdate != null)
                {
                    log.Info($"[DOWNLOADMODE:DATE]value:{filesdate.Value}");
                    files = ftp.DownloadFilesForDate(filesdate.Value);
                }
                else
                {
                    log.Info($"[DOWNLOADMODE:MOSTRECENTFILES]");
                    files = ftp.DownloadMostRecentFiles();
                }
                if ((files != null) && (files.Count() > 0))
                {
                    log.Warn($"[COUNTERS:DOWNLOADEDFILES] {files.Count()}");
                    IEnumerable<DateTime> dtlist = files.Select(x => x.FileNameDate).Distinct();
                    foreach (DateTime packagedate in dtlist)
                    {
                        IEnumerable<string> unzippedfiles = ftp.UnzipFiles(packagedate, files);
                        ftp.MarkFiles(packagedate, files, unzippedfiles);
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
            log.Debug("Main out");
        }
    }
}
