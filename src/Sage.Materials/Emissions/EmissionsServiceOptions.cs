#nullable enable
/* This source code licensed under the GNU Affero General Public License */

using System.Collections.Generic;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// Options for configuring the emissions service.
    /// </summary>
    public sealed class EmissionsServiceOptions
    {
        /// <summary>
        /// Gets or sets the equation set name used by emission models.
        /// </summary>
        public string EquationSet { get; set; } = "CTG";

        /// <summary>
        /// Gets or sets whether unknown model types should be ignored.
        /// </summary>
        public bool IgnoreUnknownModelTypes { get; set; } = false;

        /// <summary>
        /// Gets or sets whether emission processing is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether emission models may over-emit relative to targets.
        /// </summary>
        public bool PermitOverEmission { get; set; } = false;

        /// <summary>
        /// Gets or sets whether emission models may under-emit relative to targets.
        /// </summary>
        public bool PermitUnderEmission { get; set; } = false;

        /// <summary>
        /// Gets or sets the emission models available to the service.
        /// </summary>
        public IReadOnlyList<IEmissionModel>? Models { get; set; } = null;
    }
}
