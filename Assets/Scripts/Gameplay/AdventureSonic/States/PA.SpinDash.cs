using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2.StateMachine;

public class SonicState_SpinDash
{
    [System.Serializable]
    public class SonicStateData_SpinDash{
        public float acumPerSecond;
        public float minRelease;
        public float maxRelease;
        public float rotationSpeed;
    }

    float acumSpeed;

    SonicStateData_SpinDash data;
    
    // Start is called before the first frame update
    private PlayerAdventure trg;
    private PlayerStateMachine<PlayerAdventure> machine;
    bool isJumping;
    int idIsRoll;
    public PlayerState<PlayerAdventure> GetState(SonicStateData_SpinDash stateData)
    {
        PlayerState<PlayerAdventure> state = new PlayerState<PlayerAdventure>("SpinDash");
        state.Update += Update;
        state.Begin += Begin;
        state.End += End;
        state.BeforePhys += BeforePhys;
        state.Build += OnBuild;
        state.ParamChange += ParamChange;
        data = stateData;

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
        trg.player.frc = trg.currPms.dcc;
    }

    void Begin()
    {
        isJumping = false;
        trg.anim.anim.SetTrigger("toRoll");
        ParamChange();
        trg.jumpball.SetActive(true);
        trg.anim.anim.SetBool(idIsRoll, true);
        trg.steps.source.PlayOneShot(trg.steps.Spin);
        acumSpeed=data.minRelease;
        trg.jumpballAnimator.SetTrigger("Do");
    }

    void End()
    {
        trg.anim.anim.SetBool(idIsRoll, false);
        trg.jumpball.SetActive(false);
        trg.player.slopeFactor = trg.currPms.slopeFactor;
        trg.anim.anim.ResetTrigger("toRoll");
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") && trg.player.GetIsGround)
        {
            machine.TransitionTo("Jump");
            return;
        }

        if(!Input.GetButton("Roll")){
            trg.player.InternalSpeed = trg.globalFacing * acumSpeed;
            trg.steps.source.PlayOneShot(trg.steps.Spin);
            trg.jumpballAnimator.SetTrigger("Do");
            machine.TransitionTo("Roll");
        }
    }

    void BeforePhys()
    {
        if(trg.input.mag > 0){
            trg.globalFacing = trg.input.playerDir;
        }
        trg.player.doFriction = !(trg.input.mag > 0) || trg.player.GetIsControlLock;
        acumSpeed += data.acumPerSecond * Time.deltaTime;
        acumSpeed=Mathf.Min(acumSpeed, data.maxRelease);
        Debug.Log("acumSpeed=" + acumSpeed);
    }
}
