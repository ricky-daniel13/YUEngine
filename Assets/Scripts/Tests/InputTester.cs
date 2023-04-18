using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputTester : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform ReferencePoint;
    public Transform GravityDir;
    public Transform Plane;
    public Transform Player;
    public bool FakeInput;
    public Vector2 FakeInputVector;

    public Vector2 realInput;
    public Vector2 InputRaw;
    public bool showFakeInputs;
    public bool upIsPlane, upIsNegGrav, transformPlayerInput, doInput1, doInput2, doInput3, slopeMode;
    public Vector3 direction;
    public Vector3 finalDirection;
    void Start()
    {
        InputRaw = Vector2.up;
        direction = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        Physics.gravity = GravityDir.up * -9.81f;

        realInput.x = Input.GetAxis("Horizontal");
        realInput.y = Input.GetAxis("Vertical");
        if (realInput.magnitude > 0.1)
            InputRaw = realInput;

        if (FakeInput)
        {
            InputRaw = FakeInputVector;
        }

        Vector3 forw = ReferencePoint.forward;
        Vector3 rgt = ReferencePoint.right;
        Vector3 up = ReferencePoint.up;
        if (upIsPlane)
            up = Plane.up;

        if (upIsNegGrav)
            up = -Physics.gravity.normalized;

        if (doInput1)
        {
            Vector3 forward = Extensions.ProjectDirectionOnPlane(forw, up);
            Vector3 right = Extensions.ProjectDirectionOnPlane(rgt, up);
            direction = Quaternion.FromToRotation(up, Plane.up) * (forward * InputRaw.y + right * InputRaw.x).normalized;
            if (transformPlayerInput)
                direction = Player.InverseTransformDirection(Quaternion.FromToRotation(up, Plane.up) * direction);
            if (!transformPlayerInput)
                Debug.DrawRay(Player.transform.position + Player.transform.up, direction, Color.blue);
            else
                Debug.DrawRay(Player.transform.position + Player.transform.up, Player.TransformDirection(direction), Color.blue);

        }


        if (doInput2)
        {
            Vector3 globalUp = GravityDir.up;
            Vector3 globalForw = Extensions.FromToRot(Vector3.up, globalUp) * Vector3.forward;
            Debug.DrawRay(Player.transform.position + Vector3.right, globalUp.normalized, Color.green);
            Debug.DrawRay(Player.transform.position + Vector3.right, globalForw.normalized, Color.blue);

            Vector3 forward = Vector3.forward;

            Debug.Log("Dot de forw y arriba: " + Vector3.Dot(forw, globalUp) + " Dot de forw y adelante: " + Vector3.Dot(up, globalUp));

            /*if (Vector3.Dot(forw, globalUp) < -0.98 || Vector3.Dot(up, globalUp)<0)
                forward = Quaternion.FromToRotation(globalForw, globalUp) * Extensions.ProjectDirectionOnPlane(forw, globalForw);
            else
                forward = Extensions.ProjectDirectionOnPlane(forw, globalUp);*/
            if (Vector3.Dot(up, globalUp) < 0)
                forward = Extensions.ProjectDirectionOnPlane(-forw, globalUp);
            else
                forward = Extensions.ProjectDirectionOnPlane(forw, globalUp);

            forward = Extensions.ProjectDirectionOnPlane(forw, globalUp);

            //Vector3 right = Extensions.ProjectDirectionOnPlane(rgt, up);
            Vector3 right = Vector3.Cross(globalUp, forward.normalized);

            if (realInput.magnitude > 0.1)
                direction = (forward * InputRaw.y + right * InputRaw.x).normalized;

            Vector3 finalDirection = Extensions.FromToRot(globalUp, Plane.up) * direction;


            Debug.DrawRay(Player.transform.position + Player.transform.up, forward.normalized, Color.blue);
            Debug.DrawRay(Player.transform.position + Player.transform.up, right, Color.red);

            Debug.DrawRay(Player.transform.position + Player.transform.up, transformPlayerInput ? finalDirection : direction, Color.black);
        }

        if (doInput3)
        {
            float fwrDot = (Vector3.Dot(Player.up, ReferencePoint.forward));
            float rightDot = (Vector3.Dot(Player.up, ReferencePoint.right));
            Vector3 forward = Vector3.zero;
            //Vector3 right = Vector3.Cross(Player.up, forward.normalized);
            Vector3 right = Vector3.zero;
            if (slopeMode)
            {
                if (rightDot < -1f + float.Epsilon || rightDot > 1f - float.Epsilon)
                    right = ReferencePoint.up;
                else
                    right = ReferencePoint.right;

                right = Extensions.ProjectDirectionOnPlane(right, Player.up);
                forward = -Vector3.Cross(Player.up, right.normalized);
            }

            if(!slopeMode)
            {
                if (fwrDot < -1f + float.Epsilon || fwrDot > 1f - float.Epsilon)
                    forward = ReferencePoint.up;
                else
                    forward = ReferencePoint.forward;

                forward = Extensions.ProjectDirectionOnPlane(forward, Player.up);
                right = Vector3.Cross(Player.up, forward.normalized);
            }

            Debug.Log("Fwr Grd Dot = " + fwrDot + ", Right Grd Dot = " + rightDot);
            
            direction = (forward * InputRaw.y + right * InputRaw.x).normalized;


            Debug.DrawRay(Player.transform.position + Player.transform.up, forward.normalized, Color.blue);
            Debug.DrawRay(Player.transform.position + Player.transform.up, right, Color.red);

            Debug.DrawRay(Player.transform.position + Player.transform.up, direction, Color.black);
        }

    }
}
