/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Histogram1D_DateTime creates a one dimensional histogram from an array of DateTime data.
    /// </summary>
    public class Histogram1D_DateTime : Histogram1DBase<long>
    {
        public Histogram1D_DateTime(DateTime[] rawData, DateTime lowBound, DateTime highBound, uint nBins, string name, Guid guid)
            : base(rawData.Select(x => x.Ticks).ToArray(), lowBound.Ticks, highBound.Ticks, nBins, name, guid)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
        public Histogram1D_DateTime(DateTime[] rawData, DateTime lowBound, DateTime highBound, uint nBins, string name) : this(rawData, lowBound, highBound, nBins, name, Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        public Histogram1D_DateTime(DateTime[] rawData, DateTime lowBound, DateTime highBound, uint nBins) : this(rawData, lowBound, highBound, nBins, "", Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
        /// <param name="guid">The GUID of the Histogram.</param>
        public Histogram1D_DateTime(DateTime lowBound, DateTime highBound, uint nBins, string name, Guid guid) : this(Array.Empty<DateTime>(), lowBound, highBound, nBins, name, guid) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        /// <param name="name">The name of the Histogram.</param>
        public Histogram1D_DateTime(DateTime lowBound, DateTime highBound, uint nBins, string name) : this(Array.Empty<DateTime>(), lowBound, highBound, nBins, name, Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Histogram1D_DateTime"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="highBound">The high bound.</param>
        /// <param name="nBins">The number of bins.</param>
        public Histogram1D_DateTime(DateTime lowBound, DateTime highBound, uint nBins) : this(Array.Empty<DateTime>(), lowBound, highBound, nBins, "", Guid.Empty) { }

        /// <summary>
        /// Provides the default label provider for the specified coordinates.
        /// </summary>
        /// <param name="coords">The specified coordinates.</param>
        /// <returns></returns>
        public override string DefaultLabelProvider(int coords)
        {
            int whichBin = coords;
            long highBound = HighBound;
            long lowBound = LowBound;
            long binIncrement = (highBound - lowBound) / NumBins;
            long lowBoundThisBin = lowBound + (whichBin * binIncrement);
            long highBoundThisBin = lowBound + ((whichBin + 1) * binIncrement);


            return
                $"[{FormatTimeSpan(new DateTime(lowBoundThisBin))}, {FormatTimeSpan(new DateTime(highBoundThisBin))})";
        }
        
        private static string FormatTimeSpan(DateTime dt)
        {
            return $"{dt.Year:d2}:{dt.Month:d2}:{dt.Day:d2}:{dt.Hour:d2}:{dt.Minute:d2}:{dt.Second:d2}";
        }
    }
}

