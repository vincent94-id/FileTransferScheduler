using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileTransferScheduler.Data
{
    public class HttpRequest : IHttpRequest
    {
        private readonly ILogger logger;
        private static readonly HttpClient client = new HttpClient();

        public HttpRequest(ILoggerFactory loggerFactory)
        {
            //this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<HttpRequest>();
        }
        public string post(string url, string data)
        {


            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {


                streamWriter.Write(data);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            string result;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        public async Task<(string, string)> postAsync(string url, string data)
        {

            var response = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();
            return (response.StatusCode.ToString(), result.ToString());
        }

        public async Task<(string, string)> getAsync(string url, int timeout)
        {
            HttpResponseMessage response = null;
            string result = null;
            client.Timeout = TimeSpan.FromSeconds(timeout);
            try
            {
                response = await client.GetAsync(url);
                result = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return (e.Message, JsonConvert.SerializeObject(new XFileReponse() { message = "Generate fail" }));
            }
            return (response.StatusCode.ToString(), result.ToString());
        }

        public void Dispose()
        {
            this.Dispose();
        }
    }
}
