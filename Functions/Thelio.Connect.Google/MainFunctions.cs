#region Assemblies Core
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#endregion

namespace Thelio.Connect.Google
{

    public static class MainFunctions
    {

        /// <summary>
        /// Get value of KeyVault storage
        /// </summary>
        /// <param name="secretName">name of the key value</param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static async Task<string> GetSecretValue(string secretName, TraceWriter log)
        {
            //https://keys-stockage.vault.azure.net/secrets
            var secretURL = String.Format("{0}/{1}",Env("keyVaultAddress"), secretName);
            try
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var secret = await keyVaultClient.GetSecretAsync(secretURL).ConfigureAwait(false);

                return await Task.FromResult(secret.Value);
            }
            catch (Exception e)
            {
                log.Error(e.Message + "---" + e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Read a value from the Function environment 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string Env(string name) => Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);


        /// <summary>
        /// Public function name. to get the Google Analytics values 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Analytics")]
        public static async Task<HttpResponseMessage> GetAnalytics([HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequest req, TraceWriter log)
        {
            var AccountGA = GetSecretValue("AccountGA", log).Result;
            var privateKeyGA = GetSecretValue("privateKeyGA", log).Result;
            var projectId = Env("project_id");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string argMetrics = data?.metrics;
            string response =  Analytics.Start(AccountGA, privateKeyGA, projectId, argMetrics, log);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }

        /// <summary>
        /// Public function name. to get the Google Reporting values 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Reporting")]
        public static async Task<IActionResult> GetReporting([HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequest req, TraceWriter log)
        {
            try
            {
                var AccountGA = GetSecretValue("AccountGA", log).Result;
                var privateKeyGA = GetSecretValue("privateKeyGA", log).Result;
                string requestBody = new StreamReader(req.Body).ReadToEnd();

                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string projectId = data?.projectId;
                string viewsId = data?.viewId;
                string startDate = data?.startDate;
                string endDate = data?.endDate;
                string dimension = data?.dimension;
                string argMetrics = data?.metrics;
                string response = string.Empty;

                response =  ReportingV4.Start(projectId, viewsId, privateKeyGA, startDate, endDate, argMetrics, dimension, log);

                return (ActionResult)new OkObjectResult(response);

            }
            catch (Exception exp)
            {
                log.Error("Error with Reporting", exp);
                return (ActionResult)new OkObjectResult("Erreur" + exp.Message);
            }
        }
    }
}