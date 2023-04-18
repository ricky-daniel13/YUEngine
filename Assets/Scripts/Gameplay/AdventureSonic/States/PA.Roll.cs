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

        trg.mvm.accOnDesiredDir = false;
        trg.mvm.rotaDecelFactor = 10;
        trg.mvm.brakeAngle = -10;
    }

    void Begin()
    {
        isJumping = false;
        ParamChange();
    }

    void End()
    {
        trg.anim.anim.SetBool(idIsRoll, false);
        trg.trail.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.player.GetIsGround && !isJumping)
        {
            trg.player.InternalSpeed += trg.player.GetGroundNormal * trg.currPms.jumpForce;
            trg.player.physBody.position = trg.transform.position += (trg.player.GetGroundNormal) * 0.2f;
            trg.player.physBody.rotation = Quaternion.FromToRotation(Vector3.up, -trg.player.gravityDir);
            //Debug.Log("Jump! + floornor" + player.GetGroundNormal);
            trg.player.GetIsGround = false;
            isJumping = true;
            trg.player.skipNextCol = true;
        }
    }

    void BeforePhys()
    {

        float slopeUpDot = Vector3.Dot(trg.transform.up, trg.player.GetGroundNormal);
        if ((trg.player.InternalSpeed.magnitude < trg.currPms.rollStopSpeed && slopeUpDot > Extensions.DegCos(trg.player.maxStandAngle)) && (trg.player.GetIsGround))
           machine.TransitionTo("Walk");

        if (trg.player.GetIsGround)
            isJumping = false;

        if (Vector3.Dot(trg.player.GetSlopeVector, trg.player.InternalSpeed.normalized) < 0)
            trg.player.slopeFactor = trg.currPms.rollSlopeUpFactor;
        else
            trg.player.slopeFactor = trg.currPms.rollSlopeDownFactor;

         trg.mvm.DoInputRota(0, trg.currPms.rollDcl, Mathf.Infinity, trg.currPms.rollRotaSpeed, trg.input);

    }
}
