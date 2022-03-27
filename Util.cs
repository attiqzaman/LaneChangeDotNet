using System;
using System.Collections.Generic;

namespace LaneChangeDotNet
{
    public static class Util
    {
		public static bool AreSnapshotsOnSamePoint(Snapshot a, Snapshot b, bool useGooglePoints)
		{
			return useGooglePoints ?
				a.GoogleLatitude == b.GoogleLatitude && a.GoogleLongitude == b.GoogleLongitude :
				a.Latitude == b.Latitude && a.Longitude == b.Longitude;
		}

		public static double CalculatePathAveragedValue(
			int start,
			int end,
			List<double> sourceData,
			List<double> distances,
			List<double> accumulativeDistances)
		{
			var sumOfDifferenceOfSourceParameter = new List<double>() { 0 };
			double pathAveragedValue = 0;
			for (int i = start; i <= end; i++)
			{
				var currentValue = sourceData[i] * distances[i];
				sumOfDifferenceOfSourceParameter.Add(currentValue + sumOfDifferenceOfSourceParameter[i - 1]);

				// This could just calculate pathAverageValue at the end but for now I am trying to keep
				// things somewhat similar to original until we know the code has been translated successfully.
				var currentAccumulativeDistanceFromStart = accumulativeDistances[i] - accumulativeDistances[start - 1];
				pathAveragedValue = sumOfDifferenceOfSourceParameter[i] / currentAccumulativeDistanceFromStart;
			}

			return pathAveragedValue;
		}


		// Not exactly sure what dh1 is representing but its used in calculation of straight sections.
		public static List<double> GetDH1 (List<double> differentialHeadings)
        {
			var limit1 = 0.08;
			var limit2 = 0.07;
			var limit3 = 0.09;
			var dh1 = new List<double>(differentialHeadings);

			// We are updating array while looping over it?
            for (int i = 1; i < differentialHeadings.Count; i++)
            {
                if (dh1[i] > limit1 && dh1[i + 1] < limit1 && dh1[i - 1] < limit1 )
                {
					dh1[i] = limit1;
                }
                else if (dh1[i] < -limit1 && dh1[i + 1] > -limit1 && dh1[i - 1] > -limit1)
                {
					dh1[i] = -limit1;
				}
				else if (dh1[i] < limit1 && dh1[i + 1] > limit1 && dh1[i - 1] > limit1)
				{
					dh1[i] = limit3;
				}
				else if ((dh1[i] > -limit1 && dh1[i + 1] < -limit1 && dh1[i - 1] < -limit1))
				{
					dh1[i] = -limit3;
				}
				else if (dh1[i] > limit1 && dh1[i + 1] > limit1 && dh1[i + 2] > limit1 && dh1[i + 3] < limit1 && dh1[i - 1] < limit1 && dh1[i - 2] < limit1)
				{
					dh1[i] = limit2;
					dh1[i + 1] = limit2;
					dh1[i + 2] = limit2;
				}
				else if (dh1[i] > limit1 && dh1[i + 1] > limit1 && dh1[i + 2] < limit1 && dh1[i + 3] < limit1 && dh1[i - 1] < limit1)
				{
					dh1[i] = limit2;
					dh1[i + 1] = limit2;
				}
				else if (dh1[i] < -limit1 && dh1[i + 1] < -limit1 && dh1[i + 2] < -limit1 && dh1[i + 3] < -limit1 && dh1[i + 4] > -limit1 && dh1[i - 1] > -limit1 && dh1[i - 2] > -limit1)
				{
					dh1[i] = -limit2;
					dh1[i + 1] = -limit2;
					dh1[i + 2] = -limit2;
				}
				//for N2_RL
				else if (dh1[i] < limit1 && dh1[i - 1] > limit1 && dh1[i - 2] > limit1 && dh1[i - 3] > limit1 && dh1[i - 4] > limit1 && dh1[i - 5] > limit1 && dh1[i - 6] > limit1 && dh1[i - 7] > limit1 && dh1[i - 8] < limit1 && dh1[i + 1] < limit1)
				{
					dh1[i - 1] = limit2;
					dh1[i - 2] = limit2;
					dh1[i - 3] = limit2;
					dh1[i - 4] = limit2;
				}
				else
				{
					dh1[i] = dh1[i]; // hopefully just an oversight.
				}
			}

			return dh1;
		}

		// This returns a dictionary where each kvp represent a straight section, values are indices of the differential heading array. 
		public static Dictionary<int, int> GetStraightSections(List<double> dh1)
        {
			var sections = new Dictionary<int, int>();

            // We don't know if we started in a curve (meaning we will hit start point of a section first) or we
            // already started in a straight section (meaning we will first hit end of straight section) so we need to
            // first determine that, we will loop through the differential headings looking for both start and end of straight section, 
            // depending on what we find first we will process the rest of the array accordingly.
            var i = 6;
            int startIndex;
            int endIndex;
            while (i < dh1.Count)
            {
                if (IsStartOfStraightSection(i, dh1))
                {
                    // We started in a curver.
                    startIndex = i;
                    endIndex = FindEndOfStraightSection(i, dh1);
                    sections.Add(startIndex, endIndex);
                    i = endIndex; // should this be endIndex + 1?
                }
                else if (IsEndOfStraightSection(i, dh1))
                {
                    // We started in a straight section, so assume start of straight section was the first data point.
                    startIndex = 0;
                    endIndex = i;
                    sections.Add(startIndex, endIndex);
                    i = endIndex; // should this be endIndex + 1?
                }
                else
                {
                    i++;
                }
            }

            if (sections.Count > 0)
            {
				while (i < dh1.Count)
                {
					startIndex = FindStartOfStraightSection(i, dh1);
                    if (startIndex != -1)
                    {
						endIndex = FindEndOfStraightSection(i, dh1);
						sections.Add(startIndex, endIndex);
						i = endIndex; // should this be endIndex + 1?
					}
				}

			}
            else
            {
				// we just were in a curve throughout the route.
				// TODO: Handle the curve
			}

			return sections;
		}

