using Highpoint.Sage.SimCore;
using System.Numerics;

namespace Highpoint.Sage.Mathematics;

public interface IHistogram<out T> : IHasIdentity
    where T : INumber<T> 
{
    /// <summary>
    /// Gets the number of dimensions in this Histogram (a linear histogram is 1-dimensional.
    /// </summary>
    /// <value>The dimension.</value>
    uint Dimension
    {
        get;
    }

    /// <summary>
    /// Clears this Histogram.
    /// </summary>
    void Clear();

    /// <summary>
    /// Recalculates this Histogram, resulting in new bins and counts.
    /// </summary>
    void Recalculate();

    /// <summary>
    /// Gets or sets the label provider.
    /// </summary>
    /// <value>The label provider.</value>
    LabelProvider1d LabelProvider
    {
        get; set;
    }

    /// <summary>
    /// Returns the sum of values in all of the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.
    /// </summary>
    /// <param name="hbc">The HistogramBinCategory.</param>
    /// <returns>The sum of values.</returns>
    T SumEntries(HistogramBinCategory hbc);
        
    /// <summary>
    /// Counts the entries in the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.
    /// </summary>
    /// <param name="hbc">The HistogramBinCategory.</param>
    /// <returns>The number of entries in the bins identified by the given <see cref="Highpoint.Sage.Mathematics.HistogramBinCategory"/>.</returns>
    uint CountEntries(HistogramBinCategory hbc);
}