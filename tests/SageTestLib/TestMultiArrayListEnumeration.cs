/* This source code licensed under the GNU Affero General Public License */
using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Summary description for zTestInterpolations.
    /// </summary>

    public class MultiArrayListEnumerationTester : IDisposable
    {
        private readonly ArrayList _al1;
        private readonly ArrayList _al2;
        private readonly ArrayList _al3;
        private readonly ArrayList _ale;
        private static readonly string _expected123 = "AlphaBravoCharleyDeltaEchoFoxtrotGolfHotelIndia";
        public MultiArrayListEnumerationTester()
        {
            Init();
            _al1 = new ArrayList();
            _al1.Add("Alpha");
            _al1.Add("Bravo");
            _al1.Add("Charley");
            _al2 = new ArrayList();
            _al2.Add("Delta");
            _al2.Add("Echo");
            _al2.Add("Foxtrot");
            _al3 = new ArrayList();
            _al3.Add("Golf");
            _al3.Add("Hotel");
            _al3.Add("India");
            _ale = new ArrayList();
        }


        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
                    GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Basic test.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists under one enumerator.")]
        public void TestBasicsOfEnumerator()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string s in Enumerate(_al1, _al2, _al3))
                sb.Append(s);

            string result = sb.ToString();
            Console.WriteLine("Simple three list aggregation - " + result + ".");
            Assert.True(result.Equals(_expected123, StringComparison.Ordinal), "Failed test");
        }
        /// <summary>
        /// Basic test.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists and one empty one in various locations under one enumerator.")]
        public void TestEnumeratorWithEmptyArrays()
        {
            Validate(new ArrayList[] { _ale, _al1, _al2, _al3 }, _expected123, "Leading", "Empty Arraylist at leading element of arraylists.");
            Validate(new ArrayList[] { _al1, _ale, _al2, _al3 }, _expected123, "Internal", "Empty Arraylist at internal element of arraylists.");
            Validate(new ArrayList[] { _al1, _al2, _al3, _ale }, _expected123, "Trailing", "Empty Arraylist at trailing element of arraylists.");
        }

        private void Validate(ArrayList[] arraylists, string expected, string name, string description)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string s in Enumerate(arraylists))
                sb.Append(s);
            string result = sb.ToString();
            Console.WriteLine(name + "\r\n\texpected = \"" + expected + "\",\r\n\tresult   = \"" + result + "\".\r\n\t\t" + (result.Equals(expected, StringComparison.Ordinal) ? "Passed.\r\n" : "Failed.\r\n"));
            Assert.True(result.Equals(expected, StringComparison.Ordinal), "Failed test");
        }

        private static IEnumerable<string> Enumerate(params ArrayList[] arraylists)
        {
            return arraylists.SelectMany(arraylist => arraylist.Cast<string>());
        }
    }
}

