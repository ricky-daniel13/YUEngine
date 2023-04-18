using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimSpeedTest : MonoBehaviour
{

    public float mtsPorSeg = 1;
    public float mts = 25f;
    public bool syncSpeedAndAnimSpeed;
    public float animSpeed;
    Vector3 startPos, endPos;
    float duracionMov = 0, prcMov;
    public Animator anim;


    private void Start()
    {
        startPos = transform.position;
        endPos = transform.position + Vector3.forward*mts;
        prcMov = 2;
    }

    // Update is called once per frame
    void Update()
    {

        if (prcMov > 1)
        {
            prcMov = 0;
        }

        duracionMov = ((startPos - endPos).magnitude / mtsPorSeg);

        prcMov += Time.deltaTime / duracionMov;
        transform.position = Vector3.Lerp(startPos, endPos, prcMov);
    }

    private void LateUpdate()
    {
        anim.SetFloat("speed", mtsPorSeg);
        anim.SetFloat("animspeed", (syncSpeedAndAnimSpeed ? mtsPorSeg : animSpeed));
    }

}
