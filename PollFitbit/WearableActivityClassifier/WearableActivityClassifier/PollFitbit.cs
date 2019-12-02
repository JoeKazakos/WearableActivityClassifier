using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WearableActivityClassifier.Models;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WearableActivityClassifier
{
    public static class PollFitbit
    {
        public static CloudTable cloudTable;

        [FunctionName("PollFitbit")]
        public static async void RunAsync([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                var cloudTable = GetStorageTableConnection(log,"WearableActivityTable");
                
                var deviceSyncTime = await GetDeviceSyncTime(log);

                var cloudTableQuery = new TableQuery().Take(1);
                var mostRecentEntry = cloudTable.ExecuteQuery(cloudTableQuery).FirstOrDefault();
                var mostRecentEntryTime = new DateTime(DateTime.MaxValue.Ticks - Convert.ToInt64(mostRecentEntry.RowKey));
                var mostRecentEntryInsertTime = DateTime.Parse(mostRecentEntry.Properties.Where(d => d.Key=="InsertTime").FirstOrDefault().Value.ToString());

                // in some cases we want to exit early so we don't strain the API
                var ticksInMinute = 10000000 * 60;
                var minutesSinceInsertedEntry = (DateTime.Now.Ticks - mostRecentEntryInsertTime.Ticks) / ticksInMinute;
                var minutesSinceLastEntry = (DateTime.Now.Ticks - mostRecentEntryTime.Ticks) / ticksInMinute;
                var minutesSinceSync = (DateTime.Now.Ticks - deviceSyncTime.Ticks) / ticksInMinute;
                var minutesEntryBehindSync = minutesSinceLastEntry - minutesSinceSync;

                log.LogInformation("\tminutesSinceInsertedEntry:" + minutesSinceInsertedEntry);
                log.LogInformation("\tminutesSinceLastEntry:" + minutesSinceLastEntry);
                log.LogInformation("\tminutesSinceSync:" + minutesSinceSync);
                log.LogInformation("\tminutesEntryBehindSync:" + minutesEntryBehindSync);


                var completeDataUpdate = true;

                // If we are behind by more than an hour AND we just wrote an entry, then exit.
                // This will be hit if we are catching up. We get 6 hours of data at a time, so
                // we can take small breaks.
                if (minutesEntryBehindSync > 60 && minutesSinceInsertedEntry < 2)
                    completeDataUpdate = false;

                // if entries is caught up to the sync time, then we don't
                // have any data to update
                if (minutesEntryBehindSync == 0)
                    completeDataUpdate = false;

                log.LogInformation("\tcompleteDataUpdate:" + completeDataUpdate);
                if (completeDataUpdate)
                {

                    DateTime startTime = mostRecentEntryTime.AddMinutes(1);
                    DateTime endTime = mostRecentEntryTime.AddMinutes(60 * 12);

                    if (endTime > deviceSyncTime)
                        endTime = deviceSyncTime;

                    if (startTime.Date != endTime.Date) // rolling over midnight
                        endTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, 23, 59, 00);

                    log.LogInformation("\tstartTime:" + startTime.ToString("MM/dd/yyyy HH:mm:ss"));
                    log.LogInformation("\tendTimeTime:" + endTime.ToString("MM/dd/yyyy HH:mm:ss"));

                    var heartRateResponse = GetHRDataAsync(startTime, endTime, log).Result;
                    var stepResponse = GetStepDataAsync(startTime, endTime, log).Result;

                    var heartRateData = heartRateResponse.heartRateIntraday.dataset;
                    var stepData = stepResponse.stepIntraday.dataset;

                    List<String> allTimes = new List<String>();
                    var heartRateTimes = heartRateData.Select(d => d.time).ToList();
                    var stepTimes = stepData.Select(d => d.time).ToList();
                    allTimes.AddRange(heartRateTimes);

                    // in the case that the fitbit doesn't collect data for several hours
                    // we won't ever make a new entry. We need log an entry with no data, 
                    // so that we can move on
                    if (minutesEntryBehindSync > 120)
                    {
                        DateTime fillerTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                            endTime.Hour, endTime.Minute, 0);
                        allTimes.Add(fillerTime.ToString("yyyy-MM-dd HH:mm:ss"));

                    }

                    allTimes.Distinct();


                    foreach (String timeDatapoint in allTimes)
                    {
                        var heartRateDatapoint = heartRateData.Where(d => d.time == timeDatapoint).FirstOrDefault();
                        var stepDatapoint = stepData.Where(d => d.time == timeDatapoint).FirstOrDefault();

                        var heartRate = heartRateDatapoint != null ? heartRateDatapoint.value : 0;
                        var steps = stepDatapoint != null ? stepDatapoint.value : 0;
                        DateTime dt = DateTime.Parse(timeDatapoint);
                        StorageEntry storageEntry = new StorageEntry(heartRate, steps, dt);

                        var tableOperation = TableOperation.InsertOrReplace(storageEntry);
                        cloudTable.Execute(tableOperation);
                    }
                }
            }
            catch(Exception e)
            {
                log.LogError(e.Message);
            }
        }

        public static CloudTable GetStorageTableConnection(ILogger log, String tableName)
        {
            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString", EnvironmentVariableTarget.Process);

            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            var cloudStorageClient = cloudStorageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var cloudTableReference = cloudStorageClient.GetTableReference(tableName);
            return cloudTableReference;
        }
 
        private static async Task<DateTime> GetDeviceSyncTime(ILogger log)
        {
            var deviceInfoResponse = await GetDeviceInfoAsync(log);
            var userDeviceMac = Environment.GetEnvironmentVariable("DEVICE_MAC", EnvironmentVariableTarget.Process);
            var device = deviceInfoResponse.Where(x => x.mac == userDeviceMac).FirstOrDefault();
            if (device == null)
                throw new Exception("Can't find device");
            else
                return device.lastSyncTime;
        }

        private static async Task<StepResponse> GetStepDataAsync(DateTime startTime, DateTime endTime, ILogger log)
        {
            string dayString = startTime.ToString("yyyy-MM-dd");
            string startTimeString = startTime.ToString("HH:mm");
            string endTimeString = endTime.ToString("HH:mm");
            var url = "https://api.fitbit.com/1/user/-/activities/steps/date/{0}/1d/1min/time/{1}/{2}.json";
            var parameterUrl = String.Format(url, dayString, startTimeString, endTimeString);
            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(parameterUrl);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var stepResponse = JsonConvert.DeserializeObject<StepResponse>(resultString);

                foreach(var datapoint in stepResponse.stepIntraday.dataset)
                {
                    string dayTimeString = dayString + " " + datapoint.time;
                    datapoint.time = DateTime.Parse(dayTimeString).ToString("yyyy-MM-dd HH:mm:ss");
                }

                return stepResponse;
            }
        }

        private static async Task<List<DeviceInfoResponse>> GetDeviceInfoAsync(ILogger log)
        {
            var url = " https://api.fitbit.com/1/user/-/devices.json";
            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(url);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var deviceInfoResponse = JsonConvert.DeserializeObject<List<DeviceInfoResponse>>(resultString);
                return deviceInfoResponse;
            }
        }

        private static async Task<HeartRateResponse> GetHRDataAsync(DateTime startTime, DateTime endTime, ILogger log)
        {
            string dayString = startTime.ToString("yyyy-MM-dd");
            string startTimeString = startTime.ToString("HH:mm");
            string endTimeString = endTime.ToString("HH:mm");

            //var url = "https://api.fitbit.com/1/user/-/activities/heart/date/2019-09-02/1d/1sec/time/10:01/10:04.json";
            var url = "https://api.fitbit.com/1/user/-/activities/heart/date/{0}/1d/1min/time/{1}/{2}.json";

            var parameterUrl = String.Format(url, dayString, startTimeString, endTimeString);

            var fitbitOauthToken = Environment.GetEnvironmentVariable("FITBIT_OAUTH", EnvironmentVariableTarget.Process);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fitbitOauthToken);
                var response = client.GetAsync(parameterUrl);

                var resultString = await response.Result.Content.ReadAsStringAsync();
                var heartRateResponse = JsonConvert.DeserializeObject<HeartRateResponse>(resultString);

                foreach (var datapoint in heartRateResponse.heartRateIntraday.dataset)
                {
                    string dayTimeString = dayString + " " + datapoint.time;
                    datapoint.time = DateTime.Parse(dayTimeString).ToString("yyyy-MM-dd HH:mm:ss");
                }

                return heartRateResponse;
            }
        }

        [FunctionName("GetLoggedActivityData")]
        public static async Task<IActionResult> GetLoggedActivityData(
        [HttpTrigger("post", Route = null)] HttpRequest req,
        ILogger log)
        {
            try
            {
                var cloudTable = GetStorageTableConnection(log, "LoggedActivity");
                var dataTable = GetStorageTableConnection(log, "WearableActivityTable");

                List<dynamic> returnData = new List<dynamic>();

                var cloudTableQuery = new TableQuery();
                var entries = cloudTable.ExecuteQuery(cloudTableQuery).ToList();

                foreach (var entry in entries)
                {
                    var p = entry.Properties;
                    var activityType = p["ActivityType"].StringValue;
                    var startTime = p["StartTime"].StringValue;
                    var endTime = p["EndTime"].StringValue;

                    var startTimeDt = DateTime.Parse(startTime);
                    var endTimeDt = DateTime.Parse(endTime);

                    var startRowKey = (DateTime.MaxValue.Ticks - startTimeDt.Ticks).ToString("d19");
                    var endRowKey = (DateTime.MaxValue.Ticks - endTimeDt.Ticks).ToString("d19");

                    string afterStartFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, startRowKey);
                    string beforeEndFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, endRowKey);

                    string combinedFilter = TableQuery.CombineFilters(afterStartFilter, TableOperators.And, beforeEndFilter);

                    var dataQuery = new TableQuery().Where(combinedFilter);

                    var activityData = dataTable.ExecuteQuery(dataQuery).ToList();

                    List<dynamic> listOfActivityData = new List<dynamic>();
                    foreach (var activityDatapoint in activityData)
                    {
                        var dpProperties = activityDatapoint.Properties;
                        var time = dpProperties["Time"].StringValue;
                        var heartRate = dpProperties["HeartRate"].Int32Value;
                        var steps = dpProperties["Steps"].Int32Value;

                        var returnDatapoint = new
                        {
                            activityType = activityType,
                            time = time,
                            heartRate = heartRate,
                            steps = steps
                        };
                        listOfActivityData.Add(returnDatapoint);
                    }

                    returnData.AddRange(listOfActivityData);
                }

                return new OkObjectResult(returnData);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("BROKEN!!");
            }

        }
    }
}
