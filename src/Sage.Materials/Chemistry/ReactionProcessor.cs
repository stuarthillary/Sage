/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using Highpoint.Sage.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Debug = System.Diagnostics.Debug;


namespace Highpoint.Sage.Materials.Chemistry
{


    /// <summary>
    /// Delegate implemented by a method that wants to be called when a reaction is added to
    /// or removed from, a ReactionProcessor.
    /// </summary>
    public delegate void ReactionProcessorEvent(ReactionProcessor rxnProcessor, Reaction reaction);

    /// <summary>
    /// A reaction processor knows of a set of chemical reactions, and watches a set of mixtures.
    /// Whenever a material is added to, or removed from, a mixture, the reaction processor examines
    /// that mixture to see if any of the reactions it knows of are capable of occurring. If any are,
    /// then it proceeds to execute that reaction, eliminating the appropriate quantity of reactants,
    /// generating the appropriate quantity of products (or vice versa) and changing the mixture's
    /// thermal characteristics.
    /// </summary>
    public class ReactionProcessor : IHasIdentity, IXmlPersistable
    {

        public event ReactionProcessorEvent? ReactionAddedEvent;
        public event ReactionProcessorEvent? ReactionRemovedEvent;

        private readonly ArrayList _reactions = new ArrayList();
        private readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ReactionProcessor");

        public ReactionProcessor()
        {
        }

        public void AddReaction(Reaction reaction)
        {
            if (!reaction.IsValid)
                throw new ReactionDefinitionException(reaction);
            if (!_reactions.Contains(reaction))
            {
                _reactions.Add(reaction);
                ReactionAddedEvent?.Invoke(this, reaction);
            }
        }

        public void RemoveReaction(Reaction reaction)
        {
            if (_reactions.Contains(reaction))
            {
                _reactions.Remove(reaction);
                ReactionRemovedEvent?.Invoke(this, reaction);
            }
        }

        /// <summary>
        /// Gets the reactions known to this processor as a typed read-only list.
        /// </summary>
        public IReadOnlyList<Reaction> Reactions => _reactions.Cast<Reaction>().ToList().AsReadOnly();

        public Reaction? GetReaction(Guid rxnGuid)
        {
            return _reactions.Cast<Reaction>().FirstOrDefault(rxn => rxn.Guid.Equals(rxnGuid));
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine)
        {
            IMaterial result;
            IReadOnlyList<Reaction> observedReactions;
            IReadOnlyList<ReactionInstance> observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result)
        {
            IReadOnlyList<Reaction> observedReactions;
            IReadOnlyList<ReactionInstance> observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        /// <summary>
        /// Combines the supplied materials and reports the reactions that occurred.
        /// </summary>
        /// <param name="materialsToCombine">The materials to combine.</param>
        /// <param name="observedReactions">The reactions that occurred while combining the materials.</param>
        /// <returns><see langword="true"/> if at least one reaction occurred; otherwise, <see langword="false"/>.</returns>
        public bool CombineMaterials(IMaterial[] materialsToCombine, out IReadOnlyList<Reaction> observedReactions)
        {
            IMaterial result;
            IReadOnlyList<ReactionInstance> observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        /// <summary>
        /// Combines the supplied materials and reports both the resulting material and the observed reactions.
        /// </summary>
        /// <param name="materialsToCombine">The materials to combine.</param>
        /// <param name="result">The resulting combined material.</param>
        /// <param name="observedReactions">The reactions that occurred while combining the materials.</param>
        /// <param name="observedReactionInstances">The specific reaction instances that occurred while combining the materials.</param>
        /// <returns><see langword="true"/> if at least one reaction occurred; otherwise, <see langword="false"/>.</returns>
        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result, out IReadOnlyList<Reaction> observedReactions, out IReadOnlyList<ReactionInstance> observedReactionInstances)
        {
            Mixture scratch = new Mixture(null, "scratch mixture");
            Watch(scratch);
            ReactionCollector rc = new ReactionCollector(scratch);
            foreach (IMaterial material in materialsToCombine)
            {
                scratch.AddMaterial(material);
            }
            observedReactions = rc.Reactions;
            observedReactionInstances = rc.ReactionInstances;
            rc.Disconnect();
            result = scratch;
            return (observedReactions != null && observedReactions.Count > 0);
        }

        public void Watch(IMaterial material)
        {
            material.MaterialChanged += OnMaterialChanged;
        }

        public void Ignore(IMaterial material)
        {
            material.MaterialChanged -= OnMaterialChanged;
        }

        /// <summary>
        /// Gets the reactions in which the specified material participates as either a reactant or a product.
        /// </summary>
        /// <param name="targetMt">The material type to search for.</param>
        /// <returns>A typed read-only list of matching reactions.</returns>
        public IReadOnlyList<Reaction> GetReactionsByParticipant(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Either);
        }

