using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnityEngine.ProBuilder.MeshOperations
{
	public sealed class MeshImporter
	{
		private static readonly MeshImportSettings k_DefaultImportSettings = new MeshImportSettings
		{
			quads = true,
			smoothing = true,
			smoothingAngle = 1f
		};

		private Mesh m_SourceMesh;

		private Material[] m_SourceMaterials;

		private ProBuilderMesh m_Destination;

		private Vertex[] m_Vertices;

		public MeshImporter(GameObject gameObject)
		{
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			m_SourceMesh = component.sharedMesh;
			if (m_SourceMesh == null)
			{
				throw new ArgumentNullException("gameObject", "GameObject does not contain a valid MeshFilter.sharedMesh.");
			}
			m_Destination = gameObject.DemandComponent<ProBuilderMesh>();
			m_SourceMaterials = gameObject.GetComponent<MeshRenderer>()?.sharedMaterials;
		}

		public MeshImporter(Mesh sourceMesh, Material[] sourceMaterials, ProBuilderMesh destination)
		{
			if (sourceMesh == null)
			{
				throw new ArgumentNullException("sourceMesh");
			}
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			m_SourceMesh = sourceMesh;
			m_SourceMaterials = sourceMaterials;
			m_Destination = destination;
		}

		[Obsolete]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public MeshImporter(ProBuilderMesh destination)
		{
			m_Destination = destination;
		}

		[Obsolete]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool Import(GameObject go, MeshImportSettings importSettings = null)
		{
			try
			{
				m_SourceMesh = go.GetComponent<MeshFilter>().sharedMesh;
				m_SourceMaterials = go.GetComponent<MeshRenderer>()?.sharedMaterials;
				Import(importSettings);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
			return true;
		}

		public void Import(MeshImportSettings importSettings = null)
		{
			if (importSettings == null)
			{
				importSettings = k_DefaultImportSettings;
			}
			Vertex[] vertices = m_SourceMesh.GetVertices();
			List<Vertex> list = new List<Vertex>();
			List<Face> list2 = new List<Face>();
			int num = 0;
			int num2 = ((m_SourceMaterials != null) ? m_SourceMaterials.Length : 0);
			for (int i = 0; i < m_SourceMesh.subMeshCount; i++)
			{
				switch (m_SourceMesh.GetTopology(i))
				{
				case MeshTopology.Triangles:
				{
					int[] indices2 = m_SourceMesh.GetIndices(i);
					for (int k = 0; k < indices2.Length; k += 3)
					{
						list2.Add(new Face(new int[3]
						{
							num,
							num + 1,
							num + 2
						}, Math.Clamp(i, 0, num2 - 1), AutoUnwrapSettings.tile, 0, -1, -1, manualUVs: true));
						list.Add(vertices[indices2[k]]);
						list.Add(vertices[indices2[k + 1]]);
						list.Add(vertices[indices2[k + 2]]);
						num += 3;
					}
					break;
				}
				case MeshTopology.Quads:
				{
					int[] indices = m_SourceMesh.GetIndices(i);
					for (int j = 0; j < indices.Length; j += 4)
					{
						list2.Add(new Face(new int[6]
						{
							num,
							num + 1,
							num + 2,
							num + 2,
							num + 3,
							num
						}, Math.Clamp(i, 0, num2 - 1), AutoUnwrapSettings.tile, 0, -1, -1, manualUVs: true));
						list.Add(vertices[indices[j]]);
						list.Add(vertices[indices[j + 1]]);
						list.Add(vertices[indices[j + 2]]);
						list.Add(vertices[indices[j + 3]]);
						num += 4;
					}
					break;
				}
				default:
					throw new NotSupportedException("ProBuilder only supports importing triangle and quad meshes.");
				}
			}
			m_Vertices = list.ToArray();
			m_Destination.Clear();
			m_Destination.SetVertices(m_Vertices);
			m_Destination.faces = list2;
			m_Destination.sharedVertices = SharedVertex.GetSharedVerticesWithPositions(m_Destination.positionsInternal);
			m_Destination.sharedTextures = new SharedVertex[0];
			HashSet<Face> hashSet = new HashSet<Face>();
			if (importSettings.quads)
			{
				List<WingedEdge> wingedEdges = WingedEdge.GetWingedEdges(m_Destination, m_Destination.facesInternal, oneWingPerFace: true);
				Dictionary<EdgeLookup, float> dictionary = new Dictionary<EdgeLookup, float>();
				for (int l = 0; l < wingedEdges.Count; l++)
				{
					using WingedEdgeEnumerator wingedEdgeEnumerator = new WingedEdgeEnumerator(wingedEdges[l]);
					while (wingedEdgeEnumerator.MoveNext())
					{
						WingedEdge current = wingedEdgeEnumerator.Current;
						if (current.opposite != null && !dictionary.ContainsKey(current.edge))
						{
							float quadScore = GetQuadScore(current, current.opposite);
							dictionary.Add(current.edge, quadScore);
						}
					}
				}
				List<SimpleTuple<Face, Face>> list3 = new List<SimpleTuple<Face, Face>>();
				foreach (WingedEdge item in wingedEdges)
				{
					if (!hashSet.Add(item.face))
					{
						continue;
					}
					float num3 = 0f;
					Face face = null;
					using (WingedEdgeEnumerator wingedEdgeEnumerator2 = new WingedEdgeEnumerator(item))
					{
						while (wingedEdgeEnumerator2.MoveNext())
						{
							WingedEdge current3 = wingedEdgeEnumerator2.Current;
							if ((current3.opposite == null || !hashSet.Contains(current3.opposite.face)) && dictionary.TryGetValue(current3.edge, out var value) && value > num3 && item.face == GetBestQuadConnection(current3.opposite, dictionary))
							{
								num3 = value;
								face = current3.opposite.face;
							}
						}
					}
					if (face != null)
					{
						hashSet.Add(face);
						list3.Add(new SimpleTuple<Face, Face>(item.face, face));
					}
				}
				MergeElements.MergePairs(m_Destination, list3, !importSettings.smoothing);
			}
			if (importSettings.smoothing)
			{
				Smoothing.ApplySmoothingGroups(m_Destination, m_Destination.facesInternal, importSettings.smoothingAngle, m_Vertices.Select((Vertex x) => x.normal).ToArray());
				MergeElements.CollapseCoincidentVertices(m_Destination, m_Destination.facesInternal);
			}
		}

		private static Face GetBestQuadConnection(WingedEdge wing, Dictionary<EdgeLookup, float> connections)
		{
			float num = 0f;
			Face result = null;
			using WingedEdgeEnumerator wingedEdgeEnumerator = new WingedEdgeEnumerator(wing);
			while (wingedEdgeEnumerator.MoveNext())
			{
				WingedEdge current = wingedEdgeEnumerator.Current;
				float value = 0f;
				if (connections.TryGetValue(current.edge, out value) && value > num)
				{
					num = connections[current.edge];
					result = current.opposite.face;
				}
			}
			return result;
		}

		private float GetQuadScore(WingedEdge left, WingedEdge right, float normalThreshold = 0.9f)
		{
			int[] array = WingedEdge.MakeQuad(left, right);
			if (array == null)
			{
				return 0f;
			}
			Vector3 lhs = Math.Normal(m_Vertices[array[0]].position, m_Vertices[array[1]].position, m_Vertices[array[2]].position);
			Vector3 rhs = Math.Normal(m_Vertices[array[2]].position, m_Vertices[array[3]].position, m_Vertices[array[0]].position);
			float num = Vector3.Dot(lhs, rhs);
			if (num < normalThreshold)
			{
				return 0f;
			}
			Vector3 vector = m_Vertices[array[1]].position - m_Vertices[array[0]].position;
			Vector3 vector2 = m_Vertices[array[2]].position - m_Vertices[array[1]].position;
			Vector3 vector3 = m_Vertices[array[3]].position - m_Vertices[array[2]].position;
			Vector3 vector4 = m_Vertices[array[0]].position - m_Vertices[array[3]].position;
			vector.Normalize();
			vector2.Normalize();
			vector3.Normalize();
			vector4.Normalize();
			float num2 = Mathf.Abs(Vector3.Dot(vector, vector2));
			float num3 = Mathf.Abs(Vector3.Dot(vector2, vector3));
			float num4 = Mathf.Abs(Vector3.Dot(vector3, vector4));
			float num5 = Mathf.Abs(Vector3.Dot(vector4, vector));
			num += 1f - (num2 + num3 + num4 + num5) * 0.25f;
			num += Mathf.Abs(Vector3.Dot(vector, vector3)) * 0.5f;
			num += Mathf.Abs(Vector3.Dot(vector2, vector4)) * 0.5f;
			return num * 0.33f;
		}
	}
}
