using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2.Splines;

public class Path : MonoBehaviour
{
	public Spline spl;

	private float currentDistance;
	private float lastDistance = float.MaxValue;
	private float closestFloat = 0;
	private float f = 0;
	private float lastT;

	[Range(0.01f, 0.001f)]
	public float pathFindPrecision = 0.01f;
	public bool fastMode = true;
	private Vector3 pathPosition;
	// Start is called before the first frame update
	public float ClosestPoint(Vector3 point, float precision)
	{
		lastDistance = float.MaxValue;
		closestFloat = 0;
		f = 0;

		while (f <= 1)
		{
			Vector3 diff = spl.GetPoint(f) - point;

			currentDistance = diff.sqrMagnitude;

			if (currentDistance < lastDistance)
			{
				closestFloat = f;

				lastDistance = currentDistance;
			}
			f += precision;
		}
		return closestFloat;
	}

	public float ClosestPointFast(Vector3 point)
	{
		float multiplier = 0.1f;
		float lastMultiplier = 0;

		float pt = ClosestPoint(point, multiplier);

		for (int i = 0; i < 20; i++)
		{
			lastMultiplier = multiplier;
			multiplier *= 0.5f;
			pt = CalculateClosest(point, pt, lastMultiplier, multiplier);
		}

		return pt;
	}

	public float CalculateClosest(Vector3 point, float current, float lastPrecision, float precision)
	{
		lastDistance = float.MaxValue;
		closestFloat = 0;
		f = current - lastPrecision;

		while (f < current + lastPrecision)
		{
			Vector3 diff = spl.GetPoint(f) - point;

			currentDistance = diff.sqrMagnitude;

			if (currentDistance < lastDistance)
			{
				closestFloat = f;

				lastDistance = currentDistance;
			}

			f += precision;
		}

		return closestFloat;
	}

	public BezierKnot GetKnot(float t)
	{
		return new BezierKnot(spl.GetPoint(t), spl.GetTangent(t), spl.GetBinormal(t), spl.GetNormal(t), t);
	}


	public Vector3 PutOnPath(Vector3 position, Vector3 normal, PutOnPathMode putOnPathMode, out BezierKnot bezierKnot, out float closestTimeOnSpline, float atractForce = 15, float binormalOffset = 0.5f)
	{
		closestTimeOnSpline = fastMode == true ? ClosestPointFast(position) : ClosestPoint(position, pathFindPrecision);
		bezierKnot = GetKnot(closestTimeOnSpline);

		Vector3 rgt;

		rgt = Extensions.ProjectDirectionOnPlane(bezierKnot.binormal, normal);
		//forw = Extensions.ProjectDirectionOnPlane(bezierKnot.tangent, target.up);

		//up = Vector3.Cross(forw, rgt);

		float rgtAmount = Vector3.Dot(rgt, bezierKnot.point);
		Vector3 rgtVector = rgt * rgtAmount;
		float rgtTrgAmount = Vector3.Dot(rgt, position);
		Vector3 newPos = position - (rgt * rgtTrgAmount);
		return Vector3.MoveTowards(position, newPos + rgtVector, (atractForce <= 0 ? Mathf.Infinity : atractForce) * Time.fixedDeltaTime);
	}

}
