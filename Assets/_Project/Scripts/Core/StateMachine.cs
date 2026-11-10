using System;
using System.Collections.Generic;

namespace Gimersia.Core
{
    public sealed class StateMachine
    {
        private IState _currentState;
        private readonly Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>(8);
        private List<Transition> _currentTransitions = new List<Transition>(4);
        private readonly List<Transition> _anyTransitions = new List<Transition>(4);
        private static readonly List<Transition> EmptyTransitions = new List<Transition>(0);

        public IState CurrentState => _currentState;

        public void Tick(float deltaTime)
        {
            Transition transition = GetTransition();
            if (transition != null)
            {
                ChangeState(transition.To);
            }

            _currentState?.OnTick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            _currentState?.OnFixedTick(fixedDeltaTime);
        }

        public void ChangeState(IState newState)
        {
            if (newState == null || ReferenceEquals(newState, _currentState))
            {
                return;
            }

            _currentState?.OnExit();
            _currentState = newState;

            if (!_transitions.TryGetValue(_currentState.GetType(), out _currentTransitions))
            {
                _currentTransitions = EmptyTransitions;
            }

            _currentState.OnEnter();
        }

        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            Type fromType = from.GetType();
            if (!_transitions.TryGetValue(fromType, out List<Transition> stateTransitions))
            {
                stateTransitions = new List<Transition>(4);
                _transitions[fromType] = stateTransitions;
            }

            stateTransitions.Add(new Transition(to, predicate));
        }

        public void AddAnyTransition(IState to, Func<bool> predicate)
        {
            _anyTransitions.Add(new Transition(to, predicate));
        }

        private Transition GetTransition()
        {
            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                if (_anyTransitions[i].Condition())
                {
                    return _anyTransitions[i];
                }
            }

            for (int i = 0; i < _currentTransitions.Count; i++)
            {
                if (_currentTransitions[i].Condition())
                {
                    return _currentTransitions[i];
                }
            }

            return null;
        }

        private sealed class Transition
        {
            public IState To { get; }
            public Func<bool> Condition { get; }

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }
    }
}
