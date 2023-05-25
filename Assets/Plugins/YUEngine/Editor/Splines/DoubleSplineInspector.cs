using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YU2.Splines {
	[CustomEditor(typeof(DoubleSpline))]
	public class DoubleSplineInspector : Editor
    {
		private const int stepsPerCurve = 2;
		private const float directionScale = 0.25f;
		// Start is called before the first frame update

		private void OnSceneGUI()
		{
			DoubleSpline spline = target as DoubleSpline;
			if (!spline.left||!spline.right)
			{
				return;
			}
			int steps = stepsPerCurve * spline.left.CurveCount;
			for (int i = 0; i <= steps; i++)
			{
				float t = (i / (float)steps);
				Vector3 point = spline.GetPoint(i / (float)steps);
				Handles.color = Color.gray;
				Handles.DrawLine(spline.left.GetPoint(i / (float)steps), spline.right.GetPoint(i / (float)steps));


				Handles.color = Color.blue;
				Handles.DrawLine(point, point + spline.GetTangent(i / (float)steps) * directionScale);
				Handles.color = Color.red;
				Handles.DrawLine(point, point + spline.GetBinormal(i / (float)steps) * directionScale);
				Handles.color = Color.green;
				Handles.DrawLine(point, point + spline.GetNormal(i / (float)steps) * directionScale);
			}

			Vector3 p0 = spline.left.transform.TransformPoint(spline.left.GetControlPoint(0, BezierKind.Point));
			for (int i = 0; i < spline.left.CurveCount; i += 1)
			{
				
				Vector3 p1 = spline.left.transform.TransformPoint(spline.left.GetControlPoint(i, BezierKind.TangentOut));
				Vector3 p2 = spline.left.transform.TransformPoint(spline.left.GetControlPoint(i + 1, BezierKind.TangentIn));
				Vector3 p3 = spline.left.transform.TransformPoint(spline.left.GetControlPoint(i + 1, BezierKind.Point));

				Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
				p0 = p3;
			}

			p0 = spline.right.transform.TransformPoint(spline.right.GetControlPoint(0, BezierKind.Point));
			for (int i = 0; i < spline.left.CurveCount; i += 1)
			{

				Vector3 p1 = spline.right.transform.TransformPoint(spline.right.GetControlPoint(i, BezierKind.TangentOut));
				Vector3 p2 = spline.right.transform.TransformPoint(spline.right.GetControlPoint(i + 1, BezierKind.TangentIn));
				Vector3 p3 = spline.right.transform.TransformPoint(spline.right.GetControlPoint(i + 1, BezierKind.Point));

				Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
				p0 = p3;
			}
		}
	}
}
