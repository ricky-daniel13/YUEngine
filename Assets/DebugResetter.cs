using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugResetter : MonoBehaviour
{
    public PlayerAdventure PA;
    public GameObject camera;
    public float deathDistance;

    // Update is called once per frame
    void Update()
    {
        if(PA.transform.position.y < deathDistance){
            PA.transform.position=this.transform.position;
            PA.player.InternalSpeed = Vector3.zero;
            camera.transform.position = this.transform.position - (this.transform.forward * -5);
        }
    }
}
