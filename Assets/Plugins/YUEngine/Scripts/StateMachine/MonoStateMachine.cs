using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YU2.StateMachine
{
    public class MonoStateMachine<T>
    {
        private T target;
        private Dictionary<string, MonoState<T>> states;
        private MonoState<T> currState;
        public string startState = "";

#if UNITY_EDITOR
        private bool isBuilt = false;
#endif
        public string GetStateName()
        {
            return currState.stateName;
        }

        public void DoUpdate()
        {
            currState.Update?.Invoke();
        }

        public void DoLateUpdate()
        {
            currState.LateUpdate?.Invoke();
        }

        public void DoFixedUpdate()
        {
            currState.FixedUpdate?.Invoke();
        }

        public void DoOnGui()
        {
            currState.OnGui?.Invoke();
        }

        public void DoOnEnable()
        {
            currState.OnEnable?.Invoke();
        }

        public void DoOnDisable()
        {
            currState.OnDisable?.Invoke();
        }

        public void DoOnTriggerEnter(Collider other)
        {
            currState.OnTriggerEnter?.Invoke(other);
        }

        public void DoOnTriggerStay(Collider other)
        {
            currState.OnTriggerStay?.Invoke(other);
        }

        public void DoOnTriggerExit(Collider other)
        {
            currState.OnTriggerExit?.Invoke(other);
        }

        public MonoStateMachine(T target)
        {
            this.target = target;
            states = new Dictionary<string, MonoState<T>>();
        }

        public void Build()
        {
#if UNITY_EDITOR
            if(isBuilt)
            {
                Debug.LogException(new System.Exception("Intentando reconstruir maquina ya construida."));
                EditorApplication.isPlaying = false;
            }
            Debug.Log("Building Machine");
            if (!states.ContainsKey(startState))
            {
                Debug.LogException(new System.Exception("Estado inexistente en maquina: (" + startState + ")"));
                EditorApplication.isPlaying = false;
            }
            isBuilt = true;
#endif
            foreach (MonoState<T> state in states.Values)
            {
                state.Build(target, this);
            }

        currState = states[startState];
        }

        public void DoStart()
        {

            Debug.Log("Starting machine");
            currState.Begin?.Invoke();
        }

        public void AddState(MonoState<T> state)
        {
#if UNITY_EDITOR
            if (isBuilt)
            {
                Debug.LogException(new System.Exception("Maquina ya construida, imposible agregar estado"));
                EditorApplication.isPlaying = false;
            }
            if (states.ContainsKey(state.stateName))
            {
                Debug.LogException(new System.Exception("Estado inexistente en maquina: (" + state.stateName + ")"));
                EditorApplication.isPlaying = false;
            }
#endif
            state.machine = this;
            states.Add(state.stateName, state);
        }

        public void AddStartState(MonoState<T> state)
        {
            AddState(state);
            this.startState = state.stateName;
        }



        public void TransitionTo(string state)
        {
            currState.OnDisable?.Invoke();
            currState.End?.Invoke();
#if UNITY_EDITOR
            if (!states.ContainsKey(state))
            {
                Debug.LogException(new System.Exception("Estado inexistente en maquina: (" + state + ")"));
                EditorApplication.isPlaying = false;
            }
#endif
            currState = states[state];
            currState.Begin?.Invoke();
            currState.OnEnable?.Invoke();
        }
    }

    public class MonoState<T>
    {
        internal MonoStateMachine<T> machine;
        public string stateName;
        public delegate void BuildFunc(T target, MonoStateMachine<T> machine);
        public delegate void TrgFunc(Collider other);

        public MonoState(string name)
        {
            stateName = name;
        }

        public BuildFunc Build;
        public Action Update, LateUpdate,
            FixedUpdate, OnEnable, OnDisable, OnGui, Begin, End;
        public TrgFunc OnTriggerEnter, OnTriggerExit, OnTriggerStay;

    }

}
