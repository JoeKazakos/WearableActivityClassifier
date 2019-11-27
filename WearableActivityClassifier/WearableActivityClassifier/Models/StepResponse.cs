using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WearableActivityClassifier.Models
{
    [DataContract]
    class StepResponse
    {
        [DataMember(Name = "activities-steps-intraday")]
        public StepIntraday stepIntraday { get; set; }
    }

    class StepIntraday
    {
        public List<StepIntradayDatapoint> dataset { get; set; }
        public int datasetInterval { get; set; }
        public string datasetType { get; set; }
    }

    class StepIntradayDatapoint
    {
        public string time { get; set; }
        public int value { get; set; }
    }
}
