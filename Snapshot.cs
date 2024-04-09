using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace LaneChangeDotNet
{
    [FirestoreData]
    public class Segment
    {
        [FirestoreProperty("data")]
        public Dictionary<string, Snapshot> Data { get; set; }
    }

    [FirestoreData]
    public class Snapshot
    {
        public Snapshot()
        {
            // needed for Firestore sdk conversion.
        }

        [FirestoreProperty("googleLatitude")]
        public double GoogleLatitude { get; set; }
        [FirestoreProperty("googleLongitude")]
        public double GoogleLongitude { get; set; }
        [FirestoreProperty("latitude")]
        public double Latitude { get; set; }
        [FirestoreProperty("longitude")]
        public double Longitude { get; set; }
        [FirestoreProperty("isManipulated")]
        public bool IsManipulated { get; set; }
        [FirestoreProperty("timeStamp")]
        public string TimeStampAsString { get; set; }
        public DateTime TimeStamp
        {
            get
            {
                if (TimeStampAsString == null)
                {
                    return DateTime.Now;
                }
                return DateTime.Parse(TimeStampAsString);
            }
        }
        [FirestoreProperty("accuracy")]
        public float Accuracy { get; set; }
        [FirestoreProperty("heading")]
        public int Count { get; set; }
    }
}
