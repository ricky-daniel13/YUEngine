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
    bool isBraking;
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
        if (!trg.player.GetIsGround)
        {
            machine.TransitionTo("Fall");
            return;
        }
        trg.anim.anim.SetTrigger("toGround");
        trg.mvm.isBraking = false;
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
        trg.mvm.runSpeed = trg.currPms.runSpeed;
        rotaDecRate = (trg.currPms.rotaModeSpeed - trg.currPms.rotaModeSpeedFast) / ((trg.currPms.rotaModeSpeedMax - trg.currPms.rotaModeSpeedMin) * trg.currPms.rotaModeSpeedFast);
    }


    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Jump");
            return;
        }

        if (Input.GetButtonDown("Roll") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Roll");
            return;
        }
    }

    void BeforePhys()
    {
        float evAcc = trg.currPms.acc * trg.currPms.accOverSpeed.Evaluate(Mathf.Max(0, trg.player.InternalSpeed.magnitude) / (trg.currPms.topSpeed));
        float evTang = trg.currPms.tangentDrag * trg.currPms.tangOverSpeed.Evaluate(Mathf.Max(0, trg.player.InternalSpeed.magnitude) / (trg.currPms.topSpeed));

        
        float evMaxSpeed = trg.currPms.maxSpeedOverPush.Evaluate(trg.input.mag) * trg.currPms.topSpeed;

        //Debug.Log("Input magnitude: " + trg.input.mag + " ev:" + evMaxSpeed + " evaluation: " + trg.currPms.maxSpeedOverPush.Evaluate(trg.input.mag));

        if (!trg.player.GetIsGround)
        {
            machine.TransitionTo("Fall");
            return;
        }
        
        trg.player.doFriction = !(trg.input.mag > 0) || trg.player.GetIsControlLock;
        float rotaSpeed = trg.currPms.rotaModeSpeed / (1 + rotaDecRate * Mathf.Max(0, trg.player.InternalSpeed.magnitude - trg.currPms.rotaModeSpeedMin));

        if (!trg.player.GetIsControlLock)
        {
            string currentState = trg.physState.GetStateName();
            switch (currentState)
            {
                case "Loop2D":
                    {
                        trg.mvm.DoInput1Dir(evAcc, trg.currPms.dcc, trg.currPms.topSpeed, trg.input, trg.inputFrame);
                        break;
                    }
                default:
                    {
                        if (!trg.DamTurning)
                            trg.mvm.DoInputRota(evAcc * trg.currPms.maxSpeedOverPush.Evaluate(trg.input.mag), trg.currPms.dcc, evMaxSpeed, rotaSpeed, trg.input);
                        else
                            trg.mvm.DoInputDamizean(evAcc * trg.input.mag, trg.currPms.dcc, evTang, evMaxSpeed, trg.input);
                        break;
                    }
            }
        }

        if (isBraking != trg.mvm.isBraking)
        {
            if (trg.mvm.isBraking)
            {
                trg.steps.source.clip = trg.steps.Brake;
                trg.steps.source.time = 0;
                trg.steps.source.Play();
            }

            else
            {
                trg.steps.source.Stop();
            }

            isBraking = trg.mvm.isBraking;
            trg.anim.anim.SetBool("isBraking", isBraking);
        }

    }

}
