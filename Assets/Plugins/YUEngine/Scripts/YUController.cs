using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YU2
{
    public enum GroundNormalType { bySphere, byRay, basedOnSphere }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class YUController : MonoBehaviour
    {
        public delegate void CollideEvent(Vector3 colNormal, Vector3 colPosition);
        public event Action BeforeCol, AfterCol, BeforePhys, AfterPhys, BeforeUploadSpeed, AfterUploadSpeed, onSlopeSlip, onLand;
        public event CollideEvent OnWallCol, OnFloorCol, OnCeilingCol;

        int colCount = 0, ceilCount = 0, wallCount = 0;
        public Rigidbody physBody;
        Rigidbody connBody, prevConnBody;
        public CapsuleCollider physShape;

        Vector3 averageFloorNor = Vector3.zero;
        Vector3 averageFloorPos = Vector3.zero;
        Vector3 averageCeiNor = Vector3.zero;
        Vector3 averageCeiPos = Vector3.zero;
        Vector3 averageWallNor = Vector3.zero;
        Vector3 averageWallPos = Vector3.zero, oldNormal, slopeVector, finalSpeed;

        bool grounded = true;

        [Header("Movement")]
        public float frc = 6;
        public float slopeFactor=20, maxGroundSpeed=80, airDragLimit=6, airDragHrLimit = 0.46875f, drag = 0.0625f, controlLockTimer, controlLockTime, airToGroundSpeedMultiplier=1;
        public bool slopeSpeedAdjust, doControlLock, doFriction=true, doAirDrag = true, doGravity=true, doPhysics=true, doSpeedUpload=true, gravBasedSlope=false;
        [Header("Collision")]
        [Tooltip("El angulo maximo que puede tener una superficie antes de dejar de ser suelo")]
        public float maxFloorAngle = 50;
        [Tooltip("El angulo minimo que puede tener una superficie para ser techo")]
        public float maxCeiAngle = 140;
        [Tooltip("El angulo maximo en el que Sonic puede pararse sin que lo arrastre la pendiente")]
        public float maxStandAngle = 10;
        [Tooltip("Angulo maximo al que Sonic puede pararse en una superficie sin ir corriendo")]
        public float maxSlipAngle = 40;
        [Tooltip("Velocidad minima antes de que Sonic se suelte de una superficie arriba del maximo")]
        public float slipSpeedLimit = 10;
        public float tryGroundDistance = 0.5f;
        public GroundNormalType groundMethod;
        public LayerMask groundLayer, movingLayer;
        public bool stopOnCeil, stopOnWall;
        Vector3 connWorldPos, connLocalPos, connDiff, connTorq, connWorldRota, connLocalRota;
        [Header("Gravity")]
        public float gravityForce = 30;
        public float maxGravityForce=70;
        public Vector3 gravityDir=Vector3.down;
        [System.NonSerialized]
        public bool skipNextCol;

        private float cosMaxAngle, cosMinAngle, cosMaxCei, cosSlipAngle;

        public bool GetIsControlLock{ get { return controlLockTimer > 0; }
        }
        public bool GetIsGround
        {
            get { return grounded; }
            set { grounded = value; }
        }

        public bool GetIsCeil { get { return ceilCount > 0; } }
        public bool GetIsWall { get { return wallCount > 0; } }

        public Vector3 GetGroundNormal
        {
            get
            {
                return averageFloorNor;
            }
        }

        public Vector3 GetGroundPosition
        {
            get
            {
                return averageFloorPos;
            }
        }

        public Vector3 GetWallNormal { get { return averageWallNor; } }

        public Vector3 GetWallPosition { get { return averageWallPos; } }

        public Vector3 GetCeiNormal { get { return averageCeiNor; } }

        public Vector3 GetCeiPosition { get { return averageCeiPos; } }

        public Vector3 GetSlopeVector { get { return slopeVector; } }

        public float GetUpSpeed { get { return Vector3.Dot(InternalSpeed, -gravityDir); } }

        public Vector3 InternalSpeed
        {
            get { return finalSpeed; }
            set { finalSpeed = value;
            }
        }

        int keepRotation;

        public void processAngleValues()
        {
            cosMaxAngle = Extensions.DegCos(maxFloorAngle);
            cosMaxCei = Extensions.DegCos(maxCeiAngle);
            cosMinAngle = Extensions.DegCos(maxStandAngle);
            cosSlipAngle = Extensions.DegCos(maxSlipAngle);
        }

        private void Awake()
        {
            processAngleValues();
            grounded = TryGround();
        }

        private void Start()
        {
            processAngleValues();
        }


        private void OnCollisionStay(Collision col)
        {
            int cacheContacts = col.contactCount;

            for (int i = 0; i < cacheContacts; i++)
            {

                if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized) > cosMaxAngle)
                {
                    if (colCount == 0)
                    {
                        averageFloorNor = Vector3.zero;
                        averageFloorPos = Vector3.zero;
                    }
                    averageFloorNor += col.GetContact(i).normal;
                    averageFloorPos += col.GetContact(i).point;
                    if (colCount > 0)
                    {
                        averageFloorNor *= 0.5f;
                        averageFloorPos *= 0.5f;
                    }

                    if (movingLayer.Contains(col.gameObject.layer))
                    {
                        connBody = col.rigidbody;
                    }

                    OnFloorCol?.Invoke(col.GetContact(i).normal, col.GetContact(i).point);

                    colCount++;
                }
                else if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized) < cosMaxCei)
                {
                    if (ceilCount == 0)
                    {
                        averageCeiNor = Vector3.zero;
                        averageCeiPos = Vector3.zero;
                    }

                    averageCeiNor += col.GetContact(i).normal;
                    averageCeiPos += col.GetContact(i).point;
                    if (ceilCount > 0)
                    {
                        averageCeiNor *= 0.5f;
                        averageCeiPos *= 0.5f;
                    }

                    OnCeilingCol?.Invoke(col.GetContact(i).normal, col.GetContact(i).point);

                    ceilCount++;
                }
                else
                {
                    if (wallCount == 0)
                    {
                        averageWallNor = Vector3.zero;
                        averageWallPos = Vector3.zero;
                    }

                    averageWallNor += col.GetContact(i).normal;
                    averageWallPos += col.GetContact(i).point;
                    if (wallCount > 0)
                    {
                        averageWallNor *= 0.5f;
                        averageWallPos *= 0.5f;
                    }
                    Vector3 flatWallCol = Extensions.ProjectDirectionOnPlane(col.GetContact(i).normal, transform.up);
                    float speedToWall = Vector3.Dot(InternalSpeed, -flatWallCol);
                    //Debug.DrawRay(averageWallPos, col.GetContact(i).normal, Color.magenta, Time.fixedDeltaTime);
                    if (stopOnWall && speedToWall > 0)
                        InternalSpeed += flatWallCol * speedToWall;

                    wallCount++;

                    OnWallCol?.Invoke(col.GetContact(i).normal, col.GetContact(i).point);
                }
            }

        }

        private void Update()
        {
            //Debug.DrawRay(transform.position, InternalSpeed, Color.green, Time.deltaTime);

        }

        private void FixedUpdate()
        {
            BeforeCol?.Invoke();
            if (skipNextCol)
            {
                connBody = null;
                ResetCol();
            }
            ClearCol();

            NormalCollisionChecks();

            skipNextCol = false;

            AfterCol?.Invoke();

            BeforePhys?.Invoke();
            if (doPhysics)
                NormalPhysics();
            AfterPhys?.Invoke();

            BeforeUploadSpeed?.Invoke();
            if (doSpeedUpload)
                physBody.velocity = InternalSpeed;
            if(connBody)
                transform.position+=connDiff;
            AfterUploadSpeed?.Invoke();
            ResetCol();

        }

        void NormalCollisionChecks()
        {
            bool willGround = false;

            if (!skipNextCol)
            {
                DoColNormal();

                UpdateConnection();

                if (colCount > 0)
                {
                    if (grounded)
                    {
                        finalSpeed = Quaternion.FromToRotation(oldNormal, averageFloorNor) * finalSpeed;
                    }
                    willGround = true;
                }
                else if (grounded)
                    willGround = TryGround();
            }
            if(willGround && grounded)
            {
                if (slopeSpeedAdjust)
                    finalSpeed = Quaternion.FromToRotation(oldNormal, averageFloorNor) * finalSpeed;
            }

            grounded = willGround;

            if (grounded)
            {
                slopeVector = (gravityDir - Vector3.Dot(gravityDir, averageFloorNor) * averageFloorNor).normalized;

                //Vector3 hSpeed = Vector3.zero, vSpeed = Vector3.zero;
                

                transform.rotation = Quaternion.FromToRotation(Vector3.up, averageFloorNor);
                oldNormal = averageFloorNor;
                keepRotation = 0;
                if (controlLockTimer >= 0 && doControlLock)
                {
                    controlLockTimer -= Time.fixedDeltaTime;
                }
            }
            else
            {
                keepRotation += 1;
                if (keepRotation < 5)
                {
                    transform.rotation = Quaternion.FromToRotation(Vector3.up, oldNormal);
                }
                else
                {
                    transform.rotation = Quaternion.FromToRotation(Vector3.up, averageFloorNor);
                    oldNormal = averageFloorNor;
                }
            }
        }

        void NormalPhysics()
        {
            if (!gravBasedSlope)
                DoSlopes();
            else
                DoSlopesPhys();

            if (grounded){
                if (doFriction)
                    DoFriction();
            }
            else
            {
                if (doAirDrag)
                    DoAirFriction();
            }

            if(doGravity)
                DoGravity();

            if(stopOnWall)
                DoStopOnWall();
        }

        void UpdateConnection()
        {

            if (connBody)
            {
                if (connBody == prevConnBody)
                {
                    connDiff = connBody.transform.TransformPoint(connLocalPos) - connWorldPos;
                }
                connWorldPos = transform.position;
                connLocalPos = connBody.transform.InverseTransformPoint(connWorldPos);
            }
        }

        Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        void DoColNormal()
        {
            switch (groundMethod)
            {
                case GroundNormalType.byRay:
                    {
                        ColByRay(-transform.up);
                        break;
                    }
                case GroundNormalType.basedOnSphere:
                    {
                        ColByRay(-averageFloorNor);
                        break;
                    }
            }

        }

        void ColByRay(Vector3 direction)
        {
            RaycastHit hit;
            Vector3 capsuleTop, capsuleBottom;
            physShape.ToWorldSpaceCapsule(out capsuleTop, out capsuleBottom, out _);

            averageFloorNor = -gravityDir;
            averageFloorPos = Vector3.zero;
            colCount = 0;
            connBody = null;

            if (Physics.Raycast(capsuleBottom + (-direction * 0.1f), direction, out hit, 0.2f, groundLayer))
            {
                if (Vector3.Dot(transform.up, hit.normal) > cosMaxAngle)
                {
                    Vector3 projectionVector = Vector3.Project(direction*(hit.distance-0.1f), -transform.up);
                    averageFloorPos = hit.point;
                    averageFloorNor = hit.normal;
                    colCount = 1;
                    if (movingLayer.Contains(hit.collider.gameObject.layer))
                    {
                        /*if(prevConnBody==null)
                            Debug.Log("Landed on plat");*/
                        connBody = hit.rigidbody;
                    }

                    transform.position=transform.position+projectionVector;
                }
            }
        }


        bool TryGround()
        {
            RaycastHit hit;
            Vector3 capsuleTop, capsuleBottom;
            physShape.ToWorldSpaceCapsule(out capsuleTop, out capsuleBottom, out _);

            if (Physics.Raycast(capsuleBottom + (transform.up * 0.25f), -transform.up, out hit, 0.25f+tryGroundDistance, groundLayer))
            {
                if (Vector3.Dot(transform.up, hit.normal) > cosMaxAngle)
                {
                    //Vector3 projectionVector = Vector3.Project(-transform.up * (hit.distance - 0.1f), -transform.up);
                    colCount = 1;
                    averageFloorPos = hit.point;
                    averageFloorNor = hit.normal;
                    if (movingLayer.Contains(hit.collider.gameObject.layer))
                    {
                        /*if (prevConnBody == null)
                            Debug.Log("Landed on plat");*/
                        connBody = hit.rigidbody;
                    }

                    transform.position = transform.position - (transform.up * (hit.distance - 0.25f));
                    return true;
                }
                /*else
                {
                    Debug.Log("TooMuchAngle: " + Vector3.Dot(transform.up, hit.normal));
                    return false;
                }*/

            }
            //Debug.Log("No Ground");
            return false;
        }

        void DoStopOnWall()
        {
            if (ceilCount > 0)
            {
                float dotFromCei = Vector3.Dot(InternalSpeed, -averageCeiNor);
                if (dotFromCei > 0)
                    InternalSpeed += averageCeiNor * dotFromCei;
            }
        }

        void DoFriction()
        {
            transform.BreakDownSpeed(finalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);
            if (hSpeed.magnitude > 0)
            {
                hSpeed = Vector3.MoveTowards(hSpeed, Vector3.zero, frc * Time.fixedDeltaTime);
                finalSpeed = transform.TransformDirection(hSpeed +vSpeed);
            }
        }

        void DoAirFriction()
        {
            transform.BreakDownSpeed(finalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);

            float vMagn = Vector3.Dot(InternalSpeed, -gravityDir);
            if (hSpeed.magnitude > airDragHrLimit && (0 < vMagn && vMagn < airDragLimit))
            {

                //float dragCalc = Mathf.Pow(hSpeed.magnitude, 2) * drag; // The variable 

                //hSpeed = Vector3.MoveTowards(hSpeed, Vector3.zero, dragCalc * Time.fixedDeltaTime);
                finalSpeed = transform.TransformDirection((hSpeed * (Mathf.Pow(drag,Time.fixedDeltaTime))) + vSpeed);
            }

        }

        void DoSlopes()
        {
            // based on-> https://stackoverflow.com/a/4372760 
            float slopeUpDot = Vector3.Dot(-gravityDir, averageFloorNor);
            //float speedInUp = Vector3.Dot(InternalSpeed, Vector3.up);
            Debug.Log("Slope up dot:" + slopeUpDot + "/" + cosMinAngle +", " + slopeFactor * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor)))));
            if (slopeUpDot < cosMinAngle|| controlLockTimer > 0)
            {
                Debug.DrawRay(transform.position, slopeVector, Color.red);
                InternalSpeed += slopeVector * (slopeFactor * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor))))) * Time.fixedDeltaTime;
            }
            //float newSpeedInUp = Vector3.Dot(InternalSpeed, Vector3.up);
            //Debug.Log("Added speed: " + (newSpeedInUp - speedInUp));
            DoSlopeSlip(slopeUpDot);
        }

        void DoSlopeSlip(float slopeUpDot)
        {
            if (InternalSpeed.magnitude < slipSpeedLimit && controlLockTimer < 0 && slopeUpDot < cosSlipAngle)
            {
                onSlopeSlip?.Invoke();
                grounded = false;
                colCount = 0;
                controlLockTimer = controlLockTime;
                if (slopeUpDot < cosMaxAngle)
                {
                    transform.rotation = Quaternion.FromToRotation(Vector3.up, -gravityDir);
                    oldNormal = -gravityDir;
                    MoveAbovePoint(averageFloorPos, averageFloorNor);
                }
            }
        }

        void DoSlopesPhys()
        {
            float slopeUpDot = Vector3.Dot(-gravityDir, averageFloorNor);
            if (slopeUpDot < cosMinAngle)
            {
                // based on-> https://stackoverflow.com/a/4372760 
                Vector3 downGrav = gravityDir * (gravityForce)*Time.fixedDeltaTime;

                Vector3 slope = (downGrav - Vector3.Dot(downGrav, averageFloorNor) * averageFloorNor);

                //Debug.Log("DownGrav = " + downGrav.magnitude + " slope = " + slope.magnitude);

                InternalSpeed += slope;// * (slp * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor))))) * Time.fixedDeltaTime;
            }

            DoSlopeSlip(slopeUpDot);

            
        }

        void DoGravity()
        {

            if (grounded)
            {
                transform.BreakDownSpeed(finalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);
                vSpeed = new Vector3(0.0f, 0.0f, 0.0f);
                finalSpeed = transform.TransformDirection(hSpeed + vSpeed);
            }
            else if (Vector3.Dot(InternalSpeed, gravityDir) < (maxGravityForce))
            {
                InternalSpeed += gravityDir * gravityForce * Time.fixedDeltaTime;
            }
        }

        void MoveAbovePoint(Vector3 point, Vector3 normal)
        {

            float yFactor = Vector3.Angle(normal, transform.rotation * Vector3.up);
            float xFactor = Vector3.Angle(normal, transform.rotation * Vector3.right);
            float zFactor = Vector3.Angle(normal, transform.rotation * Vector3.forward);

            yFactor = Mathf.Abs(1 - (yFactor >= 180 ? yFactor - 180 : yFactor) / 90);
            xFactor = Mathf.Abs(1 - (xFactor >= 180 ? xFactor - 180 : xFactor) / 90);
            zFactor = Mathf.Abs(1 - (zFactor >= 180 ? zFactor - 180 : zFactor) / 90);


            float fYPos = ((physShape.height / 2) * yFactor + physShape.radius * xFactor + physShape.radius * zFactor);

            transform.position = transform.position + normal * fYPos + transform.rotation * -physShape.center;
        }

        void ClearCol()
        {
            if (colCount == 0)
            {
                averageFloorNor = -gravityDir;
                averageFloorPos = Vector3.zero;
            }
            if (ceilCount == 0)
            {
                averageCeiNor = gravityDir;
                averageCeiPos = Vector3.zero;
            }

            if (wallCount == 0)
            {
                averageWallNor = -transform.forward;
                averageWallPos = Vector3.zero;
            }
        }

        void ResetCol()
        {
            wallCount = 0;
            ceilCount = 0;
            colCount = 0;

            if (!connBody && prevConnBody)
            {
                prevConnBody = null;
                finalSpeed += (connDiff / Time.fixedDeltaTime);
                connDiff = Vector3.zero;
                //Debug.Log("Launched");
            }

            prevConnBody = connBody;
            connBody = null;

        }
    }
}
