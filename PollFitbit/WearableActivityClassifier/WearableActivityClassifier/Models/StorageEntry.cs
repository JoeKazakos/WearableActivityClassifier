using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace WearableActivityClassifier.Models
{
    class StorageEntry : TableEntity
    {
        public StorageEntry(int heartRate, int steps, DateTime time)
        {
            HeartRate = heartRate;
            Steps = steps;
            Time = time.ToString("MM/dd/yyyy HH:mm:ss");
            InsertTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            PartitionKey = "WearableActivity_V1";//time.ToString("yyyyMMdd");
            RowKey = (DateTime.MaxValue.Ticks - time.Ticks).ToString("d19");
        }

        public int HeartRate { get; set; }
        public int Steps { get; set; }
        public string Time { get; set; }
        public string InsertTime { get; set; }
    }
}
