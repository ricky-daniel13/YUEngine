using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YU2.Splines
{
    public abstract class Spline : MonoBehaviour
    {
        public abstract Vector3 GetPoint(float t);
        public abstract Vector3 GetVelocity(float t);
        public abstract Vector3 GetTangent(float t);
        public abstract Vector3 GetBinormal(float t);
        public abstract Vector3 GetNormal(float t);
    }
}
