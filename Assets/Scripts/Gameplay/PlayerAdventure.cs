/*H**********************************************************************
* FILENAME :        PlayerAdventure.cs             DESIGN REF: ---
*
* DESCRIPTION :
*       Base class for the PlayerAdventure code. Runs the state machine
*
* NOTES :
* AUTHOR :    Ricardo Daniel Garibaldi Oliva        START DATE :    02 Feb 2023

* CHANGES :
*   15 Apr 2023:    Added Loop State.

* 
*H*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2;
using YU2.Splines;
using YU2.StateMachine;

public class PlayerAdventure : MonoBehaviour
{
    public YUInput inputCont;
    public YUCameraControl cameraInput;
    public YUController physPly;
    public PlayerInput input;
    public InputRef inputFrame;

    public bool DamTurning;

    public PAAnimator anim;
    public Footsetps steps;
    public GameObject trail, jumpball, jumpballBall;
    public float trailOffset, jumpballOffset, minBallSpeed, anglePerUnitSpeed;
    public Animator jumpballAnimator;
    public YUMovement mvm=new YUMovement();
    public PathLoop loopPath;
    public BezierKnot loopKnot;

    public AdventurePlayerSettings currPms;
    [Header("Params")]
    public AdventurePlayerSettings threeD;

    MoveMode_FreeRoam frState = new MoveMode_FreeRoam();
    MoveMode_Loop lpState = new MoveMode_Loop();

    SonicState_Fall stateFall = new SonicState_Fall();
    SonicState_Walk stateWalk = new SonicState_Walk();
    SonicState_Jump stateJump = new SonicState_Jump();
    SonicState_Roll stateRoll = new SonicState_Roll();

    SonicState_SpinDash stateSpinDash = new SonicState_SpinDash();
    public SonicState_SpinDash.SonicStateData_SpinDash spinDashData;


    public MonoStateMachine<PlayerAdventure> moveState;
    public PlayerStateMachine<PlayerAdventure> actionState;

    Vector3 localFacing = Vector3.forward;
    public Vector3 globalFacing { get { return physPly.transform.TransformVector(localFacing); } 
    set {localFacing=physPly.transform.InverseTransformVector(value);} }

    private void Awake()
    {
        localFacing = transform.forward;
        transform.rotation = Quaternion.identity;
        actionState = new PlayerStateMachine<PlayerAdventure>(this);
        actionState.AddStartState(stateWalk.GetState());
        actionState.AddState(stateRoll.GetState());
        actionState.AddState(stateFall.GetState());
        actionState.AddState(stateJump.GetState());
        actionState.AddState(stateSpinDash.GetState(spinDashData));
        actionState.Build();

        moveState = new MonoStateMachine<PlayerAdventure>(this);
        moveState.AddStartState(frState.GetState());
        moveState.AddState(lpState.GetState());
        moveState.Build();

        mvm.player = physPly;
        mvm.transform = transform;
    }

    private void OnEnable()
    {
        moveState.DoOnEnable();

        physPly.BeforeCol += PlayerCollision;
        physPly.BeforePhys += BeforePhys;
        physPly.AfterPhys += AfterPhys;
        physPly.BeforeUploadSpeed += BeforeUploadSpeed;

    }

    private void OnGUI()
    {
        moveState.DoOnGui();
        GUI.Box(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 75), "Controles: Stick/WASD: moverse, \nA/Espacio: Saltar, \nB/Ctrl: Rodar, \nX/Alt: Flotar");
    }

    private void OnDisable()
    {
        moveState.DoOnDisable();
    }

    private void Start()
    {
        moveState.DoStart();
        actionState.DoStart();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        moveState.DoUpdate();
        actionState.DoUpdate();

        if (Input.GetKey(KeyCode.Escape)){
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None ? CursorLockMode.Locked : CursorLockMode.None);
        }
        
    }

    private void FixedUpdate()
    {
        moveState.DoFixedUpdate();
    }

    private void LateUpdate()
    {
        //Debug.Log("Projected speed: " + Vector3.ProjectOnPlane(player.InternalSpeed, transform.up).sqrMagnitude);
        if (Vector3.ProjectOnPlane(physPly.InternalSpeed, physPly.GroundNormal).sqrMagnitude > 0.00001f)
        {
            localFacing = physPly.transform.InverseTransformVector(Extensions.ProjectDirectionOnPlane(physPly.InternalSpeed.normalized, transform.up));
        }

        

        moveState.DoLateUpdate();
        trail.transform.position = transform.position + transform.up * trailOffset;
        jumpball.transform.parent.position = transform.position + anim.transform.up * jumpballOffset;
        jumpball.transform.parent.transform.rotation = Quaternion.LookRotation(globalFacing, anim.transform.up);
        jumpball.transform.transform.localRotation = jumpball.transform.transform.localRotation * Quaternion.AngleAxis(Mathf.Max(minBallSpeed, physPly.InternalSpeed.magnitude)*anglePerUnitSpeed*Time.deltaTime, Vector3.right);
    }

    void BeforePhys()
    {
        actionState.DoBeforePhys();
    }

    void AfterPhys()
    {
        actionState.DoAfterPhys();

        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(physPly.InternalSpeed, -physPly.gravityDir);
            physPly.InternalSpeed -= speedUp;
            physPly.InternalSpeed += -physPly.gravityDir * currPms.jumpForce;
            physPly.controlLockTimer = -1f;
            physPly.transform.position = transform.position + (physPly.GroundNormal * 0.1f);
            physPly.GetIsGround = false;
            physPly.skipNextCol = true;
        }

        if (physPly.ConnBody)
        {
            localFacing = physPly.transform.InverseTransformVector(Extensions.ProjectDirectionOnPlane(physPly.connRota * globalFacing, transform.up));
        }
    }

    void BeforeUploadSpeed()
    {
        actionState.DoAfterPhys();
    }

    void PlayerCollision()
    {
        physPly.tryGroundDistance = physPly.physBody.velocity.magnitude < currPms.runSpeed ? currPms.tryGroundDistance : currPms.tryGroundDistanceFast;
        actionState.DoBeforeCol();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spline"))
        {
            PathTrigger pth = other.GetComponent<PathTrigger>();
            if (pth.pathType == PathTrigger.PathType.Loop)
            {
                if (!pth.IsExit)
                {
                    PathLoop newPath = pth.pathHolder.GetComponent<PathLoop>(); 
                    if (newPath != loopPath && (Vector3.Dot(other.transform.forward, physPly.InternalSpeed) > 0))
                    {
                        loopPath = newPath;
                        moveState.TransitionTo("Loop");
                    }
                }
                else
                {
                    moveState.TransitionTo("FreeRoam");
                }
            }
        }
        moveState.DoOnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        moveState.DoOnTriggerExit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        moveState.DoOnTriggerStay(other);
    }
}
