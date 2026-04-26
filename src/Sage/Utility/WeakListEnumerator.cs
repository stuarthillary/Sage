/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility
{
    internal class WeakListEnumerator : IEnumerator
    {
        private readonly IList<MyWeakReference> _list;
        private int _cursor;
        public WeakListEnumerator(IList<MyWeakReference> list)
        {
            _list = list;
            _cursor = -1;
        }
        #region IEnumerator Members

        public void Reset()
        {
            _cursor = -1;
        }

        public object? Current
        {
            get
            {
                if (_cursor == -1)
                    throw new ApplicationException("Called Current on an enumerator without first having called MoveNext.");
                return _list[_cursor].Target;
            }
        }

        public bool MoveNext()
        {
            if (_cursor == (_list.Count - 1))
                return false;
            _cursor++;
            return true;
        }

        #endregion
    }
}
