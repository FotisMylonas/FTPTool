using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using Blogica.Interfaces;
using Blogica.Interfaces.DB;

namespace DownloadInfo
{
    public static class DownloadInfo
    {
        [FunctionName("DownloadInfo")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "blogica/ftpdownloadinfo/{externalid}")]HttpRequestMessage req, string externalid, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            List<Package> plist=new List<Package>();
            if (!string.IsNullOrWhiteSpace(externalid))
            {                
                DBContext db = new DBContext();
                plist = db.GetPackages(externalid);
            }
            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse<IEnumerable<Package>>(HttpStatusCode.OK, plist);
        }
    }
}
