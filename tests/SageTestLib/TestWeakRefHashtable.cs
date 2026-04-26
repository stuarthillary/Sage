/* This source code licensed under the GNU Affero General Public License */

using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Highpoint.Sage.Utility
{
    using Xunit;
    using System;
    using System.Collections;

    /// <summary>
    /// Summary description for zTestChemistry.
    /// </summary>

    public class WeakReferenceHashtableTester : IDisposable
    {
        public WeakReferenceHashtableTester()
        {
        }


        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
                    GC.SuppressFinalize(this);
        }

        [Fact]
        public void TestWRHTBasics()
        {
            ArrayList keepers = new ArrayList();
            WeakHashtable wht = new WeakHashtable();
            for (int i = 0; i < 15; i++)
            {
                string s = "Object " + i;
                wht.Add(i, s);
                if (i % 3 == 0)
                    keepers.Add(s);
            }
            Console.WriteLine("The following are in the persistent array...");
            foreach (string s in keepers)
                Console.WriteLine(s);
            Console.WriteLine("The following are in the WR Hashtable...");
            //			foreach ( string s in wht.Values ) Console.WriteLine(s);
            foreach (DictionaryEntry de in wht)
            {
                Console.WriteLine(de.Key + ", " + de.Value);
            }
            System.GC.Collect(4);
            Console.WriteLine("Doing GC - now let's see what remains...");
            foreach (DictionaryEntry de in wht)
            {
                Console.WriteLine(de.Key + ", " + de.Value);
            }
            //			foreach ( string s in wht.Values ) Console.WriteLine(s);
        }

        [Fact]
        public void WeakHashtable_Indexer_RemovesCollectedEntryWhenRead()
        {
            WeakHashtable wht = new WeakHashtable();
            WeakReference collectible = AddCollectibleValue(wht, "ephemeral");

            ForceCollection(collectible);

            Assert.True(wht.Contains("ephemeral"));
            Assert.Null(wht["ephemeral"]);
            Assert.False(wht.Contains("ephemeral"));
            Assert.Empty(wht.Keys.Cast<object>());
        }

        [Fact]
        public void WeakHashtable_Values_ReturnsLiveTargetsAndCleansDeadEntries()
        {
            WeakHashtable wht = new WeakHashtable();
            Payload live = new Payload("live");

            wht.Add("live", live);
            WeakReference firstDead = AddCollectibleValue(wht, "dead-1");
            WeakReference secondDead = AddCollectibleValue(wht, "dead-2");

            ForceCollection(firstDead, secondDead);

            object[] values = wht.Values.Cast<object>().ToArray();

            Assert.Equal(new object[] { live }, values);
            Assert.Single(wht.Keys.Cast<object>());
            Assert.True(wht.Contains("live"));
            Assert.False(wht.Contains("dead-1"));
            Assert.False(wht.Contains("dead-2"));

            GC.KeepAlive(live);
        }

        [Fact]
        public void WeakHashtable_Enumeration_SkipsCollectedEntries()
        {
            WeakHashtable wht = new WeakHashtable();
            Payload live = new Payload("live");

            wht.Add("live", live);
            WeakReference collectible = AddCollectibleValue(wht, "dead");

            ForceCollection(collectible);

            IDictionaryEnumerator enumerator = wht.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal("live", enumerator.Key);
            Assert.Same(live, enumerator.Value);
            Assert.False(enumerator.MoveNext());

            GC.KeepAlive(live);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference AddCollectibleValue(WeakHashtable table, object key)
        {
            Payload payload = new Payload(key.ToString()!);
            WeakReference reference = new WeakReference(payload);

            table.Add(key, payload);

            return reference;
        }

        private static void ForceCollection(params WeakReference[] references)
        {
            for (int i = 0; i < 10; i++)
            {
                if (references.All(reference => !reference.IsAlive))
                {
                    return;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.All(references, reference => Assert.False(reference.IsAlive));
        }

        private sealed class Payload
        {
            public Payload(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }
    }
}

