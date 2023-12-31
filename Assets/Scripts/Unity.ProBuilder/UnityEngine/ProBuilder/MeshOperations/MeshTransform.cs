using System;

namespace UnityEngine.ProBuilder.MeshOperations
{
	public static class MeshTransform
	{
		internal static void SetPivot(this ProBuilderMesh mesh, PivotLocation pivotType, int firstVertexIndex = 0)
		{
			switch (pivotType)
			{
			case PivotLocation.Center:
				mesh.CenterPivot(null);
				break;
			case PivotLocation.FirstVertex:
				mesh.CenterPivot(new int[1] { firstVertexIndex });
				break;
			}
		}

		public static void CenterPivot(this ProBuilderMesh mesh, int[] indexes)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			Vector3 zero = Vector3.zero;
			if (indexes != null && indexes.Length != 0)
			{
				Vector3[] positionsInternal = mesh.positionsInternal;
				if (positionsInternal == null || positionsInternal.Length < 3)
				{
					return;
				}
				foreach (int num in indexes)
				{
					zero += positionsInternal[num];
				}
				zero = mesh.transform.TransformPoint(zero / indexes.Length);
			}
			else
			{
				zero = mesh.transform.TransformPoint(mesh.mesh.bounds.center);
			}
			Vector3 offset = mesh.transform.position - zero;
			mesh.transform.position = zero;
			mesh.ToMesh();
			mesh.TranslateVerticesInWorldSpace(mesh.mesh.triangles, offset);
			mesh.Refresh();
		}

		public static void SetPivot(this ProBuilderMesh mesh, Vector3 worldPosition)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			Vector3 offset = mesh.transform.position - worldPosition;
			mesh.transform.position = worldPosition;
			mesh.ToMesh();
			mesh.TranslateVerticesInWorldSpace(mesh.mesh.triangles, offset);
			mesh.Refresh();
		}

		public static void FreezeScaleTransform(this ProBuilderMesh mesh)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			Vector3[] positionsInternal = mesh.positionsInternal;
			for (int i = 0; i < positionsInternal.Length; i++)
			{
				positionsInternal[i] = Vector3.Scale(positionsInternal[i], mesh.transform.localScale);
			}
			mesh.transform.localScale = new Vector3(1f, 1f, 1f);
		}
	}
}
