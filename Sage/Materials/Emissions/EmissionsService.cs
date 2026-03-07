#nullable disable
/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{

    public class EmissionsService
    {

        private static volatile EmissionsService _instance;
        private static readonly object padlock = new object();
        private static EmissionsServiceOptions _options = new EmissionsServiceOptions();
        public static EmissionsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (padlock)
                    {
                        if (_instance == null)
                            _instance = new EmissionsService(_options);
                    }
                }
                return _instance;
            }
        }

        private readonly Hashtable _models;
        private readonly bool _ignoreUnknownModelTypes;
        private readonly bool _enabled;

        private EmissionsService(EmissionsServiceOptions options = null)
        {
            options ??= new EmissionsServiceOptions();
            _enabled = options.Enabled;
            _ignoreUnknownModelTypes = options.IgnoreUnknownModelTypes;

            string equationSet = string.IsNullOrWhiteSpace(options.EquationSet) ? "CTG" : options.EquationSet;
            EmissionModel.ActiveEquationSet =
                (EmissionModel.EquationSet)Enum.Parse(typeof(EmissionModel.EquationSet), equationSet);

            IReadOnlyList<IEmissionModel> models = options.Models;
            if (models == null)
            {
                if (UnitTestDetector.IsInUnitTest)
                {
                    models = CreateDefaultModels();
                }
                else if (!_enabled)
                {
                    _models = new Hashtable();
                    return;
                }
                else
                {
                    throw new Utility.InitFailureException(_msgMissingConfigSection);
                }
            }

            _models = BuildModelTable(models, options.PermitOverEmission, options.PermitUnderEmission);
        }

        /// <summary>
        /// Configures the emissions service. Call before first use to override defaults.
        /// </summary>
        /// <param name="options">The options to apply.</param>
        public static void Configure(EmissionsServiceOptions options)
        {
            _options = options ?? new EmissionsServiceOptions();
        }

        /// <summary>
        /// Resets the singleton instance so it can be reconfigured.
        /// </summary>
        public static void Reset()
        {
            lock (padlock)
            {
                _instance = null;
            }
        }

        public Hashtable KnownModels => _models;

        public bool Enabled => _enabled;

        public void ProcessEmissions(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            string emissionModelKey,
            Hashtable parameters)
        {

            final = null;
            emission = null;

            IEmissionModel em = (IEmissionModel)_models[emissionModelKey];

            if (em == null)
            {
                if (!_ignoreUnknownModelTypes)
                {
                    string msg = "Emission Model Key \"" + emissionModelKey
                        + "\" does not match known emission model keys, which include ";
                    msg += Utility.StringOperations.ToCommasAndAndedList(new ArrayList(_models.Keys));
                    throw new ApplicationException(msg);
                }
            }
            else
            {
                if (_enabled)
                {
                    em.Process(initial, out final, out emission, modifyInPlace, parameters);
                }
                else
                {
                    final = (Mixture)initial.Clone();
                    emission = new Mixture();
                }

            }
        }

        private static string _msgMissingConfigSection = @"The emissions service was started, but its configuration data is missing.

Configure the service before first use with EmissionsService.Configure(new EmissionsServiceOptions { ... }),
including the emission models to register.";

        private static IReadOnlyList<IEmissionModel> CreateDefaultModels()
        {
            return new IEmissionModel[]
            {
                new AirDryModel(),
                new EvacuateModel(),
                new FillModel(),
                new GasEvolutionModel(),
                new GasSweepModel(),
                new HeatModel(),
                new MassBalanceModel(),
                new NoEmissionModel(),
                new VacuumDistillationModel(),
                new VacuumDistillationWScrubberModel(),
                new VacuumDryModel(),
                new PressureTransferModel()
            };
        }

        private static Hashtable BuildModelTable(IEnumerable<IEmissionModel> models, bool permitOverEmission, bool permitUnderEmission)
        {
            Hashtable table = new Hashtable();
            foreach (IEmissionModel model in models)
            {
                model.PermitOverEmission = permitOverEmission;
                model.PermitUnderEmission = permitUnderEmission;
                foreach (string key in model.Keys)
                {
                    table.Add(key, model);
                }
            }
            return table;
        }
    }
}

