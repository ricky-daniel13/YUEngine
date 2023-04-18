using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleTest : MonoBehaviour
{
    int colCount = 0;
    Vector3 averageFloorCol = Vector3.zero;
    Vector3 averageFloorPos = Vector3.zero;
    public float floorColThreshold;
    Vector3 cacheMove = Vector3.zero;
    Vector3 finalSpeed;
    float ySpeed = 0;
    public float gravityForce;
    public float maxGravityForce;
    bool isGrounded = true;


    private void OnCollisionStay(Collision col)
    {
        int cacheContacts = col.contactCount;

        for (int i = 0; i < cacheContacts; i++)
        {
            Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.red, Time.fixedDeltaTime);
            cacheMove += col.GetContact(i).normal * col.GetContact(i).separation;
            //Debug.Log("Col count=" + i + " dot = " + Vector3.Dot(transform.up, col.GetContact(i).normal.normalized));
            if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized) > floorColThreshold)
            {
                averageFloorCol += col.GetContact(i).normal;
                averageFloorPos += col.GetContact(i).point;
                if (colCount > 0)
                {
                    averageFloorCol *= 0.5f;
                    averageFloorPos *= 0.5f;
                }
                colCount++;
                isGrounded = true;
            }
        }

    }

    private void Update()
    {
        finalSpeed = Vector3.zero;
        Debug.DrawRay(averageFloorPos, averageFloorCol, Color.blue, Time.fixedDeltaTime);
        finalSpeed += Vector3.forward * 3;

        DoGravity();
        Debug.Log("FinalSpeed = " + finalSpeed + "Dot of velocity and gravity= " + Vector3.Dot(Physics.gravity.normalized, finalSpeed) + " maxGSpeed: " + maxGravityForce * Time.deltaTime);

        transform.position += (finalSpeed * Time.deltaTime);

    }

    private void FixedUpdate()
    {
        transform.position -= cacheMove;
        ResetCol();
    }

    void DoGravity()
    {
        if (isGrounded)
            ySpeed = 0;
        else if (ySpeed > (-maxGravityForce))
        {
            ySpeed += Physics.gravity.normalized.y * gravityForce * Time.deltaTime;
        }

        finalSpeed.y = ySpeed;
    }

    void ResetCol()
    {
        cacheMove = Vector3.zero;
        averageFloorCol = Vector3.zero;
        averageFloorPos = Vector3.zero;
        colCount = 0;
        isGrounded = false;
    }
}
