/*H**********************************************************************
* FILENAME :        PA.Walk.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the walking state
*
* NOTES :
* AUTHOR :    Ricardo Daniel Garibaldi Oliva        START DATE :    02 Feb 2023

* CHANGES :
*   19 Feb 2023:    Refactored for only one layer of states.
* 
*H*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2;
using YU2.StateMachine;

public class SonicState_Walk
{
    private PlayerAdventure trg;
    private PlayerStateMachine<PlayerAdventure> machine;
    float rotaDecRate;
    public PlayerState<PlayerAdventure> GetState()
    {
        PlayerState<PlayerAdventure> state = new PlayerState<PlayerAdventure>("Walk");
        state.Update += Update;
        state.Begin += Begin;
        state.BeforePhys += BeforePhys;
        state.Build += OnBuild;
        state.ParamChange += ParamChange;

        return state;
    }

    void OnBuild(PlayerAdventure target, PlayerStateMachine<PlayerAdventure> machine)
    {
        this.trg = target;
        this.machine = machine;
        return;
    }

    void Begin()
    {
        ParamChange();
    }

    void ParamChange()
    {
        trg.player.gravityForce = trg.currPms.gravityForce;
        trg.player.slopeFactor = trg.currPms.slopeFactor;
        trg.player.frc = trg.currPms.frc;
        trg.anim.isGrounded = true;
        trg.mvm.accOnDesiredDir = true;
        trg.mvm.rotaDecelFactor = trg.currPms.rotaModeSpdMul;
        trg.mvm.brakeAngle = Extensions.DegCos(trg.currPms.inputBehindAngle);
        trg.mvm.isBraking = false;
        rotaDecRate = (trg.currPms.rotaModeSpeed - trg.currPms.rotaModeSpeedFast) / ((trg.currPms.rotaModeSpeedMax - trg.currPms.rotaModeSpeedMin) * trg.currPms.rotaModeSpeedFast);
    }


    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Jump");
        }

        if (Input.GetButtonDown("Roll") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Roll");
        }
    }

    void BeforePhys()
    {
        float evAcc = trg.currPms.acc * trg.currPms.accOverSpeed.Evaluate(Mathf.Max(0, trg.player.InternalSpeed.magnitude) / (trg.currPms.topSpeed));
        float evTang = trg.currPms.tangentDrag * trg.currPms.tangOverSpeed.Evaluate(Mathf.Max(0, trg.player.InternalSpeed.magnitude) / (trg.currPms.topSpeed));

        if (!trg.player.GetIsGround)
            machine.TransitionTo("Fall");
        
        trg.player.doFriction = !(trg.input.mag > 0) || trg.player.GetIsControlLock;
        float rotaSpeed = trg.currPms.rotaModeSpeed / (1 + rotaDecRate * Mathf.Max(0, trg.player.InternalSpeed.magnitude - trg.currPms.rotaModeSpeedMin));

        if (!trg.player.GetIsControlLock)
        {
            string currentState = trg.physState.GetStateName();
            switch (currentState)
            {
                case "Loop":
                    {
                        trg.mvm.DoInput1Dir(evAcc, trg.currPms.dcc, trg.currPms.topSpeed, trg.input, trg.inputFrame);
                        break;
                    }
                default:
                    {
                        if (!trg.DamTurning)
                            trg.mvm.DoInputRota(evAcc, trg.currPms.dcc, trg.currPms.topSpeed, rotaSpeed, trg.input);
                        else
                            trg.mvm.DoInputDamizean(evAcc, trg.currPms.dcc, evTang, trg.currPms.topSpeed, trg.input);
                        break;
                    }
            }
            
        }
    }
}
