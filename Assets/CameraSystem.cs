using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float rad, azim, elev, radSpeed, azimSpeed, elevSpeed, defRad, defAzim, defElev;
    float radVel, azimVel, elevVel;
    public Vector3 playerOffset;
    public Transform player, cam;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentUp=Vector3.up, upVel;
    private void LateUpdate() {
        Vector3 localCamPos;
        currentUp = Vector3.SmoothDamp(currentUp, Vector3.Angle(currentUp, player.up)  < 30 ? Vector3.up : player.up, ref upVel, 0.2f);
        localMat.SetTRS(player.transform.position + playerOffset, Quaternion.FromToRotation(Vector3.up, currentUp), Vector3.one);
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        CartesianToSpherical(localCamPos, out rad, out azim, out elev);
        
        rad = Mathf.SmoothDamp(rad, defRad, ref radVel, radSpeed);
        
        Vector3 localXzCam = Vector3.ProjectOnPlane(localCamPos, Vector3.up);
        float elevDif = Vector3.Angle((localXzCam).normalized, (localXzCam + (Vector3.up*defElev)).normalized);
        elev = Mathf.Deg2Rad*Mathf.SmoothDampAngle(elev*Mathf.Rad2Deg, elevDif, ref elevVel, elevSpeed);
        
        SphericalToCartesian(rad, azim, elev, out localCamPos);

        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);;
        cam.transform.LookAt(player.transform.position + playerOffset, Vector3.up);
    }

    public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart){
        float a = radius * Mathf.Cos(elevation);
        outCart.x = a * Mathf.Cos(polar);
        outCart.y = radius * Mathf.Sin(elevation);
        outCart.z = a * Mathf.Sin(polar);
    }

    public static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation){
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;
        outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                        + (cartCoords.y * cartCoords.y)
                        + (cartCoords.z * cartCoords.z));
        outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            outPolar += Mathf.PI;
        outElevation = Mathf.Asin(cartCoords.y / outRadius);
    }
}
