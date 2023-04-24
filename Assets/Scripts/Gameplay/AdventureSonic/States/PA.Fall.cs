/*H**********************************************************************
* FILENAME :        PA.FreeRoam.Fall.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the falling state in the free roaming state.
*
* NOTES :
* AUTHOR :    Ricardo Daniel Garibaldi Oliva        START DATE :    07 Feb 2023

* CHANGES :
*
* 
*H*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2;
using YU2.StateMachine;

public class SonicState_Fall
{
    private PlayerAdventure trg;
    private PlayerStateMachine<PlayerAdventure> machine;
    public PlayerState<PlayerAdventure> GetState()
    {
        PlayerState<PlayerAdventure> state = new PlayerState<PlayerAdventure>("Fall");
        state.Update += Update;
        state.BeforePhys += BeforePhys;
        state.Build += OnBuild;
        state.Begin += Begin;

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
        trg.anim.isGrounded = false;
        trg.player.gravityForce = trg.currPms.gravityForce;
        trg.anim.anim.SetTrigger("toFall");
    }


    void Update()
    {
    }

    void BeforePhys()
    {
        if (trg.player.GetIsGround)
        {
            machine.TransitionTo("Walk");
            trg.steps.source.PlayOneShot(trg.steps.Land);
        }
        if(!trg.player.GetIsControlLock)
            trg.mvm.DoInputDamizean(trg.currPms.air, trg.currPms.air, trg.currPms.jumpTangentDrag, Mathf.Infinity, trg.input);
    }
}
