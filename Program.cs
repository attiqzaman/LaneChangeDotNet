using System;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LaneChangeDotNet
{
    class Program
    {
        static async Task Main(string[] args)
        {
            FirestoreDb db = FirestoreDb.Create("lanechangewarning-dev");

            foreach (var userId in GetAllUsers(db))
            {
                foreach (var route in await GetAllRoutes(db, userId))
                {
                    var routeDate = DateTime.Parse(route.Id.Substring(0, 10));
                    if (routeDate <= DateTime.Parse("2023-04-10"))
                    {
                        continue;
                    }

                    var snapshots = await GetAllSnapshotsInTheRoute(db, userId, route.Id);
                    var json = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true });
                    var dateTimeAsString = snapshots.First().TimeStamp.ToString("MM_dd__hh_mm");
                    var fileName = $"{userId}_Snapshots_{dateTimeAsString}.json";
                    File.WriteAllText(fileName, json);
                }
            }
        }

        private static List<string> GetAllUsers(FirestoreDb db)
        {
            return new List<string> { "ZSb8lol4EBMaSeET5zVN7z6kMRg2" };
        }

        private static async Task<List<DocumentReference>> GetAllRoutes(FirestoreDb db, string userId)
        {
            return await (db.Collection($"Users/{userId}/Routes")).ListDocumentsAsync().ToListAsync();
        }

        private static async Task<List<Snapshot>> GetAllSnapshotsInTheRoute(FirestoreDb db, string userId, string routeId)
        {
            var querySnapshot = await (db.Collection($"Users/{userId}/Routes/{routeId}/segments")).GetSnapshotAsync();
            var completeRoute = new List<Snapshot>();
            foreach (var documentSnapshot in querySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    var doc = documentSnapshot.ConvertTo<Segment>();
                    completeRoute.AddRange(doc.Data.Values.ToList());
                }
            }

            return completeRoute.OrderBy(x => x.Count).ToList();
        }
    }
}
