using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OAuth2;
using WearableActivityClassifier.Models;

namespace WearableActivityClassifier
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                var deviceInfoResponse = GetDeviceInfoAsync(log);
                var heartRateResponse = GetHRDataAsync(log);
                var stepResponse = GetStepDataAsync(log);
            }
            catch(Exception e)
            {
                log.LogError(e.Message);
            }
        }

        private static async Task<StepResponse> GetStepDataAsync(ILogger log)
        {

            var url = "https://api.fitbit.com/1/user/-/activities/steps/date/2019-11-26/1d/1min/time/14:15/14:30.json";
            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(url);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var stepResponse = JsonConvert.DeserializeObject<StepResponse>(resultString);
                return stepResponse;
            }
        }

        private static async Task<DeviceInfoResponse> GetDeviceInfoAsync(ILogger log)
        {
            var url = " https://api.fitbit.com/1/user/-/devices.json";
            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(url);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var deviceInfoResponse = JsonConvert.DeserializeObject<List<DeviceInfoResponse>>(resultString);
                var device = deviceInfoResponse[0];
                return device;
            }
        }

        private static async Task<HeartRateResponse> GetHRDataAsync(ILogger log)
        {

            var url = "https://api.fitbit.com/1/user/-/activities/heart/date/2019-09-02/1d/1sec/time/10:01/10:04.json";
            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(url);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var heartRateResponse = JsonConvert.DeserializeObject<HeartRateResponse>(resultString);
                return heartRateResponse;
            }
        }
    }
}
