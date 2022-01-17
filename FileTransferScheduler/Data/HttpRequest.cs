using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    class HttpRequest
    {
        private static readonly HttpClient client = new HttpClient();

        public HttpRequest()
        {

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

        public async Task<(string,string)> postAsync(string url, string data)
        {

            var response = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/json")) ;
            var result = await response.Content.ReadAsStringAsync();
            return (response.StatusCode.ToString(),result.ToString());
        }
        
        public async Task<(string,string)> getAsync(string url)
        {
            var response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            return (response.StatusCode.ToString(), result.ToString());
        }
    }
}
