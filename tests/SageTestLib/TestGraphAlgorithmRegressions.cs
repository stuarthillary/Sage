#nullable enable
/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.Analysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Highpoint.Sage.Graphs
{
    public class GraphAlgorithmRegressionTests
    {
        [Fact]
        public void CpmAnalyst_BranchingGraph_PreservesCriticalPathAndSlip()
        {
            DurationEdge setup = new DurationEdge("Setup", TimeSpan.FromMinutes(2));
            DurationEdge longWork = new DurationEdge("LongWork", TimeSpan.FromMinutes(5));
            DurationEdge shortWork = new DurationEdge("ShortWork", TimeSpan.FromMinutes(1));
            DurationEdge finish = new DurationEdge("Finish", TimeSpan.FromMinutes(3));

            setup.AddSuccessor(longWork);
            setup.AddSuccessor(shortWork);
            longWork.AddSuccessor(finish);
            shortWork.AddSuccessor(finish);

            TestableCpmAnalyst analyst = new TestableCpmAnalyst(setup, (Vertex)setup.PreVertex!, (Vertex)finish.PostVertex!);

            analyst.Analyze();

            Assert.True(analyst.Analyzed);

            Assert.Equal(TimeSpan.Zero.Ticks, analyst.GetEarliestStart(setup));
            Assert.Equal(TimeSpan.FromMinutes(2).Ticks, analyst.GetEarliestFinish(setup));
            Assert.Equal(TimeSpan.FromMinutes(2).Ticks, analyst.GetEarliestStart(longWork));
            Assert.Equal(TimeSpan.FromMinutes(7).Ticks, analyst.GetEarliestFinish(longWork));
            Assert.Equal(TimeSpan.FromMinutes(2).Ticks, analyst.GetEarliestStart(shortWork));
            Assert.Equal(TimeSpan.FromMinutes(3).Ticks, analyst.GetEarliestFinish(shortWork));
            Assert.Equal(TimeSpan.FromMinutes(7).Ticks, analyst.GetEarliestStart(finish));
            Assert.Equal(TimeSpan.FromMinutes(10).Ticks, analyst.GetEarliestFinish(finish));

            Assert.Equal(TimeSpan.FromMinutes(2).Ticks, analyst.GetLatestStart(longWork));
            Assert.Equal(TimeSpan.FromMinutes(6).Ticks, analyst.GetLatestStart(shortWork));
            Assert.Equal(TimeSpan.FromMinutes(4).Ticks, analyst.GetAcceptableSlip(shortWork));

            Assert.True(analyst.IsCriticalPath(setup));
            Assert.True(analyst.IsCriticalPath(longWork));
            Assert.True(analyst.IsCriticalPath(finish));
            Assert.False(analyst.IsCriticalPath(shortWork));
        }

        [Fact]
        public void PertAnalyst_BranchingGraph_UsesLigaturesAndReturnsReadOnlyCriticalPath()
        {
            PertEdge setup = new PertEdge("Setup", 1, 2, 3);
            PertEdge longWork = new PertEdge("LongWork", 3, 4, 5);
            PertEdge shortWork = new PertEdge("ShortWork", 1, 1, 1);
            PertEdge finish = new PertEdge("Finish", 5, 6, 7);

            setup.AddSuccessor(longWork);
            setup.AddSuccessor(shortWork);
            longWork.AddSuccessor(finish);
            shortWork.AddSuccessor(finish);

            TestablePertAnalyst analyst = new TestablePertAnalyst(setup, (Vertex)setup.PreVertex!, (Vertex)finish.PostVertex!);

            analyst.Analyze();

            Assert.Equal(new Edge[] { setup, longWork, finish }, analyst.CriticalPath.Cast<Edge>());
            Assert.Throws<NotSupportedException>(() => analyst.CriticalPath.Add(shortWork));

            Assert.Equal(TimeSpan.FromMinutes(12), analyst.CriticalPathMean);

            long varianceTicks = (long)Math.Sqrt(
                Math.Pow(TimeSpan.FromMinutes(2).Ticks, 2) * 3);

            Assert.Equal(TimeSpan.FromTicks(varianceTicks), analyst.CriticalPathVariance);
        }

        [Fact]
        public void DagDeadlockChecker_DuplicateSuccessors_DoNotCreateFalseDeadlocks()
        {
            Edge root = new Edge("Root");
            Edge branchA = new Edge("BranchA");
            Edge branchB = new Edge("BranchB");
            Edge join = new Edge("Join");

            StubDagDeadlockChecker checker = new StubDagDeadlockChecker(
                root,
                new Dictionary<object, object[]>
                {
                    [root.PreVertex!] = new object[] { branchA, branchB },
                    [branchA] = new object[] { join, join },
                    [branchB] = new object[] { join },
                    [join] = Array.Empty<object>()
                });

            Assert.True(checker.Check());
            Assert.Empty(checker.Errors);
            Assert.True(checker.Check());
        }

        [Fact]
        public void DagDeadlockChecker_CycleReportsSingleResidualFrontierTargetWithoutDuplicates()
        {
            Edge root = new Edge("Root");
            Edge first = new Edge("First");
            Edge second = new Edge("Second");

            StubDagDeadlockChecker checker = new StubDagDeadlockChecker(
                root,
                new Dictionary<object, object[]>
                {
                    [root.PreVertex!] = new object[] { first },
                    [first] = new object[] { second, second },
                    [second] = new object[] { first }
                });

            Assert.False(checker.Check());
            Assert.Single(checker.Errors);

            DagStructureError error = Assert.IsType<DagStructureError>(((IList)checker.Errors)[0]);
            IList targets = Assert.IsAssignableFrom<IList>(error.Target);

            Assert.Equal("A deadlock was detected in the graph.", error.Narrative);
            Assert.Single(targets);
            Assert.Same(first, targets[0]);

            IList readOnlyErrors = Assert.IsAssignableFrom<IList>(checker.Errors);
            Assert.True(readOnlyErrors.IsReadOnly);
        }

        private sealed class TestableCpmAnalyst : CpmAnalyst
        {
            public TestableCpmAnalyst(Edge seedEdge, Vertex start, Vertex finish)
                : base(seedEdge)
            {
                Start = start;
                Finish = finish;
                Reset();
            }
        }

        private sealed class TestablePertAnalyst : PertAnalyst
        {
            public TestablePertAnalyst(Edge seedEdge, Vertex start, Vertex finish)
                : base(seedEdge)
            {
                Start = start;
                Finish = finish;
                Reset();
            }
        }

        private sealed class DurationEdge : Edge, ISupportsCpmAnalysis
        {
            private readonly TimeSpan _duration;

            public DurationEdge(string name, TimeSpan duration)
                : base(name)
            {
                _duration = duration;
            }

            public TimeSpan GetNominalDuration() => _duration;
        }

        private sealed class PertEdge : Edge, ISupportsPertAnalysis
        {
            private readonly TimeSpan _optimistic;
            private readonly TimeSpan _nominal;
            private readonly TimeSpan _pessimistic;

            public PertEdge(string name, double optimisticMinutes, double nominalMinutes, double pessimisticMinutes)
                : base(name)
            {
                _optimistic = TimeSpan.FromMinutes(optimisticMinutes);
                _nominal = TimeSpan.FromMinutes(nominalMinutes);
                _pessimistic = TimeSpan.FromMinutes(pessimisticMinutes);
            }

            public TimeSpan GetNominalDuration() => _nominal;

            public TimeSpan GetOptimisticDuration() => _optimistic;

            public TimeSpan GetPessimisticDuration() => _pessimistic;
        }

        private sealed class StubDagDeadlockChecker : DagDeadlockChecker
        {
            private readonly IReadOnlyDictionary<object, object[]> _graph;

            public StubDagDeadlockChecker(Edge rootEdge, IReadOnlyDictionary<object, object[]> graph)
                : base(rootEdge)
            {
                _graph = graph;
            }

            protected override ArrayList GetSuccessors(object element)
            {
                return _graph.TryGetValue(element, out object[]? successors)
                    ? new ArrayList(successors)
                    : new ArrayList();
            }
        }
    }
}
