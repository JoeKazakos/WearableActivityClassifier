using System;
using System.Collections.Generic;
using System.Text;

namespace WearableActivityClassifier.Models
{
    class DeviceInfoResponse
    {
        public string mac { get; set; }
        public DateTime lastSyncTime { get; set; }
    }
}
