using System.Collections.Generic;
using System.Linq;
using Geolocation;

namespace LaneChangeDotNet
{
    /// <summary>
    /// A wrapper to keep all list of double arrays needed to generate road sections.
    /// </summary>

    // TODO: Clean this once we have finalized the implementation.
    public class ProcessedRouteWrapper
	{
		public string UserId { get; }
		public string RouteId { get; }
		public List<Snapshot> SortedSnapshots { get; }

        public List<double> Distances { get; }
		public List<double> Latitudes { get; }
		public List<double> Longitudes { get; }
		public List<double> OutHeadings { get; }
		public List<double> Slopes { get; }
		public List<double> Accuracies { get; }
		public List<double> AccumulativeDistances { get; }
		public List<double> AverageHeadings { get; }
		public List<double> DifferentialHeadings { get; }

		// We will most likely use Google points but foer now keeping the non-google provided GPS data too.
		public List<double> DistancesNotGoogle { get; }
		public List<double> LatitudesNotGoogle { get; }
		public List<double> LongitudesNotGoogle { get; }
		public List<double> OutHeadingsNotGoogle { get; }
		public List<double> SlopesNotGoogle { get; }
		public List<double> AccuraciesNotGoogle { get; }
		public List<double> AccumulativeDistancesNotGoogle { get; }
		public List<double> AverageHeadingsNotGoogle { get; }
		public List<double> DifferentialHeadingsNotGoogle { get; }

		public ProcessedRouteWrapper(string userId, string routeId, IEnumerable<Snapshot> snapshots)
		{
			UserId = userId;
			RouteId = routeId;

			// make sure snapshots are ordered correctly.
			SortedSnapshots = snapshots.OrderBy(x => x.TimeStamp).ToList();

			(Latitudes, Longitudes, Accuracies, Distances, OutHeadings, Slopes,
				AccumulativeDistances, AverageHeadings, DifferentialHeadings) = ProcessRoute(true);

			(LatitudesNotGoogle, LongitudesNotGoogle, AccuraciesNotGoogle, DistancesNotGoogle,
				OutHeadingsNotGoogle, SlopesNotGoogle, AccumulativeDistancesNotGoogle, AverageHeadingsNotGoogle, 
				DifferentialHeadingsNotGoogle) = ProcessRoute(false);
		}

		private (
			List<double> latitudes,
			List<double> longitudes,
			List<double> accuracy,
			List<double> distances,
			List<double> OutHeadings,
			List<double> slopes,
			List<double> accumulativeDistance,
			List<double> averageHeading9,
			List<double> differentialHeading9
			) ProcessRoute(bool useGooglePoints)
        {
			// we are looping way too many times, refactor this later.

			// clean data abit (remove duplicated points etc)
			var sortedCompleteRoute = new List<Snapshot>(SortedSnapshots.Count);
			for (int i = 1; i < SortedSnapshots.Count; i++)
			{
				if (Util.AreSnapshotsOnSamePoint(SortedSnapshots[i - 1], SortedSnapshots[i], useGooglePoints))
				{
					continue;
				}

				sortedCompleteRoute.Add(SortedSnapshots[i]);
			}

			var latitudes = new List<double>(sortedCompleteRoute.Count);
			var longitudes = new List<double>(sortedCompleteRoute.Count);
			var distances = new List<double>(sortedCompleteRoute.Count) { 0 };
			var outHeadings = new List<double>(sortedCompleteRoute.Count) { 0 };
			var slopes = new List<double>(sortedCompleteRoute.Count) { 0 };
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

				if (longitudes.Count > 1) // Refactor later, basically make sure we have atleast 2 clean points before calculating rest of the data.
				{
					distances.Add(GeoCalculator.GetDistance(latitudes[index - 1], longitudes[index - 1], latitudes[index], longitudes[index], 12) * 1000);
					outHeadings.Add(GeoCalculator.GetBearing(latitudes[index - 1], longitudes[index - 1], latitudes[index], longitudes[index]));
					var slope = (outHeadings[index] - outHeadings[index - 1]) / distances[index];
					slopes.Add(slope);
					accuracy.Add(currentSnapshot.Accuracy);
					accumulativeDistance.Add(distances[index] + accumulativeDistance[index - 1]);
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

			return (latitudes, longitudes, accuracy, distances, outHeadings, slopes, accumulativeDistance, averageHeading9, differentialHeading9);
		}
	}
}
