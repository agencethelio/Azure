#region Assemblies GoogleAnalytics
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
#endregion
#region Assemblies Core
using System;
using System.Collections.Generic;
#endregion

using Microsoft.Azure.WebJobs.Host;

//https://blogs.msdn.microsoft.com/cloud_solution_architect/2017/12/22/using-managed-service-identities-in-functions-to-access-key-vault/

namespace Thelio.Connect.Google
{
    public static class Analytics
    {
        public static string Start(string AccountGA, string privateKeyGA,string projectId, string metrics, TraceWriter log)
        {
            string t = string.Empty;
            string storage = string.Empty;
    
            try
            {
                string[] scopes = new string[] { AnalyticsService.Scope.Analytics };
                var credential_google = GoogleCredential.FromJson(privateKeyGA).CreateScoped(scopes);
                var service = new AnalyticsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential_google,
                    ApplicationName = projectId,
                });

                var request = service.Data.Ga.Get(projectId, "30daysAgo", "yesterday", metrics);
                request.MaxResults = 1000;
                var result = request.Execute();
                foreach (var headers in result.ColumnHeaders)
                {
                    t += String.Format("{0} - {1} - {2}", headers.Name, headers.ColumnType, headers.DataType);
                    log.Info(String.Format("{0} - {1} - {2}", headers.Name, headers.ColumnType, headers.DataType));
                }

                foreach (List<string> row in result.Rows)
                {
                    foreach (string col in row)
                    {
                        t += col + " ";
                        log.Info(col + " ");
                    }
                    t += "\r\n";
                    log.Info(t);

                }

            }
            catch (Exception e)
            {
                log.Error("erreur  Stack : " + e.StackTrace + "___" + e.Message);
                t = t + "..." + e.Message;
            }

            return t;
        }
    }
}