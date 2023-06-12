using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DummyAnimator : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator anim;
    public PlayerAdventure player;
    int idSpeed, idIsJump, idIsRoll, idIsGround, idHMove, idVMove, framesToLand = 0;
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

    public Vector3 desiredSpeed;
    public Vector3 currDesired;
    void Start()
    {
        idSpeed = Animator.StringToHash("speed");
        idIsRoll = Animator.StringToHash("isRoll");
        idIsJump = Animator.StringToHash("isJump");
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

        anim.SetFloat(idSpeed, player.player.physBody.velocity.magnitude);
        anim.SetBool(idIsRoll, false);
        anim.SetBool(idIsJump, false);
        anim.SetBool(idIsGround, framesToLand > 0);
        /*if (player.player.physBody.velocity.sqrMagnitude > 0.5)
        {
            if (player.player.GetIsGround)
                fwr = player.player.physBody.velocity.normalized;
            else
            {
                player.transform.BreakDownSpeed(player.player.physBody.velocity, out _, out Vector3 lateralVelocity);
                if (lateralVelocity.sqrMagnitude > 0.1)
                    fwr = lateralVelocity.normalized;
            }
        }
        else
        {
            fwr = Vector3.ProjectOnPlane(currFwr, player.transform.up).normalized;
        }*/

        fwr = player.getGlobalFacing;
        

        currUp = Vector3.SmoothDamp(currUp, player.transform.up, ref vscUp, slopeRotaSpeed);

        if (Vector3.Dot(currFwr, fwr) > 0)
            currFwr = Extensions.ProjectDirectionOnPlane(Vector3.RotateTowards(currFwr, fwr, faceSpeed * Time.deltaTime, 0), currUp);
        else
            currFwr = Extensions.ProjectDirectionOnPlane(Vector3.RotateTowards(currFwr, fwr, rotaSpeed * Time.deltaTime, 0), currUp);

        transform.position = player.transform.position;
        transform.rotation = Quaternion.LookRotation(currFwr, currUp);
        Vector3 desDir = player.transform.InverseTransformDirection(player.input.playerDir);
        desiredSpeed = Quaternion.LookRotation(player.transform.rotation * desDir, player.transform.up) * Quaternion.Inverse(Quaternion.LookRotation(fwr, player.transform.up)) * Vector3.forward;

        currDesired = Vector3.MoveTowards(currDesired, player.input.mag > 0 ? desiredSpeed : Vector3.zero, inputChangeSpeed * Time.deltaTime);

        anim.SetFloat(idHMove, currDesired.x);
        anim.SetFloat(idVMove, currDesired.z);
        //transform.rotation = Quaternion.S

    }
}
