using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class testAngleAxis : MonoBehaviour
{
    public Transform axis;
    public float roll = 0;
    public Vector3 vAxis = Vector3.forward;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (axis == null)
            return;
        axis.rotation.ToAngleAxis(out roll, out vAxis);

        Debug.DrawRay(axis.transform.position, vAxis, Color.blue, Time.deltaTime);
    }
}
