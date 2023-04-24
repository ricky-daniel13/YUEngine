/*H**********************************************************************
* FILENAME :        PlayerAdventure.FreeRoam.cs             DESIGN REF: ---
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
    public PlayerStateMachine<PlayerAdventure> actionState;

    private void Awake()
    {
        actionState = new PlayerStateMachine<PlayerAdventure>(this);
        actionState.AddStartState(stateWalk.GetState());
        actionState.AddState(stateRoll.GetState());
        actionState.AddState(stateFall.GetState());
        actionState.AddState(stateJump.GetState());
        actionState.Build();

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
        actionState.DoStart();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        physState.DoUpdate();
        actionState.DoUpdate();

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
        physState.DoLateUpdate();
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
            Vector3 speedUp = Vector3.Project(player.InternalSpeed, -player.gravityDir);
            player.InternalSpeed -= speedUp;
            player.InternalSpeed += -player.gravityDir * currPms.jumpForce;
            player.controlLockTimer = -1f;
            player.physBody.position = transform.position + (player.GetGroundNormal * 0.1f);
            player.GetIsGround = false;
            player.skipNextCol = true;
        }
    }

    void BeforeUploadSpeed()
    {
        actionState.DoAfterPhys();
    }

    void PlayerCollision()
    {
        player.tryGroundDistance = player.physBody.velocity.magnitude < currPms.runSpeed ? currPms.tryGroundDistance : currPms.tryGroundDistanceFast;
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
