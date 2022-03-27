using System;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LaneChangeDotNet
{
	class Program
	{
		private static FirestoreDb _db;

		static async Task Main(string[] args)
		{
			if (_db == null)
			{
				// If you don't specify credentials when constructing the client, the
				// client library will look for credentials in the environment.
				// var credential = GoogleCredential.FromFile(@"C:\GCP\lanechangewarning-dev.json");
				_db = FirestoreDb.Create("lanechangewarning-dev");
			}

			await ProcessAndPrintRouteAsync();
		}

		private static async Task ProcessAndPrintRouteAsync()
        {
			var userId = "yvCAvg8O6fc1GT8cBPPlEGPYNIx1";
			var routeId = "2022-03-13 – 12:05";
			var snapshots = await GetAllSnapshotsInTheRoute(_db, userId, routeId);
			// var realCoords = snapshots.Where(x => x.IsManipulated == true);
			snapshots = snapshots.OrderBy(x => x.TimeStamp).ToList();
			PrintRouteAsJson(snapshots, userId);
		}

		private static async Task ProcessAllUsersAndRoutesAsync()
        {
			var allProcessedRoutes = new List<ProcessedRouteWrapper>();
			foreach (var userId in GetAllUsers(_db))
			{
				foreach (var route in await GetAllRoutes(_db, userId))
				{
					if (DateTime.Parse(route.Id.Substring(0, 10)) <= DateTime.Parse("2021-09-05"))
					{
						continue;
					}

					var snapshots = await GetAllSnapshotsInTheRoute(_db, userId, route.Id);
					if (snapshots.Count > 2)
					{
						allProcessedRoutes.Add(new ProcessedRouteWrapper(userId, route.Id, snapshots));
						/*						ProcessRoute(snapshots, true, userId);
												ProcessRoute(snapshots, false, userId);*/
					}
				}
			}

			foreach (var processedRoute in allProcessedRoutes)
			{
				PrintRoute(processedRoute);
			}
		}

		private static List<string> GetAllUsers(FirestoreDb db)
		{
			//return await (db.Collection("Users")).ListDocumentsAsync().ToListAsync();
			return new List<string> { "sJTO2ShsBFas7SJs14dLFVCtdHG2", "KLfW91CBWUPp9weLZs9TVk9EHQA2" };
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

			return completeRoute;
		}

		private static void PrintRoute(ProcessedRouteWrapper route)
        {
			var fileName = $"{route.UserId}_notGoogle_{route.SortedSnapshots.First().TimeStamp.ToString("MM_dd__hh_mm")}.csv";

			using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"C:\GCP\processed files", fileName)))
			{
				outputFile.WriteLine($"Lat, long, accuracy, distance, heading, accumulated distance, avg heading_9, diff heading_9");

				for (int i = 1; i < route.OutHeadingsNotGoogle.Count; i++)
				{
					var diffHeading = i <= 5 || i >= route.OutHeadingsNotGoogle.Count - 6 ? 0 : route.DifferentialHeadingsNotGoogle[i];
					/*						var diffHeading5 = i <= 3 || i >= outHeadings.Count - 4 ? 0 : differentialHeading5[i];
											var diffHeading3 = i <= 2 || i >= outHeadings.Count - 3 ? 0 : differentialHeading3[i];*/

					outputFile.WriteLine($"{route.LatitudesNotGoogle[i]}, {route.LongitudesNotGoogle[i]}," +
						$" {route.AccuraciesNotGoogle[i]}, {route.DistancesNotGoogle[i]}, {route.OutHeadingsNotGoogle[i]}," +
						$" {route.AccumulativeDistancesNotGoogle[i]}, {route.AverageHeadingsNotGoogle[i]}, {diffHeading}");
				}
			}

			fileName = $"{route.UserId}_Google_{route.SortedSnapshots.First().TimeStamp.ToString("MM_dd__hh_mm")}.csv";
			using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"C:\GCP\processed files", fileName)))
			{
				outputFile.WriteLine($"Lat, long, accuracy, distance, heading, accumulated distance, avg heading_9, diff heading_9");

				for (int i = 1; i < route.OutHeadings.Count; i++)
				{
					var diffHeading = i <= 5 || i >= route.OutHeadings.Count - 6 ? 0 : route.DifferentialHeadings[i];
					/*						var diffHeading5 = i <= 3 || i >= outHeadings.Count - 4 ? 0 : differentialHeading5[i];
											var diffHeading3 = i <= 2 || i >= outHeadings.Count - 3 ? 0 : differentialHeading3[i];*/

					outputFile.WriteLine($"{route.Latitudes[i]}, {route.Longitudes[i]}," +
						$" {route.Accuracies[i]}, {route.Distances[i]}, {route.OutHeadings[i]}," +
						$" {route.AccumulativeDistances[i]}, {route.AverageHeadings[i]}, {diffHeading}");
				}
			}
		}

		private static void PrintRouteAsJson(IEnumerable<Snapshot> snapshots, string userId)
        {
			var fileName = $@"C:\Users\attiq\Downloads\{userId}_Snapshots_{snapshots.First().TimeStamp.ToString("MM_dd__hh_mm")}.json";
			string json = JsonConvert.SerializeObject(snapshots, Formatting.Indented);
			File.WriteAllText(fileName, json);
		}

		/*private static void ProcessRoute(List<Snapshot> completeRoute, bool useGooglePoints, string userId)
		{
			if (completeRoute.Count == 0)
			{
				return;
			}
			
			// we are looping way too many times, refactor this later.

			// clean data abit (remove duplicated points etc)
			var sortedCompleteRoute = new List<Snapshot>(completeRoute.Count);
			for (int i = 1; i < completeRoute.Count; i++)
			{
				if (AreSnapshotsOnSamePoint(completeRoute[i - 1], completeRoute[i], useGooglePoints))
				{
					continue;
				}

				sortedCompleteRoute.Add(completeRoute[i]);
			}

			var distances = new List<double>(sortedCompleteRoute.Count) { 0 };
			var latitudes = new List<double>(sortedCompleteRoute.Count);
			var longitudes = new List<double>(sortedCompleteRoute.Count);
			var outHeadings = new List<double>(sortedCompleteRoute.Count) { 0 };
			var accuracy = new List<double>(sortedCompleteRoute.Count) { 0 };
			var accumulativeDistance = new List<double>(sortedCompleteRoute.Count) { 0 };
			for (int i = 1; i < sortedCompleteRoute.Count; i++)
			{
				var index = i - 1;
				var currentSnapshot = sortedCompleteRoute[i];

				if (useGooglePoints)
				{
					longitudes.Add(currentSnapshot.GoogleLongitude);
					latitudes.Add(currentSnapshot.GoogleLatitude);
				}
				else
				{
					longitudes.Add(currentSnapshot.Longitude);
					latitudes.Add(currentSnapshot.Latitude);
				}

				if (longitudes.Count > 1) // Refactor, basically make sure we have atleast 2 clean points before calculating rest of the data.
				{
					distances.Add(GeoCalculator.GetDistance(latitudes[index- 1], longitudes[index- 1], latitudes[index], longitudes[index], 12) * 1000);
					outHeadings.Add(GeoCalculator.GetBearing(latitudes[index- 1], longitudes[index- 1], latitudes[index], longitudes[index]));
					accuracy.Add(currentSnapshot.Accuracy);
					accumulativeDistance.Add(distances[index] + accumulativeDistance[index- 1]);
				}
			}

			var averageHeading9 = new List<double>(outHeadings.Count) { 0 };
			var differentialHeading9 = new List<double>(outHeadings.Count) { 0 };
			var averageHeading5 = new List<double>(outHeadings.Count) { 0 };
			var differentialHeading5 = new List<double>(outHeadings.Count) { 0 };
			var averageHeading3 = new List<double>(outHeadings.Count) { 0 };
			var differentialHeading3 = new List<double>(outHeadings.Count) { 0 };

			for (int i = 1; i < outHeadings.Count; i++)
			{
				if (i < 5 || i > outHeadings.Count - 6)
				{
					// They really want to see 0 instead in excel instead of just not showing the row or keeping the raw value.
					averageHeading9.Add(0);
				}
				else
				{
					averageHeading9.Add((outHeadings[i - 4] + outHeadings[i - 3] + outHeadings[i - 2] + outHeadings[i - 1] +
						outHeadings[i] + outHeadings[i + 1] + outHeadings[i + 2] + outHeadings[i + 3] + outHeadings[i + 4]) / 9);
				}

				differentialHeading9.Add(averageHeading9[i] - averageHeading9[i - 1]);

				if (i < 3 || i > outHeadings.Count - 4)
				{
					averageHeading5.Add(0);
				}
				else
				{
					averageHeading5.Add((outHeadings[i - 2] + outHeadings[i - 1] +
						outHeadings[i] + outHeadings[i + 1] + outHeadings[i + 2]) / 5);
				}

				differentialHeading5.Add(averageHeading5[i] - averageHeading5[i - 1]);

				if (i < 2 || i > outHeadings.Count - 3)
				{
					averageHeading3.Add(0);
				}
				else
				{
					averageHeading3.Add((outHeadings[i - 1] + outHeadings[i] + outHeadings[i + 1]) / 3);
				}

				differentialHeading3.Add(averageHeading3[i] - averageHeading3[i - 1]);
			}



			
		}
*/
		/*Query segments = db.CollectionGroup("segments");
  QuerySnapshot querySnapshot = await segments.GetSnapshotAsync();
  foreach (DocumentSnapshot document in querySnapshot.Documents)
  {

	  Console.WriteLine($"{document.Reference.Path}: {document.GetValue<string>("Name")}");
  }*/


	}
}
