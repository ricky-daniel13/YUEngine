using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCamera : MonoBehaviour
{
    [SerializeField]
    private float rotatePower;
    [SerializeField]
    private float maxStandAngle;
    [SerializeField]
    private float timeBeforeAutoControl;
    [SerializeField]
    private float hzSpeed;
    [SerializeField]
    private float vrSpeed;
    [SerializeField]
    private float vrSpeedFast;
    [SerializeField]
    private float vrMinPlayerSpeedToMove;
    [SerializeField]
    private float vrMinPlayerSpeed;
    [SerializeField]
    private float vrMaxPlayerSpeed;
    [SerializeField]
    private float vrMinDifferenceToMove;

    [SerializeField]
    private Transform controllerTransform;
    [SerializeField]
    private Rigidbody controllerBody;
    [SerializeField]
    private Transform modelTransform;

    private Quaternion currHRota, currVRota, HDampDeriv, VDampDeriv, targetVRota;
    private float currCameraTimer;

    //QuatSmoothDamp;
    public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
    {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }


    // Start is called before the first frame update
    void Start()
    {
        currHRota = Quaternion.identity;
        currVRota = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion targetHRota, newVRota;

        float cosMaxAngle = Extensions.DegCos(maxStandAngle);
        if (Vector3.Dot(Vector3.up, controllerTransform.up) < cosMaxAngle)
            targetHRota = controllerTransform.rotation;
        else
            targetHRota = Quaternion.identity;


        currHRota = SmoothDamp(currHRota, targetHRota, ref HDampDeriv, hzSpeed);

        float inpDelta = Input.GetAxis("Mouse X");
        if (inpDelta > 0.1f || inpDelta < -0.1f)
            currCameraTimer = timeBeforeAutoControl;

        currCameraTimer -= Time.deltaTime;

        if(currCameraTimer > 0)
        {
            currVRota *= Quaternion.Euler(new Vector3(0, (inpDelta * rotatePower)*Time.deltaTime, 0));
            targetVRota = currVRota;
        }
        else
        {

            if (controllerBody.velocity.magnitude > vrMinPlayerSpeedToMove)
            {
                newVRota = Quaternion.Euler(new Vector3(0, (Quaternion.Inverse(targetHRota) * modelTransform.rotation).eulerAngles.y, 0));
                //Debug.Log("Dot from rotations: " + Quaternion.Dot(newVRota, targetVRota) + " check passed?" + (Quaternion.Dot(newVRota, targetVRota) < vrMinDifferenceToMove) );
                if (Quaternion.Dot(newVRota, targetVRota) < vrMinDifferenceToMove)
                    targetVRota = newVRota;
            }
            float verticalSpeed = Mathf.Lerp(vrSpeed, vrSpeedFast, Mathf.Max(0, controllerBody.velocity.magnitude - vrMinPlayerSpeed) / (vrMaxPlayerSpeed - vrMinPlayerSpeed));

            currVRota = SmoothDamp(currVRota, targetVRota, ref VDampDeriv, verticalSpeed);
            //targetVRota = Quaternion.Euler(new Vector3(0, (Quaternion.Inverse(targetHRota) * modelTransform.rotation).eulerAngles.y, 0));

        }

        transform.rotation = currHRota * currVRota;

    }
}
