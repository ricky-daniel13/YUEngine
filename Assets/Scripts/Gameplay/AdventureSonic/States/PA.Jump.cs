/*H**********************************************************************
* FILENAME :        PA.FreeRoam.Jump.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the jump state in the free roaming state.
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

public class SonicState_Jump
{
    int idIsJump;
    private PlayerAdventure trg;
    private PlayerStateMachine<PlayerAdventure> machine;
    public PlayerState<PlayerAdventure> GetState()
    {
        PlayerState<PlayerAdventure> state = new PlayerState<PlayerAdventure>("Jump");
        state.Update += Update;
        state.BeforePhys += BeforePhys;
        state.Build += OnBuild;
        state.Begin += Begin;
        state.End += End;

        return state;
    }

    void OnBuild(PlayerAdventure target, PlayerStateMachine<PlayerAdventure> machine)
    {
        this.trg = target;
        this.machine = machine;
        idIsJump = Animator.StringToHash("isJump");
        return;
    }

    void Begin()
    {
        trg.physPly.gravityForce = trg.currPms.upGravityForce;

        trg.physPly.InternalSpeed += trg.physPly.GroundNormal * trg.currPms.jumpForce;
        trg.physPly.transform.position=trg.transform.position += (trg.physPly.GroundNormal) * 0.2f;
        //trg.player.transform.rotation = Quaternion.FromToRotation(Vector3.up, -trg.player.gravityDir);
        trg.physPly.GetIsGround = false;
        trg.physPly.skipNextCol = true;
        trg.anim.anim.SetBool(idIsJump, true);
        trg.steps.source.PlayOneShot(trg.steps.Jump);
        trg.jumpball.SetActive(true);
        //trg.jumpballAnimator.SetTrigger("Do");
        trg.jumpballBall.SetActive(false);
    }


    void End()
    {
        trg.anim.anim.SetBool(idIsJump, false);
        trg.jumpball.SetActive(false);
        trg.jumpballBall.SetActive(true);
    }


    void Update()
    {
        float minSpeed;
        Vector3 speedUp = Vector3.Project(trg.physPly.InternalSpeed, -trg.physPly.gravityDir);
        minSpeed = Mathf.Max(speedUp.magnitude, (trg.physPly.InternalSpeed - speedUp).magnitude);
        if (!Input.GetButton("Jump") && Vector3.Dot(trg.physPly.InternalSpeed, -trg.physPly.gravityDir) > trg.currPms.lowJumpSpeed && Vector3.Dot(trg.physPly.InternalSpeed, -trg.physPly.gravityDir) > 0)
        {
            trg.physPly.InternalSpeed -= speedUp;
            trg.physPly.InternalSpeed += speedUp.normalized * trg.currPms.lowJumpSpeed;
        }

        trg.anim.OverridenSpeed = Mathf.Max(minSpeed, trg.anim.minJumpSpeed);
    }

    void BeforePhys()
    {
        if (trg.physPly.GetIsGround)
        {
            machine.TransitionTo("Walk");
            trg.steps.source.PlayOneShot(trg.steps.Land);
        }

        if (!trg.physPly.GetIsControlLock)
            trg.mvm.DoInputDamizean(trg.currPms.air, trg.currPms.air, trg.currPms.jumpTangentDrag, Mathf.Infinity, trg.input);

        if (trg.physPly.GetUpSpeed > 0)
            trg.physPly.gravityForce = trg.currPms.upGravityForce;
        else
            trg.physPly.gravityForce = trg.currPms.gravityForce;
    }
}
