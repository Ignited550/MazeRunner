using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.ProBuilder.MeshOperations
{
	public static class AppendElements
	{
		internal static Face AppendFace(this ProBuilderMesh mesh, Vector3[] positions, Color[] colors, Vector2[] uvs, Face face, int[] common)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (positions == null)
			{
				throw new ArgumentNullException("positions");
			}
			if (face == null)
			{
				throw new ArgumentNullException("face");
			}
			int num = positions.Length;
			if (common == null)
			{
				common = new int[num];
				for (int i = 0; i < num; i++)
				{
					common[i] = -1;
				}
			}
			int vertexCount = mesh.vertexCount;
			bool flag = mesh.HasArrays(MeshArrays.Color);
			bool flag2 = colors != null;
			bool flag3 = mesh.HasArrays(MeshArrays.Texture0);
			bool flag4 = uvs != null;
			Vector3[] array = new Vector3[vertexCount + num];
			Color[] array2 = ((flag || flag2) ? new Color[vertexCount + num] : null);
			Vector2[] array3 = ((flag3 || flag4) ? new Vector2[vertexCount + num] : null);
			List<Face> list = new List<Face>(mesh.facesInternal);
			Array.Copy(mesh.positionsInternal, 0, array, 0, vertexCount);
			Array.Copy(positions, 0, array, vertexCount, num);
			if (flag || flag2)
			{
				Array.Copy(flag ? mesh.colorsInternal : ArrayUtility.Fill(Color.white, vertexCount), 0, array2, 0, vertexCount);
				Array.Copy(flag2 ? colors : ArrayUtility.Fill(Color.white, num), 0, array2, vertexCount, colors.Length);
			}
			if (flag3 || flag4)
			{
				Array.Copy(flag3 ? mesh.texturesInternal : ArrayUtility.Fill(Vector2.zero, vertexCount), 0, array3, 0, vertexCount);
				Array.Copy(flag4 ? uvs : ArrayUtility.Fill(Vector2.zero, num), 0, array3, mesh.texturesInternal.Length, num);
			}
			face.ShiftIndexesToZero();
			face.ShiftIndexes(vertexCount);
			list.Add(face);
			for (int j = 0; j < common.Length; j++)
			{
				if (common[j] < 0)
				{
					mesh.AddSharedVertex(new SharedVertex(new int[1] { j + vertexCount }));
				}
				else
				{
					mesh.AddToSharedVertex(common[j], j + vertexCount);
				}
			}
			mesh.positions = array;
			mesh.colors = array2;
			mesh.textures = array3;
			mesh.faces = list;
			return face;
		}

		public static Face[] AppendFaces(this ProBuilderMesh mesh, Vector3[][] positions, Color[][] colors, Vector2[][] uvs, Face[] faces, int[][] shared)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (positions == null)
			{
				throw new ArgumentNullException("positions");
			}
			if (colors == null)
			{
				throw new ArgumentNullException("colors");
			}
			if (uvs == null)
			{
				throw new ArgumentNullException("uvs");
			}
			if (faces == null)
			{
				throw new ArgumentNullException("faces");
			}
			List<Vector3> list = new List<Vector3>(mesh.positionsInternal);
			List<Color> list2 = new List<Color>(mesh.colorsInternal);
			List<Vector2> list3 = new List<Vector2>(mesh.texturesInternal);
			List<Face> list4 = new List<Face>(mesh.facesInternal);
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			int num = mesh.vertexCount;
			for (int i = 0; i < faces.Length; i++)
			{
				list.AddRange(positions[i]);
				list2.AddRange(colors[i]);
				list3.AddRange(uvs[i]);
				faces[i].ShiftIndexesToZero();
				faces[i].ShiftIndexes(num);
				list4.Add(faces[i]);
				if (shared != null && positions[i].Length != shared[i].Length)
				{
					Debug.LogError("Append Face failed because shared array does not match new vertex array.");
					return null;
				}
				bool flag = shared != null;
				for (int j = 0; j < shared[i].Length; j++)
				{
					sharedVertexLookup.Add(j + num, flag ? shared[i][j] : (-1));
				}
				num = list.Count;
			}
			mesh.positions = list;
			mesh.colors = list2;
			mesh.textures = list3;
			mesh.faces = list4;
			mesh.SetSharedVertices(sharedVertexLookup);
			return faces;
		}

		public static Face CreatePolygon(this ProBuilderMesh mesh, IList<int> indexes, bool unordered)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			SharedVertex[] sharedVerticesInternal = mesh.sharedVerticesInternal;
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			HashSet<int> sharedVertexHandles = mesh.GetSharedVertexHandles(indexes);
			List<Vertex> list = new List<Vertex>(mesh.GetVertices());
			List<Vertex> list2 = new List<Vertex>();
			foreach (int item in sharedVertexHandles)
			{
				int index = sharedVerticesInternal[item][0];
				list2.Add(new Vertex(list[index]));
			}
			FaceRebuildData faceRebuildData = FaceWithVertices(list2, unordered);
			if (faceRebuildData != null)
			{
				faceRebuildData.sharedIndexes = sharedVertexHandles.ToList();
				List<Face> faces = new List<Face>(mesh.facesInternal);
				FaceRebuildData.Apply(new FaceRebuildData[1] { faceRebuildData }, list, faces, sharedVertexLookup);
				mesh.SetVertices(list);
				mesh.faces = faces;
				mesh.SetSharedVertices(sharedVertexLookup);
				return faceRebuildData.face;
			}
			Log.Info(unordered ? "Too Few Unique Points Selected" : "Points not ordered correctly");
			return null;
		}

		public static ActionResult CreateShapeFromPolygon(this PolyShape poly)
		{
			return poly.mesh.CreateShapeFromPolygon(poly.m_Points, poly.extrude, poly.flipNormals);
		}

		internal static void ClearAndRefreshMesh(this ProBuilderMesh mesh)
		{
			mesh.Clear();
			mesh.ToMesh();
			mesh.Refresh();
		}

		public static ActionResult CreateShapeFromPolygon(this ProBuilderMesh mesh, IList<Vector3> points, float extrude, bool flipNormals)
		{
			return mesh.CreateShapeFromPolygon(points, extrude, flipNormals, null);
		}

		[Obsolete("Face.CreateShapeFromPolygon is deprecated as it no longer relies on camera look at.")]
		public static ActionResult CreateShapeFromPolygon(this ProBuilderMesh mesh, IList<Vector3> points, float extrude, bool flipNormals, Vector3 cameraLookAt, IList<IList<Vector3>> holePoints = null)
		{
			return mesh.CreateShapeFromPolygon(points, extrude, flipNormals, null);
		}

		public static ActionResult CreateShapeFromPolygon(this ProBuilderMesh mesh, IList<Vector3> points, float extrude, bool flipNormals, IList<IList<Vector3>> holePoints)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (points == null || points.Count < 3)
			{
				mesh.ClearAndRefreshMesh();
				return new ActionResult(ActionResult.Status.NoChange, "Too Few Points");
			}
			Vector3[] array = points.ToArray();
			Vector3[][] array2 = null;
			if (holePoints != null && holePoints.Count > 0)
			{
				array2 = new Vector3[holePoints.Count][];
				for (int i = 0; i < holePoints.Count; i++)
				{
					if (holePoints[i] == null || holePoints[i].Count < 3)
					{
						mesh.ClearAndRefreshMesh();
						return new ActionResult(ActionResult.Status.NoChange, "Too Few Points in hole " + i);
					}
					array2[i] = holePoints[i].ToArray();
				}
			}
			Log.PushLogLevel(LogLevel.Error);
			if (Triangulation.TriangulateVertices(array, out var triangles, array2))
			{
				Vector3[] array3 = null;
				if (array2 != null)
				{
					array3 = new Vector3[array.Length + array2.Sum((Vector3[] arr) => arr.Length)];
					Array.Copy(array, array3, array.Length);
					int num = array.Length;
					Vector3[][] array4 = array2;
					foreach (Vector3[] array5 in array4)
					{
						Array.ConstrainedCopy(array5, 0, array3, num, array5.Length);
						num += array5.Length;
					}
				}
				else
				{
					array3 = array;
				}
				int[] array6 = triangles.ToArray();
				if (Math.PolygonArea(array3, array6) < Mathf.Epsilon)
				{
					mesh.ClearAndRefreshMesh();
					Log.PopLogLevel();
					return new ActionResult(ActionResult.Status.Failure, "Polygon Area < Epsilon");
				}
				mesh.Clear();
				mesh.positionsInternal = array3;
				Face face = new Face(array6);
				mesh.facesInternal = new Face[1] { face };
				mesh.sharedVerticesInternal = SharedVertex.GetSharedVerticesWithPositions(array3);
				mesh.InvalidateCaches();
				if (face.distinctIndexesInternal.Length != array3.Length)
				{
					mesh.ClearAndRefreshMesh();
					Log.PopLogLevel();
					return new ActionResult(ActionResult.Status.Failure, "Triangulation missing points");
				}
				Vector3 direction = Math.Normal(mesh, mesh.facesInternal[0]);
				direction = mesh.gameObject.transform.TransformDirection(direction);
				if (flipNormals ? (Vector3.Dot(mesh.gameObject.transform.up, direction) > 0f) : (Vector3.Dot(mesh.gameObject.transform.up, direction) < 0f))
				{
					mesh.facesInternal[0].Reverse();
				}
				if (extrude != 0f)
				{
					mesh.DuplicateAndFlip(mesh.facesInternal);
					mesh.Extrude(new Face[1] { flipNormals ? mesh.facesInternal[1] : mesh.facesInternal[0] }, ExtrudeMethod.IndividualFaces, extrude);
					if ((extrude < 0f && !flipNormals) || (extrude > 0f && flipNormals))
					{
						Face[] facesInternal = mesh.facesInternal;
						for (int j = 0; j < facesInternal.Length; j++)
						{
							facesInternal[j].Reverse();
						}
					}
				}
				mesh.ToMesh();
				mesh.Refresh();
				Log.PopLogLevel();
				return new ActionResult(ActionResult.Status.Success, "Create Polygon Shape");
			}
			mesh.ClearAndRefreshMesh();
			Log.PopLogLevel();
			return new ActionResult(ActionResult.Status.Failure, "Failed Triangulating Points");
		}

		internal static FaceRebuildData FaceWithVertices(List<Vertex> vertices, bool unordered = true)
		{
			if (Triangulation.TriangulateVertices(vertices, out var triangles, unordered))
			{
				return new FaceRebuildData
				{
					vertices = vertices,
					face = new Face(triangles)
				};
			}
			return null;
		}

		internal static List<FaceRebuildData> TentCapWithVertices(List<Vertex> path)
		{
			int count = path.Count;
			Vertex item = Vertex.Average(path);
			List<FaceRebuildData> list = new List<FaceRebuildData>();
			for (int i = 0; i < count; i++)
			{
				List<Vertex> vertices = new List<Vertex>
				{
					path[i],
					item,
					path[(i + 1) % count]
				};
				FaceRebuildData faceRebuildData = new FaceRebuildData();
				faceRebuildData.vertices = vertices;
				faceRebuildData.face = new Face(new int[3] { 0, 1, 2 });
				list.Add(faceRebuildData);
			}
			return list;
		}

		public static void DuplicateAndFlip(this ProBuilderMesh mesh, Face[] faces)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (faces == null)
			{
				throw new ArgumentNullException("faces");
			}
			List<FaceRebuildData> list = new List<FaceRebuildData>();
			List<Vertex> list2 = new List<Vertex>(mesh.GetVertices());
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			foreach (Face face in faces)
			{
				FaceRebuildData faceRebuildData = new FaceRebuildData();
				faceRebuildData.vertices = new List<Vertex>();
				faceRebuildData.face = new Face(face);
				faceRebuildData.sharedIndexes = new List<int>();
				Dictionary<int, int> dictionary = new Dictionary<int, int>();
				int num = faceRebuildData.face.indexesInternal.Length;
				for (int j = 0; j < num; j++)
				{
					if (!dictionary.ContainsKey(face.indexesInternal[j]))
					{
						dictionary.Add(face.indexesInternal[j], dictionary.Count);
						faceRebuildData.vertices.Add(list2[face.indexesInternal[j]]);
						faceRebuildData.sharedIndexes.Add(sharedVertexLookup[face.indexesInternal[j]]);
					}
				}
				int[] array = new int[num];
				for (int k = 0; k < num; k++)
				{
					array[num - (k + 1)] = dictionary[faceRebuildData.face[k]];
				}
				faceRebuildData.face.SetIndexes(array);
				list.Add(faceRebuildData);
			}
			FaceRebuildData.Apply(list, mesh, list2);
		}

		public static Face Bridge(this ProBuilderMesh mesh, Edge a, Edge b, bool allowNonManifoldGeometry = false)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			SharedVertex[] sharedVerticesInternal = mesh.sharedVerticesInternal;
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			if (!allowNonManifoldGeometry && (ElementSelection.GetNeighborFaces(mesh, a).Count > 1 || ElementSelection.GetNeighborFaces(mesh, b).Count > 1))
			{
				return null;
			}
			Face[] facesInternal = mesh.facesInternal;
			foreach (Face face in facesInternal)
			{
				if (mesh.IndexOf(face.edgesInternal, a) >= 0 && mesh.IndexOf(face.edgesInternal, b) >= 0)
				{
					Log.Warning("Face already exists between these two edges!");
					return null;
				}
			}
			Vector3[] positionsInternal = mesh.positionsInternal;
			bool flag = mesh.HasArrays(MeshArrays.Color);
			Color[] array = (flag ? mesh.colorsInternal : null);
			AutoUnwrapSettings u = AutoUnwrapSettings.tile;
			int submeshIndex = 0;
			if (EdgeUtility.ValidateEdge(mesh, a, out var validEdge) || EdgeUtility.ValidateEdge(mesh, b, out validEdge))
			{
				u = new AutoUnwrapSettings(validEdge.item1.uv);
				submeshIndex = validEdge.item1.submeshIndex;
			}
			Vector3[] array2;
			Color[] array3;
			int[] array4;
			if (a.Contains(b.a, sharedVertexLookup) || a.Contains(b.b, sharedVertexLookup))
			{
				array2 = new Vector3[3];
				array3 = new Color[3];
				array4 = new int[3];
				bool flag2 = Array.IndexOf(sharedVerticesInternal[mesh.GetSharedVertexHandle(a.a)].arrayInternal, b.a) > -1;
				bool flag3 = Array.IndexOf(sharedVerticesInternal[mesh.GetSharedVertexHandle(a.a)].arrayInternal, b.b) > -1;
				bool flag4 = Array.IndexOf(sharedVerticesInternal[mesh.GetSharedVertexHandle(a.b)].arrayInternal, b.a) > -1;
				bool flag5 = Array.IndexOf(sharedVerticesInternal[mesh.GetSharedVertexHandle(a.b)].arrayInternal, b.b) > -1;
				if (flag2)
				{
					array2[0] = positionsInternal[a.a];
					if (flag)
					{
						array3[0] = array[a.a];
					}
					array4[0] = mesh.GetSharedVertexHandle(a.a);
					array2[1] = positionsInternal[a.b];
					if (flag)
					{
						array3[1] = array[a.b];
					}
					array4[1] = mesh.GetSharedVertexHandle(a.b);
					array2[2] = positionsInternal[b.b];
					if (flag)
					{
						array3[2] = array[b.b];
					}
					array4[2] = mesh.GetSharedVertexHandle(b.b);
				}
				else if (flag3)
				{
					array2[0] = positionsInternal[a.a];
					if (flag)
					{
						array3[0] = array[a.a];
					}
					array4[0] = mesh.GetSharedVertexHandle(a.a);
					array2[1] = positionsInternal[a.b];
					if (flag)
					{
						array3[1] = array[a.b];
					}
					array4[1] = mesh.GetSharedVertexHandle(a.b);
					array2[2] = positionsInternal[b.a];
					if (flag)
					{
						array3[2] = array[b.a];
					}
					array4[2] = mesh.GetSharedVertexHandle(b.a);
				}
				else if (flag4)
				{
					array2[0] = positionsInternal[a.b];
					if (flag)
					{
						array3[0] = array[a.b];
					}
					array4[0] = mesh.GetSharedVertexHandle(a.b);
					array2[1] = positionsInternal[a.a];
					if (flag)
					{
						array3[1] = array[a.a];
					}
					array4[1] = mesh.GetSharedVertexHandle(a.a);
					array2[2] = positionsInternal[b.b];
					if (flag)
					{
						array3[2] = array[b.b];
					}
					array4[2] = mesh.GetSharedVertexHandle(b.b);
				}
				else if (flag5)
				{
					array2[0] = positionsInternal[a.b];
					if (flag)
					{
						array3[0] = array[a.b];
					}
					array4[0] = mesh.GetSharedVertexHandle(a.b);
					array2[1] = positionsInternal[a.a];
					if (flag)
					{
						array3[1] = array[a.a];
					}
					array4[1] = mesh.GetSharedVertexHandle(a.a);
					array2[2] = positionsInternal[b.a];
					if (flag)
					{
						array3[2] = array[b.a];
					}
					array4[2] = mesh.GetSharedVertexHandle(b.a);
				}
				return mesh.AppendFace(array2, flag ? array3 : null, new Vector2[array2.Length], new Face((!(flag2 || flag3)) ? new int[3] { 0, 1, 2 } : new int[3] { 2, 1, 0 }, submeshIndex, u, 0, -1, -1, manualUVs: false), array4);
			}
			array2 = new Vector3[4];
			array3 = new Color[4];
			array4 = new int[4];
			array2[0] = positionsInternal[a.a];
			if (flag)
			{
				array3[0] = mesh.colorsInternal[a.a];
			}
			array4[0] = mesh.GetSharedVertexHandle(a.a);
			array2[1] = positionsInternal[a.b];
			if (flag)
			{
				array3[1] = mesh.colorsInternal[a.b];
			}
			array4[1] = mesh.GetSharedVertexHandle(a.b);
			Vector3 normalized = Vector3.Cross(positionsInternal[b.a] - positionsInternal[a.a], positionsInternal[a.b] - positionsInternal[a.a]).normalized;
			Vector2[] array5 = Projection.PlanarProject(new Vector3[4]
			{
				positionsInternal[a.a],
				positionsInternal[a.b],
				positionsInternal[b.a],
				positionsInternal[b.b]
			}, null, normalized);
			Vector2 intersect = Vector2.zero;
			if (!Math.GetLineSegmentIntersect(array5[0], array5[2], array5[1], array5[3], ref intersect))
			{
				array2[2] = positionsInternal[b.a];
				if (flag)
				{
					array3[2] = mesh.colorsInternal[b.a];
				}
				array4[2] = mesh.GetSharedVertexHandle(b.a);
				array2[3] = positionsInternal[b.b];
				if (flag)
				{
					array3[3] = mesh.colorsInternal[b.b];
				}
				array4[3] = mesh.GetSharedVertexHandle(b.b);
			}
			else
			{
				array2[2] = positionsInternal[b.b];
				if (flag)
				{
					array3[2] = mesh.colorsInternal[b.b];
				}
				array4[2] = mesh.GetSharedVertexHandle(b.b);
				array2[3] = positionsInternal[b.a];
				if (flag)
				{
					array3[3] = mesh.colorsInternal[b.a];
				}
				array4[3] = mesh.GetSharedVertexHandle(b.a);
			}
			return mesh.AppendFace(array2, flag ? array3 : null, new Vector2[array2.Length], new Face(new int[6] { 2, 1, 0, 2, 3, 1 }, submeshIndex, u, 0, -1, -1, manualUVs: false), array4);
		}

		public static Face AppendVerticesToFace(this ProBuilderMesh mesh, Face face, Vector3[] points)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (face == null)
			{
				throw new ArgumentNullException("face");
			}
			if (points == null)
			{
				throw new ArgumentNullException("points");
			}
			List<Vertex> list = mesh.GetVertices().ToList();
			List<Face> faces = new List<Face>(mesh.facesInternal);
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			Dictionary<int, int> dictionary = null;
			if (mesh.sharedTextures != null)
			{
				dictionary = new Dictionary<int, int>();
				SharedVertex.GetSharedVertexLookup(mesh.sharedTextures, dictionary);
			}
			List<Edge> list2 = WingedEdge.SortEdgesByAdjacency(face);
			List<Vertex> list3 = new List<Vertex>();
			List<int> list4 = new List<int>();
			List<int> list5 = ((dictionary != null) ? new List<int>() : null);
			for (int i = 0; i < list2.Count; i++)
			{
				list3.Add(list[list2[i].a]);
				list4.Add(sharedVertexLookup[list2[i].a]);
				if (dictionary != null)
				{
					if (dictionary.TryGetValue(list2[i].a, out var value))
					{
						list5.Add(value);
					}
					else
					{
						list5.Add(-1);
					}
				}
			}
			for (int j = 0; j < points.Length; j++)
			{
				int num = -1;
				float num2 = float.PositiveInfinity;
				Vector3 vector = points[j];
				int count = list3.Count;
				for (int k = 0; k < count; k++)
				{
					Vector3 position = list3[k].position;
					Vector3 position2 = list3[(k + 1) % count].position;
					float num3 = Math.DistancePointLineSegment(vector, position, position2);
					if (num3 < num2)
					{
						num2 = num3;
						num = k;
					}
				}
				Vertex vertex = list3[num];
				Vertex vertex2 = list3[(num + 1) % count];
				float sqrMagnitude = (vector - vertex.position).sqrMagnitude;
				float sqrMagnitude2 = (vector - vertex2.position).sqrMagnitude;
				Vertex item = Vertex.Mix(vertex, vertex2, sqrMagnitude / (sqrMagnitude + sqrMagnitude2));
				list3.Insert((num + 1) % count, item);
				list4.Insert((num + 1) % count, -1);
				list5?.Insert((num + 1) % count, -1);
			}
			List<int> triangles;
			try
			{
				Triangulation.TriangulateVertices(list3, out triangles, unordered: false);
			}
			catch
			{
				Debug.Log("Failed triangulating face after appending vertices.");
				return null;
			}
			FaceRebuildData faceRebuildData = new FaceRebuildData();
			faceRebuildData.face = new Face(triangles.ToArray(), face.submeshIndex, new AutoUnwrapSettings(face.uv), face.smoothingGroup, face.textureGroup, -1, face.manualUV);
			faceRebuildData.vertices = list3;
			faceRebuildData.sharedIndexes = list4;
			faceRebuildData.sharedIndexesUV = list5;
			FaceRebuildData.Apply(new List<FaceRebuildData> { faceRebuildData }, list, faces, sharedVertexLookup, dictionary);
			Face face2 = faceRebuildData.face;
			mesh.SetVertices(list);
			mesh.faces = faces;
			mesh.SetSharedVertices(sharedVertexLookup);
			mesh.SetSharedTextures(dictionary);
			Vector3 lhs = Math.Normal(mesh, face);
			Vector3 rhs = Math.Normal(mesh, face2);
			if (Vector3.Dot(lhs, rhs) < 0f)
			{
				face2.Reverse();
			}
			mesh.DeleteFace(face);
			return face2;
		}

		public static List<Edge> AppendVerticesToEdge(this ProBuilderMesh mesh, Edge edge, int count)
		{
			return mesh.AppendVerticesToEdge(new Edge[1] { edge }, count);
		}

		public static List<Edge> AppendVerticesToEdge(this ProBuilderMesh mesh, IList<Edge> edges, int count)
		{
			if (mesh == null)
			{
				throw new ArgumentNullException("mesh");
			}
			if (edges == null)
			{
				throw new ArgumentNullException("edges");
			}
			if (count < 1 || count > 512)
			{
				Log.Error("New edge vertex count is less than 1 or greater than 512.");
				return null;
			}
			List<Vertex> list = new List<Vertex>(mesh.GetVertices());
			Dictionary<int, int> sharedVertexLookup = mesh.sharedVertexLookup;
			Dictionary<int, int> sharedTextureLookup = mesh.sharedTextureLookup;
			List<int> list2 = new List<int>();
			List<Edge> list3 = mesh.GetSharedVertexHandleEdges(edges).Distinct().ToList();
			Dictionary<Face, FaceRebuildData> dictionary = new Dictionary<Face, FaceRebuildData>();
			int num = sharedVertexLookup.Count();
			int num2 = num;
			foreach (Edge item3 in list3)
			{
				Edge edgeWithSharedVertexHandles = mesh.GetEdgeWithSharedVertexHandles(item3);
				List<Vertex> list4 = new List<Vertex>(count);
				for (int i = 0; i < count; i++)
				{
					list4.Add(Vertex.Mix(list[edgeWithSharedVertexHandles.a], list[edgeWithSharedVertexHandles.b], (float)(i + 1) / ((float)count + 1f)));
				}
				foreach (SimpleTuple<Face, Edge> neighborFace in ElementSelection.GetNeighborFaces(mesh, edgeWithSharedVertexHandles))
				{
					Face item = neighborFace.item1;
					if (!dictionary.TryGetValue(item, out var value))
					{
						value = new FaceRebuildData();
						value.face = new Face(new int[0], item.submeshIndex, new AutoUnwrapSettings(item.uv), item.smoothingGroup, item.textureGroup, -1, item.manualUV);
						value.vertices = new List<Vertex>(list.ValuesWithIndexes(item.distinctIndexesInternal));
						value.sharedIndexes = new List<int>();
						value.sharedIndexesUV = new List<int>();
						int[] distinctIndexesInternal = item.distinctIndexesInternal;
						foreach (int key in distinctIndexesInternal)
						{
							if (sharedVertexLookup.TryGetValue(key, out var value2))
							{
								value.sharedIndexes.Add(value2);
							}
							if (sharedTextureLookup.TryGetValue(key, out value2))
							{
								value.sharedIndexesUV.Add(value2);
							}
						}
						list2.AddRange(item.distinctIndexesInternal);
						dictionary.Add(item, value);
					}
					value.vertices.AddRange(list4);
					for (int k = 0; k < count; k++)
					{
						value.sharedIndexes.Add(num2 + k);
						value.sharedIndexesUV.Add(-1);
					}
				}
				num2 += count;
			}
			List<Face> list5 = dictionary.Keys.ToList();
			List<FaceRebuildData> list6 = dictionary.Values.ToList();
			List<EdgeLookup> list7 = new List<EdgeLookup>();
			for (int l = 0; l < list5.Count; l++)
			{
				Face face = list5[l];
				FaceRebuildData faceRebuildData = list6[l];
				Vector3 direction = Math.Normal(mesh, face);
				Vector2[] points = Projection.PlanarProject(faceRebuildData.vertices.Select((Vertex x) => x.position).ToArray(), null, direction);
				int count2 = list.Count;
				if (!Triangulation.SortAndTriangulate(points, out var indexes))
				{
					continue;
				}
				faceRebuildData.face.indexesInternal = indexes.ToArray();
				faceRebuildData.face.ShiftIndexes(count2);
				face.CopyFrom(faceRebuildData.face);
				for (int m = 0; m < faceRebuildData.vertices.Count; m++)
				{
					sharedVertexLookup.Add(count2 + m, faceRebuildData.sharedIndexes[m]);
				}
				if (faceRebuildData.sharedIndexesUV.Count == faceRebuildData.vertices.Count)
				{
					for (int n = 0; n < faceRebuildData.vertices.Count; n++)
					{
						sharedTextureLookup.Add(count2 + n, faceRebuildData.sharedIndexesUV[n]);
					}
				}
				list.AddRange(faceRebuildData.vertices);
				Edge[] edgesInternal = face.edgesInternal;
				for (int j = 0; j < edgesInternal.Length; j++)
				{
					Edge local = edgesInternal[j];
					EdgeLookup item2 = new EdgeLookup(new Edge(sharedVertexLookup[local.a], sharedVertexLookup[local.b]), local);
					if (item2.common.a >= num || item2.common.b >= num)
					{
						list7.Add(item2);
					}
				}
			}
			list2 = list2.Distinct().ToList();
			int delCount = list2.Count;
			List<Edge> result = (from x in list7.Distinct()
				select x.local - delCount).ToList();
			mesh.SetVertices(list);
			mesh.SetSharedVertices(sharedVertexLookup);
			mesh.SetSharedTextures(sharedTextureLookup);
			mesh.DeleteVertices(list2);
			return result;
		}
	}
}
