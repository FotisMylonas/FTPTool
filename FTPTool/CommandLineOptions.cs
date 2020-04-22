using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace FTPTool.CommandLineUtils
{
    public class CommandLineOptions
    {
        [Option('l', "localfolder", Required = true,
          HelpText = "local folder")]
        public string LocalFolder { get; set; }

        [Option('r', "remoteftpfolder", Required = true,
          HelpText = "ftp folder")]
        public string RemoteFTPFolder { get; set; }

        [Option('u', "username", Required = true,
          HelpText = "username")]
        public string UserName { get; set; }

        [Option('w', "password", Required = true,
          HelpText = "Password")]
        public string Password { get; set; }

        [Option('e', "removefiles", Required = true,
            HelpText = "RemoveFiles")]
        public bool RemoveFiles { get; set; }

        [Option('a', "action", Required = true,
            HelpText = "Action")]
        public string Action { get; set; }

        [Option('i', "userid", Required = true,
            HelpText = "external user ID")]
        public string UserID { get; set; }

        [Option('f', "unzipfolder", Required = true,
            HelpText = "Folder to unzip downloaded files to")]
        public string UnzipFolder { get; set; }

        [Option('p', "process", Required = true,
            HelpText = "Process")]
        public string Process { get; set; }

        [Option('d', "DownloadDate", Required = true,
            HelpText = "Download files for date")]
        public string DownloadDate { get; set; }

    }
}