        /// <summary>
        /// Gets the reactions in which the specified material participates as a reactant.
        /// </summary>
        /// <param name="targetMt">The material type to search for.</param>
        /// <returns>A typed read-only list of matching reactions.</returns>
        public IReadOnlyList<Reaction> GetReactionsByReactant(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Reactant);
        }

        /// <summary>
        /// Gets the reactions in which the specified material participates as a product.
        /// </summary>
        /// <param name="targetMt">The material type to search for.</param>
        /// <returns>A typed read-only list of matching reactions.</returns>
        public IReadOnlyList<Reaction> GetReactionsByProduct(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Product);
        }

        private IReadOnlyList<Reaction> GetReactionsByFilter(MaterialType targetMt, Reaction.MaterialRole filter)
        {
            List<Reaction> reactions = new List<Reaction>();
            foreach (Reaction reaction in Reactions)
            {
                if (filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Reactant)
                {
                    foreach (Reaction.ReactionParticipant rp in reaction.Reactants)
                    {
                        if (rp.MaterialType.Equals(targetMt))
                            reactions.Add(reaction);
                    }
                }
                if (filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Product)
                {
                    foreach (Reaction.ReactionParticipant rp in reaction.Products)
                    {
                        if (rp.MaterialType.Equals(targetMt))
                            reactions.Add(reaction);
                    }
                }
            }
            return reactions.AsReadOnly();
        }


        public void OnMaterialChanged(IMaterial material, MaterialChangeType mct)
        {
            if (_diagnostics)
                _Debug.WriteLine("ReactionProcessor notified of change type " + mct + " to material " + material);
            if (mct == MaterialChangeType.Contents)
            {
                Mixture? tmpMixture = material as Mixture;
                if (tmpMixture != null)
                {
                    Mixture mixture = tmpMixture;
                    ReactionInstance? ri = null;
                    if (_diagnostics)
                        _Debug.WriteLine("Processing change type " + mct + " to mixture " + mixture.Name);

                    // If multiple reactions could occur? Only the first happens, but then the next change allows the next reaction, etc.
                    foreach (Reaction reaction in _reactions)
                    {
                        if (ri != null)
                            continue;
                        if (_diagnostics)
                            _Debug.WriteLine("Examining mixture for presence of reaction " + reaction.Name);
                        ri = reaction.React(mixture);
                    }
                }
            }
        }

        public object? Tag
        {
            get; set;
        }

        #region >>> Implementation of IHasIdentity <<<
        private readonly string _name = "Reaction Processor";
        /// <summary>
        /// The name of this reaction processor.
        /// </summary>
        public string Name => _name;

        private readonly string? _description = null;
        /// <summary>
        /// A description of this Reaction Processor.
        /// </summary>
        public string Description => _description ?? _name;

        /// <summary>
        /// The Guid by which this reaction processor will be known.
        /// </summary>
        public Guid Guid { get; } = Guid.Empty;

        #endregion


        private class ReactionCollector
        {
            private readonly List<Reaction> _reactions;
            private readonly List<ReactionInstance> _reactionInstances;
            private readonly Mixture _mixture;
            private readonly ReactionHappenedEvent _reactionHandler;
            public ReactionCollector(Mixture mixture)
            {
                _mixture = mixture;
                _reactions = new List<Reaction>();
                _reactionInstances = new List<ReactionInstance>();
                _reactionHandler = OnReactionHappened;
                _mixture.OnReactionHappened += _reactionHandler;
            }
            public void Disconnect()
            {
                _mixture.OnReactionHappened -= _reactionHandler;
            }

            private void OnReactionHappened(ReactionInstance ri)
            {
                _reactions.Add(ri.Reaction);
                _reactionInstances.Add(ri);
            }

            public IReadOnlyList<Reaction> Reactions => _reactions.AsReadOnly();

            public IReadOnlyList<ReactionInstance> ReactionInstances => _reactionInstances.AsReadOnly();
        }

        #region IXmlPersistable Members

        /// <summary>
        /// Serializes this object to the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Reactions", _reactions);
        }

        /// <summary>
        /// Deserializes this object from the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {

        }

        #endregion
    }
}

