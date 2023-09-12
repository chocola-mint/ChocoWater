/*
The following code is modified from:
https://github.com/DMeville/RefractedTransparentRenderPass
which is licensed under the following terms:

The MIT License (MIT)

Copyright (c) 2021 Dylan

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace CHM.ChocoWater
{
    /// <summary>
    /// This render feature is needed to simulate a Grab Pass, which URP doesn't have.<br/>
    /// Here we grab the screen color after the transparent pass,<br/>
    /// and write it to a Texture2D named _CWScreenColor.
    /// </summary>
    sealed class ChocoWaterRenderFeature : ScriptableRendererFeature
    {
        private class GrabPass : ScriptableRenderPass
        {
            private readonly RenderTargetHandle tempColorTarget;
            private RenderTargetIdentifier cameraTarget;
            private const string ScreenColorName = "_CWScreenColor";
            public GrabPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                tempColorTarget.Init(ScreenColorName);
            }
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) 
            { 
                cameraTarget = renderingData.cameraData.renderer.cameraColorTarget; 
            }
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                cmd.GetTemporaryRT(tempColorTarget.id, cameraTextureDescriptor);
                cmd.SetGlobalTexture(ScreenColorName, tempColorTarget.Identifier());
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                Blit(cmd, cameraTarget, tempColorTarget.Identifier());

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(tempColorTarget.id);
            }
        }
        /// <summary>
        /// This pass is used to draw objects specified in the postTransparentLayerMask.<br/>
        /// It will also enable the _POSTTRANSPARENTPASS global shader keyword during<br/>
        /// rendering.
        /// </summary>
        private class RenderPass : ScriptableRenderPass
        {
            private readonly List<ShaderTagId> shaderTagIdList = new(){
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("LightweightForward"),
            };

            private FilteringSettings filteringSettings;
            private RenderStateBlock renderStateBlock;
            private GlobalKeyword keywordPostTransparentPass;
            public RenderPass(LayerMask layerMask)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;

                filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
                renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                keywordPostTransparentPass = GlobalKeyword.Create("_POSTTRANSPARENTPASS");
            }
            
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                cmd.EnableKeyword(keywordPostTransparentPass);
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawSettings;
                drawSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings, ref renderStateBlock);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.DisableKeyword(keywordPostTransparentPass);
            }
        }

        [SerializeField] 
        [Tooltip("The layer to draw AFTER transparents. For ChocoWater, this should be "
        + "the same layer used by the WaterVolume objects. (Water layer by default)\n")]
        private LayerMask postTransparentLayerMask;
        private GrabPass grabPass;
        private RenderPass renderPass;

        public override void Create()
        {
            grabPass = new GrabPass();
            renderPass = new RenderPass(postTransparentLayerMask);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(grabPass);
            renderer.EnqueuePass(renderPass);
        }
    }
}
