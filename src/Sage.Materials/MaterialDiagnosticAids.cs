/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials;
using Highpoint.Sage.Materials.Chemistry;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Diagnostics
{
    /// <summary>
    /// Diagnostic helpers for Materials types. Extends <see cref="DiagnosticAids"/>
    /// with overloads that depend on the Materials assembly.
    /// </summary>
    public static class MaterialDiagnosticAids
    {
        private static string _dblFmt = "F1";

        /// <summary>
        /// Provides a human-readable string with the contents of a material.
        /// </summary>
        /// <param name="material">The material whose contents are of interest.</param>
        public static void DumpMaterial(IMaterial material)
        {
            if (material is Mixture mix)
            {
                Dump(mix);
            }
            else if (material is Substance substance)
            {
                Dump(substance);
            }
        }

        private static void Dump(Mixture mix)
        {
            double totalEnergy = 0.0;
            foreach (Substance substance in mix.Constituents)
            {
                double energy = (substance.Temperature + Constants.CELSIUS_TO_KELVIN) * substance.Mass * substance.MaterialType.SpecificHeat;
                totalEnergy += energy;
                _Debug.WriteLine("\t{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
                    substance.MaterialType.Name,
                    substance.Mass.ToString(_dblFmt),
                    substance.Temperature.ToString(_dblFmt),
                    substance.Volume.ToString(_dblFmt),
                    energy.ToString(_dblFmt));
            }
            _Debug.WriteLine("{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
                mix.Name,
                mix.Mass.ToString(_dblFmt),
                mix.Temperature.ToString(_dblFmt),
                mix.Volume.ToString(_dblFmt),
                totalEnergy.ToString(_dblFmt));
            _Debug.WriteLine("");
        }

        private static void Dump(Substance substance)
        {
            double energy = (substance.Temperature + Constants.CELSIUS_TO_KELVIN) * substance.Mass * substance.MaterialType.SpecificHeat;
            _Debug.WriteLine("\t{0} - {1} kg, {2} C, and {3} liters. ({4} Joules of thermal energy)",
                substance.MaterialType.Name,
                substance.Mass.ToString(_dblFmt),
                substance.Temperature.ToString(_dblFmt),
                substance.Volume.ToString(_dblFmt),
                energy.ToString(_dblFmt));
            _Debug.WriteLine("");
        }
    }
}
