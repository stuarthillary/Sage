// nullable enabled
/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{

    /// <summary>
    /// An Expression is a class that contains a list of expression elements, a sequence of text snippets,
    /// references to things that have names (i.e. steps &amp; transitions), and macros. 
    /// </summary>
    public class Expression : ExpressionElement
    {

        private class UnknownReferenceElement : ExpressionElement
        {
            private Guid _guid = Guid.Empty;
            public UnknownReferenceElement(Guid guid)
            {
                _guid = guid;
            }
            public override Guid Guid
            {
                get
                {
                    return _guid;
                }
            }

            public override string ToString(ExpressionType t, object? forWhom)
            {
                throw new Exception("Directory was unable to map an expression element to the guid, " + _guid + ".");
            }
        }

        #region Private Fields
        private bool _hasUnknowns = false;
        private ParticipantDirectory? _participantDirectory;
        private List<ExpressionElement> _elements = null!; // assigned in factory methods before use
        private object? _owner;
        private static Regex _singQuotes =
            new Regex(@" \'                   " +
                      @"   (?>                " +
                      @"       [^\']+         " +
                      @"     |                " +
                      @"       \' (?<DEPTH>)  " +
                      @"     |                " +
                      @"       \' (?<-DEPTH>) " +
                      @"   )*                 " +
                      @"   (?(DEPTH)(?!))     " +
                      @" \'                   ", RegexOptions.IgnorePatternWhitespace);
        private static Regex _guidFinder =
            new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
        #endregion 

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Expression"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        private Expression(object owner)
        {
            _elements = new List<ExpressionElement>();
            _owner = owner;
        }

        /// <summary>
        /// Creates an Expression from a user-friendly representation of the expression. In that representation, 
        /// a macro is expressed with a leading &quot;'μ&quot;, as in 'μPreviousComplete' ...
        /// </summary>
        /// <param name="uf">Thr user-friendly representation of the expression from which it will be created.</param>
        /// <param name="directory">The directory into which the Expression Elements that are created from this
        /// representation will be stored.</param>
        /// <param name="owner">The owner of the expression - usually, the Transition to which it is attached.</param>
        /// <returns>The newly-created expression.</returns>
        public static Expression FromUf(string uf, ParticipantDirectory directory, object owner)
        {
            Expression expression = new Expression(owner);
            expression._participantDirectory = directory;

            int cursor = 0;
            foreach (Match match in _singQuotes.Matches(uf))
            {
                if (match.Value.StartsWith(Macro.MACRO_START, StringComparison.Ordinal))
                {
                    Macro macro = (Macro)directory[match.Value.Trim('\'')]!; // macro is registered before parsing
                    expression._elements.Add(macro);
                    cursor = match.Index + match.Value.Length;
                }
                else
                {
                    expression._elements.Add(new RoteString(uf.Substring(cursor, (match.Index - cursor + 1))));
                    string token = match.Value.Substring(1, match.Value.Length - 2);
                    if (token.Contains("/", StringComparison.Ordinal))
                    {
                        int slashNdx = token.IndexOf('/', StringComparison.Ordinal);
                        expression._elements.Add(directory.RegisterMapping(token.Substring(0, slashNdx)));
                        expression._elements.Add(new RoteString(token.Substring(slashNdx)));
                        cursor = match.Index + match.Value.Length - 1;
                    }
                    else
                    {
                        expression._elements.Add(directory.RegisterMapping(token));
                        expression._elements.Add(new RoteString(match.Value.Substring(match.Value.Length - 1, 1)));
                        cursor = match.Index + match.Value.Length;
                    }
                }
            }

            if (cursor != uf.Length)
            {
                expression._elements.Add(new RoteString(uf.Substring(cursor)));
            }

            List<ExpressionElement> tmp = new List<ExpressionElement>(expression._elements);
            expression._elements.Clear();

            #region Consolidate sequential RoteString elements.

            StringBuilder sb = new StringBuilder();
            foreach (ExpressionElement ee in tmp)
            {
                if (!ee.GetType().Equals(typeof(RoteString)))
                {
                    if (sb.Length > 0)
                    {
                        expression._elements.Add(new RoteString(sb.ToString()));
                        sb = new StringBuilder();
                    }

                    expression._elements.Add(ee);

                }
                else
                {
                    sb.Append(ee);
                }
            }

            if (sb.Length > 0)
            {
                expression._elements.Add(new RoteString(sb.ToString()));
            }

            #endregion

            return expression;
        }

        /// <summary>
        /// Creates an Expression from a user-hostile representation of the expression. In that representation,
        /// we have guids and we have rote strings.
        /// </summary>
        /// <param name="uh">The user-hostile representation of the expression.</param>
        /// <param name="directory">The directory from which the Expression Elements that are referenced here by guid, come.</param>
        /// <param name="owner">The owner of the expression - usually, the Transition to which it is attached.</param>
        /// <returns>The newly-created expression.</returns>
        public static Expression FromUh(string uh, ParticipantDirectory directory, object owner)
        {
            Expression expression = new Expression(owner);
            expression._participantDirectory = directory;

            int cursor = 0;
            foreach (Match match in _guidFinder.Matches(uh))
            {

                if (match.Index != cursor)
                {
                    expression._elements.Add(new RoteString(uh.Substring(cursor, (match.Index - cursor))));
                    cursor = match.Index;
                }

                Guid guid = new Guid(match.Value);
                cursor += match.Value.Length;

                if (directory.Contains(guid))
                {
                    expression._elements.Add(directory[guid]!); // Contains check guarantees non-null
                }
                else
                {
                    expression._elements.Add(new UnknownReferenceElement(guid));
                    expression._hasUnknowns = true;
                }
            }

            if (cursor != uh.Length)
            {
                expression._elements.Add(new RoteString(uh.Substring(cursor)));
            }

            return expression;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return ToString(ExpressionType.Friendly, null);
        }

        /// <summary>
        /// Returns the string for this macro that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the macro, usually a Transition.</param>
        /// <returns>The string for this macro.</returns>
        public override string ToString(ExpressionType t, object? forWhom)
        {

            if (_hasUnknowns)
            {
                ResolveUnknowns();
            }

            StringBuilder sb = new StringBuilder();
            foreach (ExpressionElement ee in _elements)
            {
                sb.Append(ee.ToString(t, forWhom));
            }

            return sb.ToString();
        }

        internal void ResolveUnknowns()
        {
            if (_hasUnknowns)
            {
                List<ExpressionElement> temp = _elements;
                _elements = new List<ExpressionElement>();

                foreach (ExpressionElement ee in temp)
                {
                    if (ee is UnknownReferenceElement)
                    {
                        Guid guid = ee.Guid;
                        if (_participantDirectory!.Contains(guid)) // non-null: set during FromUf/FromUh before ResolveUnknowns
                        {
                            _elements.Add(_participantDirectory![guid]!); // Contains check guarantees non-null
                        }
                        else
                        {
                            IPfcTransitionNode? trans = _owner as IPfcTransitionNode;
                            string msg;
                            if (trans != null)
                            {
                                msg = string.Format("Failed to map Guid {0} into an object on behalf of {1} in Pfc {2}.", guid, trans.Name, trans.Parent!.Name); // Parent non-null for any real transition
                            }
                            else
                            {
                                msg = string.Format("Failed to map Guid {0} into an object on behalf of {1}.", guid, _owner);
                            }
                            throw new ApplicationException(msg);
                        }
                    }
                    else
                    {
                        _elements.Add(ee);
                    }
                }
                _hasUnknowns = false;
            }
        }

        internal List<ExpressionElement> Elements
        {
            get
            {
                if (_elements != null)
                {
                    return _elements;
                }
                else
                {
                    List<ExpressionElement> le = new List<ExpressionElement>();
                    le.Add(this);
                    return le;
                }
            }
        }
    }
}
