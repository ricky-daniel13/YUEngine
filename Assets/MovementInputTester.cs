using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementInputTester : MonoBehaviour
{
    public LayerMask groundMask;
    public float acc, maxSpeed, upOffset, groundDistance, gravity;
    public InputTester tester;
    public Vector3 speed = Vector3.zero;
    public Rigidbody body;

    public bool ground = false;


    // Start is called before the first frame update
    private void FixedUpdate()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position + transform.up * upOffset, -transform.up, out hit, groundDistance+upOffset, groundMask))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(tester.GravityDir.up, hit.normal);
            ground = true;
        }
        else
        {
            ground = false;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, tester.GravityDir.up);
        }

        if (ground) {
            speed -= (-transform.up) * Vector3.Dot(speed, -transform.up);
        }

        speed += -tester.GravityDir.up * gravity * Time.fixedDeltaTime;

        float vrSpeed = Vector3.Dot(speed, transform.up);

        if (tester.realInput.magnitude > 0.1f)
        {
            speed = (tester.direction * maxSpeed);
        }
        else
        {
            speed = Vector3.zero;
        }
        speed += transform.up * vrSpeed;


        Debug.DrawRay(transform.position, tester.direction, Color.blue, Time.fixedDeltaTime);

        body.velocity = speed;
    }
}
