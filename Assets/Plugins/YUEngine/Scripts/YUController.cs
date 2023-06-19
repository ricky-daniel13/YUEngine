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
        Vector3 connWorldPos, connLocalPos, connDiff, connTorq, connWorldRota, connLocalRota, newNormal;
        [Header("Gravity")]
        public float gravityForce = 30;
        public float maxGravityForce=70;
        public Vector3 gravityDir=Vector3.down;
        [System.NonSerialized]
        public bool skipNextCol, forceNormal;

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

        public Vector3 GroundNormal
        {
            get
            {
                return averageFloorNor;
            }
            set
            {
                oldNormal = averageFloorNor;
                averageFloorNor = value;
                newNormal = value;
                forceNormal = true;
                Debug.Log("Normal setted! " + Time.deltaTime);
            }
        }

        public Vector3 GroundPosition
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

        public Vector3 ConnDiff { get => connDiff;}

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

        private void OnDisable()
        {
            physBody.velocity = Vector3.zero;
        }


        private void OnCollisionStay(Collision col)
        {
            if (!isActiveAndEnabled)
                return;
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
                    float speedToWall = Vector3.Dot(finalSpeed, -flatWallCol);
                    //Debug.DrawRay(averageWallPos, col.GetContact(i).normal, Color.magenta, Time.fixedDeltaTime);
                    if (stopOnWall && speedToWall > 0)
                        finalSpeed += flatWallCol * speedToWall;

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
            //Debug.Log("physvel: " + physBody.velocity.sqrMagnitude + ", finsoeed: " + finalSpeed.sqrMagnitude);
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
                physBody.velocity = finalSpeed;
            if (finalSpeed.sqrMagnitude == 0)
            {
                physBody.Sleep();
                //Debug.Log("Frozen!");
            }
            //Debug.Log("physvel: " + physBody.velocity.sqrMagnitude + ", connDif: " + connDiff);
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

                if (forceNormal)
                {
                    averageFloorNor = newNormal;
                    newNormal = Vector3.zero;
                    forceNormal = false;
                }

                if (colCount > 0)
                {
                    if (grounded)
                    {
                        //Debug.Log("Normal changed?: " + (oldNormal != averageFloorNor));
                        finalSpeed = Quaternion.FromToRotation(oldNormal, averageFloorNor) * finalSpeed;
                    }
                    willGround = true;
                }
                else if (grounded)
                    willGround = TryGround();
            }

            grounded = willGround;

            if (grounded)
            {
                slopeVector = Vector3.Cross(Vector3.Cross(averageFloorNor, gravityDir), averageFloorNor).normalized;
                Debug.DrawRay(transform.position, slopeVector);

                transform.rotation = Quaternion.FromToRotation(Vector3.up, averageFloorNor);
                oldNormal = averageFloorNor;
                keepRotation = 0;
                if (controlLockTimer > 0 && doControlLock)
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
           // Debug.Log("starting phys");
            if (!gravBasedSlope)
                DoSlopes();
            else
                DoSlopesPhys();

            transform.BreakDownSpeed(finalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);

            if (grounded){
                if (doFriction)
                    DoFriction(ref hSpeed, ref vSpeed);
            }
            else
            {
                if (doAirDrag)
                    DoAirFriction(ref hSpeed, ref vSpeed);
            }

            //Debug.Log("Speed after friction: " + hSpeed.sqrMagnitude + ", " + vSpeed.sqrMagnitude);

            if (doGravity)
                DoGravity(ref hSpeed, ref vSpeed);

            //Debug.Log("Speed after grav: " + hSpeed.sqrMagnitude + ", " + vSpeed.sqrMagnitude);

            finalSpeed = transform.TransformVector(hSpeed + vSpeed);

            //Debug.Log("Speed after trans: " + finalSpeed.sqrMagnitude);

            if (stopOnCeil)
                DoStopOnCei();
            //Debug.Log("Ending phys");
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

            }
            return false;
        }

        void DoStopOnCei()
        {
            if (ceilCount > 0)
            {
                float dotFromCei = Vector3.Dot(finalSpeed, -averageCeiNor);
                if (dotFromCei > 0)
                    finalSpeed += averageCeiNor * dotFromCei;
            }
        }

        void DoFriction(ref Vector3 hSpeed, ref Vector3 vSpeed)
        {
            //Debug.Log("speed: " + hSpeed.sqrMagnitude + ", " + vSpeed.sqrMagnitude + "speed to remove: " + frc * Time.fixedDeltaTime);
            hSpeed = Vector3.MoveTowards(hSpeed, Vector3.zero, frc * Time.fixedDeltaTime);
        }

        void DoAirFriction(ref Vector3 hSpeed, ref Vector3 vSpeed)
        {
            float vMagn = Vector3.Dot(vSpeed, transform.InverseTransformVector(-gravityDir));
            if (hSpeed.magnitude > airDragHrLimit && (0 < vMagn && vMagn < airDragLimit))
            {
                //float dragCalc = Mathf.Pow(hSpeed.magnitude, 2) * drag; // The variable 
                //hSpeed = Vector3.MoveTowards(hSpeed, Vector3.zero, dragCalc * Time.fixedDeltaTime);
                hSpeed = hSpeed * (Mathf.Pow(drag,Time.fixedDeltaTime));
            }

        }

        void DoSlopes()
        {
            // based on-> https://stackoverflow.com/a/4372760 
            float slopeUpDot = Vector3.Dot(-gravityDir, averageFloorNor);
            //Debug.Log("slopeupdot: " + slopeUpDot + "/" + cosMinAngle + ", timer: " + controlLockTimer + " speed to add: " + (slopeFactor * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor))))) * Time.fixedDeltaTime);
            if (slopeUpDot < cosMinAngle || controlLockTimer > 0)
            {
                Debug.DrawRay(transform.position, slopeVector, Color.red);
                finalSpeed += slopeVector * (slopeFactor * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor))))) * Time.fixedDeltaTime;
            }
            DoSlopeSlip(slopeUpDot);
        }

        void DoSlopeSlip(float slopeUpDot)
        {
            if (finalSpeed.magnitude < slipSpeedLimit && controlLockTimer <= 0 && slopeUpDot < cosSlipAngle)
            {
                Debug.Log("i FELT!");
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

                finalSpeed += slope;// * (slp * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-gravityDir, averageFloorNor))))) * Time.fixedDeltaTime;
            }

            DoSlopeSlip(slopeUpDot);
        }

        void DoGravity(ref Vector3 hSpeed, ref Vector3 vSpeed)
        {
            //Debug.Log("Received speed: " + hSpeed.sqrMagnitude + ", and " + vSpeed.sqrMagnitude);
            if (grounded)
            {
                vSpeed = Vector3.zero;
            }
            else if (Vector3.Dot(vSpeed, transform.InverseTransformVector(gravityDir)) < (maxGravityForce))
            {
                transform.BreakDownSpeed(gravityDir * gravityForce * Time.fixedDeltaTime, out Vector3 gravV, out Vector3 gravH);
                hSpeed += gravH;
                vSpeed += gravV;  
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
