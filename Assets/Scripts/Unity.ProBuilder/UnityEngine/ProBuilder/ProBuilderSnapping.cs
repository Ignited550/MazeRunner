using System.Collections.Generic;

namespace UnityEngine.ProBuilder
{
	internal static class ProBuilderSnapping
	{
		private const float k_MaxRaySnapDistance = float.PositiveInfinity;

		public static Vector3 SnapValue(Vector3 vertex, float snpVal)
		{
			return new Vector3(snpVal * Mathf.Round(vertex.x / snpVal), snpVal * Mathf.Round(vertex.y / snpVal), snpVal * Mathf.Round(vertex.z / snpVal));
		}

		public static float SnapValue(float val, float snpVal)
		{
			if (snpVal < Mathf.Epsilon)
			{
				return val;
			}
			return snpVal * Mathf.Round(val / snpVal);
		}

		public static Vector3 SnapValue(Vector3 vertex, Vector3 snap)
		{
			float x = vertex.x;
			float y = vertex.y;
			float z = vertex.z;
			return new Vector3((Mathf.Abs(snap.x) < 0.0001f) ? x : (snap.x * Mathf.Round(x / snap.x)), (Mathf.Abs(snap.y) < 0.0001f) ? y : (snap.y * Mathf.Round(y / snap.y)), (Mathf.Abs(snap.z) < 0.0001f) ? z : (snap.z * Mathf.Round(z / snap.z)));
		}

		public static void SnapVertices(ProBuilderMesh mesh, IEnumerable<int> indexes, Vector3 snap)
		{
			Vector3[] positionsInternal = mesh.positionsInternal;
			foreach (int index in indexes)
			{
				positionsInternal[index] = mesh.transform.InverseTransformPoint(SnapValue(mesh.transform.TransformPoint(positionsInternal[index]), snap));
			}
		}

		internal static Vector3 GetSnappingMaskBasedOnNormalVector(Vector3 normal)
		{
			return new Vector3(Mathf.Approximately(Mathf.Abs(normal.x), 1f) ? 0f : 1f, Mathf.Approximately(Mathf.Abs(normal.y), 1f) ? 0f : 1f, Mathf.Approximately(Mathf.Abs(normal.z), 1f) ? 0f : 1f);
		}

		internal static Vector3 SnapValueOnRay(Ray ray, float distance, float snap, Vector3Mask mask)
		{
			float num = float.PositiveInfinity;
			Ray ray2 = new Ray(ray.origin, ray.direction);
			Ray ray3 = new Ray(ray.origin, -ray.direction);
			for (int i = 0; i < 3; i++)
			{
				if (!(mask[i] > 0f))
				{
					continue;
				}
				Vector3Mask vector3Mask = new Vector3Mask(new Vector3Mask((byte)(1 << i)));
				Vector3 vector = Vector3.Project(ray.direction * Math.MakeNonZero(distance), vector3Mask * Mathf.Sign(ray.direction[i]));
				Vector3 vertex = ray.origin + vector;
				Plane plane = new Plane(vector3Mask, SnapValue(vertex, vector3Mask * snap));
				if (Mathf.Abs(plane.GetDistanceToPoint(ray.origin)) < 0.0001f)
				{
					num = 0f;
					continue;
				}
				if (plane.Raycast(ray2, out var enter) && Mathf.Abs(enter) < Mathf.Abs(num))
				{
					num = enter;
				}
				if (plane.Raycast(ray3, out enter) && Mathf.Abs(enter) < Mathf.Abs(num))
				{
					num = 0f - enter;
				}
			}
			return ray.origin + ray.direction * ((Mathf.Abs(num) >= float.PositiveInfinity) ? distance : num);
		}
	}
}
