#nullable disable
/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs
{
    public class ChannelMonitor
    {
        private readonly IVertex _vertex;
        private readonly object _channelMarker;
        private readonly List<Edge> _myEdges;
        private readonly List<Edge> _preEdgesSatisfied;

        public ChannelMonitor(Vertex vertex, object channelMarker)
        {
            _vertex = vertex;
            _channelMarker = channelMarker;
            _myEdges = new List<Edge>();
            _preEdgesSatisfied = new List<Edge>();
            foreach (Edge e in vertex.PredecessorEdges)
            {
                if (channelMarker.Equals(e.Channel))
                    _myEdges.Add(e);
            }
        }

        public bool RegisterSatisfiedEdge(IDictionary graphContext, Edge edge)
        {
            if (!_myEdges.Contains(edge))
                throw new ApplicationException("Unknown edge (" + edge + ") signaled completion to " + this);

            if (_preEdgesSatisfied.Contains(edge))
                throw new ApplicationException("Edge (" + edge + ") signaled completion twice, to " + this);
            
            _preEdgesSatisfied.Add(edge);

            return (_preEdgesSatisfied.Count == _myEdges.Count);
        }
    }
}

