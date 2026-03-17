/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Examples
{
    /// <summary>
    /// Implemented by all example classes. Reflection discovers them automatically.
    /// </summary>
    internal interface IExample
    {
        void Run();
    }
}
