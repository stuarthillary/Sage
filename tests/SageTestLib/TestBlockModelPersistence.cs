/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.Core;
using System;
using Xunit;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Blocks
{

    /// <summary>BlockModelPersistence.
    /// </summary>

    public class BlockModelTester : IDisposable
    {

        #region MSTest Goo

        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        public BlockModelTester()
        {
        }

        [Fact]
        public void TestBlockModelPersistence()
        {

            Model model = new Model();

            model.RandomServer = new Randoms.RandomServer(12345, 100);



        }
    }
}
