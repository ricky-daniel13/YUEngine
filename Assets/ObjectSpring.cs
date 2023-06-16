using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpring : MonoBehaviour
{
    // Start is called before the first frame update
    public LayerMask playerLayer;

    public float Speed;
    public float OutOfControl;
    public bool CenterOnDash;
    public bool AlignToDash;
    public bool Additive;

    public AudioClip dashSound;
    public AudioSource dashEffector;

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if(playerLayer.Contains(other.gameObject.layer))
        {
            PlayerAdventure pa = other.GetComponent<PlayerAdventure>();
            if (pa.actionState.AllowExtMove())
            {
                pa.actionState.TransitionTo("Fall");
                if (Additive)
                    pa.player.InternalSpeed += transform.forward * Speed;
                else
                {
                    pa.player.InternalSpeed = transform.forward * Speed;
                }

                pa.player.transform.position=pa.transform.position += (transform.forward) * 0.2f;
                //trg.player.transform.rotation = Quaternion.FromToRotation(Vector3.up, -trg.player.gravityDir);
                pa.player.GetIsGround = false;
                pa.player.skipNextCol = true;

                dashEffector.PlayOneShot(dashSound);
            }
        }
    }
}