using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YU2.Splines
{
	public class DoubleSpline : Spline
	{
		[SerializeField]
		public BezierSpline left, right;

		public override Vector3 GetPoint(float t)
		{
			if (!left || !right)
				return Vector3.zero;
			return Vector3.Lerp(left.GetPoint(t), right.GetPoint(t), 0.5f);
		}

		public override Vector3 GetVelocity(float t)
		{
			if (!left || !right)
				return Vector3.zero;
			return Vector3.Slerp(left.GetVelocity(t), right.GetVelocity(t), 0.5f);
		}

		public override Vector3 GetTangent(float t)
		{
			if (!left || !right)
				return Vector3.zero;
			return GetVelocity(t).normalized;
		}

		public override Vector3 GetBinormal(float t)
		{
			if (!left || !right)
				return Vector3.zero;
			Vector3 retVec = (right.GetPoint(t) - GetPoint(t)).normalized;
			Vector3 tang = GetTangent(t);
			Vector3.OrthoNormalize(ref tang, ref retVec);
			return retVec;
		}

		public override Vector3 GetNormal(float t)
		{
			if (!left || !right)
				return Vector3.zero;
			return Vector3.Cross(GetTangent(t), GetBinormal(t)).normalized;
		}
	}
}
