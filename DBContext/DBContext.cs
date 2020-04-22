using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using log4net;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Configuration;
using System.Reflection;
using Blogica.Directory.ConnectionStringManager;
using Blogica.Common.DB;
using Blogica.Common.DB.Dapper;

namespace Blogica.Interfaces.DB
{
    public class Package : DapperBaseClass
    {
        public int ID { get; set; }

        [DBAttributeMaxLen(50)]
        public string Process { get; set; }

        [DBAttributeMaxLen(10)]
        public string ExternalID { get; set; }

        [DBAttributeMaxLen(250)]
        public string PackageName { get; set; }

        [DBAttributes(SqlDbType.Date)]
        public DateTime PackageDate { get; set; }

        [DBAttributeMaxLen(100)]
        public string PackageState { get; set; }

        public List<PackageFile> PackageFiles = new List<PackageFile>();
    }

    public class PackageFile : DapperBaseClass
    {
        public int ID { get; set; }
        public int PackageID { get; set; }

        [DBAttributeMaxLen(250)]
        public string FileName { get; set; }

        [DBAttributeMaxLen(250)]
        public string ExternalFileName { get; set; }

        [DBAttributeMaxLen(250)]
        public string LocalFileName { get; set; }

        [DBAttributeMaxLen(100)]
        public string FileState { get; set; }
    }
    public class DBContext
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<Package> GetPackages(string externalId)
        {
            List<Package> plist = null;
            string connectionString = MultitenantConnectionStringManager.Instance.GetConnectionStringForExternalTenantID(externalId);
            log.Debug($"externalID:{externalId}, connectionString:{connectionString}");                
            string sql = "[PandekthsInterfaces].[GetPackageData]";
            var dynparams = new DynamicParameters(new
            {
                ExternalID = externalId
            });
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    var reader=con.QueryMultiple(sql, dynparams, commandType: CommandType.StoredProcedure);
                    plist = reader.Read<Package>().ToList();
                    var packagefiles = reader.Read<PackageFile>().ToList();
                    if (packagefiles != null)
                    {
                        foreach (Package p in plist)
                        {
                            p.PackageFiles.AddRange(packagefiles.Where(x => x.PackageID == p.ID));
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
            return plist;
        }
        public void NewPackage(Package package)
        {
            log.Debug("NewPackage in");
            if (package == null) return;
            string connectionString = MultitenantConnectionStringManager.Instance.GetConnectionStringForExternalTenantID(package.ExternalID);
            log.Debug($"externalID:{package.ExternalID}, connectionString:{connectionString}");
            string sql = @"[PandekthsInterfaces].CreatePackage";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                List<SqlDataRecord> packagerec = new List<SqlDataRecord>();
                packagerec.Add(package.GetSQLRecordWithValues());
                List<SqlDataRecord> packagefilesrec = new List<SqlDataRecord>();
                foreach (PackageFile f in package.PackageFiles)
                {
                    packagefilesrec.Add(f.GetSQLRecordWithValues());
                }
                var dynparams = new DynamicParameters(new
                {
                    PackageInfo = packagerec.AsTableValuedParameter("[PandekthsInterfaces].[Package]"),
                    PackageFiles = packagefilesrec.AsTableValuedParameter("[PandekthsInterfaces].[ProcessFiles]")
                });
                try
                {
                    con.Execute(sql, dynparams, commandType: CommandType.StoredProcedure);
                }
                catch (Exception e)
                {
                    log.Error($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                    if (e.InnerException != null)
                    {
                        log.Error($"{e.InnerException.Message}{Environment.NewLine}{e.InnerException.StackTrace}");
                    }
                    throw;
                }
            }
            log.Debug("NewPackage out");
        }

        public bool PackageAlreadyDownloadedForDate(string externalID, DateTime date)
        {
            log.Debug("PackageAlreadyDownloadedForDate in");
            bool res = false;
            string connectionString = MultitenantConnectionStringManager.Instance.GetConnectionStringForExternalTenantID(externalID);
            log.Debug($"externalID:{externalID}, connectionString:{connectionString}");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    var dynparams = new DynamicParameters(new
                    {
                        Date = date
                    });
                    res = con.ExecuteScalar<bool>("select [PandekthsInterfaces].[PackageDownloadedForDate](@Date)", dynparams, commandType: CommandType.Text);
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
            log.Debug("PackageAlreadyDownloadedForDate out");
            return res;
        }
    }
}
