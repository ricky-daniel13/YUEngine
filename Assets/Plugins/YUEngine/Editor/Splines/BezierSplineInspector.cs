using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YU2.Splines {
	[CustomEditor(typeof(BezierSpline))]
	public class BezierSplineInspector : Editor {
		private const int stepsPerCurve = 3;

		private const float handleSize = 0.04f;
		private const float pickSize = 0.06f;

		private int selectedIndex = -1;
		private BezierKind selectedKind = BezierKind.Point;

		private const float directionScale = 0.25f;

		private BezierSpline spline;
		private Transform handleTransform;
		private Quaternion handleRotation;

		private static Color[] modeColors = {
		Color.white,
		Color.yellow,
		Color.cyan
		};

		public override void OnInspectorGUI()
		{
			spline = target as BezierSpline;

			EditorGUI.BeginChangeCheck();
			bool loop = EditorGUILayout.Toggle("Loop", spline.Loop);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Toggle Loop");
				EditorUtility.SetDirty(spline);
				spline.Loop = loop;
			}

			if (selectedIndex >= 0 && selectedIndex < spline.CurveCount+1)
			{
				DrawSelectedPointInspector();
			}

			if (GUILayout.Button("Add Curve"))
			{
				Undo.RecordObject(spline, "Add Curve");
				spline.AddCurve();
				EditorUtility.SetDirty(spline);
			}
		}

		private void OnSceneGUI()
		{
			spline = target as BezierSpline;
			handleTransform = spline.transform;
			handleRotation = Tools.pivotRotation == PivotRotation.Local ?
				handleTransform.rotation : Quaternion.identity;

			Vector3 p0 = ShowPoint(0, BezierKind.Point);
			for (int i = 0; i < spline.CurveCount; i += 1)
			{
				Vector3 p1 = ShowPoint(i, BezierKind.TangentOut);
				Vector3 p2 = ShowPoint(i+1, BezierKind.TangentIn);
				Vector3 p3 = ShowPoint(i+1, BezierKind.Point);

				//Handles.color = Color.gray;
				//Handles.DrawLine(p0, p1);
				//Handles.DrawLine(p2, p3);

				//ShowDirections();
				Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
				p0 = p3;
			}
		}

		private void ShowDirections()
		{
			Handles.color = Color.blue;
			Vector3 point = spline.GetPoint(0f);
			Handles.DrawLine(point, point + spline.GetTangent(0f) * directionScale);
			Handles.color = Color.red;
			Handles.DrawLine(point, point + spline.GetBinormal(0f) * directionScale);
			Handles.color = Color.green;
			Handles.DrawLine(point, point + spline.GetNormal(0f) * directionScale);
			int steps = stepsPerCurve * spline.CurveCount;
			for (int i = 1; i <= steps; i++)
			{
				point = spline.GetPoint(i / (float)steps);
				Handles.color = Color.blue;
				Handles.DrawLine(point, point + spline.GetTangent(i / (float)steps) * directionScale);
				Handles.color = Color.red;
				Handles.DrawLine(point, point + spline.GetBinormal(i / (float)steps) * directionScale);
				Handles.color = Color.green;
				Handles.DrawLine(point, point + spline.GetNormal(i / (float)steps) * directionScale);
			}
		}

		private void DrawSelectedPointInspector()
		{
			GUILayout.Label("Selected Point: " + selectedIndex + ", " + selectedKind);
			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex, selectedKind));
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move Point");
				EditorUtility.SetDirty(spline);
				spline.SetControlPoint(selectedIndex, selectedKind, point);
			}
			EditorGUI.BeginChangeCheck();
			BezierControlPointMode mode = (BezierControlPointMode)
				EditorGUILayout.EnumPopup("Mode", spline.GetControlPointMode(selectedIndex));
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Change Point Mode");
				spline.SetControlPointMode(selectedIndex, mode);
				EditorUtility.SetDirty(spline);
			}
		}

		private Vector3 ShowPoint(int index, BezierKind kind)
		{
			Vector3 point;

			point = handleTransform.TransformPoint(spline.GetControlPoint(index,kind));
			if(!spline.GetEditable())
				return point;

			float size = HandleUtility.GetHandleSize(point);
			Handles.color = modeColors[(int)spline.GetControlPointMode(index)];

			if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
			{
				selectedIndex = index;
				selectedKind = kind;

				Repaint();
			}
			if (selectedIndex == index && selectedKind == kind)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(spline, "Move Point");
					EditorUtility.SetDirty(spline);
					spline.SetControlPoint(index, kind, handleTransform.InverseTransformPoint(point));
				}
			}
			return point;

		}
	}
}
