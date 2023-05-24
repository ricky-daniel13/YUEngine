using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float rad, azim, elev, radSpeed, azimSpeed, elevSpeed, defRad, defAzim, defElev, lookSpeed,minAngle,fovSpeed,normalFOV,fastFov,maxAngle;
    float radVel, azimVel, elevVel,fovVel,currFov;
    public Vector3 playerOffset;
    public Transform player, cam;
    Camera camera;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentUp=Vector3.up, currentDir = Vector3.up, upVel, lookVel,lastPlayerPos;
    bool shouldMove = false;

    private void Start()
    {
        lastPlayerPos = player.transform.position;
        camera = cam.GetComponent<Camera>();
        currFov = normalFOV;
    }
    private void LateUpdate() {
        Vector3 localCamPos;

        Vector3 playerVelocity = (player.transform.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.transform.position;
        float groundSpeed = Vector3.ProjectOnPlane(playerVelocity, player.up).magnitude;
        //Debug.Log("Fov calc = " + Mathf.Max(0, groundSpeed - 12) / 12 + ", groundSpeed: " + groundSpeed);
        currFov = Mathf.SmoothDamp(currFov, Mathf.Lerp(normalFOV, fastFov, Mathf.Max(0, groundSpeed - 12) / 12), ref fovVel, fovSpeed);
        camera.fieldOfView = currFov;

        currentUp = Vector3.SmoothDamp(currentUp, Vector3.Angle(Vector3.up, player.up)  < maxAngle ? Vector3.up : player.up, ref upVel, 0.2f);
        localMat.SetTRS(player.transform.position + playerOffset, Quaternion.FromToRotation(Vector3.up, currentUp), Vector3.one);
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        Vector3 sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;
        
        rad = Mathf.SmoothDamp(rad, defRad, ref radVel, radSpeed);



        float targetElev = Mathf.Asin(Mathf.Max(Mathf.Min(localCamPos.y, rad-Mathf.Epsilon),-(rad-Mathf.Epsilon))/rad);

        Debug.Log("targetElev: r=" + targetElev + ", a=" + Mathf.Rad2Deg*targetElev);
        
        elev = Mathf.Deg2Rad*Mathf.SmoothDampAngle(elev*Mathf.Rad2Deg, targetElev*Mathf.Rad2Deg, ref elevVel, elevSpeed);
        
        

        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);

        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        
        currentDir = cam.transform.forward;
        Vector3 toPlayerDir = ((player.transform.position + playerOffset) - cam.transform.position).normalized;
        float angleBetweenDirs = Vector3.Angle(currentDir, toPlayerDir);
        
        shouldMove = (!shouldMove && angleBetweenDirs > minAngle) || (shouldMove && angleBetweenDirs > Mathf.Epsilon);
        Vector3 upAngle = Mathf.Abs(Vector3.Dot(toPlayerDir, Vector3.up)) > 0.9f ? player.up : Vector3.up;

        cam.transform.rotation = Extensions.SmoothDampQuaternion(cam.transform.rotation, Quaternion.LookRotation(toPlayerDir, upAngle), ref lookVel, lookSpeed);
    }

    
}
