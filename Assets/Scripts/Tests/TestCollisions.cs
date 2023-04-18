using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCollisions : MonoBehaviour
{
    public bool debugIsKinematic;
    int colCount = 0;
    public Rigidbody physBody;
    public CapsuleCollider physShape;
    Vector3 averageFloorCol=Vector3.zero;
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
       
        for(int i = 0; i < cacheContacts; i++)
        {
           
            cacheMove += col.GetContact(i).normal * col.GetContact(i).separation;

            float radius = physShape.radius;
            float height = physShape.height;
            Vector3 dir = transform.TransformDirection(Vector3.up);
            if (height < radius * 2f)
            {
                dir = Vector3.zero;
            }

            Vector3 bottom = transform.TransformPoint(physShape.center) - dir * (height * 0.5f - radius);

            if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized)>floorColThreshold)
            {
                Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.red, Time.fixedDeltaTime);
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
            else
            {
                Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.black, Time.fixedDeltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        finalSpeed = Vector3.zero;
        Debug.DrawRay(averageFloorPos, averageFloorCol, Color.blue, Time.fixedDeltaTime);
        //finalSpeed += Vector3.forward * 3;

        //DoGravity();
        //Debug.Log("FinalSpeed = " + finalSpeed + "Dot of velocity and gravity= " + Vector3.Dot(Physics.gravity.normalized,finalSpeed) + " maxGSpeed: " + maxGravityForce * Time.fixedDeltaTime);

        if(physBody.isKinematic!=debugIsKinematic)
        {
            physBody.isKinematic = debugIsKinematic;
        }

        if(debugIsKinematic)
        {
            //transform.position ;
            physBody.MovePosition(transform.position += (finalSpeed * Time.fixedDeltaTime)/* - cacheMove*/);
        }
        else
        {
            physBody.velocity = finalSpeed;
        }
        

        ResetCol();
        
    }

    void DoGravity()
    {
        if (isGrounded)
            ySpeed = 0;
        else if(ySpeed > (-maxGravityForce))
        {
            ySpeed += Physics.gravity.normalized.y * gravityForce * Time.fixedDeltaTime;
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
