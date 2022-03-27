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
        [FirestoreProperty("googleLatitude")]
        public double GoogleLatitude { get; set; }
        [FirestoreProperty("googleLongitude")]
        public double GoogleLongitude { get; set; }
        [FirestoreProperty("latitude")]
        public double Latitude { get; set; }
        [FirestoreProperty("longitude")]
        public double Longitude { get; set; }
        [FirestoreProperty("heading")]
        public int SnapshotNumber { get; set; }
        [FirestoreProperty("isManipulated")]
        public bool IsManipulated { get; set; }
        [FirestoreProperty("timeStamp")]
        public string TimeStampAsString { get; set; }
        public DateTime TimeStamp { 
            get
            {
                return DateTime.Parse(TimeStampAsString);
            }
        }
        [FirestoreProperty("accuracy")]
        public float Accuracy { get; set; }
/*        [FirestoreProperty("heading")]
        public float? Heading { get; set; }*/
    }
}
