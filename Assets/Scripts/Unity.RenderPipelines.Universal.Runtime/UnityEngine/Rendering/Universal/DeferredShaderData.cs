using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
	internal class DeferredShaderData : IDisposable
	{
		private struct ComputeBufferInfo
		{
			public uint frameUsed;

			public ComputeBufferType type;
		}

		private static DeferredShaderData m_Instance;

		private NativeArray<PreTile>[] m_PreTiles;

		private ComputeBuffer[] m_Buffers;

		private ComputeBufferInfo[] m_BufferInfos;

		private int m_BufferCount;

		private int m_CachedBufferIndex;

		private uint m_FrameIndex;

		internal static DeferredShaderData instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new DeferredShaderData();
				}
				return m_Instance;
			}
		}

		private DeferredShaderData()
		{
			m_PreTiles = new NativeArray<PreTile>[3];
			m_Buffers = new ComputeBuffer[64];
			m_BufferInfos = new ComputeBufferInfo[64];
		}

		public void Dispose()
		{
			DisposeNativeArrays(ref m_PreTiles);
			for (int i = 0; i < m_Buffers.Length; i++)
			{
				if (m_Buffers[i] != null)
				{
					m_Buffers[i].Dispose();
					m_Buffers[i] = null;
				}
			}
			m_BufferCount = 0;
		}

		internal void ResetBuffers()
		{
			m_FrameIndex++;
		}

		internal NativeArray<PreTile> GetPreTiles(int level, int count)
		{
			return GetOrUpdateNativeArray(ref m_PreTiles, level, count);
		}

		internal ComputeBuffer ReserveBuffer<T>(int count, bool asCBuffer) where T : struct
		{
			int num = Marshal.SizeOf<T>();
			int count2 = (asCBuffer ? (Align(num * count, 16) / num) : count);
			return GetOrUpdateBuffer(count2, num, asCBuffer);
		}

		private NativeArray<T> GetOrUpdateNativeArray<T>(ref NativeArray<T>[] nativeArrays, int level, int count) where T : struct
		{
			if (!nativeArrays[level].IsCreated)
			{
				nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
			}
			else if (count > nativeArrays[level].Length)
			{
				nativeArrays[level].Dispose();
				nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
			}
			return nativeArrays[level];
		}

		private void DisposeNativeArrays<T>(ref NativeArray<T>[] nativeArrays) where T : struct
		{
			for (int i = 0; i < nativeArrays.Length; i++)
			{
				if (nativeArrays[i].IsCreated)
				{
					nativeArrays[i].Dispose();
				}
			}
		}

		private ComputeBuffer GetOrUpdateBuffer(int count, int stride, bool isConstantBuffer)
		{
			ComputeBufferType computeBufferType = (isConstantBuffer ? ComputeBufferType.Constant : ComputeBufferType.Structured);
			int maxQueuedFrames = QualitySettings.maxQueuedFrames;
			for (int i = 0; i < m_BufferCount; i++)
			{
				int num = (m_CachedBufferIndex + i + 1) % m_BufferCount;
				if (IsLessCircular(m_BufferInfos[num].frameUsed + (uint)maxQueuedFrames, m_FrameIndex) && m_BufferInfos[num].type == computeBufferType && m_Buffers[num].count == count && m_Buffers[num].stride == stride)
				{
					m_BufferInfos[num].frameUsed = m_FrameIndex;
					m_CachedBufferIndex = num;
					return m_Buffers[num];
				}
			}
			if (m_BufferCount == m_Buffers.Length)
			{
				ComputeBuffer[] array = new ComputeBuffer[m_BufferCount * 2];
				for (int j = 0; j < m_BufferCount; j++)
				{
					array[j] = m_Buffers[j];
				}
				m_Buffers = array;
				ComputeBufferInfo[] array2 = new ComputeBufferInfo[m_BufferCount * 2];
				for (int k = 0; k < m_BufferCount; k++)
				{
					array2[k] = m_BufferInfos[k];
				}
				m_BufferInfos = array2;
			}
			m_Buffers[m_BufferCount] = new ComputeBuffer(count, stride, computeBufferType, ComputeBufferMode.Immutable);
			m_BufferInfos[m_BufferCount].frameUsed = m_FrameIndex;
			m_BufferInfos[m_BufferCount].type = computeBufferType;
			m_CachedBufferIndex = m_BufferCount;
			return m_Buffers[m_BufferCount++];
		}

		private void DisposeBuffers(ComputeBuffer[,] buffers)
		{
			for (int i = 0; i < buffers.GetLength(0); i++)
			{
				for (int j = 0; j < buffers.GetLength(1); j++)
				{
					if (buffers[i, j] != null)
					{
						buffers[i, j].Dispose();
						buffers[i, j] = null;
					}
				}
			}
		}

		private static bool IsLessCircular(uint a, uint b)
		{
			if (a == b)
			{
				return false;
			}
			return b - a < 2147483648u;
		}

		private static int Align(int s, int alignment)
		{
			return (s + alignment - 1) / alignment * alignment;
		}
	}
}
