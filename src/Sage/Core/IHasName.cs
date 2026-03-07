#nullable enable
/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An interface implemented by anything that is known by a name. The name is not necessarily required to be unique.
    /// </summary>
    public interface IHasName
    {
        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        string Name
        {
            get;
        }
    }


    /// <summary>
    /// A Comparer that is used to sort implementers of IHasIdentity on their names.
    /// </summary>
    public class HasNameComparer : IComparer
    {
        #region IComparer Members
        private IComparer _comparer = Comparer.Default;
        public int Compare(object? x, object? y)
        {
            System.ArgumentNullException.ThrowIfNull(x);
            System.ArgumentNullException.ThrowIfNull(y);
            return _comparer.Compare(((IHasName)x).Name, ((IHasName)y).Name);
        }
        #endregion
    }

    /// <summary>
    /// A Comparer that is used to sort implementers of IHasIdentity on their names.
    /// </summary>
    public class HasNameComparer<T> : System.Collections.Generic.Comparer<T> where T : IHasName
    {
        public override int Compare(T? x, T? y)
        {
            System.ArgumentNullException.ThrowIfNull(x);
            System.ArgumentNullException.ThrowIfNull(y);
            return Comparer.Default.Compare(x.Name, y.Name);
        }
    }
}


