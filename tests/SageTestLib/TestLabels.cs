/* This source code licensed under the GNU Affero General Public License */
using Xunit;
using System;

namespace Highpoint.Sage.Utility
{

    public class LabelTester
    {

        public void BasicTest()
        {
            LabelManager lm1 = new LabelManager();
            LabelManager lm2 = new LabelManager();

            lm1.SetLabel("Plain", null);
            Assert.True(lm1.GetLabel(null).Equals("Plain", StringComparison.Ordinal));

            LabelManager.SetContext("Brown");
            lm1.Label = "Chocolate";

            LabelManager.SetContext("Red");
            lm1.Label = "Cherry";

            LabelManager.SetContext(null);
            Console.WriteLine(lm1.Label);
            Assert.True(lm1.Label.Equals("Plain", StringComparison.Ordinal));

            LabelManager.SetContext("Brown");

            Console.WriteLine(lm1.Label);
            Assert.True(lm1.Label.Equals("Chocolate", StringComparison.Ordinal));

            LabelManager.SetContext("Red");

            Console.WriteLine(lm1.Label);
            Assert.True(lm1.Label.Equals("Cherry", StringComparison.Ordinal));

            LabelManager.SetContext("Orange");

            Console.WriteLine(lm1.GetLabel(null));
            Assert.True(lm1.Label.Equals("", StringComparison.Ordinal));

            lm2.Label = "Bob";

            LabelManager.SetContext("Brown");
            Assert.True(lm2.Label.Equals("", StringComparison.Ordinal));

            LabelManager.SetContext("Orange");
            Assert.True(lm2.Label.Equals("Bob", StringComparison.Ordinal));

        }
    }
}
