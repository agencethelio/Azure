#region Assemblies Core
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
#endregion

#region Assemblies Azure
using Microsoft.Azure.WebJobs.Host;
#endregion

#region Assemblies GoogleAnalytics
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
#endregion


namespace Thelio.Connect.Google
{

    /// <summary>
    /// Class to user Google Reporting more details: https://developers.google.com/analytics/devguides/reporting/core/v3/reference
    /// </summary>
    class ReportingV4
    {
        #region constants
        const char sep = ',';
        #endregion

        #region variables
        static List<Metric> _metrics;
        static List<Dimension> _dimensions;
        static string _jsonCertificat = string.Empty;
        static string _projectId = string.Empty;
        #endregion

        #region Private methods
        private static void SetMetric(string metrics)
        {
            _metrics = new List<Metric>();
            foreach (var item in metrics.Split(sep))
            {
                _metrics.Add(new Metric { Expression = item, Alias = item });
            }
        }

        private static void SetDimensions(string dimensions)
        {

            _dimensions = new List<Dimension>();
            foreach (var item in dimensions.Split(sep))
            {
                _dimensions.Add(new Dimension { Name = item });
            }

        }

        private static Report GetReport(string sViewId, string startDate, string endDate)
        {
            string[] scopes = new string[] { AnalyticsReportingService.Scope.Analytics };

            var credential_google = GoogleCredential.FromJson(_jsonCertificat).CreateScoped(scopes);

            AnalyticsReportingService analyticsreporting = new AnalyticsReportingService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential_google,
                ApplicationName = _projectId,
            });

            DateRange dateRange = new DateRange() { StartDate = startDate, EndDate = endDate };

            ReportRequest reportRequest = new ReportRequest
            {
                ViewId = sViewId,
                DateRanges = new List<DateRange>() { dateRange },
                Dimensions = _dimensions,
                Metrics = _metrics
            };

            List<ReportRequest> requests = new List<ReportRequest>
            {
                reportRequest
            };

            // Create the GetReportsRequest object.
            GetReportsRequest getReport = new GetReportsRequest() { ReportRequests = requests };

            // Call the batchGet method.
            GetReportsResponse response = analyticsreporting.Reports.BatchGet(getReport).Execute();

            return response.Reports[0];
        }

        private static string PrintReports(IList<Report> reports)
        {
            Stream stream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(stream);
            List<string> globalRow = new List<string>();

            foreach (Report report in reports)
            {
                string sdimensionHeaders = string.Empty;
                string smetricHeaders = string.Empty;

                ColumnHeader header = report.ColumnHeader;
                List<string> dimensionHeaders = (List<string>)header.Dimensions;

                List<MetricHeaderEntry> metricHeaders = (List<MetricHeaderEntry>)header.MetricHeader.MetricHeaderEntries;
                List<ReportRow> rows = (List<ReportRow>)report.Data.Rows;
                List<string> myrows = new List<string>();

                if (rows != null && rows.Count > 0)
                {
                    for (int k = 0; k < metricHeaders.Count(); k++)
                    {
                        smetricHeaders += metricHeaders[k].Name + sep;
                    }

                    for (int i = 0; i < dimensionHeaders.Count(); i++)
                    {
                        sdimensionHeaders += dimensionHeaders[i] + sep;
                    }

                    if (globalRow.Count() == 0)
                        myrows.Add(sdimensionHeaders + smetricHeaders);
                    else
                        myrows.Add(smetricHeaders);

                    foreach (ReportRow row in rows)
                    {
                        List<string> dimensions = (List<string>)row.Dimensions;
                        List<DateRangeValues> metrics = (List<DateRangeValues>)row.Metrics;

                        string lined = string.Empty;
                        string liner = string.Empty;

                        for (int i = 0; i < dimensionHeaders.Count() && i < dimensions.Count(); i++)
                        {
                            lined += dimensions[i] + sep;
                        }

                        for (int j = 0; j < metrics.Count(); j++)
                        {
                            DateRangeValues values = metrics[j];
                            int headers = metricHeaders.Count();
                            for (int k = 0; k < values.Values.Count() && k < headers; k++)
                            {
                                liner += values.Values[k];
                                if (k != headers - 1)
                                    liner += sep;
                            }
                        }
                        if (globalRow.Count() == 0)
                            myrows.Add(lined + liner);
                        else
                            myrows.Add(liner);
                    }
                }
                if (globalRow.Count() == 0)
                    globalRow.AddRange(myrows);
                else
                {
                    for (int i = 0; i < myrows.Count(); i++)
                    {
                        if (globalRow[i].EndsWith(sep.ToString()))
                            globalRow[i] += sep + myrows[i];
                        else
                            globalRow[i] += myrows[i];
                    }
                }
            }
            StringWriter stringWriter = new StringWriter();
            string response = string.Empty;

            foreach (string item in globalRow)
            {
                stringWriter.WriteLine(item);
            }
            return stringWriter.ToString();
        }
        #endregion

        public static string Start(string projectId, string viewID, string jsCert, string startDate, string endDate, string argMetrics, string dimensions,TraceWriter log)
        {

            try
            {
                List<string> strTemp = new List<string>();
                List<string> lmetrics = new List<string>();

                var aMetrics = argMetrics.Split(sep);

                int nbItems = aMetrics.Count(); // nb metrics
                int nbCalls = nbItems / 10;     // APIV4 limit to 10 metrics

                for (int j = 0; j < nbCalls; j++)
                {
                    string ligne = string.Empty;
                    int flag = j * 10;

                    for (int i = 0 + flag; i < 10 + flag; i++)
                    {
                        ligne += aMetrics.GetValue(i) + sep.ToString();
                    }
                    lmetrics.Add(ligne.Remove(ligne.Length - 1));
                }

                List<Report> reports = new List<Report>();
                DateTime t1 = System.DateTime.Now;

                _jsonCertificat = jsCert;
                _projectId = projectId;
                SetDimensions(dimensions);

                foreach (var metrics in lmetrics)
                {
                    SetMetric(metrics);

                    Report report = GetReport(viewID, startDate, endDate);
                    reports.Add(report);
                }
                return PrintReports(reports);
            }
            catch (Exception exp)
            {
                log.Error("Start Reporting Error ", exp);
                throw;
            }
        }


    }
}
