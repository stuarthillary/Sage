/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    internal delegate List<ExecEvent> FilterMethod(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison);

    internal class ExecEventRemover
    {
        private readonly IExecEventSelector _ees = null;
        private readonly long _eventId;
        private readonly object _target = null;
        private readonly FilterMethod _filterMethod;

        public ExecEventRemover(IExecEventSelector ees)
        {
            _ees = ees;
            _filterMethod = new FilterMethod(FilterOnFullData);
        }
        public ExecEventRemover(long eventId)
        {
            _eventId = eventId;
            _filterMethod = new FilterMethod(FilterOnEventId);
        }

        public ExecEventRemover(Delegate target)
        {
            _target = target;
            _filterMethod = new FilterMethod(FilterOnDelegateAll);
        }

        public ExecEventRemover(object target)
        {
            _target = target;
            _filterMethod = new FilterMethod(FilterOnTargetAll);
        }

        public List<ExecEvent> Filter(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            return _filterMethod(events, comparison);
        }

        private List<ExecEvent> FilterOnFullData(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            List<ExecEvent> remainingEvents = new List<ExecEvent>(events.Count);
            foreach (ExecEvent ee in events)
            {
                if (!_ees.SelectThisEvent(ee.ExecEventReceiver, ee.When, ee.Priority, ee.UserData, ee.EventType))
                {
                    remainingEvents.Add(ee);
                }
            }
            return remainingEvents;
        }

        private List<ExecEvent> FilterOnEventId(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            bool removed = false;
            List<ExecEvent> remainingEvents = new List<ExecEvent>(events.Count);
            foreach (ExecEvent ee in events)
            {
                if (!removed && ee.Key == _eventId)
                {
                    removed = true;
                    continue;
                }
                remainingEvents.Add(ee);
            }

            if (!removed)
            {
                throw new ApplicationException("Attempted to remove an event from the executive by its event ID (" + _eventId + "), where that event ID was not in the event list.");
            }

            return remainingEvents;
        }

        private List<ExecEvent> FilterOnTarget(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            ExecEvent eventToRemove = FindFirstMatchingEvent(events, comparison, MatchesTarget);
            return RemoveSingleEvent(events, eventToRemove);
        }

        private List<ExecEvent> FilterOnDelegate(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            ExecEvent eventToRemove = FindFirstMatchingEvent(events, comparison, MatchesDelegate);
            return RemoveSingleEvent(events, eventToRemove);
        }

        private List<ExecEvent> FilterOnTargetAll(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            List<ExecEvent> remainingEvents = new List<ExecEvent>(events.Count);
            Type soughtTargetType = _target.GetType();
            Type eventTargetType;

            foreach (ExecEvent ee in events)
            {
                if (ee.ExecEventReceiver.Target is DetachableEvent)
                {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                    eventTargetType = eer.Target.GetType();
                }
                else
                {
                    // The callback could be static, so if it is, then we need the targetType a different way.
                    eventTargetType = ee.ExecEventReceiver.Target == null ? ee.ExecEventReceiver.Method.ReflectedType : ee.ExecEventReceiver.Target.GetType();
                }

                // We're comparing at the object level - we can't compare any higher, since we
                // have no control over what kinds of objects we may be comparing. To avoid an
                // invalid cast exception, we treat them both as objects.
                //if ( object.Equals(eventTarget,m_target) ) {
                if (!eventTargetType.Equals(soughtTargetType))
                {
                    remainingEvents.Add(ee);
                }
            }

            return remainingEvents;
        }

        private List<ExecEvent> FilterOnDelegateAll(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison)
        {
            List<ExecEvent> remainingEvents = new List<ExecEvent>(events.Count);
            object eventTarget;
            foreach (ExecEvent ee in events)
            {
                DetachableEvent de = ee.ExecEventReceiver.Target as DetachableEvent;
                if (de != null)
                {
                    eventTarget = de.RootEvent.ExecEventReceiver.Target;
                }
                else
                {
                    eventTarget = ee.ExecEventReceiver;
                }

                if (!((Delegate)eventTarget).Equals((Delegate)_target))
                {
                    remainingEvents.Add(ee);
                }
            }

            return remainingEvents;
        }

        private static ExecEvent FindFirstMatchingEvent(IReadOnlyList<ExecEvent> events, Comparison<ExecEvent> comparison, Predicate<ExecEvent> predicate)
        {
            List<ExecEvent> orderedEvents = new List<ExecEvent>(events);
            orderedEvents.Sort(comparison);
            foreach (ExecEvent ee in orderedEvents)
            {
                if (predicate(ee))
                    return ee;
            }
            return null;
        }

        private static List<ExecEvent> RemoveSingleEvent(IReadOnlyList<ExecEvent> events, ExecEvent eventToRemove)
        {
            if (eventToRemove == null)
                return new List<ExecEvent>(events);

            List<ExecEvent> remainingEvents = new List<ExecEvent>(events.Count - 1);
            foreach (ExecEvent ee in events)
            {
                if (!ReferenceEquals(ee, eventToRemove))
                    remainingEvents.Add(ee);
            }
            return remainingEvents;
        }

        private bool MatchesTarget(ExecEvent ee)
        {
            object eventTarget;
            if (ee.ExecEventReceiver.Target is DetachableEvent)
            {
                ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                eventTarget = eer.Target;
            }
            else
            {
                eventTarget = ee.ExecEventReceiver.Target;
            }

            // We're comparing at the object level - we can't compare any higher, since we
            // have no control over what kinds of objects we may be comparing. To avoid an
            // invalid cast exception, we treat them both as objects.
            return Equals(eventTarget, _target);
        }

        private bool MatchesDelegate(ExecEvent ee)
        {
            object eventTarget;
            if (ee.ExecEventReceiver.Target is DetachableEvent)
            {
                ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                eventTarget = eer;
            }
            else
            {
                eventTarget = ee.ExecEventReceiver;
            }

            return ((Delegate)eventTarget).Equals((Delegate)_target);
        }
    }
}
