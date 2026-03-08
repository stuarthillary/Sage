/* This source code licensed under the GNU Affero General Public License */
using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Utility
{

    public class HashtableOfListsTester : IDisposable
    {

        public HashtableOfListsTester()
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
            foreach (string str in htol)
                Console.WriteLine(str);

            htol.Remove("Horse", "Arabian");
            htol.Remove("Horse", "Clydesdale");

            foreach (string str in htol)
                Console.WriteLine(str);

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
    }
}