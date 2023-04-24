using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateByAxis : MonoBehaviour
{
    public float rotatePower, xRota, yRota, redirectToZ;
    public bool conditional;
    // Update is called once per frame
    void Update()
    {
        if ((!conditional && !Input.GetButton("Roll")) || (conditional && Input.GetButton("Roll")))
        {
            xRota += Input.GetAxis("Mouse X") * rotatePower;
            yRota += Input.GetAxis("Mouse Y") * rotatePower;
            //transform.Rotate(0, Input.GetAxis("Mouse X") * rotatePower, 0);
            //transform.rotation = Quaternion.Euler(xRota*(1-redirectToZ)+ yRota * (redirectToZ), xRota * (1 - redirectToZ), xRota * (redirectToZ));

            transform.rotation = Quaternion.Euler(yRota, 0, xRota);

            transform.rotation = Quaternion.Euler(yRota, xRota * (1 - redirectToZ), xRota * (redirectToZ));
        }
    }
}
