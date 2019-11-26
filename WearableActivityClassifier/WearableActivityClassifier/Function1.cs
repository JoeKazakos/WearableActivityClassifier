using System;
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

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var heartRateResponse = GetHRDDataAsync(log);
        }

        private static async Task<HeartRateResponse> GetHRDDataAsync(ILogger log)
        {
            try
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
            catch(Exception e)
            {
                log.LogError(e.Message);
                return null;
            }
        }

    }
}
