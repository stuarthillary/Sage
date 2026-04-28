/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.Core;
using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;


namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// 
    /// </summary>

    public class GraphLoopingTester : IDisposable
    {

        #region MSTest Goo

        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
                    GC.SuppressFinalize(this);
        }
        #endregion

        private System.Text.StringBuilder _out;
        private readonly string _loopResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub3 is running.";
        private readonly string _branchResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub3 is running.";

        public GraphLoopingTester()
        {

        }

        [Fact]
        public void TestBasicLooping()
        {

            _out = new System.Text.StringBuilder();

            Model model = new Model("Model");
            model.AddService<ITaskManagementService>(new TaskManagementService());

            Task root = new Task(model, "Root");
            new TaskProcessor(model, "taskProcessor", root); // It is added into the model automatically.

            Edge sub1 = new MyEdge("Sub1", _out);
            Edge sub2 = new MyEdge("Sub2", _out);
            Edge sub3 = new MyEdge("Sub3", _out);

            CreateLoopback(model, (Vertex)sub2.PostVertex!, (Vertex)sub2.PreVertex!, "LoopbackChannelMarker", 5);

            ArrayList children = new ArrayList();
            children.Add(sub1);
            children.Add(sub2);
            children.Add(sub3);
            root.AddChainOfChildren(children);

            model.Start();

            Assert.True(_loopResult.Equals(_out.ToString(), StringComparison.Ordinal), "Looping tester failed to match expected results.");

        }

        [Fact]
        public void TestBasicBranching()
        {
            _out = new System.Text.StringBuilder();
            Model model = new Model("Model");
            model.AddService<ITaskManagementService>(new TaskManagementService());
            object branchChannelMarker = "BranchChannelMarker";

            Task root = new Task(model, "Root");
            new TaskProcessor(model, "taskProcessor", root); // It is added into the model automatically.

            Edge sub1 = new MyEdge("Sub1", _out);
            Edge sub2 = new MyEdge("Sub2", _out);
            Edge sub3 = new MyEdge("Sub3", _out);

            Edge.Connect(root.PreVertex!, sub1.PreVertex!);
            Edge.Connect(sub1.PostVertex!, sub2.PreVertex!)!.Channel = branchChannelMarker;
            Edge.Connect(sub2.PostVertex!, sub1.PreVertex!)!.Channel = branchChannelMarker;
            Edge.Connect(sub1.PostVertex!, sub3.PreVertex!); //Accept default channel marker.
            Edge.Connect(sub3.PostVertex!, root.PostVertex!);

            sub2.Channel = "AlternateChannelMarker";
            ((Vertex)sub1.PostVertex!).EdgeFiringManager = new CountedBranchManager(model, new object[] { branchChannelMarker, Edge.NULL_CHANNEL_MARKER }, new int[] { 2, 1 });
            ((Vertex)sub1.PreVertex!).EdgeReceiptManager = new MultiChannelEdgeReceiptManager((Vertex)sub1.PreVertex!);
            ((Vertex)sub2.PreVertex!).EdgeReceiptManager = new MultiChannelEdgeReceiptManager((Vertex)sub2.PreVertex!);
            ((Vertex)sub3.PreVertex!).EdgeReceiptManager = new MultiChannelEdgeReceiptManager((Vertex)sub3.PreVertex!);

            model.Start();

            Assert.True(_branchResult.Equals(_out.ToString(), StringComparison.Ordinal), "Branching tester failed to match expected results.");
        }

        private void CreateLoopback(IModel model, Vertex from, Vertex to, object channelMarker, int howManyTimes)
        {
            Edge loopback = Edge.Connect(from, to);
            loopback.Channel = channelMarker;
            from.EdgeFiringManager = new CountedBranchManager(model, new object[] { channelMarker, Edge.NULL_CHANNEL_MARKER }, new int[] { howManyTimes, 1 });
            to.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(to);
        }

        // ── Vertex edge-collection tests ─────────────────────────────────────────
        // These tests guard against regressions in the Vertex.PreEdges/PostEdges
        // ArrayList→List<Edge> Phase 2 migration.

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that PreEdges and PostEdges contain the correct edges after graph construction — guards against ArrayList→List migration regressions.")]
        public void TestVertexPreAndPostEdgesAfterConstruction()
        {
            // Each Edge creates Pre and Post vertices automatically, then wires them.
            // Edge.AddSuccessor / Edge.Connect creates ligature edges between vertices.
            Edge e1 = new Edge("E1");
            Edge e2 = new Edge("E2");

            // Connect e1's post-vertex to e2's pre-vertex: e1 → e2
            e2.AddPredecessor(e1);

            // e1.PostVertex should have e2 in its successor edges (PostEdges)
            IReadOnlyList<Edge> e1PostSuccessors = e1.PostVertex!.SuccessorEdges;
            Assert.True(e1PostSuccessors.Count > 0, "E1.PostVertex should have at least one successor edge after connecting E2 as successor");

            // e2.PreVertex should have e1's post-vertex's outgoing edge in its predecessor edges (PreEdges)
            IReadOnlyList<Edge> e2PrePredecessors = e2.PreVertex!.PredecessorEdges;
            Assert.True(e2PrePredecessors.Count > 0, "E2.PreVertex should have at least one predecessor edge after connecting E1 as predecessor");
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that edges can be directly added to and removed from a Vertex's PreEdges and PostEdges.")]
        public void TestVertexAddAndRemoveEdges()
        {
            Edge principal = new Edge("Principal");
            Vertex v = (Vertex)principal.PreVertex!; // Use the auto-created pre-vertex

            Edge extra1 = new Edge("Extra1");
            Edge extra2 = new Edge("Extra2");

            int initialPostCount = v.SuccessorEdges.Count;

            v.AddPostEdge(extra1);
            v.AddPostEdge(extra2);
            Assert.Equal(initialPostCount + 2, v.SuccessorEdges.Count);

            v.RemovePostEdge(extra1);
            Assert.Equal(initialPostCount + 1, v.SuccessorEdges.Count);
            Assert.DoesNotContain(extra1, v.SuccessorEdges);
            Assert.Contains(extra2, v.SuccessorEdges);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 3: Verifies that IVertex exposes predecessor and successor edges as IReadOnlyList<Edge>.")]
        public void TestVertexInterfaceEdgesTypedAsReadOnlyList()
        {
            Edge e1 = new Edge("TypeCheck1");
            Edge e2 = new Edge("TypeCheck2");
            e2.AddPredecessor(e1);

            IVertex preVertex = ((IEdge)e2).PreVertex!;
            IVertex postVertex = ((IEdge)e1).PostVertex!;

            IReadOnlyList<Edge> predecessors = preVertex.PredecessorEdges;
            IReadOnlyList<Edge> successors = postVertex.SuccessorEdges;

            Assert.Single(predecessors);
            Assert.Single(successors);
            Assert.True(((ICollection<Edge>)predecessors).IsReadOnly);
            Assert.True(((ICollection<Edge>)successors).IsReadOnly);
            Assert.Same(e2.PreVertex, predecessors[0].PostVertex);
            Assert.Same(e1.PostVertex, successors[0].PreVertex);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 3: Verifies that Edge and IEdge expose IVertex endpoints and IReadOnlyList<Edge> child edges.")]
        public void TestEdgeInterfaceExposesTypedEndpointsAndChildren()
        {
            Edge parent = new Edge("Parent");
            Edge child = new Edge("Child");
            parent.AddChildEdge(child);

            IEdge edge = parent;

            IVertex preVertex = edge.PreVertex!;
            IVertex postVertex = edge.PostVertex!;
            IReadOnlyList<Edge> childEdges = edge.ChildEdges;

            Assert.Same(parent.PreVertex, preVertex);
            Assert.Same(parent.PostVertex, postVertex);
            Assert.Single(childEdges);
            Assert.Same(child, childEdges[0]);
            Assert.True(((ICollection<Edge>)childEdges).IsReadOnly);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 3: Verifies that Vertex's concrete predecessor/successor collection properties align with the IReadOnlyList<Edge> public API break.")]
        public void TestVertexConcreteEdgesAreTypedAsReadOnlyList()
        {
            PropertyInfo predecessorEdges = typeof(Vertex).GetProperty(nameof(Vertex.PredecessorEdges))!;
            PropertyInfo successorEdges = typeof(Vertex).GetProperty(nameof(Vertex.SuccessorEdges))!;

            Assert.Equal(typeof(IReadOnlyList<Edge>), predecessorEdges.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<Edge>), successorEdges.PropertyType);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 3: Verifies that Edge's concrete endpoints and child collection align with the interface-facing public API break.")]
        public void TestEdgeConcreteApiMatchesInterfaceSurface()
        {
            PropertyInfo preVertex = typeof(Edge).GetProperty(nameof(Edge.PreVertex))!;
            PropertyInfo postVertex = typeof(Edge).GetProperty(nameof(Edge.PostVertex))!;
            PropertyInfo predecessorEdges = typeof(Edge).GetProperty(nameof(Edge.PredecessorEdges))!;
            PropertyInfo successorEdges = typeof(Edge).GetProperty(nameof(Edge.SuccessorEdges))!;
            PropertyInfo childEdges = typeof(Edge).GetProperty(nameof(Edge.ChildEdges))!;

            Assert.Equal(typeof(IVertex), preVertex.PropertyType);
            Assert.Equal(typeof(IVertex), postVertex.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<Edge>), predecessorEdges.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<Edge>), successorEdges.PropertyType);
            Assert.Equal(typeof(IReadOnlyList<Edge>), childEdges.PropertyType);
        }

        sealed class MyEdge : Edge
        {
            private System.Text.StringBuilder _out;
            public MyEdge(string name, System.Text.StringBuilder _out) : base(name)
            {
                this._out = _out;
                this.EdgeStartingEvent += new EdgeEvent(EdgeStarting);
            }
            private void EdgeStarting(IDictionary graphContext, Edge theEdge)
            {
                _out.Append("Edge " + theEdge.Name + " is running.");
            }
        }


    }
}

