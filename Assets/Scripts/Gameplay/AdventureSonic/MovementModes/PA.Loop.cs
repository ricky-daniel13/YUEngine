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
public class MoveMode_Loop
{
    private PlayerAdventure trg;
    private MonoStateMachine<PlayerAdventure> mch;

    private float splineRightPos, lastTime, pathBound;
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
        Vector3 newPos = trg.loopPath.PutOnPath(trg.player.transform.position, trg.player.transform.up, out trg.loopKnot, out lastTime, out pathBound);
        splineRightPos = Vector3.Dot(trg.player.transform.position - newPos, trg.loopKnot.binormal);
        trg.player.physBody.MovePosition(newPos + (trg.loopKnot.binormal * splineRightPos));
        Debug.Log("Starting Loop!");
        oldSplineDir = trg.player.transform.InverseTransformDirection(Extensions.ProjectDirectionOnPlane(trg.loopKnot.tangent, trg.player.GetGroundNormal));
    }

    void OnBuild(PlayerAdventure target, MonoStateMachine<PlayerAdventure> machine)
    {
        this.trg = target;
        this.mch = machine;
    }

    void OnEnable()
    {
        trg.player.BeforeCol += PlayerCollision;
        trg.player.AfterPhys += AfterPhys;
        trg.player.BeforeUploadSpeed += BeforeUploadSpeed;
    }

    private void OnDisable()
    {
        trg.player.BeforeCol -= PlayerCollision;
        trg.player.AfterPhys -= AfterPhys;
        trg.player.BeforeUploadSpeed -= BeforeUploadSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        trg.inputFrame = YUInput.GetRefFrame(trg.loopKnot);
        trg.input = trg.inputCont.TransformInput(trg.inputFrame);
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
        float lastValidPoint = lastTime;
        float searchStart = ((lastTime < trg.loopPath.getSwitch) ? trg.loopPath.getStart : trg.loopPath.getEnd).x;
        float searchEnd = ((lastTime < trg.loopPath.getSwitch) ? trg.loopPath.getStart : trg.loopPath.getEnd).y;

        if (trg.player.GetIsGround)
        {
            searchEnd = 1;
            searchStart = 0;
        }
        Vector3 pathPos = trg.loopPath.PutOnPath(trg.player.transform.position - (trg.loopKnot.binormal * splineRightPos), trg.player.transform.up, out trg.loopKnot, out lastTime, out pathBound, searchStart, searchEnd);

        if (!trg.player.GetIsGround)
            lastTime = lastValidPoint;

        splineRightPos = Mathf.Min(MathF.Max(splineRightPos, -pathBound), pathBound);

        SplineSpeedCorrection();

        trg.player.physBody.MovePosition(pathPos + trg.loopKnot.binormal * splineRightPos);
        Debug.DrawRay(pathPos + Vector3.up, trg.loopKnot.tangent);
        /*Vector3 projRgt = Extensions.ProjectDirectionOnPlane(trg.loopKnot.binormal, trg.transform.up);
        float rgtSpeed = Vector3.Dot(projRgt, trg.player.InternalSpeed);
        trg.player.InternalSpeed -= projRgt * rgtSpeed;*/
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

        Vector3 currDir = trg.player.transform.InverseTransformDirection(Extensions.ProjectDirectionOnPlane(trg.loopKnot.tangent, trg.player.GetGroundNormal));

        //Vector3 rgt = Vector3.Cross(Vector3.up, currDir).normalized;

        Quaternion splineDif = Quaternion.FromToRotation(oldSplineDir, currDir);
        lateralVelocity = splineDif * lateralVelocity;

        //trg.player.InternalSpeed = trg.player.transform.TransformDirection((lateralVelocity) + verticalVelocity);

        float speedToRight = Vector3.Dot(trg.loopKnot.binormal, trg.player.InternalSpeed);
        //trg.player.InternalSpeed -= trg.loopKnot.binormal * speedToRight;
        splineRightPos += speedToRight * Time.fixedDeltaTime;
        //Debug.Log("Forwards speed = " + (lateralVelocity - (trg.loopKnot.binormal * speedToRight)).magnitude);
        
        float speedDec = (lateralVelocity - (trg.loopKnot.binormal * speedToRight)).magnitude;
        speedDec/=2;

        trg.player.InternalSpeed -= trg.loopKnot.binormal * Mathf.Sign(splineRightPos)*Mathf.Min(speedDec,Mathf.Abs(speedToRight));
        splineRightPos = Mathf.MoveTowards(splineRightPos, 0, speedDec * Time.fixedDeltaTime);
        
        /*if ((trg.player.GetIsGround && (trg.loopPath.getStart.y < lastTime) && (lastTime < trg.loopPath.getEnd.x))|| (splineRightPos > pathBound) || (splineRightPos < -pathBound))
        {
            trg.player.InternalSpeed -= trg.loopKnot.binormal * speedToRight;
        }*/

        oldSplineDir = currDir;
    }

}
