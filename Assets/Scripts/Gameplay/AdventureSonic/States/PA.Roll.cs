/*H**********************************************************************
* FILENAME :        PA.FreeRoam.Roll.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the rolling state in the free roaming state.
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

public class SonicState_Roll
{
    int idIsRoll;
    private PlayerAdventure trg;
    private PlayerStateMachine<PlayerAdventure> machine;
    bool isJumping;
    public PlayerState<PlayerAdventure> GetState()
    {
        PlayerState<PlayerAdventure> state = new PlayerState<PlayerAdventure>("Roll");
        state.Update += Update;
        state.Begin += Begin;
        state.End += End;
        state.BeforePhys += BeforePhys;
        state.Build += OnBuild;
        state.ParamChange += ParamChange;

        return state;
    }

    void OnBuild(PlayerAdventure target, PlayerStateMachine<PlayerAdventure> machine)
    {
        idIsRoll = Animator.StringToHash("isRoll");
        this.trg = target;
        this.machine = machine;
        return;
    }

    void ParamChange()
    {
        trg.player.gravityForce = trg.currPms.gravityForce;

        trg.player.frc = trg.currPms.rollFrc;
        trg.player.doFriction = true;

        trg.anim.isGrounded = true;
        trg.anim.anim.SetBool(idIsRoll, true);
        trg.trail.SetActive(true);
        trg.jumpball.SetActive(true);
        //trg.jumpballBall.SetActive(false);

        trg.mvm.accOnDesiredDir = false;
        trg.mvm.rotaDecelFactor = 10;
        trg.mvm.brakeAngle = -10;
    }

    void Begin()
    {
        isJumping = false;
        trg.anim.anim.SetTrigger("toRoll");
        ParamChange();
        trg.steps.source.PlayOneShot(trg.steps.Spin);
    }

    void End()
    {
        trg.anim.anim.SetBool(idIsRoll, false);
        trg.trail.SetActive(false);
        trg.jumpball.SetActive(false);
        //trg.jumpballBall.SetActive(true);
        trg.player.slopeFactor = trg.currPms.slopeFactor;
        trg.anim.anim.ResetTrigger("toRoll");
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Jump");
            return;
        }
    }

    void BeforePhys()
    {
        float slopeUpDot = Vector3.Dot(trg.transform.up, trg.player.GetGroundNormal);
        if ((trg.player.InternalSpeed.magnitude < trg.currPms.rollStopSpeed && slopeUpDot > Extensions.DegCos(trg.player.maxStandAngle)) && (trg.player.GetIsGround))
        {
            machine.TransitionTo("Walk");
            trg.steps.source.PlayOneShot(trg.steps.Land);
            return;
        }

        if (trg.player.GetIsGround)
        {
            if(isJumping)
                trg.steps.source.PlayOneShot(trg.steps.Land);
            isJumping = false;
        }
        else
        {
            isJumping = true;
        }

        if (Vector3.Dot(trg.player.GetSlopeVector, trg.player.InternalSpeed.normalized) < 0)
            trg.player.slopeFactor = trg.currPms.rollSlopeUpFactor;
        else
            trg.player.slopeFactor = trg.currPms.rollSlopeDownFactor;

        trg.player.doFriction = !(trg.input.mag > 0) || trg.player.GetIsControlLock;
         trg.mvm.DoInputRota(0, trg.currPms.rollDcl, Mathf.Infinity, trg.currPms.rollRotaSpeed, trg.input);

    }
}
