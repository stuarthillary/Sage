/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Numerics;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Base class for a 1-dimensional histogram. Since this derives from a base-level interface
    /// that is intended for all histograms, indices are specified as an array of integers. So for
    /// a 1-D histogram, bin #3 would be referred to as having index int[]{3}. For a 2-D histogram,
    /// bin 4,2 would be referred to as having index int[]{4,2}. In addition, bins are separated into
    /// three categories, None, OffScaleLow, InRange, OffScaleHigh, and All. These are flags that can
    /// be and'ed together. Most queries can be applied to a range of bins, or to a full category or
    /// set of categories.
    /// </summary>
    public abstract class Histogram1D_Base<T> : IHistogram1D<T>
        where T : INumber<T>
    {

        #region >>> Local Private Variables. <<<
        /// <summary>
        /// The raw data array that provides the underlying histogram data.
        /// </summary>
        private T[]? _rawData;

        protected uint[] _bins = [];


        private LabelProvider1d? _labelProvider;
        #endregion

        /// <summary>
        /// Creates a 1D histogram.
        /// </summary>
        /// <param name="rawData">An array of 1D data that contains the data to be binned.</param>
        /// <param name="lowBound">The data that represents the low bound of the histogram.</param>
        /// <param name="highBound">The data that represents the high bound of the histogram.</param>
        /// <param name="nBins">The number of bins that the data will pe placed in, between lo-bound and hi-bound.</param>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="guid">The guid of the histogram.</param>
        // ReSharper disable once PublicConstructorInAbstractClass
        public Histogram1D_Base(T[] rawData, T lowBound, T highBound, uint nBins, string name, Guid guid)
        {
            _rawData = rawData;
            LowBound = lowBound;
            HighBound = highBound;
            _name = name;
            Guid = guid;
            NumBins = nBins;
            _labelProvider = DefaultLabelProvider;
        }

        #region IHistogram Members


        
        /// <summary>
        /// The data that represents the low bound of the in-band range.
        /// All data points that are less than this value are tallied into the m_lowBin bin.
        /// </summary>
        public T LowBound
        {
            get;
            private set;
        }

        /// <summary>
        /// The data that represents the high bound of the in-band range.
        /// All data points that are greater than this value are tallied into the m_highBin bin.
        /// </summary>
        public T HighBound
        {
            get;
            private set;
        }

        public uint LowBinCount
        {
            get;
            protected set;
        }

        public uint HighBinCount
        {
            get;
            protected set;
        }



        /// <summary>
        /// Gets and sets the object that provides the name of a specified bin.
        /// </summary>
        /// <value>The label provider.</value>
        public LabelProvider1d LabelProvider
        {
            get
            {
                return _labelProvider!;
            }
            set
            {
                _labelProvider = value;
            }
        }

        /// <summary>
        /// Gets the label for the bin at the specified coordiantes.
        /// </summary>
        /// <param name="coords">The coordinates of the desired bin.</param>
        /// <returns>The label for the bin at the specified coordiantes.</returns>
        public string GetLabel(int coords)
        {
            return _labelProvider!(coords);
        }

        /// <summary>
        /// Gets or sets the raw data that comprises this Histogram.
        /// </summary>
        /// <value>The raw data.</value>
        public T[] RawData
        {
            get
            {
                return _rawData!;
            }
            set
            {
                Clear();
                _rawData = value;
                Recalculate();
            }
        }

        /// <summary>
        /// Gets the bins that are a part of this Histogram.
        /// </summary>
        /// <value>The bins.</value>
        public IReadOnlyList<uint> BinCounts => _bins;

        public uint NumBins
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of dimensions in this Histogram (a linear histogram is 1-dimensional).
        /// </summary>
        /// <value>The dimension.</value>
        public uint Dimension => 1;

        /// <summary>
        /// Clears this Histogram.
        /// </summary>
        public void Clear()
        {
            _bins = [];
            NumBins = 0;
            LowBinCount = 0;
            HighBinCount = 0;
        }

        /// <summary>
        /// Returns the sum of values in all of the bins identified by the given <see cref="HistogramBinCategory"/>.
        /// </summary>
        /// <param name="hbc">The HistogramBinCategory.</param>
        /// <returns>The sum of values.</returns>
        public T SumEntries(HistogramBinCategory hbc)
        {
            T sumEntries = default!;
            bool sumLow = false;
            bool sumHigh = false;
            if (hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All)
                sumLow = true;
            if (hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All)
                sumHigh = true;
            bool inRange = hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All;

            foreach (var val in RawData)
            {
                if (Operations<T>.LessThan(val, LowBound))
                {
                    if (sumLow)
                        sumEntries = Operations<T>.Add(sumEntries, val);
                }
                else if (Operations<T>.GreaterThanOrEqual(val, HighBound))
                {
                    if (sumHigh)
                        sumEntries = Operations<T>.Add(sumEntries, val);
                }
                else
                {
                    if (inRange)
                        sumEntries = Operations<T>.Add(sumEntries, val);
                }
            }
            return sumEntries;
        }

        /// <summary>
        /// Counts the number of entries in a given range (low bin, in-band bins or high bin.)
        /// </summary>
        /// <param name="hbc">An enumerator that describes whether the count is for low, in-band, or high bins.</param>
        /// <returns>The number of entries that fall in the specified range.</returns>
        public uint CountEntries(HistogramBinCategory hbc)
        {
            uint nEntries = 0;
            if (hbc == HistogramBinCategory.OffScaleLow || hbc == HistogramBinCategory.All)
                nEntries += LowBinCount;
            if (hbc == HistogramBinCategory.OffScaleHigh || hbc == HistogramBinCategory.All)
                nEntries += HighBinCount;
            if (hbc == HistogramBinCategory.InRange || hbc == HistogramBinCategory.All)
                nEntries += ((IHistogram1D<T>)this).CountEntries(0 , NumBins );
            
            return nEntries;
        }


        /// <summary>
        /// Recalculates this Histogram, resulting in new bins and counts.
        /// </summary>
        public void Recalculate()
        {
            _bins = new uint[NumBins];
            T lowBound = LowBound;
            T highBound = HighBound;
            T binIncrement = (Operations<T>.DivideByUInt32(Operations<T>.Subtract(highBound, lowBound), NumBins));
            foreach (var dataPoint in RawData)
            {
                if (Operations<T>.LessThan(dataPoint, lowBound))
                {
                    LowBinCount++;
                }
                else if (Operations<T>.GreaterThanOrEqual(dataPoint, highBound))
                {
                    HighBinCount++;
                }
                else
                {
                    int whichBin = Converter<T>.ToInt32(Operations<T>.Divide(Operations<T>.Subtract(dataPoint, lowBound), binIncrement));
                    _bins[whichBin]++;
                }
            }
        }

        /// <summary>
        /// Recalculates the Histogram with new high &amp; low bounds, resulting in new bins and counts.
        /// </summary>
        /// <param name="lowBounds">The low bounds of the Histogram.</param>
        /// <param name="highBounds">The high bounds of the Histogram.</param>
        /// <param name="nBins">The number of bins.</param>
        public void Recalculate(T lowBounds, T highBounds, uint nBins)
        {
            LowBound = lowBounds;
            HighBound = highBounds;
            NumBins = nBins;
            Recalculate();
        }

        /// <summary>
        /// Provides the default label provider for the specified coordinates.
        /// </summary>
        /// <param name="coords">The specified coordinates.</param>
        /// <returns></returns>
        public abstract string DefaultLabelProvider(int coords);
        #endregion

        #region IHasIdentity Members

        private readonly string _name;
        /// <summary>
        /// The name for this object. Not typically required to be unique.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name => _name;

        private readonly string? _description = null;
        /// <summary>
        /// A description of this Histogram1D_Base.
        /// </summary>
        public string? Description => _description ?? _name;

        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The object's Guid</value>
        public Guid Guid
        {
            get;
        }

        #endregion

    }
}

