/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.Graphs.Validity;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Core; // For executive.
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs
{
    public interface IVertex : IVisitable, IXmlPersistable, IHasName, IHasValidity
    {
        Vertex.WhichVertex Role
        {
            get;
        }
        Edge? PrincipalEdge
        {
            get;
        }
        IReadOnlyList<Edge> PredecessorEdges
        {
            get;
        }
        IReadOnlyList<Edge> SuccessorEdges
        {
            get;
        }
        IEdgeFiringManager? EdgeFiringManager
        {
            get;
        }
        IEdgeReceiptManager? EdgeReceiptManager
        {
            get;
        }
        void PreEdgeSatisfied(IDictionary graphContext, Edge theEdge);
        TriggerDelegate? FireVertex
        {
            get;
        }
    }

}

