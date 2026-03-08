/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;

namespace Highpoint.Sage.Utility
{
    public static class UnitTestDetector
    {
        // Checked lazily each call so late-loaded test runner assemblies are detected.
        public static bool IsInUnitTest =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.FullName is string name &&
                     (name.StartsWith("Microsoft.VisualStudio.TestPlatform.TestFramework", StringComparison.Ordinal) ||
                      name.StartsWith("xunit.core", StringComparison.Ordinal) ||
                      name.StartsWith("xunit.execution", StringComparison.Ordinal)));
    }


}

