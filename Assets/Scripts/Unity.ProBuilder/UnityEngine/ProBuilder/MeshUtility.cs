using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine.ProBuilder
{
	public static class MeshUtility
	{
		internal static Vertex[] GeneratePerTriangleMesh(Mesh mesh)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			Vertex[] vertices = mesh.GetVertices();
			int subMeshCount = mesh.subMeshCount;
			Vertex[] array = new Vertex[mesh.triangles.Length];
			int[][] array2 = new int[subMeshCount][];
			int num = 0;
			for (int i = 0; i < subMeshCount; i++)
			{
				array2[i] = mesh.GetTriangles(i);
				int num2 = array2[i].Length;
				for (int j = 0; j < num2; j++)
				{
					array[num++] = new Vertex(vertices[array2[i][j]]);
					array2[i][j] = num - 1;
				}
			}
			Vertex.SetMesh(mesh, array);
			mesh.subMeshCount = subMeshCount;
			for (int k = 0; k < subMeshCount; k++)
			{
				mesh.SetTriangles(array2[k], k);
			}
			return array;
		}

		public static void GenerateTangent(Mesh mesh)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;
			int num = triangles.Length;
			int num2 = vertices.Length;
			Vector3[] array = new Vector3[num2];
			Vector3[] array2 = new Vector3[num2];
			Vector4[] array3 = new Vector4[num2];
			for (long num3 = 0L; num3 < num; num3 += 3)
			{
				long num4 = triangles[num3];
				long num5 = triangles[num3 + 1];
				long num6 = triangles[num3 + 2];
				Vector3 vector = vertices[num4];
				Vector3 vector2 = vertices[num5];
				Vector3 vector3 = vertices[num6];
				Vector2 vector4 = uv[num4];
				Vector2 vector5 = uv[num5];
				Vector2 vector6 = uv[num6];
				float num7 = vector2.x - vector.x;
				float num8 = vector3.x - vector.x;
				float num9 = vector2.y - vector.y;
				float num10 = vector3.y - vector.y;
				float num11 = vector2.z - vector.z;
				float num12 = vector3.z - vector.z;
				float num13 = vector5.x - vector4.x;
				float num14 = vector6.x - vector4.x;
				float num15 = vector5.y - vector4.y;
				float num16 = vector6.y - vector4.y;
				float num17 = 1f / (num13 * num16 - num14 * num15);
				Vector3 vector7 = new Vector3((num16 * num7 - num15 * num8) * num17, (num16 * num9 - num15 * num10) * num17, (num16 * num11 - num15 * num12) * num17);
				Vector3 vector8 = new Vector3((num13 * num8 - num14 * num7) * num17, (num13 * num10 - num14 * num9) * num17, (num13 * num12 - num14 * num11) * num17);
				array[num4] += vector7;
				array[num5] += vector7;
				array[num6] += vector7;
				array2[num4] += vector8;
				array2[num5] += vector8;
				array2[num6] += vector8;
			}
			for (long num18 = 0L; num18 < num2; num18++)
			{
				Vector3 normal = normals[num18];
				Vector3 tangent = array[num18];
				Vector3.OrthoNormalize(ref normal, ref tangent);
				array3[num18].x = tangent.x;
				array3[num18].y = tangent.y;
				array3[num18].z = tangent.z;
				array3[num18].w = ((Vector3.Dot(Vector3.Cross(normal, tangent), array2[num18]) < 0f) ? (-1f) : 1f);
			}
			mesh.tangents = array3;
		}

		public static Mesh DeepCopy(Mesh source)
		{
			Mesh mesh = new Mesh();
			CopyTo(source, mesh);
			return mesh;
		}

		public static void CopyTo(Mesh source, Mesh destination)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			Vector3[] array = new Vector3[source.vertices.Length];
			int[][] array2 = new int[source.subMeshCount][];
			Vector2[] array3 = new Vector2[source.uv.Length];
			Vector2[] array4 = new Vector2[source.uv2.Length];
			Vector4[] array5 = new Vector4[source.tangents.Length];
			Vector3[] array6 = new Vector3[source.normals.Length];
			Color32[] array7 = new Color32[source.colors32.Length];
			Array.Copy(source.vertices, array, array.Length);
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = source.GetTriangles(i);
			}
			Array.Copy(source.uv, array3, array3.Length);
			Array.Copy(source.uv2, array4, array4.Length);
			Array.Copy(source.normals, array6, array6.Length);
			Array.Copy(source.tangents, array5, array5.Length);
			Array.Copy(source.colors32, array7, array7.Length);
			destination.Clear();
			destination.name = source.name;
			destination.vertices = array;
			destination.subMeshCount = array2.Length;
			for (int j = 0; j < array2.Length; j++)
			{
				destination.SetTriangles(array2[j], j);
			}
			destination.uv = array3;
			destination.uv2 = array4;
			destination.tangents = array5;
			destination.normals = array6;
			destination.colors32 = array7;
		}

		internal static T GetMeshChannel<T>(GameObject gameObject, Func<Mesh, T> attributeGetter) where T : IList
		{
			if (gameObject == null)
			{
				throw new ArgumentNullException("gameObject");
			}
			if (attributeGetter == null)
			{
				throw new ArgumentNullException("attributeGetter");
			}
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			Mesh mesh = ((component != null) ? component.sharedMesh : null);
			T result = default(T);
			if (mesh == null)
			{
				return result;
			}
			int vertexCount = mesh.vertexCount;
			MeshRenderer component2 = gameObject.GetComponent<MeshRenderer>();
			Mesh mesh2 = ((component2 != null) ? component2.additionalVertexStreams : null);
			if (mesh2 != null)
			{
				result = attributeGetter(mesh2);
				if (result != null && result.Count == vertexCount)
				{
					return result;
				}
			}
			result = attributeGetter(mesh);
			if (result == null || result.Count != vertexCount)
			{
				return default(T);
			}
			return result;
		}

		public static string Print(Mesh mesh)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"vertices: {mesh.vertexCount}\ntriangles: {mesh.triangles.Length}\nsubmeshes: {mesh.subMeshCount}");
			stringBuilder.AppendLine(string.Format("     {0,-28}{1,-28}{2,-28}{3,-28}{4,-28}{5,-28}{6,-28}{7,-28}", "Positions", "Normals", "Colors", "Tangents", "UV0", "UV2", "UV3", "UV4"));
			Vector3[] array = mesh.vertices;
			Vector3[] array2 = mesh.normals;
			Color[] array3 = mesh.colors;
			Vector4[] array4 = mesh.tangents;
			List<Vector4> list = new List<Vector4>();
			Vector2[] array5 = mesh.uv2;
			List<Vector4> list2 = new List<Vector4>();
			List<Vector4> list3 = new List<Vector4>();
			mesh.GetUVs(0, list);
			mesh.GetUVs(2, list2);
			mesh.GetUVs(3, list3);
			if (array != null && array.Count() != mesh.vertexCount)
			{
				array = null;
			}
			if (array2 != null && array2.Count() != mesh.vertexCount)
			{
				array2 = null;
			}
			if (array3 != null && array3.Count() != mesh.vertexCount)
			{
				array3 = null;
			}
			if (array4 != null && array4.Count() != mesh.vertexCount)
			{
				array4 = null;
			}
			if (list.Count() != mesh.vertexCount)
			{
				list = null;
			}
			if (array5.Count() != mesh.vertexCount)
			{
				array5 = null;
			}
			if (list2.Count() != mesh.vertexCount)
			{
				list2 = null;
			}
			if (list3.Count() != mesh.vertexCount)
			{
				list3 = null;
			}
			stringBuilder.AppendLine("# Attributes");
			int i = 0;
			for (int vertexCount = mesh.vertexCount; i < vertexCount; i++)
			{
				stringBuilder.AppendLine(string.Format("\t{8,-5}{0,-28}{1,-28}{2,-28}{3,-28}{4,-28}{5,-28}{6,-28}{7,-28}", (array == null) ? "null" : $"{array[i].x:F3}, {array[i].y:F3}, {array[i].z:F3}", (array2 == null) ? "null" : $"{array2[i].x:F3}, {array2[i].y:F3}, {array2[i].z:F3}", (array3 == null) ? "null" : $"{array3[i].r:F2}, {array3[i].g:F2}, {array3[i].b:F2}, {array3[i].a:F2}", (array4 == null) ? "null" : $"{array4[i].x:F2}, {array4[i].y:F2}, {array4[i].z:F2}, {array4[i].w:F2}", (list == null) ? "null" : $"{list[i].x:F2}, {list[i].y:F2}, {list[i].z:F2}, {list[i].w:F2}", (array5 == null) ? "null" : $"{array5[i].x:F2}, {array5[i].y:F2}", (list2 == null) ? "null" : $"{list2[i].x:F2}, {list2[i].y:F2}, {list2[i].z:F2}, {list2[i].w:F2}", (list3 == null) ? "null" : $"{list3[i].x:F2}, {list3[i].y:F2}, {list3[i].z:F2}, {list3[i].w:F2}", i));
			}
			stringBuilder.AppendLine("# Topology");
			for (int j = 0; j < mesh.subMeshCount; j++)
			{
				MeshTopology topology = mesh.GetTopology(j);
				int[] indices = mesh.GetIndices(j);
				stringBuilder.AppendLine($"  Submesh[{j}] ({topology})");
				switch (topology)
				{
				case MeshTopology.Points:
				{
					for (int n = 0; n < indices.Length; n++)
					{
						stringBuilder.AppendLine($"\t{indices[n]}");
					}
					break;
				}
				case MeshTopology.Lines:
				{
					for (int l = 0; l < indices.Length; l += 2)
					{
						stringBuilder.AppendLine($"\t{indices[l]}, {indices[l + 1]}");
					}
					break;
				}
				case MeshTopology.Triangles:
				{
					for (int m = 0; m < indices.Length; m += 3)
					{
						stringBuilder.AppendLine($"\t{indices[m]}, {indices[m + 1]}, {indices[m + 2]}");
					}
					break;
				}
				case MeshTopology.Quads:
				{
					for (int k = 0; k < indices.Length; k += 4)
					{
						stringBuilder.AppendLine($"\t{indices[k]}, {indices[k + 1]}, {indices[k + 2]}, {indices[k + 3]}");
					}
					break;
				}
				}
			}
			return stringBuilder.ToString();
		}

		public static uint GetIndexCount(Mesh mesh)
		{
			uint num = 0u;
			if (mesh == null)
			{
				return num;
			}
			int i = 0;
			for (int subMeshCount = mesh.subMeshCount; i < subMeshCount; i++)
			{
				num += mesh.GetIndexCount(i);
			}
			return num;
		}

		public static uint GetPrimitiveCount(Mesh mesh)
		{
			uint num = 0u;
			if (mesh == null)
			{
				return num;
			}
			int i = 0;
			for (int subMeshCount = mesh.subMeshCount; i < subMeshCount; i++)
			{
				if (mesh.GetTopology(i) == MeshTopology.Triangles)
				{
					num += mesh.GetIndexCount(i) / 3u;
				}
				else if (mesh.GetTopology(i) == MeshTopology.Quads)
				{
					num += mesh.GetIndexCount(i) / 4u;
				}
			}
			return num;
		}

		public static void Compile(ProBuilderMesh probuilderMesh, Mesh targetMesh, MeshTopology preferredTopology = MeshTopology.Triangles)
		{
			if (probuilderMesh == null)
			{
				throw new ArgumentNullException("probuilderMesh");
			}
			if (targetMesh == null)
			{
				throw new ArgumentNullException("targetMesh");
			}
			targetMesh.Clear();
			targetMesh.vertices = probuilderMesh.positionsInternal;
			targetMesh.uv = probuilderMesh.texturesInternal;
			if (probuilderMesh.HasArrays(MeshArrays.Texture2))
			{
				List<Vector4> uvs = new List<Vector4>();
				probuilderMesh.GetUVs(2, uvs);
				targetMesh.SetUVs(2, uvs);
			}
			if (probuilderMesh.HasArrays(MeshArrays.Texture3))
			{
				List<Vector4> uvs2 = new List<Vector4>();
				probuilderMesh.GetUVs(3, uvs2);
				targetMesh.SetUVs(3, uvs2);
			}
			targetMesh.normals = probuilderMesh.GetNormals();
			targetMesh.tangents = probuilderMesh.GetTangents();
			if (probuilderMesh.HasArrays(MeshArrays.Color))
			{
				targetMesh.colors = probuilderMesh.colorsInternal;
			}
			int submeshCount = probuilderMesh.GetComponent<Renderer>().sharedMaterials.Length;
			Submesh[] submeshes = Submesh.GetSubmeshes(probuilderMesh.facesInternal, submeshCount, preferredTopology);
			targetMesh.subMeshCount = submeshes.Length;
			for (int i = 0; i < targetMesh.subMeshCount; i++)
			{
				targetMesh.SetIndices(submeshes[i].m_Indexes, submeshes[i].m_Topology, i, calculateBounds: false);
			}
			targetMesh.name = $"pb_Mesh{probuilderMesh.id}";
		}

		public static Vertex[] GetVertices(this Mesh mesh)
		{
			if (mesh == null)
			{
				return null;
			}
			int vertexCount = mesh.vertexCount;
			Vertex[] array = new Vertex[vertexCount];
			Vector3[] vertices = mesh.vertices;
			Color[] colors = mesh.colors;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			Vector2[] uv = mesh.uv;
			Vector2[] uv2 = mesh.uv2;
			List<Vector4> list = new List<Vector4>();
			List<Vector4> list2 = new List<Vector4>();
			mesh.GetUVs(2, list);
			mesh.GetUVs(3, list2);
			bool flag = vertices != null && vertices.Count() == vertexCount;
			bool flag2 = colors != null && colors.Count() == vertexCount;
			bool flag3 = normals != null && normals.Count() == vertexCount;
			bool flag4 = tangents != null && tangents.Count() == vertexCount;
			bool flag5 = uv != null && uv.Count() == vertexCount;
			bool flag6 = uv2 != null && uv2.Count() == vertexCount;
			bool flag7 = list.Count() == vertexCount;
			bool flag8 = list2.Count() == vertexCount;
			for (int i = 0; i < vertexCount; i++)
			{
				array[i] = new Vertex();
				if (flag)
				{
					array[i].position = vertices[i];
				}
				if (flag2)
				{
					array[i].color = colors[i];
				}
				if (flag3)
				{
					array[i].normal = normals[i];
				}
				if (flag4)
				{
					array[i].tangent = tangents[i];
				}
				if (flag5)
				{
					array[i].uv0 = uv[i];
				}
				if (flag6)
				{
					array[i].uv2 = uv2[i];
				}
				if (flag7)
				{
					array[i].uv3 = list[i];
				}
				if (flag8)
				{
					array[i].uv4 = list2[i];
				}
			}
			return array;
		}

		public static void CollapseSharedVertices(Mesh mesh, Vertex[] vertices = null)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (vertices == null)
			{
				vertices = mesh.GetVertices();
			}
			int subMeshCount = mesh.subMeshCount;
			List<Dictionary<Vertex, int>> list = new List<Dictionary<Vertex, int>>();
			int[][] array = new int[subMeshCount][];
			int num = 0;
			for (int i = 0; i < subMeshCount; i++)
			{
				array[i] = mesh.GetTriangles(i);
				Dictionary<Vertex, int> dictionary = new Dictionary<Vertex, int>();
				for (int j = 0; j < array[i].Length; j++)
				{
					Vertex key = vertices[array[i][j]];
					if (dictionary.TryGetValue(key, out var value))
					{
						array[i][j] = value;
						continue;
					}
					array[i][j] = num;
					dictionary.Add(key, num);
					num++;
				}
				list.Add(dictionary);
			}
			Vertex[] vertices2 = list.SelectMany((Dictionary<Vertex, int> x) => x.Keys).ToArray();
			Vertex.SetMesh(mesh, vertices2);
			mesh.subMeshCount = subMeshCount;
			for (int k = 0; k < subMeshCount; k++)
			{
				mesh.SetTriangles(array[k], k);
			}
		}

		internal static string SanityCheck(ProBuilderMesh mesh)
		{
			return SanityCheck(mesh.GetVertices());
		}

		internal static string SanityCheck(Mesh mesh)
		{
			return SanityCheck(mesh.GetVertices());
		}

		internal static string SanityCheck(IList<Vertex> vertices)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int i = 0;
			for (int count = vertices.Count; i < count; i++)
			{
				Vertex vertex = vertices[i];
				if (!Math.IsNumber(vertex.position) || !Math.IsNumber(vertex.color) || !Math.IsNumber(vertex.uv0) || !Math.IsNumber(vertex.normal) || !Math.IsNumber(vertex.tangent) || !Math.IsNumber(vertex.uv2) || !Math.IsNumber(vertex.uv3) || !Math.IsNumber(vertex.uv4))
				{
					stringBuilder.AppendFormat("vertex {0} contains invalid values:\n{1}\n\n", i, vertex.ToString());
				}
			}
			return stringBuilder.ToString();
		}
	}
}
