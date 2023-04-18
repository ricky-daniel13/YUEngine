using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YU2.Splines
{
	
	public enum BezierControlPointMode
	{
		Free,
		Aligned,
		Mirrored
	}

	public enum BezierKind
	{
		Point,
		TangentIn,
		TangentOut
	}

	public enum PutOnPathMode
	{
		BinormalOnly,
		NormalOnly,
		BinormalAndNormal
	}


	[System.Serializable]
	public struct BezierCurve
	{
		public Vector3 point;
		public Vector3 inPoint;
		public Vector3 outPoint;
		public BezierControlPointMode mode;
	}

	[System.Serializable]
	public struct BezierNormalCurve
	{
		public Vector3 point;
		public Vector3 normal;
		public Vector3 inPoint;
		public Vector3 outPoint;
		public BezierControlPointMode mode;
	}

	public class BezierKnot
	{
		public Vector3 point;
		public Vector3 tangent;
		public Vector3 binormal;
		public Vector3 normal;
		public float time;

		public BezierKnot()
		{
		}

		public BezierKnot(Vector3 point, Vector3 tangent, Vector3 binormal, Vector3 normal, float time)
		{
			this.point = point;
			this.tangent = tangent;
			this.binormal = binormal;
			this.normal = normal;
			this.time = time;
		}
	}


	public class Bezier : MonoBehaviour
	{
		public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			t = Mathf.Clamp01(t);
			float oneMinusT = 1f - t;
			return
				oneMinusT * oneMinusT * oneMinusT * p0 +
				3f * oneMinusT * oneMinusT * t * p1 +
				3f * oneMinusT * t * t * p2 +
				t * t * t * p3;
		}

		public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			t = Mathf.Clamp01(t);
			float oneMinusT = 1f - t;
			return
				3f * oneMinusT * oneMinusT * (p1 - p0) +
				6f * oneMinusT * t * (p2 - p1) +
				3f * t * t * (p3 - p2);
		}
	}
}
