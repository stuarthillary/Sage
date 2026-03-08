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
            Assert.Equal("Plain", lm1.GetLabel(null));

            LabelManager.SetContext("Brown");
            lm1.Label = "Chocolate";

            LabelManager.SetContext("Red");
            lm1.Label = "Cherry";

            LabelManager.SetContext(null);
            Console.WriteLine(lm1.Label);
            Assert.Equal("Plain", lm1.Label);

            LabelManager.SetContext("Brown");

            Console.WriteLine(lm1.Label);
            Assert.Equal("Chocolate", lm1.Label);

            LabelManager.SetContext("Red");

            Console.WriteLine(lm1.Label);
            Assert.Equal("Cherry", lm1.Label);

            LabelManager.SetContext("Orange");

            Console.WriteLine(lm1.GetLabel(null));
            Assert.Equal("", lm1.Label);

            lm2.Label = "Bob";

            LabelManager.SetContext("Brown");
            Assert.Equal("", lm2.Label);

            LabelManager.SetContext("Orange");
            Assert.Equal("Bob", lm2.Label);

        }
    }
}
