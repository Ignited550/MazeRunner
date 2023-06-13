using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal struct DeferredTiler
	{
		internal struct PrePunctualLight
		{
			public float3 posVS;

			public float radius;

			public float minDist;

			public float2 screenPos;

			public ushort visLightIndex;
		}

		private enum ClipResult
		{
			Unknown = 0,
			In = 1,
			Out = 2
		}

		private int m_TilePixelWidth;

		private int m_TilePixelHeight;

		private int m_TileXCount;

		private int m_TileYCount;

		private int m_TileHeaderSize;

		private int m_AvgLightPerTile;

		private int m_TilerLevel;

		private FrustumPlanes m_FrustumPlanes;

		private bool m_IsOrthographic;

		[NativeDisableContainerSafetyRestriction]
		private NativeArray<int> m_Counters;

		[NativeDisableContainerSafetyRestriction]
		private NativeArray<ushort> m_TileData;

		[NativeDisableContainerSafetyRestriction]
		private NativeArray<uint> m_TileHeaders;

		[NativeDisableContainerSafetyRestriction]
		private NativeArray<PreTile> m_PreTiles;

		public int TilerLevel => m_TilerLevel;

		public int TileXCount => m_TileXCount;

		public int TileYCount => m_TileYCount;

		public int TilePixelWidth => m_TilePixelWidth;

		public int TilePixelHeight => m_TilePixelHeight;

		public int TileHeaderSize => m_TileHeaderSize;

		public int MaxLightPerTile
		{
			get
			{
				if (!m_Counters.IsCreated)
				{
					return 0;
				}
				return m_Counters[0];
			}
		}

		public int TileDataCapacity
		{
			get
			{
				if (!m_Counters.IsCreated)
				{
					return 0;
				}
				return m_Counters[2];
			}
		}

		public NativeArray<ushort> Tiles => m_TileData;

		public NativeArray<uint> TileHeaders => m_TileHeaders;

		public DeferredTiler(int tilePixelWidth, int tilePixelHeight, int avgLightPerTile, int tilerLevel)
		{
			m_TilePixelWidth = tilePixelWidth;
			m_TilePixelHeight = tilePixelHeight;
			m_TileXCount = 0;
			m_TileYCount = 0;
			m_TileHeaderSize = ((tilerLevel == 0) ? 4 : 2);
			m_AvgLightPerTile = avgLightPerTile;
			m_TilerLevel = tilerLevel;
			m_FrustumPlanes = new FrustumPlanes
			{
				left = 0f,
				right = 0f,
				bottom = 0f,
				top = 0f,
				zNear = 0f,
				zFar = 0f
			};
			m_IsOrthographic = false;
			m_Counters = default(NativeArray<int>);
			m_TileData = default(NativeArray<ushort>);
			m_TileHeaders = default(NativeArray<uint>);
			m_PreTiles = default(NativeArray<PreTile>);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetTileOffsetAndCount(int i, int j, out int offset, out int count)
		{
			int tileHeaderOffset = GetTileHeaderOffset(i, j);
			offset = (int)m_TileHeaders[tileHeaderOffset];
			count = (int)m_TileHeaders[tileHeaderOffset + 1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetTileHeaderOffset(int i, int j)
		{
			return (i + j * m_TileXCount) * m_TileHeaderSize;
		}

		public void Setup(int tileDataCapacity)
		{
			if (tileDataCapacity <= 0)
			{
				tileDataCapacity = m_TileXCount * m_TileYCount * m_AvgLightPerTile;
			}
			m_Counters = new NativeArray<int>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			m_TileData = new NativeArray<ushort>(tileDataCapacity, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			m_TileHeaders = new NativeArray<uint>(m_TileXCount * m_TileYCount * m_TileHeaderSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			m_Counters[0] = 0;
			m_Counters[1] = 0;
			m_Counters[2] = tileDataCapacity;
		}

		public void OnCameraCleanup()
		{
			if (m_TileHeaders.IsCreated)
			{
				m_TileHeaders.Dispose();
			}
			if (m_TileData.IsCreated)
			{
				m_TileData.Dispose();
			}
			if (m_Counters.IsCreated)
			{
				m_Counters.Dispose();
			}
		}

		public void PrecomputeTiles(Matrix4x4 proj, bool isOrthographic, int renderWidth, int renderHeight)
		{
			m_TileXCount = (renderWidth + m_TilePixelWidth - 1) / m_TilePixelWidth;
			m_TileYCount = (renderHeight + m_TilePixelHeight - 1) / m_TilePixelHeight;
			m_PreTiles = DeferredShaderData.instance.GetPreTiles(m_TilerLevel, m_TileXCount * m_TileYCount);
			int num = Align(renderWidth, m_TilePixelWidth);
			int num2 = Align(renderHeight, m_TilePixelHeight);
			m_FrustumPlanes = proj.decomposeProjection;
			m_FrustumPlanes.right = m_FrustumPlanes.left + (m_FrustumPlanes.right - m_FrustumPlanes.left) * ((float)num / (float)renderWidth);
			m_FrustumPlanes.bottom = m_FrustumPlanes.top + (m_FrustumPlanes.bottom - m_FrustumPlanes.top) * ((float)num2 / (float)renderHeight);
			m_IsOrthographic = isOrthographic;
			float num3 = (m_FrustumPlanes.right - m_FrustumPlanes.left) / (float)m_TileXCount;
			float num4 = (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / (float)m_TileYCount;
			if (!isOrthographic)
			{
				PreTile value = default(PreTile);
				for (int i = 0; i < m_TileYCount; i++)
				{
					float num5 = m_FrustumPlanes.top - num4 * (float)i;
					float y = num5 - num4;
					for (int j = 0; j < m_TileXCount; j++)
					{
						float num6 = m_FrustumPlanes.left + num3 * (float)j;
						float x = num6 + num3;
						value.planeLeft = MakePlane(new float3(num6, y, 0f - m_FrustumPlanes.zNear), new float3(num6, num5, 0f - m_FrustumPlanes.zNear));
						value.planeRight = MakePlane(new float3(x, num5, 0f - m_FrustumPlanes.zNear), new float3(x, y, 0f - m_FrustumPlanes.zNear));
						value.planeBottom = MakePlane(new float3(x, y, 0f - m_FrustumPlanes.zNear), new float3(num6, y, 0f - m_FrustumPlanes.zNear));
						value.planeTop = MakePlane(new float3(num6, num5, 0f - m_FrustumPlanes.zNear), new float3(x, num5, 0f - m_FrustumPlanes.zNear));
						m_PreTiles[j + i * m_TileXCount] = value;
					}
				}
				return;
			}
			PreTile value2 = default(PreTile);
			for (int k = 0; k < m_TileYCount; k++)
			{
				float num7 = m_FrustumPlanes.top - num4 * (float)k;
				float y2 = num7 - num4;
				for (int l = 0; l < m_TileXCount; l++)
				{
					float num8 = m_FrustumPlanes.left + num3 * (float)l;
					float x2 = num8 + num3;
					value2.planeLeft = MakePlane(new float3(num8, y2, 0f - m_FrustumPlanes.zNear), new float3(num8, y2, 0f - m_FrustumPlanes.zNear - 1f), new float3(num8, num7, 0f - m_FrustumPlanes.zNear));
					value2.planeRight = MakePlane(new float3(x2, num7, 0f - m_FrustumPlanes.zNear), new float3(x2, num7, 0f - m_FrustumPlanes.zNear - 1f), new float3(x2, y2, 0f - m_FrustumPlanes.zNear));
					value2.planeBottom = MakePlane(new float3(x2, y2, 0f - m_FrustumPlanes.zNear), new float3(x2, y2, 0f - m_FrustumPlanes.zNear - 1f), new float3(num8, y2, 0f - m_FrustumPlanes.zNear));
					value2.planeTop = MakePlane(new float3(num8, num7, 0f - m_FrustumPlanes.zNear), new float3(num8, num7, 0f - m_FrustumPlanes.zNear - 1f), new float3(x2, num7, 0f - m_FrustumPlanes.zNear));
					m_PreTiles[l + k * m_TileXCount] = value2;
				}
			}
		}

		public unsafe void CullFinalLights(ref NativeArray<PrePunctualLight> punctualLights, ref NativeArray<ushort> lightIndices, int lightStartIndex, int lightCount, int istart, int iend, int jstart, int jend)
		{
			PrePunctualLight* unsafeBufferPointerWithoutChecks = (PrePunctualLight*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(punctualLights);
			ushort* unsafeBufferPointerWithoutChecks2 = (ushort*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(lightIndices);
			uint* unsafeBufferPointerWithoutChecks3 = (uint*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_TileHeaders);
			if (lightCount == 0)
			{
				for (int i = jstart; i < jend; i++)
				{
					for (int j = istart; j < iend; j++)
					{
						int tileHeaderOffset = GetTileHeaderOffset(j, i);
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset] = 0u;
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset + 1] = 0u;
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset + 2] = 0u;
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset + 3] = 0u;
					}
				}
				return;
			}
			ushort* ptr = stackalloc ushort[lightCount * 2];
			float2* ptr2 = stackalloc float2[lightCount];
			int num = 0;
			int num2 = lightStartIndex + lightCount;
			float2 @float = new float2((m_FrustumPlanes.right - m_FrustumPlanes.left) / (float)m_TileXCount, (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / (float)m_TileYCount);
			float2 float2 = @float * 0.5f;
			float2 float3 = new float2(1f / float2.x, 1f / float2.y);
			for (int k = jstart; k < jend; k++)
			{
				float y = m_FrustumPlanes.top - (float2.y + (float)k * @float.y);
				for (int l = istart; l < iend; l++)
				{
					float x = m_FrustumPlanes.left + float2.x + (float)l * @float.x;
					_ = m_PreTiles[l + k * m_TileXCount];
					int num3 = 0;
					float num4 = float.MaxValue;
					float num5 = float.MinValue;
					if (!m_IsOrthographic)
					{
						for (int m = lightStartIndex; m < num2; m++)
						{
							ushort num6 = unsafeBufferPointerWithoutChecks2[m];
							PrePunctualLight prePunctualLight = unsafeBufferPointerWithoutChecks[(int)num6];
							float2 float4 = new float2(x, y);
							float2 float5 = prePunctualLight.screenPos - float4;
							float2 float6 = math.abs(float5 * float3);
							float num7 = 1f / max3(float6.x, float6.y, 1f);
							if (IntersectionLineSphere(rayDirection: new float3(float4.x + float5.x * num7, float4.y + float5.y * num7, 0f - m_FrustumPlanes.zNear), raySource: new float3(0f), centre: prePunctualLight.posVS, radius: prePunctualLight.radius, t0: out var t, t1: out var t2))
							{
								num4 = ((num4 < t) ? num4 : t);
								num5 = ((num5 > t2) ? num5 : t2);
								ptr2[num3] = new float2(t, t2);
								ptr[num3] = prePunctualLight.visLightIndex;
								num3++;
							}
						}
					}
					else
					{
						for (int n = lightStartIndex; n < num2; n++)
						{
							ushort num8 = unsafeBufferPointerWithoutChecks2[n];
							PrePunctualLight prePunctualLight2 = unsafeBufferPointerWithoutChecks[(int)num8];
							float2 float7 = new float2(x, y);
							float2 float8 = prePunctualLight2.screenPos - float7;
							float2 float9 = math.abs(float8 * float3);
							float num9 = 1f / max3(float9.x, float9.y, 1f);
							if (IntersectionLineSphere(rayDirection: new float3(0f, 0f, 0f - m_FrustumPlanes.zNear), raySource: new float3(float7.x + float8.x * num9, float7.y + float8.y * num9, 0f), centre: prePunctualLight2.posVS, radius: prePunctualLight2.radius, t0: out var t3, t1: out var t4))
							{
								num4 = ((num4 < t3) ? num4 : t3);
								num5 = ((num5 > t4) ? num5 : t4);
								ptr2[num3] = new float2(t3, t4);
								ptr[num3] = prePunctualLight2.visLightIndex;
								num3++;
							}
						}
					}
					num4 = max2(num4 * m_FrustumPlanes.zNear, m_FrustumPlanes.zNear);
					num5 = min2(num5 * m_FrustumPlanes.zNear, m_FrustumPlanes.zFar);
					uint num10 = 0u;
					float num11 = 1f / (num5 - num4);
					for (int num12 = 0; num12 < num3; num12++)
					{
						float num13 = max2(ptr2[num12].x * m_FrustumPlanes.zNear, m_FrustumPlanes.zNear);
						float num14 = min2(ptr2[num12].y * m_FrustumPlanes.zNear, m_FrustumPlanes.zFar);
						int num15 = (int)((num13 - num4) * 32f * num11);
						int num16 = math.min((int)((num14 - num4) * 32f * num11) - num15 + 1, 32 - num15);
						num10 |= uint.MaxValue >> 32 - num16 << num15;
						ptr[num3 + num12] = (ushort)(num15 | (num16 << 8));
					}
					float num17 = 32f * num11;
					float x2 = (0f - num4) * num17;
					int size = num3 * 2;
					int num18 = ((num3 > 0) ? AddTileData(ptr, ref size) : 0);
					int tileHeaderOffset2 = GetTileHeaderOffset(l, k);
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2] = (uint)num18;
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2 + 1] = ((size != 0) ? ((uint)num3) : 0u);
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2 + 2] = _f32tof16(num17) | (_f32tof16(x2) << 16);
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2 + 3] = num10;
					num = math.max(num, num3);
				}
			}
			m_Counters[0] = math.max(m_Counters[0], num);
		}

		public unsafe void CullIntermediateLights(ref NativeArray<PrePunctualLight> punctualLights, ref NativeArray<ushort> lightIndices, int lightStartIndex, int lightCount, int istart, int iend, int jstart, int jend)
		{
			PrePunctualLight* unsafeBufferPointerWithoutChecks = (PrePunctualLight*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(punctualLights);
			ushort* unsafeBufferPointerWithoutChecks2 = (ushort*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(lightIndices);
			uint* unsafeBufferPointerWithoutChecks3 = (uint*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_TileHeaders);
			if (lightCount == 0)
			{
				for (int i = jstart; i < jend; i++)
				{
					for (int j = istart; j < iend; j++)
					{
						int tileHeaderOffset = GetTileHeaderOffset(j, i);
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset] = 0u;
						unsafeBufferPointerWithoutChecks3[tileHeaderOffset + 1] = 0u;
					}
				}
				return;
			}
			ushort* ptr = stackalloc ushort[lightCount];
			int num = lightStartIndex + lightCount;
			for (int k = jstart; k < jend; k++)
			{
				for (int l = istart; l < iend; l++)
				{
					PreTile tile = m_PreTiles[l + k * m_TileXCount];
					int size = 0;
					for (int m = lightStartIndex; m < num; m++)
					{
						ushort num2 = unsafeBufferPointerWithoutChecks2[m];
						PrePunctualLight prePunctualLight = unsafeBufferPointerWithoutChecks[(int)num2];
						if (Clip(ref tile, prePunctualLight.posVS, prePunctualLight.radius))
						{
							ptr[size] = num2;
							size++;
						}
					}
					int num3 = ((size > 0) ? AddTileData(ptr, ref size) : 0);
					int tileHeaderOffset2 = GetTileHeaderOffset(l, k);
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2] = (uint)num3;
					unsafeBufferPointerWithoutChecks3[tileHeaderOffset2 + 1] = (uint)size;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe int AddTileData(ushort* lightData, ref int size)
		{
			int* unsafePtr = (int*)m_Counters.GetUnsafePtr();
			int num = Interlocked.Add(ref unsafePtr[1], size);
			int num2 = num - size;
			if (num <= m_TileData.Length)
			{
				ushort* unsafePtr2 = (ushort*)m_TileData.GetUnsafePtr();
				UnsafeUtility.MemCpy(unsafePtr2 + num2, lightData, size * 2);
				return num2;
			}
			m_Counters[2] = math.max(m_Counters[2], num);
			size = 0;
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IntersectionLineSphere(float3 centre, float radius, float3 raySource, float3 rayDirection, out float t0, out float t1)
		{
			float num = math.dot(rayDirection, rayDirection);
			float num2 = math.dot(raySource - centre, rayDirection);
			float num3 = math.dot(raySource, raySource) + math.dot(centre, centre) - radius * radius - 2f * math.dot(raySource, centre);
			float num4 = num2 * num2 - num * num3;
			if (num4 > 0f)
			{
				float num5 = math.sqrt(num4);
				float num6 = 1f / num;
				t0 = (0f - num2 - num5) * num6;
				t1 = (0f - num2 + num5) * num6;
				return true;
			}
			t0 = 0f;
			t1 = 0f;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Clip(ref PreTile tile, float3 posVS, float radius)
		{
			float radiusSq = radius * radius;
			int insideCount = 0;
			ClipResult clipResult = ClipPartial(tile.planeLeft, tile.planeBottom, tile.planeTop, posVS, radius, radiusSq, ref insideCount);
			if (clipResult != 0)
			{
				return clipResult == ClipResult.In;
			}
			clipResult = ClipPartial(tile.planeRight, tile.planeBottom, tile.planeTop, posVS, radius, radiusSq, ref insideCount);
			if (clipResult != 0)
			{
				return clipResult == ClipResult.In;
			}
			clipResult = ClipPartial(tile.planeTop, tile.planeLeft, tile.planeRight, posVS, radius, radiusSq, ref insideCount);
			if (clipResult != 0)
			{
				return clipResult == ClipResult.In;
			}
			clipResult = ClipPartial(tile.planeBottom, tile.planeLeft, tile.planeRight, posVS, radius, radiusSq, ref insideCount);
			if (clipResult != 0)
			{
				return clipResult == ClipResult.In;
			}
			return insideCount == 4;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ClipResult ClipPartial(float4 plane, float4 sidePlaneA, float4 sidePlaneB, float3 posVS, float radius, float radiusSq, ref int insideCount)
		{
			float num = DistanceToPlane(plane, posVS);
			if (num + radius <= 0f)
			{
				return ClipResult.Out;
			}
			if (num < 0f)
			{
				float3 p = posVS - plane.xyz * num;
				float num2 = radiusSq - num * num;
				if (SignedSq(DistanceToPlane(sidePlaneA, p)) >= 0f - num2 && SignedSq(DistanceToPlane(sidePlaneB, p)) >= 0f - num2)
				{
					return ClipResult.In;
				}
			}
			else
			{
				insideCount++;
			}
			return ClipResult.Unknown;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float4 MakePlane(float3 pb, float3 pc)
		{
			float3 x = math.cross(pb, pc);
			x = math.normalize(x);
			return new float4(x.x, x.y, x.z, 0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float4 MakePlane(float3 pa, float3 pb, float3 pc)
		{
			float3 x = pb - pa;
			float3 y = pc - pa;
			float3 x2 = math.cross(x, y);
			x2 = math.normalize(x2);
			return new float4(x2.x, x2.y, x2.z, 0f - math.dot(x2, pa));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float DistanceToPlane(float4 plane, float3 p)
		{
			return plane.x * p.x + plane.y * p.y + plane.z * p.z + plane.w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float SignedSq(float f)
		{
			return ((f < 0f) ? (-1f) : 1f) * (f * f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float min2(float a, float b)
		{
			if (!(a < b))
			{
				return b;
			}
			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float max2(float a, float b)
		{
			if (!(a > b))
			{
				return b;
			}
			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float max3(float a, float b, float c)
		{
			if (!(a > b))
			{
				if (!(b > c))
				{
					return c;
				}
				return b;
			}
			if (!(a > c))
			{
				return c;
			}
			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint _f32tof16(float x)
		{
			uint num = math.asuint(x);
			uint num2 = num & 0x7FFFF000u;
			return math.select(math.asuint(min2(math.asfloat(num2) * 1.92593E-34f, 260042750f)) + 4096 >> 13, math.select(31744u, 32256u, (int)num2 > 2139095040), (int)num2 >= 2139095040) | ((num & 0x80000FFFu) >> 16);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Align(int s, int alignment)
		{
			return (s + alignment - 1) / alignment * alignment;
		}
	}
}
