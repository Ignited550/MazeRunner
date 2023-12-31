using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
	public sealed class RenderGraphObjectPool
	{
		private class SharedObjectPool<T> where T : new()
		{
			private Stack<T> m_Pool = new Stack<T>();

			private static readonly Lazy<SharedObjectPool<T>> s_Instance = new Lazy<SharedObjectPool<T>>();

			public static SharedObjectPool<T> sharedPool => s_Instance.Value;

			public T Get()
			{
				if (m_Pool.Count != 0)
				{
					return m_Pool.Pop();
				}
				return new T();
			}

			public void Release(T value)
			{
				m_Pool.Push(value);
			}
		}

		private Dictionary<(Type, int), Stack<object>> m_ArrayPool = new Dictionary<(Type, int), Stack<object>>();

		private List<(object, (Type, int))> m_AllocatedArrays = new List<(object, (Type, int))>();

		private List<MaterialPropertyBlock> m_AllocatedMaterialPropertyBlocks = new List<MaterialPropertyBlock>();

		internal RenderGraphObjectPool()
		{
		}

		public T[] GetTempArray<T>(int size)
		{
			if (!m_ArrayPool.TryGetValue((typeof(T), size), out var value))
			{
				value = new Stack<object>();
				m_ArrayPool.Add((typeof(T), size), value);
			}
			T[] array = ((value.Count > 0) ? ((T[])value.Pop()) : new T[size]);
			m_AllocatedArrays.Add((array, (typeof(T), size)));
			return array;
		}

		public MaterialPropertyBlock GetTempMaterialPropertyBlock()
		{
			MaterialPropertyBlock materialPropertyBlock = SharedObjectPool<MaterialPropertyBlock>.sharedPool.Get();
			materialPropertyBlock.Clear();
			m_AllocatedMaterialPropertyBlocks.Add(materialPropertyBlock);
			return materialPropertyBlock;
		}

		internal void ReleaseAllTempAlloc()
		{
			foreach (var allocatedArray in m_AllocatedArrays)
			{
				m_ArrayPool.TryGetValue(allocatedArray.Item2, out var value);
				value.Push(allocatedArray.Item1);
			}
			m_AllocatedArrays.Clear();
			foreach (MaterialPropertyBlock allocatedMaterialPropertyBlock in m_AllocatedMaterialPropertyBlocks)
			{
				SharedObjectPool<MaterialPropertyBlock>.sharedPool.Release(allocatedMaterialPropertyBlock);
			}
			m_AllocatedMaterialPropertyBlocks.Clear();
		}

		internal T Get<T>() where T : new()
		{
			return SharedObjectPool<T>.sharedPool.Get();
		}

		internal void Release<T>(T value) where T : new()
		{
			SharedObjectPool<T>.sharedPool.Release(value);
		}
	}
}
