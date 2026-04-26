/* This source code licensed under the GNU Affero General Public License */
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Highpoint.Sage.Utility
{

    public class HashtableOfListsTester : IDisposable
    {

        public HashtableOfListsTester()
        {
            Init();
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
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestHTOL()
        {

            HashtableOfLists htol = new HashtableOfLists();

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Arabian");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("Test before removal...");
            List<string> beforeRemoval = htol.Cast<string>().ToList();
            foreach (string str in beforeRemoval)
                Console.WriteLine(str);

            Assert.Equal(5, beforeRemoval.Count);
            Assert.Contains("Pot-bellied", beforeRemoval);
            Assert.Equal(5L, htol.Count);
            Assert.Equal(5, htol.Values.Count);

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            List<string> afterRemoval = htol.Cast<string>().ToList();
            foreach (string str in afterRemoval)
                Console.WriteLine(str);

            Assert.Equal(3, afterRemoval.Count);
            Assert.DoesNotContain("Arabian", afterRemoval);
            Assert.DoesNotContain("Clydesdale", afterRemoval);
            Assert.False(htol.ContainsKey("Horse"));

        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestHTOLTemplate()
        {

            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>();

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Arabian");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol)
            {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys)
            {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol)
            {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys)
            {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }
        }
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the HashtableOfLists.")]
        public void TestSortedHTOLTemplate()
        {

            IComparer<String> strComp = Comparer<string>.Default;
            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>(strComp);

            htol.Add("Dog", "Collie");
            htol.Add("Pig", "Pot-bellied");
            htol.Add("Horse", "Clydesdale");
            htol.Add("Horse", "Arabian");
            htol.Add("Dog", "Chihuahua");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol)
            {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys)
            {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            Console.WriteLine("\r\nSequential dump.");
            foreach (string str in htol)
            {
                Console.WriteLine(str);
            }

            Console.WriteLine("\r\nList dump.");
            foreach (string key in htol.Keys)
            {
                Console.WriteLine(key + " --> " + StringOperations.ToCommasAndAndedList(htol[key]));
            }
        }

        [Fact]
        public void HashtableOfLists_NonGeneric_DoesNotDuplicateSameItemForKey()
        {
            HashtableOfLists htol = new HashtableOfLists();

            htol.Add("Dog", "Collie");
            htol.Add("Dog", "Collie");

            Assert.True(htol.ContainsKey("Dog"));
            Assert.Equal(1L, htol.Count);
            Assert.Single(htol["Dog"].Cast<object>());
            Assert.Equal("Collie", htol["Dog"][0]);
        }

        [Fact]
        public void HashtableOfLists_NonGeneric_PrunesEmptyWrappedKeysDuringEnumeration()
        {
            HashtableOfLists htol = new HashtableOfLists();

            htol.Add("Horse", "Arabian");
            htol.Add("Horse", "Clydesdale");

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            Assert.True(htol.ContainsKey("Horse"));
            Assert.Equal(0L, htol.Count);
            Assert.Empty(htol["Horse"].Cast<object>());

            _ = htol.Cast<object>().ToList();

            Assert.False(htol.ContainsKey("Horse"));
            Assert.Empty(htol["Horse"].Cast<object>());
        }

        [Fact]
        public void HashtableOfLists_Generic_PreservesDuplicateValuesUntilExplicitlyRemoved()
        {
            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>();

            htol.Add("Dog", "Collie");
            htol.Add("Dog", "Collie");

            Assert.Equal(2L, htol.Count);
            Assert.Equal(new[] { "Collie", "Collie" }, htol["Dog"]);

            Assert.True(htol.Remove("Dog", "Collie"));
            Assert.Equal(new[] { "Collie" }, htol["Dog"]);

            Assert.True(htol.Remove("Dog", "Collie"));
            Assert.True(htol.ContainsKey("Dog"));
            Assert.Empty(htol["Dog"]);

            htol.PruneEmptyLists();

            Assert.False(htol.ContainsKey("Dog"));
        }

        [Fact]
        public void HashtableOfLists_Generic_SortsEachKeyWhenComparerProvided()
        {
            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>(Comparer<string>.Default);

            htol.Add("Dog", "Collie");
            htol.Add("Dog", "Chihuahua");
            htol.Add("Dog", "Akita");

            Assert.Equal(new[] { "Akita", "Chihuahua", "Collie" }, htol["Dog"]);
        }

        [Fact]
        public void HashtableOfLists_Generic_ImplementsCollectionContract()
        {
            HashtableOfLists<string, string> htol = new HashtableOfLists<string, string>();
            htol.Add("Dog", "Collie");
            htol.Add("Dog", "Chihuahua");

            ICollection<KeyValuePair<string, List<string>>> collection = htol;
            KeyValuePair<string, List<string>>[] entries = new KeyValuePair<string, List<string>>[1];
            collection.CopyTo(entries, 0);

            KeyValuePair<string, List<string>> entry = Assert.Single(entries);
            Assert.Equal("Dog", entry.Key);
            Assert.Equal(new[] { "Collie", "Chihuahua" }, entry.Value);
            Assert.True(collection.Remove(entry));
            Assert.False(htol.ContainsKey("Dog"));
            Assert.False(htol.Remove("Pig", "Pot-bellied"));
        }
    }
}
