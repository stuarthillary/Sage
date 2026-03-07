/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Summary description for Histogram1D.
    /// </summary>
    public sealed class Histogram1D_Double : Histogram1D_Base<double>
    {
        #region >>> Local Private Variables. <<<
        
        //private double m_lowSum;
        //private double m_lowSumSquares;
        //private double m_highSum;
        //private double m_highSumSquares;
        #endregion

        public Histogram1D_Double(double[] rawData, double lowBound, double highBound, uint nBins, string name, Guid guid)
            : base(rawData, lowBound, highBound, nBins, name, guid) { }
        public Histogram1D_Double(double[] rawData, double lowBound, double highBound, uint nBins, string name) : this(rawData, lowBound, highBound, nBins, name, Guid.Empty) { }
        public Histogram1D_Double(double[] rawData, double lowBound, double highBound, uint nBins) : this(rawData, lowBound, highBound, nBins, "", Guid.Empty) { }
        public Histogram1D_Double(double lowBound, double highBound, uint nBins, string name, Guid guid) : this(null, lowBound, highBound, nBins, name, guid) { }
        public Histogram1D_Double(double lowBound, double highBound, uint nBins, string name) : this(null, lowBound, highBound, nBins, name, Guid.Empty) { }
        public Histogram1D_Double(double lowBound, double highBound, uint nBins) : this(null, lowBound, highBound, nBins, "", Guid.Empty) { }


        public override string DefaultLabelProvider(int coords)
        {
            int whichBin = coords;
            double highBound = (double)HighBound;
            double lowBound = (double)LowBound;
            double binIncrement = (highBound - lowBound) / NumBins;
            double lowBoundThisBin = lowBound + (whichBin * binIncrement);
            double highBoundThisBin = lowBound + ((whichBin + 1) * binIncrement);

            string fmtSpecifier = "f2";
            string fmtString = "[{0:" + fmtSpecifier + "}->{1:" + fmtSpecifier + "})";
            return string.Format(fmtString, lowBoundThisBin, highBoundThisBin);
        }

        public string DefaultLabelProviderWithError(int coords)
        {
            string fmtSpecifier = "f2";
            string fmtString = " - Err({0:" + fmtSpecifier + "})";
            return DefaultLabelProvider(coords);
        }


    }
}

