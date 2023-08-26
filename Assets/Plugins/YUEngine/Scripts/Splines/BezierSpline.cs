using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YU2.Splines
{
	public class BezierSpline : Spline
	{
		[SerializeField]
		private BezierCurve[] points;
		private bool loop;
		[SerializeField]
		private bool editable = true;
		public bool Loop
		{
			get
			{
				return loop;
			}
			set
			{
				loop = value;
				if (value == true)
				{
					points[points.Length-1].mode = points[points.Length - 1].mode;
					SetControlPoint(0, BezierKind.Point,points[0].point);
				}
			}
		}



		public int CurveCount
		{
			get
			{
				return points.Length-1;
			}
		}

		public void SetPointArray(BezierCurve[] points, bool isEditable = true)
        {
			this.points = points;
			this.editable = isEditable;
        }

		public bool GetEditable()
        {
			return editable;
        }

		public Vector3 GetControlPoint(int index, BezierKind kind)
		{
			if(kind==BezierKind.Point)
				return points[index].point;
			else if(kind == BezierKind.TangentIn)
				return points[index].inPoint;
			else
				return points[index].outPoint;
		}

		public BezierControlPointMode GetControlPointMode(int index)
		{
			return points[index].mode;
		}

		public void SetControlPointMode(int index, BezierControlPointMode mode)
		{
			points[index].mode = mode;
			if (loop)
			{
				if (index == 0)
					points[CurveCount].mode = mode;
				if (index == CurveCount)
					points[0].mode = mode;
				
			}


			EnforceMode(index, BezierKind.Point);
		}

		public void SetControlPoint(int index, BezierKind kind, Vector3 point)
		{
			if (kind == BezierKind.Point)
			{
				Vector3 delta = point - points[index].point;
				if (loop)
				{
					if(index == 0)
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
						points[CurveCount].point = point;
					}
					else if(index == CurveCount)
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
						points[0].point = point;
					}
					else
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
					}
				}
				else
				{
					points[index].outPoint += delta;
					points[index].inPoint += delta;
					
				}

				points[index].point = point;
			}
			else if (kind == BezierKind.TangentIn)
				points[index].inPoint = point;
			else
				points[index].outPoint = point;

			EnforceMode(index, kind);
		}

		private void EnforceMode(int index, BezierKind kind)
		{
			var mode = points[index].mode;

			if (mode == BezierControlPointMode.Free)
			{
				return;
			}

			Vector3 middle = points[index].point;
			int fixedIdx, enforceIdx;
			BezierKind fixedKind, enforceKind;

			//Si seleccionamos tangent out o es el primer indice, ese es nuestro anchor, de lo contrario, tangent in es.
			fixedKind = (kind == BezierKind.Point || index == 0 ? BezierKind.TangentOut : kind);
			fixedIdx = index;

			if (loop && (index == 0 || index == CurveCount))
			{
				if (index == 0)
				{
					fixedKind = BezierKind.TangentOut;
					enforceIdx = CurveCount;
					enforceKind = BezierKind.TangentIn;
				}
				else
				{
					fixedKind = BezierKind.TangentIn;
					enforceIdx = 0;
					enforceKind = BezierKind.TangentOut;
				}
			}
			else
			{
				if (fixedKind == BezierKind.TangentOut)
				{
					enforceIdx = index;
					enforceKind = BezierKind.TangentIn;
				}
				else
				{
					enforceIdx = index;
					enforceKind = BezierKind.TangentOut;
				}
			}


			Vector3 enforcedTangent = middle - (fixedKind == BezierKind.TangentOut ? points[fixedIdx].outPoint : points[fixedIdx].inPoint); 
			
			if (mode == BezierControlPointMode.Aligned)
			{
				enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, (enforceKind == BezierKind.TangentOut ? points[enforceIdx].outPoint : points[enforceIdx].inPoint));
			}

			if(enforceKind == BezierKind.TangentIn)
				points[enforceIdx].inPoint = middle + enforcedTangent;
			else
				points[enforceIdx].outPoint = middle + enforcedTangent;
		}

		public override Vector3 GetPoint(float t)
		{
			int i;
			float realT = t;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length-2;
			}
			else
			{
				t = Mathf.Clamp01(t) * (points.Length - 1);
				i = (int)t;
				t -= i;
			}

			return transform.TransformPoint(Bezier.GetPoint(points[i].point, points[i].outPoint, points[i+1].inPoint, points[i+1].point, t));
		}

		public override Vector3 GetVelocity(float t)
		{
			int i;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length-2;
			}
			else
			{
				t = Mathf.Clamp01(t) * (points.Length - 1);
				i = (int)t;
				t -= i;
			}

			return transform.TransformDirection(
				Bezier.GetFirstDerivative(points[i].point, points[i].outPoint, points[i + 1].inPoint, points[i + 1].point, t));
		}

		public override Vector3 GetTangent(float t)
		{
			return GetVelocity(t).normalized;
		}

		public override Vector3 GetBinormal(float t)
		{
			return Vector3.Cross(Vector3.up, GetTangent(t));
		}

		public override Vector3 GetNormal(float t)
		{
			return Vector3.Cross(GetTangent(t), GetBinormal(t)).normalized;
		}

		public void Reset()
		{
			points = new BezierCurve[2];
			points[0].inPoint = new Vector3(-1f, 0f, 0f);
			points[0].point = new Vector3(0f, 0f, 0f);
			points[0].outPoint = new Vector3(1f, 0f, 0f);
			points[1].inPoint = new Vector3(2f, 0f, 0f);
			points[1].point = new Vector3(3f, 0f, 0f);
			points[1].outPoint = new Vector3(4f, 0f, 0f);
		}

		public void AddCurve()
		{
			Array.Resize(ref points, points.Length + 1);
			points[points.Length - 1] = points[points.Length - 2];
			points[points.Length - 1].inPoint.x += 1;
			points[points.Length - 1].point.x = points[points.Length - 1].inPoint.x + 1;
			points[points.Length - 1].outPoint.x = points[points.Length - 1].inPoint.x + 2;

			if (loop) {
				points[points.Length - 1] = points[0];
				EnforceMode(0, BezierKind.Point);
			}

			EnforceMode(points.Length-1, BezierKind.Point);
		}

		[ContextMenu("Transform to double")]
        public void TransformToDouble()
        {
			var parSpline = new GameObject(this.name + " Parent");
			var rightSpline = new GameObject(this.name + " Right");
			
			parSpline.transform.SetPositionAndRotation(transform.position, transform.rotation);
			parSpline.transform.SetParent(transform.parent, true);

			rightSpline.transform.SetPositionAndRotation(transform.position, transform.rotation);
			rightSpline.transform.SetParent(parSpline.transform, true);
			transform.SetParent(parSpline.transform, true);

			var SplCom = rightSpline.AddComponent<BezierSpline>();

			BezierCurve[] newPoints = new BezierCurve[points.Length];
			Array.Copy(points, newPoints, points.Length);
			float length = 2f;

			

			for (int i = 0; i < newPoints.Length; i++)
            {
				float t = (float)i / (float)(points.Length-1);
				Debug.Log("New point "+ i + "/" + newPoints.Length +"   at T" + t);
				Vector3 biNorm = transform.InverseTransformDirection(GetBinormal(t));

				newPoints[i].point += biNorm.normalized * length;
				newPoints[i].outPoint += biNorm.normalized * length;
				newPoints[i].inPoint += biNorm.normalized * length;

				//if (i < newPoints.Length - 1)
					
			}

			SplCom.SetPointArray(newPoints);

			var dSpline = parSpline.AddComponent<DoubleSpline>();
			dSpline.left = this;
			dSpline.right = SplCom;
			

		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
        {
			Vector3 p0 = transform.TransformPoint(GetControlPoint(0, BezierKind.Point));
			for (int i = 0; i < CurveCount; i += 1)
			{
				Vector3 p1 = transform.TransformPoint(GetControlPoint(i, BezierKind.TangentOut));
				Vector3 p2 = transform.TransformPoint(GetControlPoint(i+1, BezierKind.TangentIn));
				Vector3 p3 = transform.TransformPoint(GetControlPoint(i+1, BezierKind.Point));
				Handles.DrawBezier(p0, p3, p1, p2, Color.grey, null, 1f);
				p0 = p3;
			}
		}
#endif
	}
}
