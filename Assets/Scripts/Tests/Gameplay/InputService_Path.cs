using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using YU2.Splines;

public class InputService_Path : MonoBehaviour
{/*
    public BezierThing path;
    public TestPlayer ply;
    public PutOnPathMode pathMode;
    Vector3 pos;
    Vector3 forw;
    Vector3 oldDir;
    float rightOffset=0;
    // Start is called before the first frame update
    void Start()
    {
        pos = path.GetPoint(0);
        ply.physBody.MovePosition(pos);
    }

    private void OnEnable()
    {
        if (ply)
            ply.AfterPhys += FixUpDash;
    }

    private void OnDisable()
    {
        ply.AfterPhys -= FixUpDash;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        path.PutOnPath(ply.transform.position, pathMode, out _, out Vector3 forw, out Vector3 rgt, out _);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pos, 0.2f);
        Gizmos.DrawRay(pos, forw);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(pos, rgt);

        Vector3 disDir = Vector3.Cross(rgt, ply.getGroundNormal).normalized;

        Vector3 nRgt = -Vector3.Cross(forw, ply.getGroundNormal).normalized;

        //Vector3 nRgt = Quaternion.LookRotation(Vector3.right) * disDir;

        Vector3 nor = Vector3.up;
        rgt = (rgt - nor * Vector3.Dot(rgt, nor)).normalized;

        Gizmos.DrawRay(pos, rgt);
        Gizmos.DrawSphere(pos + (rgt * rightOffset), 0.2f);
        Gizmos.DrawRay(pos + (rgt * rightOffset), nRgt);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos + (rgt * rightOffset), disDir);
    }

    // Update is called once per frame
    void Update()
    {
        UpOneDir();
        //ply.physBody.MovePosition(pos);
    }

    private void FixedUpdate()
    {
        //FixUpDash();
        //ply.refPoint.desiredDir = forw;
        //
    }

    void UpOneDir()
    {
        pos = path.PutOnPath(ply.transform.position, pathMode, out float tim, out forw, out Vector3 rgt, out _);
        //Vector3 acc = path.GetDirection(tim);
        ply.refPoint.forw = forw;
        ply.refPoint.rgt = rgt;
        ply.refPoint.up = path.GetNormal(tim);
        ply.refPoint.desiredDir = forw;
    }
    void FixUpOneDir()
    {
        pos = path.PutOnPath(ply.transform.position, pathMode, out _, out forw, out Vector3 rgt, out Vector3 mv);
        ply.physBody.MovePosition(pos);
    }
    void UpDash()
    {

    }
    public void FixUpDash(object sender, EventArgs e)
    {
        pos = path.PutOnPath(ply.transform.position, pathMode, out float time, out Vector3 nForw, out Vector3 rgt, out Vector3 mv);

        ply.transform.BreakDownSpeed(ply.internalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

        Vector3 disDir = Vector3.Cross(rgt, ply.getGroundNormal).normalized;
        Vector3 pastDir = Vector3.Cross(oldDir, ply.getGroundNormal).normalized;


        disDir = ply.transform.InverseTransformDirection(disDir);
        pastDir = ply.transform.InverseTransformDirection(pastDir);

        /*Vector3 nor = Vector3.up;
        rgt = (rgt - nor * Vector3.Dot(rgt, nor)).normalized;*/

    /*

        Vector3 nRgt = -Vector3.Cross(forw, ply.getGroundNormal).normalized;
        nRgt = ply.transform.InverseTransformDirection(nRgt);

        //Vector3 nRgt = Quaternion.LookRotation(Vector3.right) * disDir;
        float speedInDir = Vector3.Dot(pastDir, lateralVelocity);


        Vector3 normalVelocity = pastDir * speedInDir;
        Vector3 tangentVelocity = lateralVelocity - normalVelocity;

        float speedToRight = Vector3.Dot(nRgt, tangentVelocity);

        rightOffset += speedToRight * Time.fixedDeltaTime;

        normalVelocity = disDir * speedInDir;

        ply.internalSpeed = ply.transform.TransformDirection(normalVelocity + (nRgt*speedToRight) + verticalVelocity);


        ply.physBody.MovePosition(pos+(rgt * rightOffset));
        oldDir = rgt;

    
    }*/




}
