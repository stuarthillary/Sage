#nullable enable
/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Core
{
    /// <summary>
    /// Options for configuring executive threading and causality behavior.
    /// </summary>
    public sealed class ExecutiveOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of worker threads requested from the CLR thread pool.
        /// </summary>
        public int MaxWorkerThreads { get; set; } = 900;

        /// <summary>
        /// Gets or sets the minimum number of worker threads requested from the CLR thread pool.
        /// </summary>
        public int MinWorkerThreads { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum number of I/O completion threads requested from the CLR thread pool.
        /// </summary>
        public int MinIocThreads { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum number of I/O completion threads requested from the CLR thread pool.
        /// </summary>
        public int MaxIocThreads { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to ignore causality violations by clamping events to the current time.
        /// </summary>
        public bool IgnoreCausalityViolations { get; set; } = true;
    }

    /// <summary>
    /// Options for configuring ExecFactory defaults.
    /// </summary>
    public sealed class ExecFactoryOptions
    {
        /// <summary>
        /// Gets or sets the default executive type used when creating new executives.
        /// </summary>
        public string DefaultExecutiveType { get; set; } = "Highpoint.Sage.Core.Executive, Sage";
    }
}

namespace Highpoint.Sage.Diagnostics
{
    /// <summary>
    /// Options for configuring diagnostics flags and behavior.
    /// </summary>
    public sealed class DiagnosticsOptions
    {
        /// <summary>
        /// Gets or sets the diagnostic flags keyed by subsystem name.
        /// </summary>
        public Dictionary<string, bool> Flags { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to log missing diagnostic keys to disk.
        /// </summary>
        public bool LogMissingDiagKeys { get; set; } = false;

        /// <summary>
        /// Gets or sets the optional break time for ExecutiveFastLight temporal debugging.
        /// </summary>
        public DateTime? ExecBreakAt { get; set; } = null;
    }
}


