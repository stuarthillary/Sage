/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Summary description for Histogram1D.
    /// </summary>
    public class Histogram1D_TimeSpan : Histogram1D_Base<long>
    {
        public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, uint nBins, string name, Guid guid)
            : base(rawData.Select(x => x.Ticks).ToArray(), lowBound.Ticks, highBound.Ticks, nBins, name, guid) { }
        public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, uint nBins, string name) : this(rawData, lowBound, highBound, nBins, name, Guid.Empty) { }
        public Histogram1D_TimeSpan(TimeSpan[] rawData, TimeSpan lowBound, TimeSpan highBound, uint nBins) : this(rawData, lowBound, highBound, nBins, "", Guid.Empty) { }
        public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, uint nBins, string name, Guid guid) : this(Array.Empty<TimeSpan>(), lowBound, highBound, nBins, name, guid) { }
        public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, uint nBins, string name) : this(Array.Empty<TimeSpan>(), lowBound, highBound, nBins, name, Guid.Empty) { }
        public Histogram1D_TimeSpan(TimeSpan lowBound, TimeSpan highBound, uint nBins) : this(Array.Empty<TimeSpan>(), lowBound, highBound, nBins, "", Guid.Empty) { }


        public override string DefaultLabelProvider(int coords)
        {
            int whichBin = coords;
            long highBound = HighBound;
            long lowBound = LowBound;
            long binIncrement = (highBound - lowBound) / NumBins;
            long lowBoundThisBin = lowBound + (whichBin * binIncrement);
            long highBoundThisBin = lowBound + ((whichBin + 1) * binIncrement);

            //string fmtSpecifier = "f2";
            //string fmtString = "[{0:"+fmtSpecifier+"},{1:"+fmtSpecifier+"})";
            //return string.Format(fmtString,lowBoundThisBin,highBoundThisBin);
            return "[" + FormatTimeSpan(TimeSpan.FromTicks(lowBoundThisBin))
                + ", "
                + FormatTimeSpan(TimeSpan.FromTicks(highBoundThisBin)) + ")";
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays < 1.0)
            {
                return $"{ts.Hours:d2}:{ts.Minutes:d2}:{ts.Seconds:d2}";
            }
            else
            {
                return $"{ts.Days:d2}:{ts.Hours:d2}:{ts.Minutes:d2}:{ts.Seconds:d2}";
            }
        }
    }
}

