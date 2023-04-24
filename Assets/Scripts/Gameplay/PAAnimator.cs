using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PAAnimator : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator anim;
    public PlayerAdventure player;
    int idSpeed, idIsGround, idHMove, idVMove, idvSpeed, framesToLand=0;
    Vector3 fwr = Vector3.forward;
    Vector3 currFwr = Vector3.forward;
    Vector3 currUp = Vector3.up;
    Vector3 vscUp;
    Vector3 fwrVsc;
    public float faceSpeed; 
    public float rotaSpeed;
    public float slopeRotaSpeed;
    public float inputChangeSpeed;
    public int waitFramesToLand = 5;

    public bool isGrounded;

    public Vector3 desiredSpeed;
    public Vector3 currDesired;
    void Start()
    {
        idSpeed = Animator.StringToHash("speed");
        idvSpeed = Animator.StringToHash("verticalspeed");
        idIsGround = Animator.StringToHash("isGround");
        idHMove = Animator.StringToHash("hMove");
        idVMove = Animator.StringToHash("vMove");
    }

    // Update is called once per frame
    void Update()
    {
        if (!player.player.GetIsGround)
            framesToLand--;
        else
        {
            framesToLand = waitFramesToLand;
        }

        anim.SetFloat(idSpeed, player.player.InternalSpeed.magnitude);
        anim.SetFloat(idvSpeed, Vector3.Dot(player.player.InternalSpeed, -player.player.gravityDir));
        anim.SetBool(idIsGround, framesToLand > 0);
        if(player.player.InternalSpeed.sqrMagnitude > 0.01)
        {
            if (player.player.GetIsGround)
                fwr = player.player.InternalSpeed.normalized;
            else
            {
                player.transform.BreakDownSpeed(player.player.InternalSpeed, out _, out Vector3 lateralVelocity);

                if(lateralVelocity.sqrMagnitude > 0.01)
                fwr = lateralVelocity.normalized;
            }
        }
        else
        {
            fwr = Vector3.ProjectOnPlane(currFwr, player.transform.up).normalized;
        }

        if(Vector3.Dot(currFwr,fwr)>0)
            currFwr = Vector3.SmoothDamp(currFwr, fwr, ref fwrVsc, faceSpeed).normalized;
        else
            currFwr = Vector3.RotateTowards(currFwr, fwr, rotaSpeed * Time.deltaTime, 0);
        currUp = Vector3.SmoothDamp(currUp, player.transform.up, ref vscUp, slopeRotaSpeed);

        transform.position = player.transform.position;
        transform.rotation = Quaternion.LookRotation(currFwr, currUp);

        Vector3 desDir = player.transform.InverseTransformDirection(player.input.playerDir);


        desiredSpeed = Quaternion.LookRotation(player.transform.rotation * desDir, player.transform.up) * Quaternion.Inverse(Quaternion.LookRotation(fwr, player.transform.up)) * Vector3.forward;

        currDesired = Vector3.MoveTowards(currDesired, player.input.mag > 0 ? desiredSpeed : Vector3.zero, inputChangeSpeed * Time.deltaTime);
        
        anim.SetFloat(idHMove, currDesired.x);
        anim.SetFloat(idVMove, currDesired.z);

    }
}
