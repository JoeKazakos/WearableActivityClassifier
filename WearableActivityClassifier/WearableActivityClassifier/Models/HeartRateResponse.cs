using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WearableActivityClassifier.Models
{
    [DataContract]
    class HeartRateResponse
    {
        [DataMember(Name = "activities-heart-intraday")]
        public HeartRateIntraday heartRateIntraday { get; set;  }
    }

    class HeartRateIntraday
    {
        public List<HeartRateIntradayDatapoint> dataset { get; set; }
        public int datasetInterval { get; set; }
        public string datasetType { get; set; }
    }
}
