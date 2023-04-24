using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTester : MonoBehaviour
{
    public Path pth;

    // Update is called once per frame
    void Update()
    {





        if (!pth)
            return;

        //transform.position = pth.SetOnPath(this.transform, out _, out _);
    }

    private void OnDrawGizmos()
    {
        if (!pth)
            return;

        Gizmos.color = Color.red;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pth.PutOnPath(this.transform.position, this.transform.up, out _, out _), 0.25f);
    }

    private void OnValidate()
    {
    }
}
