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
        trg.physPly.gravityForce = trg.currPms.gravityForce;

        trg.physPly.frc = trg.currPms.rollFrc;
        trg.physPly.doFriction = true;

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
        trg.physPly.doFriction = true;
    }

    void End()
    {
        trg.anim.anim.SetBool(idIsRoll, false);
        trg.trail.SetActive(false);
        trg.jumpball.SetActive(false);
        //trg.jumpballBall.SetActive(true);
        trg.physPly.slopeFactor = trg.currPms.slopeFactor;
        trg.anim.anim.ResetTrigger("toRoll");
        trg.physPly.doFriction = false;
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.physPly.GetIsGround)
        {
            machine.TransitionTo("Jump");
            return;
        }
    }

    void BeforePhys()
    {
        float slopeUpDot = Vector3.Dot(trg.transform.up, trg.physPly.GroundNormal);
        if ((trg.physPly.InternalSpeed.magnitude < trg.currPms.rollStopSpeed && slopeUpDot > Extensions.DegCos(trg.physPly.maxStandAngle)) && (trg.physPly.GetIsGround))
        {
            machine.TransitionTo("Walk");
            trg.steps.source.PlayOneShot(trg.steps.Land);
            return;
        }

        if (trg.physPly.GetIsGround)
        {
            if(isJumping)
                trg.steps.source.PlayOneShot(trg.steps.Land);
            isJumping = false;
        }
        else
        {
            isJumping = true;
        }

        if (Vector3.Dot(trg.physPly.GetSlopeVector, trg.physPly.InternalSpeed.normalized) < 0)
            trg.physPly.slopeFactor = trg.currPms.rollSlopeUpFactor;
        else
            trg.physPly.slopeFactor = trg.currPms.rollSlopeDownFactor;

        trg.mvm.DoInputRota(0, trg.currPms.rollDcl, Mathf.Infinity, trg.currPms.rollRotaSpeed, trg.input);

    }
}
