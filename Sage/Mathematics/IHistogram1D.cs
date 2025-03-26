/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Returns true if the data in a certain Histogram bin meet a certain criteria.
    /// </summary>
    /// <param name="data">The histogram data.</param>
    /// <param name="coordinates">The coordinates of the bin in the data.</param>
    /// <returns>True if the data in a certain Histogram bin meet a certain criteria.</returns>
    public delegate bool HistogramDataFilter(Array data, int[] coordinates);

    /// <summary>
    /// Returns a string that characterizes a bin in a Histogram located at the given dimensional coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates of the bin whose label is desired.</param>
    /// <returns>A string that characterizes a bin in a Histogram located at the given dimensional coordinates.</returns>
    public delegate string LabelProvider1d(int coordinate);

    /// <summary>
    /// Implemented by an object that processes raw data items into bins and presents some
    /// basic statistics on those bins.
    /// </summary>
    public interface IHistogram1D<T> : IHistogram<T> 
        where T : INumber<T> 
    {
        /// <summary>
        /// Gets or sets the raw data that comprises this Histogram.
        /// </summary>
        /// <value>The raw data.</value>
        T[] RawData
        {
            get; set;
        }   

        /// <summary>
        /// Gets the bins that are a part of this Histogram.
        /// </summary>
        /// <value>The bins.</value>
        IReadOnlyList<uint> BinCounts
        {
            get;
        }
        
        uint NumBins
        {
            get;
        }

        /// <summary>
        /// Recalculates the Histogram with new high &amp; low bounds, resulting in new bins and counts.
        /// </summary>
        /// <param name="lowBounds">The low bounds of the Histogram.</param>
        /// <param name="highBounds">The high bounds of the Histogram.</param>
        /// <param name="nBins">The number of bins.</param>
        void Recalculate(T lowBounds, T highBounds, uint nBins);

        /// <summary>
        /// Counts the entries in the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns></returns>
        public uint CountEntries(uint lowBounds, uint highbounds)
        {
            uint nEntries = 0;
            for (var i = lowBounds; i < highbounds; i++)
            {
                nEntries += BinCounts[(int)i];
            }
            return nEntries;
        }

        /// <summary>
        /// Returns the index of the biggest bin in each dimension, among the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>Tthe index of the biggest bin in each dimension, among the bins identified by the given low and high bounds.</returns>
        public uint BiggestBin(uint lowBounds, uint highbounds)
        {
            int low = (int)lowBounds;
            int high = (int)highbounds;
            int biggestBinNum = 0;
            uint biggestBinCount = uint.MinValue;
            for (int i = low; i < high; i++)
            {
                if (BinCounts[i] >= biggestBinCount)
                    continue;
                biggestBinCount = BinCounts[i];
                biggestBinNum = i;
            }
            return (uint)biggestBinNum;
        }

        /// <summary>
        /// Returns the index of the smallest bin in each dimension, among the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowBounds">The low bounds.</param>
        /// <param name="highbounds">The highbounds.</param>
        /// <returns>Tthe index of the smallest bin in each dimension, among the bins identified by the given low and high bounds.</returns>
        uint SmallestBin(uint lowBounds, uint highbounds)
        {
            int low = (int)lowBounds;
            int high = (int)highbounds;
            int smallestBinNum = 0;
            uint smallestBinCount = uint.MaxValue;
            for (int i = low; i < high; i++)
            {
                if (BinCounts[i] >= smallestBinCount)
                    continue;
                smallestBinCount = BinCounts[i];
                smallestBinNum = i;
            }
            return (uint)smallestBinNum;
        }
        

        /// <summary>
        /// Gets the low bound of the Histogram.
        /// </summary>
        /// <value>The low bound.</value>
        T LowBound
        {
            get;
        }

        /// <summary>
        /// Gets the high bound of the Histogram..
        /// </summary>
        /// <value>The high bound.</value>
        T HighBound
        {
            get;
        }

        /// <summary>
        /// The count of data points whose values were less than the low bound.
        /// </summary>
        uint LowBinCount
        {
            get;
        }
        
        /// <summary>
        /// The count of data points whose values were greater than the high bound.
        /// </summary>
        uint HighBinCount
        {
            get;
        }
        
        /// <summary>
        /// Gets the label for the bin at the specified coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>
        /// The label for the bin at the specified coordiantes.
        /// </returns>
        string GetLabel(int coordinates);

        /// <summary>
        /// This returns a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count - note that it is only relevant if
        /// the histogram was expected to have been uniform.
        /// </summary>
        /// <param name="coordinates">An integer array that specifies the coordinates of
        /// the bin of interest. Histogram analysis of a Histogram1D_&lt;anything&gt; must be
        /// on a 1 dimensional array, therefore, this array must be of rank 1.
        /// </param>
        /// <returns> a value that indicates how far a specified bin's count
        /// deviates from the 'expected' count.</returns>
        //T Error(int coordinates);

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given low and high bounds.
        /// </summary>
        /// <param name="lowIndex">The low bounds.</param>
        /// <param name="highIndex">The high bounds.</param>
        /// <returns>The sum of values.</returns>
        T SumEntries(uint lowIndex, uint highIndex)
        {
            T binIncrement = Operations<T>.DivideByUInt32(Operations<T>.Subtract(HighBound, LowBound), NumBins);
           
            T lowThreshold = Operations<T>.Add(LowBound, Operations<T>.MultiplyByUInt32(binIncrement, lowIndex));
            T highThreshold = Operations<T>.Add(HighBound, Operations<T>.MultiplyByUInt32(binIncrement, highIndex));

            T sum = default(T);
            foreach (T dataPoint in RawData)
            {
                if (dataPoint >= lowThreshold && dataPoint < highThreshold)
                    sum += dataPoint;
                
                // if ( Operations<T>.GreaterThanOrEqual(dataPoint, lowThreshold) && Operations<T>.LessThan(dataPoint, highThreshold) )
                // {
                //     sum = Operations<T>.Add(sum, dataPoint);
                // }
            }

            return sum;
        }
        
        
        
        /// <summary>
        /// Returns the index of the biggest bin in each dimension, among the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>. 
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>he index of the biggest bin in each dimension, among the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.</returns>
        public int BiggestBin(HistogramBinCategory hbc)
        {
            int biggestBinNum = 0;
            uint biggestBinCount = uint.MaxValue;
            if (hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All)
            {
                biggestBinNum = (int)BiggestBin( 0, (uint)BinCounts.Count);
                biggestBinCount = BinCounts[biggestBinNum];
            }

            if (hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All)
            {
                if (LowBinCount > biggestBinCount)
                {
                    biggestBinNum = int.MinValue;
                    biggestBinCount = LowBinCount;
                }
            }
            if (hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All)
            {
                if (HighBinCount > biggestBinCount)
                {
                    biggestBinNum = int.MaxValue;
                }
            }
            return biggestBinNum;
        }
        
        
        /// <summary>
        /// Returns the index of the bin that contains the most entries, selected from
        /// a specified set of bins.
        /// </summary>
        /// <param name="hbc">The <see cref="HistogramBinCategory"/> that specifies the bins of interest.</param>
        /// <returns>The indexes of the bin that contains the fewest entries.</returns>
        int SmallestBin(HistogramBinCategory hbc)
        {
            int smallestBinNum = 0;
            uint smallestBinCount = uint.MaxValue;
            if (hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All)
            {
                smallestBinNum = (int)SmallestBin(0, (uint)BinCounts.Count);
                smallestBinCount = BinCounts[smallestBinNum];
            }

            if (hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All)
            {
                if (LowBinCount < smallestBinCount)
                {
                    smallestBinNum = int.MinValue;
                    smallestBinCount = LowBinCount;
                }
            }
            if (hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All)
            {
                if (HighBinCount < smallestBinCount)
                {
                    smallestBinNum = int.MaxValue;
                }
            }
            return smallestBinNum;
        }
    }
}



