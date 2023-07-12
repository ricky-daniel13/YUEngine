/*H**********************************************************************
* FILENAME :        PA.FreeRoam.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Code for the Free roaming player state.
*
* NOTES :
* AUTHOR :    Ricardo Daniel Garibaldi Oliva        START DATE :    02 Feb 2023

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
public class MoveMode_FreeRoam
{
    private PlayerAdventure trg;
    private MonoStateMachine<PlayerAdventure> mch;


    private Vector3 splinePos, splineForw, oldSplineDir;

    private void OnGUI()
    {
        trg.transform.BreakDownSpeed(trg.physPly.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);
        Vector3 inputDir = trg.transform.InverseTransformDirection(trg.input.playerDir);
        float normalSpeed = Vector3.Dot(hSpeed, inputDir);
        float dirDif = Vector3.Dot(hSpeed.normalized, inputDir);

        // Make a background box
        GUI.Box(new Rect(10, 10, 250, 250), "Sonic data: \tState: " + trg.actionState.currentState);

        GUI.Label(new Rect(15, 25, 250, 100), "isGrounded =" + trg.physPly.GetIsGround);
        GUI.Label(new Rect(15, 25 + 15, 250, 100), "position =" + trg.transform.position);
        GUI.Label(new Rect(15, 25 + 15 + 15, 250, 100), "speed magnitude =" + trg.physPly.InternalSpeed.sqrMagnitude);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15, 250, 100), "fall speed =" + Vector3.Dot(trg.physPly.InternalSpeed, trg.physPly.gravityDir).ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15, 250, 100), "ground angle =" + Vector3.Angle(-trg.physPly.gravityDir, trg.physPly.GroundNormal).ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15, 250, 100), "control lock =" + trg.physPly.controlLockTimer.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "rota speed calc =" + (Mathf.Max(0, trg.physPly.InternalSpeed.magnitude - trg.currPms.rotaModeSpeedMin) / (trg.currPms.rotaModeSpeedMax - trg.currPms.rotaModeSpeedMin)).ToString("F3"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "Input raw angle =" + Vector2.Angle(Vector2.up, trg.input.directionRaw).ToString("F2") + ", dir diff: " + dirDif.ToString("F4"));

    }

    public MonoState<PlayerAdventure> GetState()
    {
        MonoState<PlayerAdventure> state = new MonoState<PlayerAdventure>("FreeRoam");
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

        /*stateM= new PlayerStateMachine<PlayerAdventure>(target);
        stateM.AddStartState(Walk_GetState());
        stateM.AddState(Fall_GetState());
        stateM.AddState(Roll_GetState());
        stateM.AddState(Jump_GetState());
        stateM.Build();*/
    }

    void OnEnable()
    {
        /*cosBrakeAngle = Extensions.DegCos(inputBehindAngle);
        cosBrakeAngleSlow = Extensions.DegCos(inputBehindSlowAngle);
        trg.mvm.brakeAngle = cosBrakeAngle;
        trg.mvm.runSpeed = runSpeed;*/

        trg.physPly.BeforeCol += PlayerCollision;
        trg.physPly.BeforePhys += BeforePhys;
        trg.physPly.AfterPhys += AfterPhys;
    }

    private void OnDisable()
    {
        trg.physPly.BeforeCol -= PlayerCollision;
        trg.physPly.BeforePhys -= BeforePhys;
        trg.physPly.AfterPhys -= AfterPhys;
    }

    // Update is called once per frame
    void Update()
    {
        trg.input = trg.inputCont.TransformInputSimple(YUInput.GetRefFrame(trg.cameraInput.transform));
        //stateM.DoUpdate();
    }

    void BeforePhys()
    {
        //stateM.DoBeforePhys();
    }

    void AfterPhys()
    {
        //stateM.DoAfterPhys();

        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(trg.physPly.InternalSpeed, -trg.physPly.gravityDir);
            trg.physPly.InternalSpeed -= speedUp;
            trg.physPly.InternalSpeed += -trg.physPly.gravityDir * trg.currPms.jumpForce;
            trg.physPly.controlLockTimer = -1f;
            trg.physPly.transform.position = trg.transform.position + (trg.physPly.GroundNormal * 0.1f);
            trg.physPly.GetIsGround = false;
            trg.physPly.skipNextCol = true;
        }
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

}
