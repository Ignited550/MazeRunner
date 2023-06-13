using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal static class LightUtility
	{
		private struct ParametricLightMeshVertex
		{
			public float3 position;

			public Color color;

			public static readonly VertexAttributeDescriptor[] VertexLayout = new VertexAttributeDescriptor[2]
			{
				new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
				new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4)
			};
		}

		private struct SpriteLightMeshVertex
		{
			public Vector3 position;

			public Color color;

			public Vector2 uv;

			public static readonly VertexAttributeDescriptor[] VertexLayout = new VertexAttributeDescriptor[3]
			{
				new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
				new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
				new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
			};
		}

		public static bool CheckForChange(int a, ref int b)
		{
			bool result = a != b;
			b = a;
			return result;
		}

		public static bool CheckForChange(float a, ref float b)
		{
			bool result = a != b;
			b = a;
			return result;
		}

		public static bool CheckForChange(bool a, ref bool b)
		{
			bool result = a != b;
			b = a;
			return result;
		}

		public static Bounds GenerateParametricMesh(Mesh mesh, float radius, float falloffDistance, float angle, int sides)
		{
			float num = (float)Math.PI / 2f + (float)Math.PI / 180f * angle;
			if (sides < 3)
			{
				radius = 0.70710677f * radius;
				sides = 4;
			}
			if (sides == 4)
			{
				num = (float)Math.PI / 4f + (float)Math.PI / 180f * angle;
			}
			int num2 = 1 + 2 * sides;
			int length = 9 * sides;
			NativeArray<ParametricLightMeshVertex> data = new NativeArray<ParametricLightMeshVertex>(num2, Allocator.Temp);
			NativeArray<ushort> indices = new NativeArray<ushort>(length, Allocator.Temp);
			ushort num3 = (ushort)(2 * sides);
			Color color = new Color(0f, 0f, 0f, 1f);
			data[num3] = new ParametricLightMeshVertex
			{
				position = float3.zero,
				color = color
			};
			float num4 = (float)Math.PI * 2f / (float)sides;
			float3 @float = new float3(float.MaxValue, float.MaxValue, 0f);
			float3 float2 = new float3(float.MinValue, float.MinValue, 0f);
			for (int i = 0; i < sides; i++)
			{
				float num5 = (float)(i + 1) * num4;
				float3 float3 = new float3(math.cos(num5 + num), math.sin(num5 + num), 0f);
				float3 float4 = radius * float3;
				int num6 = (2 * i + 2) % (2 * sides);
				data[num6] = new ParametricLightMeshVertex
				{
					position = float4,
					color = new Color(float3.x, float3.y, 0f, 0f)
				};
				data[num6 + 1] = new ParametricLightMeshVertex
				{
					position = float4,
					color = color
				};
				int num7 = 9 * i;
				indices[num7] = (ushort)(num6 + 1);
				indices[num7 + 1] = (ushort)(2 * i + 1);
				indices[num7 + 2] = num3;
				indices[num7 + 3] = (ushort)num6;
				indices[num7 + 4] = (ushort)(2 * i);
				indices[num7 + 5] = (ushort)(2 * i + 1);
				indices[num7 + 6] = (ushort)(num6 + 1);
				indices[num7 + 7] = (ushort)num6;
				indices[num7 + 8] = (ushort)(2 * i + 1);
				@float = math.min(@float, float4 + float3 * falloffDistance);
				float2 = math.max(float2, float4 + float3 * falloffDistance);
			}
			mesh.SetVertexBufferParams(num2, ParametricLightMeshVertex.VertexLayout);
			mesh.SetVertexBufferData(data, 0, 0, num2);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0, calculateBounds: false);
			Bounds result = default(Bounds);
			result.min = @float;
			result.max = float2;
			return result;
		}

		public static Bounds GenerateSpriteMesh(Mesh mesh, Sprite sprite)
		{
			if (sprite == null)
			{
				mesh.Clear();
				return new Bounds(Vector3.zero, Vector3.zero);
			}
			_ = sprite.uv;
			NativeSlice<Vector3> vertexAttribute = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
			NativeSlice<Vector2> vertexAttribute2 = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
			NativeArray<ushort> indices = sprite.GetIndices();
			Vector3 vector = 0.5f * (sprite.bounds.min + sprite.bounds.max);
			NativeArray<SpriteLightMeshVertex> data = new NativeArray<SpriteLightMeshVertex>(indices.Length, Allocator.Temp);
			Color color = new Color(0f, 0f, 0f, 1f);
			for (int i = 0; i < vertexAttribute.Length; i++)
			{
				data[i] = new SpriteLightMeshVertex
				{
					position = new Vector3(vertexAttribute[i].x, vertexAttribute[i].y, 0f) - vector,
					color = color,
					uv = vertexAttribute2[i]
				};
			}
			mesh.SetVertexBufferParams(data.Length, SpriteLightMeshVertex.VertexLayout);
			mesh.SetVertexBufferData(data, 0, 0, data.Length);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
			return mesh.GetSubMesh(0).bounds;
		}

		public static List<Vector2> GetFalloffShape(Vector3[] shapePath)
		{
			List<Vector2> list = new List<Vector2>();
			for (int i = 0; i < shapePath.Length; i++)
			{
				int num = ((i == 0) ? (shapePath.Length - 1) : (i - 1));
				int num2 = (i + 1) % shapePath.Length;
				Vector3 vector = shapePath[num];
				Vector3 vector2 = shapePath[i];
				Vector3 vector3 = shapePath[num2];
				Vector3 vector4 = vector2 - vector;
				Vector3 vector5 = vector3 - vector2;
				if (!(vector4.magnitude < 0.001f) && !(vector5.magnitude < 0.001f))
				{
					Vector3 normalized = vector4.normalized;
					Vector3 normalized2 = vector5.normalized;
					normalized = new Vector2(0f - normalized.y, normalized.x);
					normalized2 = new Vector2(0f - normalized2.y, normalized2.x);
					Vector3 vector6 = normalized.normalized + normalized2.normalized;
					Vector3 vector7 = -vector6.normalized;
					if (vector6.magnitude > 0f && vector7.magnitude > 0f)
					{
						Vector2 item = new Vector2(vector7.x, vector7.y);
						list.Add(item);
					}
				}
			}
			return list;
		}

		public static Bounds GenerateShapeMesh(Mesh mesh, Vector3[] shapePath, float falloffDistance)
		{
			Color color = new Color(0f, 0f, 0f, 1f);
			float3 @float = new float3(float.MaxValue, float.MaxValue, 0f);
			float3 float2 = new float3(float.MinValue, float.MinValue, 0f);
			int num = shapePath.Length;
			ContourVertex[] array = new ContourVertex[num];
			for (int j = 0; j < num; j++)
			{
				array[j] = new ContourVertex
				{
					Position = new Vec3
					{
						X = shapePath[j].x,
						Y = shapePath[j].y
					}
				};
			}
			Tess tess = new Tess();
			tess.AddContour(array, ContourOrientation.Original);
			tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
			IEnumerable<int> enumerable = tess.Elements.Select((int i) => i);
			IEnumerable<float3> enumerable2 = tess.Vertices.Select((ContourVertex v) => new float3(v.Position.X, v.Position.Y, 0f));
			NativeArray<ParametricLightMeshVertex> data = new NativeArray<ParametricLightMeshVertex>(enumerable2.Count() + 2 * shapePath.Length, Allocator.Temp);
			NativeArray<ushort> indices = new NativeArray<ushort>(enumerable.Count() + 6 * shapePath.Length, Allocator.Temp);
			int num2 = 0;
			foreach (float3 item in enumerable2)
			{
				data[num2++] = new ParametricLightMeshVertex
				{
					position = item,
					color = color
				};
			}
			int num3 = 0;
			foreach (int item2 in enumerable)
			{
				indices[num3++] = (ushort)item2;
			}
			List<Vector2> falloffShape = GetFalloffShape(shapePath);
			int num4 = 2 * shapePath.Length;
			for (int k = 0; k < shapePath.Length; k++)
			{
				int num5 = 2 * k;
				int num6 = num2 + num5;
				int num7 = num2 + num5 + 1;
				int num8 = num2 + (num5 + 2) % num4;
				int num9 = num2 + (num5 + 3) % num4;
				ParametricLightMeshVertex parametricLightMeshVertex = default(ParametricLightMeshVertex);
				parametricLightMeshVertex.position = shapePath[k];
				parametricLightMeshVertex.color = new Color(0f, 0f, 0f, 1f);
				ParametricLightMeshVertex value = (data[num2 + k * 2] = parametricLightMeshVertex);
				value.color = new Color(falloffShape[k].x, falloffShape[k].y, 0f, 0f);
				data[num2 + k * 2 + 1] = value;
				indices[num3 + k * 6] = (ushort)num6;
				indices[num3 + k * 6 + 1] = (ushort)num7;
				indices[num3 + k * 6 + 2] = (ushort)num9;
				indices[num3 + k * 6 + 3] = (ushort)num9;
				indices[num3 + k * 6 + 4] = (ushort)num8;
				indices[num3 + k * 6 + 5] = (ushort)num6;
				float3 float3 = new float3(falloffShape[k].x, falloffShape[k].y, 0f);
				@float = math.min(@float, value.position + float3 * falloffDistance);
				float2 = math.max(float2, value.position + float3 * falloffDistance);
			}
			mesh.SetVertexBufferParams(data.Length, ParametricLightMeshVertex.VertexLayout);
			mesh.SetVertexBufferData(data, 0, 0, data.Length);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0, calculateBounds: false);
			Bounds result = default(Bounds);
			result.min = @float;
			result.max = float2;
			return result;
		}
	}
}
