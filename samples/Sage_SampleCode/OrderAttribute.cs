/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Examples
{
    /// <summary>
    /// Specifies the display and execution order for an <see cref="IExample"/> implementation.
    /// Lower values run first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class OrderAttribute : Attribute
    {
        public int Value { get; }
        public OrderAttribute(int value) => Value = value;
    }
}
