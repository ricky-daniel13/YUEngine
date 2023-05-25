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
    public YUController player;
    public PlayerInput input;
    public InputRef inputFrame;

    public bool DamTurning;

    public PAAnimator anim;
    public Footsetps steps;
    public GameObject trail;
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


    public MonoStateMachine<PlayerAdventure> physState;
    public PlayerStateMachine<PlayerAdventure> moveState;

    Vector3 localFacing = Vector3.forward;
    public Vector3 getGlobalFacing { get { return player.transform.TransformVector(localFacing); } }

    private void Awake()
    {
        moveState = new PlayerStateMachine<PlayerAdventure>(this);
        moveState.AddStartState(stateWalk.GetState());
        moveState.AddState(stateRoll.GetState());
        moveState.AddState(stateFall.GetState());
        moveState.AddState(stateJump.GetState());
        moveState.Build();

        physState = new MonoStateMachine<PlayerAdventure>(this);
        physState.AddStartState(frState.GetState());
        physState.AddState(lpState.GetState());
        physState.Build();

        mvm.player = player;
        mvm.transform = transform;
    }

    private void OnEnable()
    {
        physState.DoOnEnable();

        player.BeforeCol += PlayerCollision;
        player.BeforePhys += BeforePhys;
        player.AfterPhys += AfterPhys;
        player.BeforeUploadSpeed += BeforeUploadSpeed;

    }

    private void OnGUI()
    {
        physState.DoOnGui();
        GUI.Box(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 75), "Controles: Stick/WASD: moverse, \nA/Espacio: Saltar, \nB/Ctrl: Rodar, \nX/Alt: Flotar");
    }

    private void OnDisable()
    {
        physState.DoOnDisable();
    }

    private void Start()
    {
        physState.DoStart();
        moveState.DoStart();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        physState.DoUpdate();
        moveState.DoUpdate();

        if (Input.GetKey(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButton(0))
                Cursor.lockState = CursorLockMode.Locked;
        
    }

    private void FixedUpdate()
    {
        physState.DoFixedUpdate();
    }

    private void LateUpdate()
    {
        if (player.InternalSpeed.sqrMagnitude > 0.001)
        {
            if (player.GetIsGround)
               localFacing = player.transform.InverseTransformVector(Extensions.ProjectDirectionOnPlane(player.InternalSpeed.normalized,player.GetGroundNormal));
        }
            physState.DoLateUpdate();
    }

    void BeforePhys()
    {
        moveState.DoBeforePhys();
    }

    void AfterPhys()
    {
        moveState.DoAfterPhys();

        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(player.InternalSpeed, -player.gravityDir);
            player.InternalSpeed -= speedUp;
            player.InternalSpeed += -player.gravityDir * currPms.jumpForce;
            player.controlLockTimer = -1f;
            player.transform.position = transform.position + (player.GetGroundNormal * 0.1f);
            player.GetIsGround = false;
            player.skipNextCol = true;
        }
    }

    void BeforeUploadSpeed()
    {
        moveState.DoAfterPhys();
    }

    void PlayerCollision()
    {
        player.tryGroundDistance = player.physBody.velocity.magnitude < currPms.runSpeed ? currPms.tryGroundDistance : currPms.tryGroundDistanceFast;
        moveState.DoBeforeCol();
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
                    if (newPath != loopPath && (Vector3.Dot(other.transform.forward, player.InternalSpeed) > 0))
                    {
                        loopPath = newPath;
                        physState.TransitionTo("Loop");
                    }
                }
                else
                {
                    loopPath = null;
                    physState.TransitionTo("FreeRoam");
                }
            }
        }
        physState.DoOnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        physState.DoOnTriggerExit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        physState.DoOnTriggerStay(other);
    }
}
