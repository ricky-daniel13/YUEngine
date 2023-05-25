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
        trg.player.gravityForce = trg.currPms.upGravityForce;

        trg.player.InternalSpeed += trg.player.GetGroundNormal * trg.currPms.jumpForce;
        trg.player.transform.position=trg.transform.position += (trg.player.GetGroundNormal) * 0.2f;
        trg.player.transform.rotation = Quaternion.FromToRotation(Vector3.up, -trg.player.gravityDir);
        trg.player.GetIsGround = false;
        trg.player.skipNextCol = true;
        trg.anim.anim.SetTrigger("groundToJump");
        trg.anim.anim.SetBool(idIsJump, true);
        trg.steps.source.PlayOneShot(trg.steps.Jump);
    }


    void End()
    {
        trg.anim.anim.SetBool(idIsJump, false);
    }


    void Update()
    {
        if (!Input.GetButton("Jump") && Vector3.Dot(trg.player.InternalSpeed, -trg.player.gravityDir) > trg.currPms.lowJumpSpeed && Vector3.Dot(trg.player.InternalSpeed, -trg.player.gravityDir) > 0)
        {
            Vector3 speedUp = Vector3.Project(trg.player.InternalSpeed, -trg.player.gravityDir);
            trg.player.InternalSpeed -= speedUp;
            trg.player.InternalSpeed += speedUp.normalized * trg.currPms.lowJumpSpeed;
        }
    }

    void BeforePhys()
    {
        if (trg.player.GetIsGround)
        {
            machine.TransitionTo("Walk");
            trg.steps.source.PlayOneShot(trg.steps.Land);
        }

        if (!trg.player.GetIsControlLock)
            trg.mvm.DoInputDamizean(trg.currPms.air, trg.currPms.air, trg.currPms.jumpTangentDrag, Mathf.Infinity, trg.input);

        if (trg.player.GetUpSpeed > 0)
            trg.player.gravityForce = trg.currPms.upGravityForce;
        else
            trg.player.gravityForce = trg.currPms.gravityForce;
    }
}
