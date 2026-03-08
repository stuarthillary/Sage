/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Highpoint.Sage.Utility
{


    public class ExtensionTester : IDisposable
    {
        public ExtensionTester()
        {
            Init();
        }


        public void Init()
        {
        }


        public void Dispose()
        {
            Debug.WriteLine("Done.");
        }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Tests the Byte XOR extension.")]
        public void TestByteXOR()
        {
            byte[] ba1 = new byte[] { 0xF0, 0xF0 };
            byte[] ba2 = new byte[] { 0x0F, 0x0F };
            byte[] ba3 = ba1.XOR(ba2);
            Assert.Equal(ba3[0], 0xFF);
            Assert.Equal(ba3[1], 0xFF);

            ba2 = new byte[] { 0xFF, 0xFF };
            ba3 = ba1.XOR(ba2);
            Assert.Equal(ba3[0], 0x0F);
            Assert.Equal(ba3[1], 0x0F);

            ba3 = ba2.XOR(ba2);
            Assert.Equal(ba3[0], 0x00);
            Assert.Equal(ba3[1], 0x00);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Tests the CommasAndAndedList operations.")]
        public void TestCommasAndAndedListOperations()
        {

            List<string> strings = new List<string>(new string[] { "Cat", "Dog", "Horse", "Cow" });
            string[] results = new string[]{
                "Cat",
                "Cat and Dog",
                "",
                "Cat, Dog, Horse and Cow"};

            foreach (int count in new int[] { 1, 2, 4 })
            {
                List<string> tmp = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    tmp.Add(strings[i]);
                }
                string result = StringOperations.ToCommasAndAndedList(((IEnumerable<string>)tmp));
                Assert.True(result.Equals(results[count - 1], StringComparison.Ordinal));
                Console.WriteLine(result);

            }

            foreach (int count in new int[] { 1, 2, 4 })
            {
                ArrayList tmp = new ArrayList();
                for (int i = 0; i < count; i++)
                {
                    tmp.Add(strings[i]);
                }
                string result = StringOperations.ToCommasAndAndedList(tmp);
                Assert.True(result.Equals(results[count - 1], StringComparison.Ordinal));
                Console.WriteLine(result);
            }

            foreach (int count in new int[] { 1, 2, 4 })
            {
                List<Thingy> tmp = new List<Thingy>();
                for (int i = 0; i < count; i++)
                {
                    tmp.Add(new Thingy(strings[i]));
                }
                string result = StringOperations.ToCommasAndAndedListOfNames(tmp);
                Assert.True(result.Equals(results[count - 1], StringComparison.Ordinal));
                Console.WriteLine(result);
            }

            foreach (int count in new int[] { 1, 2, 4 })
            {
                List<Thingy> tmp = new List<Thingy>();
                for (int i = 0; i < count; i++)
                {
                    tmp.Add(new Thingy(strings[i]));
                }
                string result = StringOperations.ToCommasAndAndedList(tmp, n => n.Name);
                Assert.True(result.Equals(results[count - 1], StringComparison.Ordinal));
                Console.WriteLine(result);
            }

        }

        sealed class Thingy : Highpoint.Sage.Core.IHasName
        {
            private string _name;
            public Thingy(string name)
            {
                _name = name;
            }
            public string Name
            {
                get
                {
                    return _name;
                }
            }
        }
    }
}

namespace Highpoint.Sage.Mathematics
{


    public class ExtensionTester : IDisposable
    {

        public ExtensionTester()
        {
            Init();
        }


        public void Init()
        {
        }


        public void Dispose()
        {
            Debug.WriteLine("Done.");
        }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Tests the PercentileGetter extension.")]
        public void TestPercentileGetter()
        {
            double[] testData = new double[] { -99.9, -.4, .9, 1.1, 3.6, 12.5, 42.2 };
            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, false);
            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, true);

            TestPG(testData, new double[] { 0, 50, 100 }, new double[] { -99.9, 1.1, 42.2 }, false);
            TestPG(testData, new double[] { 10, 60, 95 }, new double[] { -40.2, 2.6, 33.29 }, true);

        }

        /// <summary>
        /// Tests the PercentileGetter extension.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Tests the  BoundBySigmas extension.")]
        public void TestSigmaBounding()
        {

            double[] testData = new double[] { -99.9, -.4, .9, 1.1, 3.6, 12.5, 42.2, 67.8, 123.0 };
            double[] loBound = new double[] { 3, 3, 2, 1, .5, .25 };
            double[] hiBound = new double[] { 3, 2, 1, 3, .5, .25 };
            int[] expecteds = new int[] { 9, 9, 7, 8, 6, 2 };

            RunSigmaBoundingTest(testData, expecteds, loBound, hiBound);
        }

        private void TestPG(double[] srcData, double[] targets, double[] expecteds, bool interpolate)
        {
            List<Thingy> thingies = new List<Thingy>();
            Func<Thingy, double> valueGetter = n => n.DoubleValue;

            foreach (double d in srcData)
            {
                thingies.Add(new Thingy(d));
            }

            Console.WriteLine("Created a list of Thingies, {0}.", StringOperations.ToCommasAndAndedList(thingies.ConvertAll<string>(n => n.ToString())));

            for (int i = 0; i < targets.Length; i++)
            {
                double result = thingies.GetValueAtPercentile<Thingy>(targets[i], valueGetter, interpolate);

                Console.WriteLine(" > {0} value at percentile {1} was {2} - expected {3}."
                    , (interpolate ? "Interpolated" : "Uninterpolated"), targets[i], result, expecteds[i]);

                Assert.True(Math.Abs((result - expecteds[i]) / result) < 1E-8,
                    string.Format("Getting {0} percentile returned {1}, should have returned {2}.", targets[i], result, expecteds[i]));
            }
        }

        private void RunSigmaBoundingTest(double[] srcData, int[] expecteds, double[] loBounds, double[] hiBounds)
        {

            List<Thingy> thingies = new List<Thingy>();
            foreach (double d in srcData)
            {
                thingies.Add(new Thingy(d));
            }

            Console.WriteLine("Created a list of Thingies, {0}.", StringOperations.ToCommasAndAndedList(thingies.ConvertAll<string>(n => n.ToString())));

            double average = thingies.Average<Thingy>(n => n.DoubleValue);
            double stDev = thingies.StandardDeviation<Thingy>(n => n.DoubleValue);

            Console.WriteLine("Mean = {0}\r\nStDev = {1}", average, stDev);

            for (int i = 0; i < expecteds.Length; i++)
            {
                object state = null;
                IEnumerable<Thingy> boundedThingies = thingies.BoundBySigmas<Thingy>(n => n.DoubleValue, loBounds[i], hiBounds[i], ref state);
                IEnumerable<Thingy> enumerable = boundedThingies as Thingy[] ?? boundedThingies.ToArray();
                int numBoundedThingies = enumerable.Count();
                Assert.Equal(expecteds[i], numBoundedThingies);
            }
        }

        sealed class Thingy
        {
            private string _val;
            private double _dblval;

            public Thingy(double _val)
            {
                _dblval = _val;
                this._val = _dblval.ToString();
            }

            public override string ToString()
            {
                return "\"" + _val + "\"";
            }

            public double DoubleValue
            {
                get
                {
                    return _dblval;
                }
            }
        }
    }
}
