/*H**********************************************************************
* FILENAME :        PA.Loop.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the Loop movement mode.
*
* NOTES :
* AUTHOR :    Ricardo Daniel Garibaldi Oliva        START DATE :    15 Apr 2023

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


[System.Serializable]
public partial class MoveMode_Loop
{
    private PlayerAdventure trg;
    private MonoStateMachine<PlayerAdventure> mch;

    private float splineRightOffset;
    private Vector3 splinePos, splineForw, oldSplineDir;

    private void OnGUI()
    {
        trg.transform.BreakDownSpeed(trg.player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);
        Vector3 inputDir = trg.transform.InverseTransformDirection(trg.input.playerDir);
        float normalSpeed = Vector3.Dot(hSpeed, inputDir);
        float dirDif = Vector3.Dot(hSpeed.normalized, inputDir);

        // Make a background box
        GUI.Box(new Rect(10, 10, 250, 250), "Sonic data: \tState: " + trg.actionState.currentState);

        GUI.Label(new Rect(15, 25, 250, 100), "isGrounded =" + trg.player.GetIsGround);
        GUI.Label(new Rect(15, 25 + 15, 250, 100), "position =" + trg.transform.position);
        GUI.Label(new Rect(15, 25 + 15 + 15, 250, 100), "speed magnitude =" + trg.player.physBody.velocity.magnitude.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15, 250, 100), "fall speed =" + Vector3.Dot(trg.player.InternalSpeed, trg.player.gravityDir).ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15, 250, 100), "ground angle =" + Vector3.Angle(-trg.player.gravityDir, trg.player.GetGroundNormal));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15, 250, 100), "control lock =" + trg.player.controlLockTimer.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "rota speed calc =" + (Mathf.Max(0, trg.player.InternalSpeed.magnitude - trg.currPms.rotaModeSpeedMin) / (trg.currPms.rotaModeSpeedMax - trg.currPms.rotaModeSpeedMin)).ToString("F3"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "Input raw angle =" + Vector2.Angle(Vector2.up, trg.input.directionRaw).ToString("F2") + ", dir diff: " + dirDif.ToString("F4"));

    }

    public MonoState<PlayerAdventure> GetState()
    {
        MonoState<PlayerAdventure> state = new MonoState<PlayerAdventure>("Loop");
        state.OnEnable += OnEnable;
        state.OnDisable += OnDisable;
        state.Update += Update;
        state.OnTriggerEnter += OnTriggerEnter;
        state.OnTriggerExit += OnTriggerExit;
        state.OnGui += OnGUI;
        state.Build += OnBuild;
        state.Begin += OnEnterState;

        return state;
    }

    void OnEnterState()
    {
        trg.currPms = trg.threeD;
        trg.actionState.DoParamChange();
    }

    void OnBuild(PlayerAdventure target, MonoStateMachine<PlayerAdventure> machine)
    {
        this.trg = target;
        this.mch = machine;
    }

    void OnEnable()
    {
        trg.player.BeforeCol += PlayerCollision;
        trg.player.BeforePhys += BeforePhys;
        trg.player.AfterPhys += AfterPhys;
        trg.player.BeforeUploadSpeed += BeforeUploadSpeed;
    }

    private void OnDisable()
    {
        trg.player.BeforeCol -= PlayerCollision;
        trg.player.BeforePhys -= BeforePhys;
        trg.player.AfterPhys -= AfterPhys;
        trg.player.BeforeUploadSpeed -= BeforeUploadSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        trg.inputFrame = YUInput.GetRefFrame(trg.loopKnot);
        trg.input = trg.inputCont.TransformInput(trg.inputFrame);
    }

    void BeforePhys()
    {
        trg.player.physBody.position = trg.loopPath.PutOnPath(trg.transform, YU2.Splines.PutOnPathMode.BinormalOnly, out trg.loopKnot, out _);
    }

    void AfterPhys()
    {
        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(trg.player.InternalSpeed, -trg.player.gravityDir);
            trg.player.InternalSpeed -= speedUp;
            trg.player.InternalSpeed += -trg.player.gravityDir * trg.currPms.jumpForce;
            trg.player.controlLockTimer = -1f;
            trg.player.transform.position = trg.player.transform.position + (-trg.player.gravityDir * trg.currPms.jumpForce * Time.fixedDeltaTime);
            trg.player.GetIsGround = false;
            trg.player.skipNextCol = true;
        }
    }

    void BeforeUploadSpeed()
    {
        Vector3 projRgt = Extensions.ProjectDirectionOnPlane(trg.loopKnot.binormal, trg.transform.up);
        float rgtSpeed = Vector3.Dot(projRgt, trg.player.InternalSpeed);
        trg.player.InternalSpeed -= projRgt * rgtSpeed;
    }

    void PlayerCollision()
    {

    }

    private void OnTriggerEnter(Collider other)
    {

    }

    private void OnTriggerExit(Collider other)
    {

    }

    void SplineSpeedCorrection()
    {
        Debug.DrawRay(splinePos, Vector3.up, Color.blue);

        trg.transform.BreakDownSpeed(trg.player.InternalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

        Vector3 disDir = Vector3.Cross(trg.loopKnot.binormal, trg.player.GetGroundNormal).normalized;
        Vector3 pastDir = Vector3.Cross(oldSplineDir, trg.player.GetGroundNormal).normalized;


        disDir = trg.player.transform.InverseTransformDirection(disDir);
        pastDir = trg.player.transform.InverseTransformDirection(pastDir);


        Vector3 nRgt = -Vector3.Cross(trg.loopKnot.tangent, trg.player.GetGroundNormal).normalized;
        nRgt = trg.player.transform.InverseTransformDirection(nRgt);

        //Vector3 nRgt = Quaternion.LookRotation(Vector3.right) * disDir;
        float speedInDir = lateralVelocity.magnitude;


        Vector3 normalVelocity = pastDir * speedInDir;
        Vector3 tangentVelocity = lateralVelocity - normalVelocity;

        float speedToRight = Vector3.Dot(nRgt, tangentVelocity);

        //splineRightOffset += speedToRight * Time.fixedDeltaTime;

        normalVelocity = disDir * speedInDir;

        trg.player.InternalSpeed = trg.player.transform.TransformDirection(normalVelocity + verticalVelocity);

        Debug.DrawRay(splinePos + (trg.loopKnot.tangent * splineRightOffset), Vector3.up, Color.red);
        oldSplineDir = disDir;
    }

}
