using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputService_Camera : MonoBehaviour
{
    public TestPlayer ply;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ply.refPoint.forw = transform.forward;
        ply.refPoint.rgt = transform.right;
        //ply.refPoint.up = -Physics.gravity.normalized;
        ply.refPoint.up = transform.up;
    }
}
