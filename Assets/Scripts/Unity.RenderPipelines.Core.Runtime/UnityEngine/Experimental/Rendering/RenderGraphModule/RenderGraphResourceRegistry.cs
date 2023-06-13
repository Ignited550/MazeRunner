using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
	internal class RenderGraphResourceRegistry
	{
		private class IRenderGraphResource
		{
			public bool imported;

			public int cachedHash;

			public int transientPassIndex;

			public uint writeCount;

			public bool wasReleased;

			public bool requestFallBack;

			public virtual void Reset()
			{
				imported = false;
				cachedHash = -1;
				transientPassIndex = -1;
				wasReleased = false;
				requestFallBack = false;
				writeCount = 0u;
			}

			public virtual string GetName()
			{
				return "";
			}

			public virtual bool IsCreated()
			{
				return false;
			}

			public void IncrementWriteCount()
			{
				writeCount++;
			}

			public bool NeedsFallBack()
			{
				if (requestFallBack)
				{
					return writeCount == 0;
				}
				return false;
			}
		}

		[DebuggerDisplay("Resource ({GetType().Name}:{GetName()})")]
		private class RenderGraphResource<DescType, ResType> : IRenderGraphResource where DescType : struct where ResType : class
		{
			public DescType desc;

			public ResType resource;

			protected RenderGraphResource()
			{
			}

			public override void Reset()
			{
				base.Reset();
				resource = null;
			}

			public override bool IsCreated()
			{
				return resource != null;
			}
		}

		[DebuggerDisplay("TextureResource ({desc.name})")]
		private class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
		{
			public override string GetName()
			{
				if (imported)
				{
					if (resource == null)
					{
						return "null resource";
					}
					return resource.name;
				}
				return desc.name;
			}
		}

		[DebuggerDisplay("ComputeBufferResource ({desc.name})")]
		private class ComputeBufferResource : RenderGraphResource<ComputeBufferDesc, ComputeBuffer>
		{
			public override string GetName()
			{
				if (imported)
				{
					return "ImportedComputeBuffer";
				}
				return desc.name;
			}
		}

		internal struct RendererListResource
		{
			public RendererListDesc desc;

			public RendererList rendererList;

			internal RendererListResource(in RendererListDesc desc)
			{
				this.desc = desc;
				rendererList = default(RendererList);
			}
		}

		private static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

		private static RenderGraphResourceRegistry m_CurrentRegistry;

		private DynamicArray<IRenderGraphResource>[] m_Resources = new DynamicArray<IRenderGraphResource>[2];

		private TexturePool m_TexturePool = new TexturePool();

		private int m_TextureCreationIndex;

		private ComputeBufferPool m_ComputeBufferPool = new ComputeBufferPool();

		private DynamicArray<RendererListResource> m_RendererListResources = new DynamicArray<RendererListResource>();

		private RenderGraphDebugParams m_RenderGraphDebug;

		private RenderGraphLogger m_Logger;

		private int m_CurrentFrameIndex;

		private RTHandle m_CurrentBackbuffer;

		internal static RenderGraphResourceRegistry current
		{
			get
			{
				return m_CurrentRegistry;
			}
			set
			{
				m_CurrentRegistry = value;
			}
		}

		internal RTHandle GetTexture(in TextureHandle handle)
		{
			if (!handle.IsValid())
			{
				return null;
			}
			return GetTextureResource(in handle.handle).resource;
		}

		internal bool TextureNeedsFallback(in TextureHandle handle)
		{
			if (!handle.IsValid())
			{
				return false;
			}
			return GetTextureResource(in handle.handle).NeedsFallBack();
		}

		internal RendererList GetRendererList(in RendererListHandle handle)
		{
			if (!handle.IsValid() || (int)handle >= m_RendererListResources.size)
			{
				return RendererList.nullRendererList;
			}
			return m_RendererListResources[handle].rendererList;
		}

		internal ComputeBuffer GetComputeBuffer(in ComputeBufferHandle handle)
		{
			if (!handle.IsValid())
			{
				return null;
			}
			return GetComputeBufferResource(in handle.handle).resource;
		}

		private RenderGraphResourceRegistry()
		{
		}

		internal RenderGraphResourceRegistry(RenderGraphDebugParams renderGraphDebug, RenderGraphLogger logger)
		{
			m_RenderGraphDebug = renderGraphDebug;
			m_Logger = logger;
			for (int i = 0; i < 2; i++)
			{
				m_Resources[i] = new DynamicArray<IRenderGraphResource>();
			}
		}

		internal void BeginRender(int currentFrameIndex, int executionCount)
		{
			m_CurrentFrameIndex = currentFrameIndex;
			ResourceHandle.NewFrame(executionCount);
			current = this;
		}

		internal void EndRender()
		{
			current = null;
		}

		private void CheckHandleValidity(in ResourceHandle res)
		{
			CheckHandleValidity(res.type, res.index);
		}

		private void CheckHandleValidity(RenderGraphResourceType type, int index)
		{
			DynamicArray<IRenderGraphResource> dynamicArray = m_Resources[(int)type];
			if (index >= dynamicArray.size)
			{
				throw new ArgumentException($"Trying to access resource of type {type} with an invalid resource index {index}");
			}
		}

		internal void IncrementWriteCount(in ResourceHandle res)
		{
			CheckHandleValidity(in res);
			m_Resources[res.iType][res.index].IncrementWriteCount();
		}

		internal string GetResourceName(in ResourceHandle res)
		{
			CheckHandleValidity(in res);
			return m_Resources[res.iType][res.index].GetName();
		}

		internal string GetResourceName(RenderGraphResourceType type, int index)
		{
			CheckHandleValidity(type, index);
			return m_Resources[(int)type][index].GetName();
		}

		internal bool IsResourceImported(in ResourceHandle res)
		{
			CheckHandleValidity(in res);
			return m_Resources[res.iType][res.index].imported;
		}

		internal bool IsResourceCreated(in ResourceHandle res)
		{
			CheckHandleValidity(in res);
			return m_Resources[res.iType][res.index].IsCreated();
		}

		internal bool IsRendererListCreated(in RendererListHandle res)
		{
			return m_RendererListResources[res].rendererList.isValid;
		}

		internal bool IsResourceImported(RenderGraphResourceType type, int index)
		{
			CheckHandleValidity(type, index);
			return m_Resources[(int)type][index].imported;
		}

		internal int GetResourceTransientIndex(in ResourceHandle res)
		{
			CheckHandleValidity(in res);
			return m_Resources[res.iType][res.index].transientPassIndex;
		}

		internal TextureHandle ImportTexture(RTHandle rt)
		{
			TextureResource outRes;
			int handle = AddNewResource<TextureResource>(m_Resources[0], out outRes);
			outRes.resource = rt;
			outRes.imported = true;
			return new TextureHandle(handle);
		}

		internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
		{
			if (m_CurrentBackbuffer != null)
			{
				m_CurrentBackbuffer.SetTexture(rt);
			}
			else
			{
				m_CurrentBackbuffer = RTHandles.Alloc(rt, "Backbuffer");
			}
			TextureResource outRes;
			int handle = AddNewResource<TextureResource>(m_Resources[0], out outRes);
			outRes.resource = m_CurrentBackbuffer;
			outRes.imported = true;
			return new TextureHandle(handle);
		}

		private int AddNewResource<ResType>(DynamicArray<IRenderGraphResource> resourceArray, out ResType outRes) where ResType : IRenderGraphResource, new()
		{
			int size = resourceArray.size;
			resourceArray.Resize(resourceArray.size + 1, keepContent: true);
			if (resourceArray[size] == null)
			{
				resourceArray[size] = new ResType();
			}
			outRes = resourceArray[size] as ResType;
			outRes.Reset();
			return size;
		}

		internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
		{
			ValidateTextureDesc(in desc);
			TextureResource outRes;
			int handle = AddNewResource<TextureResource>(m_Resources[0], out outRes);
			outRes.requestFallBack = desc.fallBackToBlackTexture;
			outRes.desc = desc;
			outRes.transientPassIndex = transientPassIndex;
			return new TextureHandle(handle);
		}

		internal int GetTextureResourceCount()
		{
			return m_Resources[0].size;
		}

		private TextureResource GetTextureResource(in ResourceHandle handle)
		{
			return m_Resources[0][handle] as TextureResource;
		}

		internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle)
		{
			return (m_Resources[0][handle] as TextureResource).desc;
		}

		internal RendererListHandle CreateRendererList(in RendererListDesc desc)
		{
			ValidateRendererListDesc(in desc);
			DynamicArray<RendererListResource> rendererListResources = m_RendererListResources;
			RendererListResource value = new RendererListResource(in desc);
			return new RendererListHandle(rendererListResources.Add(in value));
		}

		internal ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
		{
			ComputeBufferResource outRes;
			int handle = AddNewResource<ComputeBufferResource>(m_Resources[1], out outRes);
			outRes.resource = computeBuffer;
			outRes.imported = true;
			return new ComputeBufferHandle(handle);
		}

		internal ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc, int transientPassIndex = -1)
		{
			ValidateComputeBufferDesc(in desc);
			ComputeBufferResource outRes;
			int handle = AddNewResource<ComputeBufferResource>(m_Resources[1], out outRes);
			outRes.desc = desc;
			outRes.transientPassIndex = transientPassIndex;
			return new ComputeBufferHandle(handle);
		}

		internal ComputeBufferDesc GetComputeBufferResourceDesc(in ResourceHandle handle)
		{
			return (m_Resources[1][handle] as ComputeBufferResource).desc;
		}

		internal int GetComputeBufferResourceCount()
		{
			return m_Resources[1].size;
		}

		private ComputeBufferResource GetComputeBufferResource(in ResourceHandle handle)
		{
			return m_Resources[1][handle] as ComputeBufferResource;
		}

		internal void CreateAndClearTexture(RenderGraphContext rgContext, int index)
		{
			TextureResource textureResource = m_Resources[0][index] as TextureResource;
			if (textureResource.imported)
			{
				return;
			}
			TextureDesc desc = textureResource.desc;
			int hashCode = desc.GetHashCode();
			if (textureResource.resource != null)
			{
				throw new InvalidOperationException($"Trying to create an already created texture ({textureResource.desc.name}). Texture was probably declared for writing more than once in the same pass.");
			}
			textureResource.resource = null;
			if (!m_TexturePool.TryGetResource(hashCode, out textureResource.resource))
			{
				string name = $"RenderGraphTexture_{m_TextureCreationIndex++}";
				switch (desc.sizeMode)
				{
				case TextureSizeMode.Explicit:
					textureResource.resource = RTHandles.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite, desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
					break;
				case TextureSizeMode.Scale:
					textureResource.resource = RTHandles.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite, desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
					break;
				case TextureSizeMode.Functor:
					textureResource.resource = RTHandles.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite, desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
					break;
				}
			}
			textureResource.cachedHash = hashCode;
			FastMemoryDesc fastMemoryDesc = textureResource.desc.fastMemoryDesc;
			if (fastMemoryDesc.inFastMemory)
			{
				textureResource.resource.SwitchToFastMemory(rgContext.cmd, fastMemoryDesc.residencyFraction, fastMemoryDesc.flags);
			}
			if (textureResource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation)
			{
				bool flag = m_RenderGraphDebug.clearRenderTargetsAtCreation && !textureResource.desc.clearBuffer;
				using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClear)))
				{
					ClearFlag clearFlag = ((textureResource.desc.depthBufferBits == DepthBits.None) ? ClearFlag.Color : ClearFlag.Depth);
					Color clearColor = (flag ? Color.magenta : textureResource.desc.clearColor);
					CoreUtils.SetRenderTarget(rgContext.cmd, textureResource.resource, clearFlag, clearColor);
				}
			}
			m_TexturePool.RegisterFrameAllocation(hashCode, textureResource.resource);
			LogTextureCreation(textureResource);
		}

		internal void CreateComputeBuffer(RenderGraphContext rgContext, int index)
		{
			ComputeBufferResource computeBufferResource = m_Resources[1][index] as ComputeBufferResource;
			if (!computeBufferResource.imported)
			{
				ComputeBufferDesc desc = computeBufferResource.desc;
				int hashCode = desc.GetHashCode();
				if (computeBufferResource.resource != null)
				{
					throw new InvalidOperationException($"Trying to create an already created Compute Buffer ({computeBufferResource.desc.name}). Buffer was probably declared for writing more than once in the same pass.");
				}
				computeBufferResource.resource = null;
				if (!m_ComputeBufferPool.TryGetResource(hashCode, out computeBufferResource.resource))
				{
					computeBufferResource.resource = new ComputeBuffer(computeBufferResource.desc.count, computeBufferResource.desc.stride, computeBufferResource.desc.type);
					computeBufferResource.resource.name = $"RenderGraphComputeBuffer_{computeBufferResource.desc.count}_{computeBufferResource.desc.stride}_{computeBufferResource.desc.type}";
				}
				computeBufferResource.cachedHash = hashCode;
				m_ComputeBufferPool.RegisterFrameAllocation(hashCode, computeBufferResource.resource);
				LogComputeBufferCreation(computeBufferResource);
			}
		}

		internal void ReleaseTexture(RenderGraphContext rgContext, int index)
		{
			TextureResource textureResource = m_Resources[0][index] as TextureResource;
			if (textureResource.imported)
			{
				return;
			}
			if (textureResource.resource == null)
			{
				throw new InvalidOperationException("Tried to release a texture (" + textureResource.desc.name + ") that was never created. Check that there is at least one pass writing to it first.");
			}
			if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
			{
				using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClearDebug)))
				{
					ClearFlag clearFlag = ((textureResource.desc.depthBufferBits == DepthBits.None) ? ClearFlag.Color : ClearFlag.Depth);
					CommandBuffer cmd = rgContext.cmd;
					TextureHandle handle = new TextureHandle(index);
					CoreUtils.SetRenderTarget(cmd, GetTexture(in handle), clearFlag, Color.magenta);
				}
			}
			LogTextureRelease(textureResource);
			m_TexturePool.ReleaseResource(textureResource.cachedHash, textureResource.resource, m_CurrentFrameIndex);
			m_TexturePool.UnregisterFrameAllocation(textureResource.cachedHash, textureResource.resource);
			textureResource.cachedHash = -1;
			textureResource.resource = null;
			textureResource.wasReleased = true;
		}

		internal void ReleaseComputeBuffer(RenderGraphContext rgContext, int index)
		{
			ComputeBufferResource computeBufferResource = m_Resources[1][index] as ComputeBufferResource;
			if (!computeBufferResource.imported)
			{
				if (computeBufferResource.resource == null)
				{
					throw new InvalidOperationException("Tried to release a compute buffer (" + computeBufferResource.desc.name + ") that was never created. Check that there is at least one pass writing to it first.");
				}
				LogComputeBufferRelease(computeBufferResource);
				m_ComputeBufferPool.ReleaseResource(computeBufferResource.cachedHash, computeBufferResource.resource, m_CurrentFrameIndex);
				m_ComputeBufferPool.UnregisterFrameAllocation(computeBufferResource.cachedHash, computeBufferResource.resource);
				computeBufferResource.cachedHash = -1;
				computeBufferResource.resource = null;
				computeBufferResource.wasReleased = true;
			}
		}

		private void ValidateTextureDesc(in TextureDesc desc)
		{
		}

		private void ValidateRendererListDesc(in RendererListDesc desc)
		{
		}

		private void ValidateComputeBufferDesc(in ComputeBufferDesc desc)
		{
		}

		internal void CreateRendererLists(List<RendererListHandle> rendererLists)
		{
			foreach (RendererListHandle rendererList2 in rendererLists)
			{
				ref RendererListResource reference = ref m_RendererListResources[rendererList2];
				RendererList rendererList = RendererList.Create(in reference.desc);
				reference.rendererList = rendererList;
			}
		}

		internal void Clear(bool onException)
		{
			LogResources();
			for (int i = 0; i < 2; i++)
			{
				m_Resources[i].Clear();
			}
			m_RendererListResources.Clear();
			m_TexturePool.CheckFrameAllocation(onException, m_CurrentFrameIndex);
			m_ComputeBufferPool.CheckFrameAllocation(onException, m_CurrentFrameIndex);
		}

		internal void PurgeUnusedResources()
		{
			m_TexturePool.PurgeUnusedResources(m_CurrentFrameIndex);
			m_ComputeBufferPool.PurgeUnusedResources(m_CurrentFrameIndex);
		}

		internal void Cleanup()
		{
			m_TexturePool.Cleanup();
			m_ComputeBufferPool.Cleanup();
			RTHandles.Release(m_CurrentBackbuffer);
		}

		private void LogTextureCreation(TextureResource rt)
		{
			if (m_RenderGraphDebug.logFrameInformation)
			{
				m_Logger.LogLine($"Created Texture: {rt.desc.name} (Cleared: {rt.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation})");
			}
		}

		private void LogTextureRelease(TextureResource rt)
		{
			if (m_RenderGraphDebug.logFrameInformation)
			{
				m_Logger.LogLine("Released Texture: " + rt.desc.name);
			}
		}

		private void LogComputeBufferCreation(ComputeBufferResource buffer)
		{
			if (m_RenderGraphDebug.logFrameInformation)
			{
				m_Logger.LogLine("Created ComputeBuffer: " + buffer.desc.name);
			}
		}

		private void LogComputeBufferRelease(ComputeBufferResource buffer)
		{
			if (m_RenderGraphDebug.logFrameInformation)
			{
				m_Logger.LogLine("Released ComputeBuffer: " + buffer.desc.name);
			}
		}

		private void LogResources()
		{
			if (m_RenderGraphDebug.logResources)
			{
				m_Logger.LogLine("==== Allocated Resources ====\n");
				m_TexturePool.LogResources(m_Logger);
				m_Logger.LogLine("");
				m_ComputeBufferPool.LogResources(m_Logger);
			}
		}
	}
}
