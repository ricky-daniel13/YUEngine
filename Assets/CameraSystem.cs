using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float rad, azim, elev, radSpeed, azimSpeed, elevSpeed, defRad, defAzim, defElev, lookSpeed,fovSpeed,fovNormal,fovFast,maxAngle,angleSpeed, playerOffset;
    float radVel, azimVel, elevVel,fovVel,currFov;
    public Transform player, cam;
    Camera camera;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentUp=Vector3.up, currentDir = Vector3.up, upVel, lookVel,lastPlayerPos;
    bool shouldMove = false;

    private void Start()
    {
        lastPlayerPos = player.transform.position;
        camera = cam.GetComponent<Camera>();
        currFov = fovNormal;
    }
    private void LateUpdate() {
        Vector3 localCamPos;

        Vector3 playerVelocity = (player.transform.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.transform.position;
        float groundSpeed = Vector3.ProjectOnPlane(playerVelocity, player.up).magnitude;

        currFov = Mathf.SmoothDamp(currFov, Mathf.Lerp(fovNormal, fovFast, Mathf.Max(0, groundSpeed - 12) / 12), ref fovVel, fovSpeed);
        camera.fieldOfView = currFov;

        float globalUpDot = Vector3.Dot(currentUp, Vector3.up);

        currentUp = Vector3.SmoothDamp(currentUp, Vector3.Angle(Vector3.up, player.up)  < maxAngle ? Vector3.up : player.up, ref upVel, angleSpeed);
        localMat.SetTRS(player.transform.position + (Vector3.up*globalUpDot*playerOffset), Quaternion.FromToRotation(Vector3.up, currentUp), Vector3.one);
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        Vector3 sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;

        rad = Mathf.SmoothDamp(rad, defRad, ref radVel, radSpeed);
        float targetElev = Mathf.Asin(Mathf.Max(Mathf.Min(defElev, rad-Mathf.Epsilon),-(rad-Mathf.Epsilon))/rad);
        elev = Mathf.Deg2Rad*Mathf.SmoothDampAngle(elev*Mathf.Rad2Deg, targetElev*Mathf.Rad2Deg, ref elevVel, elevSpeed);
        
        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);

        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        
        currentDir = cam.transform.forward;
        Vector3 toPlayerDir = ((player.transform.position + (Vector3.up * globalUpDot * playerOffset)) - cam.transform.position).normalized;

        Vector3 upAngle = Mathf.Abs(Vector3.Dot(toPlayerDir, Vector3.up)) > 0.9f ? player.up : Vector3.up;

        cam.transform.rotation = Extensions.SmoothDampQuaternion(cam.transform.rotation, Quaternion.LookRotation(toPlayerDir, upAngle), ref lookVel, lookSpeed);
    }

    
}
