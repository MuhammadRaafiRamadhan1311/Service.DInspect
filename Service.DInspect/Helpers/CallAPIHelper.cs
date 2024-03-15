using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.DInspect.Helpers
{
    public class CallAPIHelper
    {
        protected HttpClient _httpClient;

        public CallAPIHelper(string accessToken)
        {
            _httpClient = SetHttpClient(accessToken);
        }

        public HttpClient SetHttpClient(string accessToken)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", accessToken);
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            return client;
        }

        public async Task<ApiResponse> Get(string url)
        {
            var res = await _httpClient.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(json);

            if (result.StatusCode != 200 && result.StatusCode != 201)
            {
                JObject jObject = JObject.Parse(json);
                string errMsg = jObject["message"] != null ? jObject["message"]?.ToString() : result?.Result?.Message;
                throw new Exception(errMsg);
            }

            return result;
        }

        public async Task<ApiResponse> Post(string url, object content)
        {
            var jsonObj = JsonConvert.SerializeObject(content);
            var jsonContent = new StringContent(jsonObj, Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync(url, jsonContent);

            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(json);

            if (result.StatusCode != 200 && result.StatusCode != 201)
            {
                JObject jObject = JObject.Parse(json);
                string errMsg = jObject["message"] != null ? jObject["message"]?.ToString() : result?.Result?.Message;
                throw new Exception(errMsg);
            }

            return result;
        }

        public async Task<ApiResponse> Put(string url, object content)
        {
            var jsonObj = JsonConvert.SerializeObject(content);
            var jsonContent = new StringContent(jsonObj, Encoding.UTF8, "application/json");
            var res = await _httpClient.PutAsync(url, jsonContent);

            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(json);

            if (result.StatusCode != 200 && result.StatusCode != 201)
            {
                JObject jObject = JObject.Parse(json);
                string errMsg = jObject["message"] != null ? jObject["message"]?.ToString() : result?.Result?.Message;
                //throw new Exception(errMsg);

                result.Result.IsError = true;
                result.Result.Content = errMsg;
            }

            return result;
        }
    }
}