		private static int FindStartOfStraightSection(int start, List<double> dh1)
		{
			for (int i = start; i < dh1.Count - 6; i++)
			{
                if (IsStartOfStraightSection(i, dh1))
                {
					return i;
                }
			}

			return -1;
		}

		private static int FindEndOfStraightSection(int start, List<double> dh1)
        {
			for (int i = start; i < dh1.Count - 6; i++)
			{
				if (IsEndOfStraightSection(i, dh1))
				{
					return i;
				}
			}

			return dh1.Count - 1;
		}

        private static bool IsStartOfStraightSection(int index, List<double> dh1)
        {
			var limit1 = 0.08;
			// This kind of things can be made simpler/faster by applying a matrix filter
			if ((Math.Abs(dh1[index - 6]) > limit1) && (Math.Abs(dh1[index - 5]) > limit1) && (Math.Abs(dh1[index - 4]) > limit1) &&
				(Math.Abs(dh1[index - 3]) > limit1) && (Math.Abs(dh1[index - 2]) > limit1) && (Math.Abs(dh1[index - 1]) > limit1) &&
				(Math.Abs(dh1[index]) <= limit1) &&
				(Math.Abs(dh1[index + 1]) <= limit1) && (Math.Abs(dh1[index + 2]) <= limit1) &&
				(Math.Abs(dh1[index + 3]) <= limit1) && (Math.Abs(dh1[index + 4]) <= limit1) && (Math.Abs(dh1[index + 5]) <= limit1) &&
				(Math.Abs(dh1[index + 6]) <= limit1))

			{
				return true;
			}

			return false;
		}

		private static bool IsEndOfStraightSection(int index, List<double> dh1)
		{
			var limit1 = 0.08;
			// This kind of things can be made simpler/faster by applying a matrix filter
			if ((Math.Abs(dh1[index - 6]) <= limit1) && (Math.Abs(dh1[index - 5]) <= limit1) && (Math.Abs(dh1[index - 4]) <= limit1) && (Math.Abs(dh1[index - 3]) <= limit1) &&
					(Math.Abs(dh1[index - 2]) <= limit1) && (Math.Abs(dh1[index - 1]) <= limit1) &&
					(Math.Abs(dh1[index]) >= limit1) && (Math.Abs(dh1[index + 1]) > limit1) && (Math.Abs(dh1[index + 2]) > limit1) &&
					(Math.Abs(dh1[index + 3]) > limit1) && (Math.Abs(dh1[index + 4]) > limit1) && (Math.Abs(dh1[index + 5]) > limit1) && (Math.Abs(dh1[index + 6]) > limit1))

			{
				return true;
			}

			return false;
		}


		// The following two are replaced by the above common implementation.
		public static double PathAveragedHeading(
			int start,
			int end,
			List<double> outHeadings,
			List<double> distances,
			List<double> accumulativeDistances)
        {
			var sumOfDifferenceOfHeading = new List<double>() { 0 };
			double pathAveragedHeading = 0;
            for (int i = start; i <= end; i++)
            {
				var currentValue = outHeadings[i] * distances[i];
				sumOfDifferenceOfHeading.Add(currentValue + sumOfDifferenceOfHeading[i - 1]);

				// This could just calcultae patgAverageHeading at the end but for now trying to keep things somewhat similar to original until
				// we know the code has been translated successfully.
				var currentAccumulativeDistanceFromStart = accumulativeDistances[i] - accumulativeDistances[start - 1];
				pathAveragedHeading = sumOfDifferenceOfHeading[i] / currentAccumulativeDistanceFromStart;
			}

			return pathAveragedHeading;
        }

		public static double PathAveragedSlope(
			int start,
			int end,
			List<double> slopes,
			List<double> distances,
			List<double> accumulativeDistances)
        {
			var sumOfDifferenceOfSlopes = new List<double>() { 0 };
			double pathAveragedSlope = 0;
			for (int i = start; i <= end; i++)
			{
				var currentValue = slopes[i] * distances[i];
				sumOfDifferenceOfSlopes.Add(currentValue + sumOfDifferenceOfSlopes[i - 1]);

				var currentAccumulativeDistanceFromStart = accumulativeDistances[i] - accumulativeDistances[start - 1];
				pathAveragedSlope = sumOfDifferenceOfSlopes[i] / currentAccumulativeDistanceFromStart;
			}

			return pathAveragedSlope;
		}
	}
}
