/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Linq;
using Xunit;

namespace Highpoint.Sage.Utility
{
    public class WeakListTester
    {
        [Fact]
        public void WeakList_UsesTargetValuesForListOperations()
        {
            WeakList list = new WeakList();

            list.Add("alpha");
            list.Insert(1, "beta");

            Assert.True(list.Contains("alpha"));
            Assert.True(list.Contains("beta"));
            Assert.Equal(0, list.IndexOf("alpha"));
            Assert.Equal(1, list.IndexOf("beta"));

            object[] values = new object[3];
            values[0] = "prefix";
            list.CopyTo(values, 1);

            Assert.Equal(new object[] { "prefix", "alpha", "beta" }, values);

            list.Remove("alpha");

            Assert.False(list.Contains("alpha"));
            Assert.Equal(new[] { "beta" }, list.Cast<object>());
        }

        [Fact]
        public void WeakList_Collapse_RemovesCollectedEntries()
        {
            WeakList list = new WeakList();
            list.Add("keeper");
            WeakReference collectible = AddCollectibleValue(list);

            ForceCollection(collectible);
            list.Collapse();

            Assert.Single(list.Cast<object>());
            Assert.Equal("keeper", list[0]);
        }

        private static WeakReference AddCollectibleValue(WeakList list)
        {
            object payload = new object();
            WeakReference reference = new WeakReference(payload);
            list.Add(payload);
            return reference;
        }

        private static void ForceCollection(params WeakReference[] references)
        {
            for (int i = 0; i < 10; i++)
            {
                if (references.All(reference => !reference.IsAlive))
                    return;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.All(references, reference => Assert.False(reference.IsAlive));
        }
    }
}
