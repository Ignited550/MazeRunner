using System;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal struct BitArray : IDisposable
	{
		private NativeArray<uint> m_Mem;

		private int m_BitCount;

		private int m_IntCount;

		public BitArray(int bitCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
		{
			m_BitCount = bitCount;
			m_IntCount = bitCount + 31 >> 5;
			m_Mem = new NativeArray<uint>(m_IntCount, allocator, options);
		}

		public void Dispose()
		{
			m_Mem.Dispose();
		}

		public void Clear()
		{
			for (int i = 0; i < m_IntCount; i++)
			{
				m_Mem[i] = 0u;
			}
		}

		public bool IsSet(int bitIndex)
		{
			return (m_Mem[bitIndex >> 5] & (uint)(1 << bitIndex)) != 0;
		}

		public void Set(int bitIndex, bool val)
		{
			if (val)
			{
				m_Mem[bitIndex >> 5] |= (uint)(1 << bitIndex);
			}
			else
			{
				m_Mem[bitIndex >> 5] &= (uint)(~(1 << bitIndex));
			}
		}
	}
}
