using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static float InputAngle(this Vector2 vector2)
    {
        if (vector2.x < 0)
        {
            return 360 - (Mathf.Atan2(vector2.x, vector2.y) * Mathf.Rad2Deg * -1);
        }
        else
        {
            return Mathf.Atan2(vector2.x, vector2.y) * Mathf.Rad2Deg;
        }
    }

    public static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    private static Vector3 AbsVec3(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    public static byte GetAnimatorParameterIndex(this Animator anim, string paramName)
    {
        for (byte i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].name == paramName)
            {
                return i;
            }
        }
        Debug.LogError("Parameter " + paramName + " doesn't exist in the animator parameter list!");
        return 0;
    }

    public static void BreakDownSpeed(this Transform trans, Vector3 speed, out Vector3 vSpeed, out Vector3 hSpeed)
    {
        Vector3 localVelocity = trans.InverseTransformDirection(speed);

        hSpeed = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
        vSpeed = new Vector3(0.0f, localVelocity.y, 0.0f);
    }

    public static void ToWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius)
    {
        var center = capsule.transform.TransformPoint(capsule.center);
        radius = 0f;
        float height = 0f;
        Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
        Vector3 dir = Vector3.zero;

        switch (capsule.direction)
        {
            case 0: // x
                radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
                height = lossyScale.x * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.right);
                break;
            case 1: // y
                radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
                height = lossyScale.y * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.up);
                break;
            case 2: // z
                radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
                height = lossyScale.z * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.forward);
                break;
        }

        if (height < radius * 2f)
        {
            dir = Vector3.zero;
        }

        point0 = center + dir * (height * 0.5f);
        point1 = center - dir * (height * 0.5f);
    }

    public static bool Contains(this LayerMask layer, int otherLayer)
    {
        return ((layer & (1 << otherLayer)) != 0);
    }

    public static void ToWorldSpaceCapsulePoints(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius)
    {
        var center = capsule.transform.TransformPoint(capsule.center);
        radius = 0f;
        float height = 0f;
        Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
        Vector3 dir = Vector3.zero;

        switch (capsule.direction)
        {
            case 0: // x
                radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
                height = lossyScale.x * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.right);
                break;
            case 1: // y
                radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
                height = lossyScale.y * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.up);
                break;
            case 2: // z
                radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
                height = lossyScale.z * capsule.height;
                dir = capsule.transform.TransformDirection(Vector3.forward);
                break;
        }

        if (height < radius * 2f)
        {
            dir = Vector3.zero;
        }

        point0 = center + dir * (height * 0.5f - radius);
        point1 = center - dir * (height * 0.5f - radius);
    }

    public static float DegCos(float angle)
    {
        return Mathf.Cos(angle * Mathf.Deg2Rad);
    }

    public static float DegSin(float angle)
    {
        return Mathf.Sin(angle * Mathf.Deg2Rad);
    }

    // Obtenido de -> https://forum.unity.com/threads/fix-inside-quaternion-fromtorotation-discontinuity.706514/
    static public Quaternion FromToRot(Vector3 dir1, Vector3 dir2,
                  Quaternion whenOppositeVectors = default(Quaternion))
    {

        float r = 1f + Vector3.Dot(dir1, dir2);

        if (r < 1E-6f)
        {
            if (whenOppositeVectors == default(Quaternion))
            {
                // simply get the default behavior
                return Quaternion.FromToRotation(dir1, dir2);
            }
            return whenOppositeVectors;
        }

        Vector3 w = Vector3.Cross(dir1, dir2);
        return new Quaternion(w.x, w.y, w.z, r).normalized;
    }

    public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
          Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
          Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
          Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
        );
    }

    public static Vector3 SphericalToCartesian(float radius, float polar, float elevation)
    {
        float a = radius * Mathf.Cos(elevation);
        Vector3 outCart = Vector3.zero;
        outCart.x = a * Mathf.Cos(polar);
        outCart.y = radius * Mathf.Sin(elevation);
        outCart.z = a * Mathf.Sin(polar);
        return outCart;
    }

    public static Vector3 CartesianToSpherical(Vector3 cartCoords)
    {
        float outRadius, outPolar, outElevation;
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;
        outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                        + (cartCoords.y * cartCoords.y)
                        + (cartCoords.z * cartCoords.z));
        outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            outPolar += Mathf.PI;
        outElevation = Mathf.Asin(cartCoords.y / outRadius);
        return new Vector3(outRadius, outPolar, outElevation);
    }
}
