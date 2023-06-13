using System.Collections.Generic;

namespace UnityEngine.ProBuilder
{
	internal static class UvUnwrapping
	{
		private static Vector2 s_TempVector2 = Vector2.zero;

		private static readonly List<int> s_IndexBuffer = new List<int>(64);

		internal static void Unwrap(ProBuilderMesh mesh, Face face, Vector3 projection = default(Vector3))
		{
			Projection.PlanarProject(mesh, face, (projection != Vector3.zero) ? projection : Vector3.zero);
			ApplyUVSettings(mesh.texturesInternal, face.distinctIndexesInternal, face.uv);
		}

		internal static void CopyUVs(ProBuilderMesh mesh, Face source, Face dest)
		{
			Vector2[] texturesInternal = mesh.texturesInternal;
			int[] distinctIndexesInternal = source.distinctIndexesInternal;
			int[] distinctIndexesInternal2 = dest.distinctIndexesInternal;
			for (int i = 0; i < distinctIndexesInternal.Length; i++)
			{
				texturesInternal[distinctIndexesInternal2[i]].x = texturesInternal[distinctIndexesInternal[i]].x;
				texturesInternal[distinctIndexesInternal2[i]].y = texturesInternal[distinctIndexesInternal[i]].y;
			}
		}

		internal static void ProjectTextureGroup(ProBuilderMesh mesh, int group, AutoUnwrapSettings unwrapSettings)
		{
			Projection.PlanarProject(mesh, group, unwrapSettings);
			s_IndexBuffer.Clear();
			Face[] facesInternal = mesh.facesInternal;
			foreach (Face face in facesInternal)
			{
				if (face.textureGroup == group)
				{
					s_IndexBuffer.AddRange(face.distinctIndexesInternal);
				}
			}
			ApplyUVSettings(mesh.texturesInternal, s_IndexBuffer, unwrapSettings);
		}

		private static void ApplyUVSettings(Vector2[] uvs, IList<int> indexes, AutoUnwrapSettings uvSettings)
		{
			int count = indexes.Count;
			switch (uvSettings.fill)
			{
			case AutoUnwrapSettings.Fill.Fit:
				FitUVs(uvs, indexes);
				break;
			case AutoUnwrapSettings.Fill.Stretch:
				StretchUVs(uvs, indexes);
				break;
			}
			if (uvSettings.scale.x != 1f || uvSettings.scale.y != 1f || uvSettings.rotation != 0f)
			{
				Vector2 origin = Bounds2D.Center(uvs, indexes);
				for (int i = 0; i < count; i++)
				{
					uvs[indexes[i]] = uvs[indexes[i]].ScaleAroundPoint(origin, uvSettings.scale);
					uvs[indexes[i]] = uvs[indexes[i]].RotateAroundPoint(origin, uvSettings.rotation);
				}
			}
			if (!uvSettings.useWorldSpace && uvSettings.anchor != AutoUnwrapSettings.Anchor.None)
			{
				ApplyUVAnchor(uvs, indexes, uvSettings.anchor);
			}
			if (uvSettings.flipU || uvSettings.flipV || uvSettings.swapUV)
			{
				for (int j = 0; j < count; j++)
				{
					float num = uvs[indexes[j]].x;
					float num2 = uvs[indexes[j]].y;
					if (uvSettings.flipU)
					{
						num = 0f - num;
					}
					if (uvSettings.flipV)
					{
						num2 = 0f - num2;
					}
					if (!uvSettings.swapUV)
					{
						uvs[indexes[j]].x = num;
						uvs[indexes[j]].y = num2;
					}
					else
					{
						uvs[indexes[j]].x = num2;
						uvs[indexes[j]].y = num;
					}
				}
			}
			for (int k = 0; k < indexes.Count; k++)
			{
				uvs[indexes[k]].x -= uvSettings.offset.x;
				uvs[indexes[k]].y -= uvSettings.offset.y;
			}
		}

		private static void StretchUVs(Vector2[] uvs, IList<int> indexes)
		{
			Bounds2D bounds2D = new Bounds2D();
			bounds2D.SetWithPoints(uvs, indexes);
			Vector2 center = bounds2D.center;
			Vector2 size = bounds2D.size;
			for (int i = 0; i < indexes.Count; i++)
			{
				Vector2 vector = uvs[indexes[i]];
				vector.x = (vector.x - center.x) / size.x + center.x;
				vector.y = (vector.y - center.y) / size.y + center.y;
				uvs[indexes[i]] = vector;
			}
		}

		private static void FitUVs(Vector2[] uvs, IList<int> indexes)
		{
			Bounds2D bounds2D = new Bounds2D();
			bounds2D.SetWithPoints(uvs, indexes);
			Vector2 center = bounds2D.center;
			float num = Mathf.Max(bounds2D.size.x, bounds2D.size.y);
			for (int i = 0; i < indexes.Count; i++)
			{
				Vector2 vector = uvs[indexes[i]];
				vector.x = (vector.x - center.x) / num + center.x;
				vector.y = (vector.y - center.y) / num + center.y;
				uvs[indexes[i]] = vector;
			}
		}

		private static void ApplyUVAnchor(Vector2[] uvs, IList<int> indexes, AutoUnwrapSettings.Anchor anchor)
		{
			s_TempVector2.x = 0f;
			s_TempVector2.y = 0f;
			Vector2 vector = Math.SmallestVector2(uvs, indexes);
			Vector2 vector2 = Math.LargestVector2(uvs, indexes);
			switch (anchor)
			{
			case AutoUnwrapSettings.Anchor.UpperLeft:
			case AutoUnwrapSettings.Anchor.MiddleLeft:
			case AutoUnwrapSettings.Anchor.LowerLeft:
				s_TempVector2.x = vector.x;
				break;
			case AutoUnwrapSettings.Anchor.UpperRight:
			case AutoUnwrapSettings.Anchor.MiddleRight:
			case AutoUnwrapSettings.Anchor.LowerRight:
				s_TempVector2.x = vector2.x - 1f;
				break;
			default:
				s_TempVector2.x = vector.x + (vector2.x - vector.x) * 0.5f - 0.5f;
				break;
			}
			switch (anchor)
			{
			case AutoUnwrapSettings.Anchor.UpperLeft:
			case AutoUnwrapSettings.Anchor.UpperCenter:
			case AutoUnwrapSettings.Anchor.UpperRight:
				s_TempVector2.y = vector2.y - 1f;
				break;
			case AutoUnwrapSettings.Anchor.MiddleLeft:
			case AutoUnwrapSettings.Anchor.MiddleCenter:
			case AutoUnwrapSettings.Anchor.MiddleRight:
				s_TempVector2.y = vector.y + (vector2.y - vector.y) * 0.5f - 0.5f;
				break;
			default:
				s_TempVector2.y = vector.y;
				break;
			}
			int count = indexes.Count;
			for (int i = 0; i < count; i++)
			{
				uvs[indexes[i]].x -= s_TempVector2.x;
				uvs[indexes[i]].y -= s_TempVector2.y;
			}
		}
	}
}
