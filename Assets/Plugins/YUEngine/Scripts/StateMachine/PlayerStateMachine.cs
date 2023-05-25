using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YU2.StateMachine
{
    public class PlayerStateMachine<T>
    {
        private T target;
        private Dictionary<string, PlayerState<T>> states;
        private PlayerState<T> currState;
        public string startState = "";

#if UNITY_EDITOR
        private bool isBuilt = false;
#endif

        public string currentState { get { return currState.stateName; } }

        public string StateName => currState.stateName;

        public void DoUpdate()
        {
            currState.Update?.Invoke();
        }

        public void DoBeforeCol()
        {
            currState.BeforeCol?.Invoke();
        }

        public void DoBeforePhys()
        {
            currState.BeforePhys?.Invoke();
        }

        public void DoAfterPhys()
        {
            currState.AfterPhys?.Invoke();
        }

        public void DoBeforeUploadSpeed()
        {
            currState.BeforeUploadSpeed?.Invoke();
        }

        public void DoParamChange()
        {
            currState.ParamChange?.Invoke();
        }

        public void DoLateUpdate()
        {
            currState.LateUpdate?.Invoke();
        }

        public PlayerStateMachine(T target)
        {
            this.target = target;
            states = new Dictionary<string, PlayerState<T>>();
        }

        public void Build()
        {
#if UNITY_EDITOR
            if (isBuilt)
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
            foreach (PlayerState<T> state in states.Values)
            {
                state.Build(target, this);
            }

            currState = states[startState];
            
            
        }

        public void DoStart()
        {
            currState.Begin?.Invoke();
        }

        public void AddState(PlayerState<T> state)
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

        public void AddStartState(PlayerState<T> state)
        {
            AddState(state);
            this.startState = state.stateName;
        }


        public void TransitionTo(string state)
        {
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
        }
    }

    public class PlayerState<T>
    {
        internal PlayerStateMachine<T> machine;
        public string stateName;
        public delegate void BuildFunc(T target, PlayerStateMachine<T> machine);
        public delegate void TrgFunc(Collider other);
        public PlayerStateMachine<T> currMachine
        {
            get { return machine; }
        }

        public PlayerState(string name)
        {
            stateName = name;
        }

        public BuildFunc Build;
        public Action Begin, End, Update, LateUpdate, ParamChange,
            BeforeCol, BeforePhys, AfterPhys, BeforeUploadSpeed;

    }

}
