using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformTest : MonoBehaviour
{
    Vector3 speed;
    public LayerMask coll;
    bool grounded = false;
    RaycastHit hit;
    public Rigidbody body;

    Rigidbody connBody, prevConnBody;
    Matrix4x4 prevMat, currMat;
    Vector3 connWorldPos, connLocalPos, connVel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        grounded = (Physics.Raycast(transform.position+(Vector3.up*0.25f), Vector3.down, out hit, 0.5f, coll));

        if (grounded)
        {
            connBody = hit.rigidbody;
            if (body)
                body.MovePosition(hit.point);
            else
                transform.position = hit.point;

            speed *= Mathf.Pow(0.5f, Time.fixedDeltaTime);
        }

        if (connBody)
        {
            if (connBody == prevConnBody)
            {
                connVel = connBody.transform.TransformPoint(connLocalPos) - connWorldPos;
                //transform.position = transform.position + connMov;
            }
            connWorldPos = body ? body.position : transform.position;
            connLocalPos = connBody.transform.InverseTransformPoint(connWorldPos);
        }


        if (!grounded)
        {
            speed += Physics.gravity * Time.fixedDeltaTime;
        }
        if (grounded)
        {
            speed -= Vector3.Dot(speed, Physics.gravity.normalized)* Physics.gravity.normalized;
        }

        if (body)
        {
            //body.velocity = speed;
            body.MovePosition(body.position + ((speed) * Time.fixedDeltaTime) + connVel);
        }
        else
            transform.position = transform.position + ((speed) * Time.fixedDeltaTime) + connVel;

        if (!connBody && prevConnBody)
        {
            prevConnBody = null;
            speed += connVel;
            connVel = Vector3.zero;
        }

        prevConnBody = connBody;
        connBody = null;


    }
}
