/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class GraphLoopingTester
    {

        #region MSTest Goo
        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        private System.Text.StringBuilder _out;
        private readonly string _loopResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub3 is running.";
        private readonly string _branchResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub3 is running.";

        public GraphLoopingTester()
        {

        }

        [TestMethod]
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

            CreateLoopback(model, sub2.PostVertex, sub2.PreVertex, "LoopbackChannelMarker", 5);

            ArrayList children = new ArrayList();
            children.Add(sub1);
            children.Add(sub2);
            children.Add(sub3);
            root.AddChainOfChildren(children);

            model.Start();

            Assert.IsTrue(_loopResult.Equals(_out.ToString(), StringComparison.Ordinal), "LoopingTester Results", "Looping tester failed to match expected results.");

        }

        [TestMethod]
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

            Edge.Connect(root.PreVertex, sub1.PreVertex);
            Edge.Connect(sub1.PostVertex, sub2.PreVertex).Channel = branchChannelMarker;
            Edge.Connect(sub2.PostVertex, sub1.PreVertex).Channel = branchChannelMarker;
            Edge.Connect(sub1.PostVertex, sub3.PreVertex); //Accept default channel marker.
            Edge.Connect(sub3.PostVertex, root.PostVertex);

            sub2.Channel = "AlternateChannelMarker";
            sub1.PostVertex.EdgeFiringManager = new CountedBranchManager(model, new object[] { branchChannelMarker, Edge.NULL_CHANNEL_MARKER }, new int[] { 2, 1 });
            sub1.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub1.PreVertex);
            sub2.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub2.PreVertex);
            sub3.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub3.PreVertex);

            model.Start();

            Assert.IsTrue(_branchResult.Equals(_out.ToString(), StringComparison.Ordinal), "BranchingTester Results", "Branching tester failed to match expected results.");
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

        [TestMethod]
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
            IList e1PostSuccessors = e1.PostVertex.SuccessorEdges;
            Assert.IsTrue(e1PostSuccessors.Count > 0, "E1.PostVertex should have at least one successor edge after connecting E2 as successor");

            // e2.PreVertex should have e1's post-vertex's outgoing edge in its predecessor edges (PreEdges)
            IList e2PrePredecessors = e2.PreVertex.PredecessorEdges;
            Assert.IsTrue(e2PrePredecessors.Count > 0, "E2.PreVertex should have at least one predecessor edge after connecting E1 as predecessor");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that edges can be directly added to and removed from a Vertex's PreEdges and PostEdges.")]
        public void TestVertexAddAndRemoveEdges()
        {
            Edge principal = new Edge("Principal");
            Vertex v = principal.PreVertex; // Use the auto-created pre-vertex

            Edge extra1 = new Edge("Extra1");
            Edge extra2 = new Edge("Extra2");

            int initialPostCount = v.SuccessorEdges.Count;

            v.AddPostEdge(extra1);
            v.AddPostEdge(extra2);
            Assert.AreEqual(initialPostCount + 2, v.SuccessorEdges.Count,
                "SuccessorEdges count should increase by 2 after adding two post-edges");

            v.RemovePostEdge(extra1);
            Assert.AreEqual(initialPostCount + 1, v.SuccessorEdges.Count,
                "SuccessorEdges count should decrease by 1 after removing one post-edge");
            Assert.IsFalse(v.SuccessorEdges.Contains(extra1), "extra1 should no longer appear in SuccessorEdges");
            Assert.IsTrue(v.SuccessorEdges.Contains(extra2),  "extra2 should still appear in SuccessorEdges");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Phase 2: Verifies that Vertex.PredecessorEdges and SuccessorEdges are typed as IReadOnlyList<Edge>.")]
        public void TestVertexEdgesTypedAsList()
        {
            Edge e = new Edge("TypeCheck");
            // After Phase 2 migration these must be IReadOnlyList<Edge>, not IList (ArrayList-backed).
            Assert.IsInstanceOfType(e.PreVertex.PredecessorEdges, typeof(IReadOnlyList<Edge>),
                "PredecessorEdges should be IReadOnlyList<Edge> after Phase 2 migration");
            Assert.IsInstanceOfType(e.PreVertex.SuccessorEdges, typeof(IReadOnlyList<Edge>),
                "SuccessorEdges should be IReadOnlyList<Edge> after Phase 2 migration");
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
