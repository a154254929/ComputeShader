using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{

    public RenderTexture hzbDepth;
    public Shader hzbShader;
    public bool stopMpde;

    [Range(0, 1)]
    public float offsetValue = 0.2f;
    public bool useHzb = false;
    public bool updateCulling = true;
    public bool computeshaderMode = true;
    private int CSCullingID;
    public ComputeShader shader;
    public Mesh drawMesh;
    public Material drawMat;
    public Texture testDepth;
    public ComputeBuffer bufferWithArgs;
    public uint[] args;
    ComputeBuffer posBuffer;

    class CustomRenderPass : ScriptableRenderPass
    {
        private Material hzbMat;
        GrassRenderFeature grassFreature;
        private CommandBuffer commandBuffer;
        static int ID_InvSize;
        public CustomRenderPass(
            GrassRenderFeature feature,
            Shader hzbShader
        )
        {
            hzbMat = new Material(hzbShader);
            grassFreature = feature;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            commandBuffer = cmd;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool isMainCamera = renderingData.cameraData.camera.name == "Main Camera";
            if (!grassFreature.stopMpde && isMainCamera)
            {
                commandBuffer.BeginSample("DepthTextureDownSampling");
                int w = grassFreature.hzbDepth.width;
                int h = grassFreature.hzbDepth.height;
                int level = 0;

                if (ID_InvSize == 0)
                {
                    ID_InvSize = Shader.PropertyToID("_InvSize");
                }

                RenderTexture lastRt = null;
                RenderTexture tempRT;
                while (h > 8)
                {
                    hzbMat.SetVector(ID_InvSize, new Vector4(1.0f / w, 1.0f / h, 0, 0));

                    tempRT = RenderTexture.GetTemporary(w, h, 0, grassFreature.hzbDepth.format);
                    tempRT.filterMode = FilterMode.Point;
                    if (lastRt == null)
                    {
                        Blit(commandBuffer, renderingData.cameraData.renderer.cameraDepthTarget, tempRT);
                    }
                    else
                    {
                        Blit(commandBuffer, lastRt, tempRT, hzbMat);
                        RenderTexture.ReleaseTemporary(lastRt);
                    }
                    commandBuffer.CopyTexture(tempRT, 0, 0, grassFreature.hzbDepth, 0, level);
                    lastRt = tempRT;
                    w /= 2;
                    h /= 2;
                    level++;
                }
                RenderTexture.ReleaseTemporary(lastRt);
                commandBuffer.EndSample("DepthTextureDownSampling");
                context.ExecuteCommandBuffer(commandBuffer);
                context.Submit();
                commandBuffer.Clear();
            }
            if (grassFreature.computeshaderMode == false)return;
            commandBuffer.BeginSample("Grass");
            if (isMainCamera)
            {
                commandBuffer.SetRenderTarget(
                    renderingData.cameraData.renderer.cameraColorTarget,
                    renderingData.cameraData.renderer.cameraDepthTarget
                );
            }
            if (grassFreature.updateCulling)
            {
                Culling();
            }
            commandBuffer.DrawMeshInstancedIndirect(
                grassFreature.drawMesh, 0,
                grassFreature.drawMat, 0,
                grassFreature.bufferWithArgs, 0
            );
            commandBuffer.EndSample("Grass");
            context.ExecuteCommandBuffer(commandBuffer);
            context.Submit();
            commandBuffer.Clear();
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        void Culling()
        {
            grassFreature.shader.SetFloat("useHzb", grassFreature.useHzb ? 1 : 0);
            grassFreature.args[1] = 0;
            grassFreature.bufferWithArgs.SetData(grassFreature.args);
            if (grassFreature.hzbDepth != null)
            {
                grassFreature.shader.SetTexture(grassFreature.CSCullingID, "HZB_Depth", grassFreature.hzbDepth);
            }

            grassFreature.shader.SetVector("cmrPos", Camera.main.transform.position);
            grassFreature.shader.SetVector("cmrDir", Camera.main.transform.forward);
            grassFreature.shader.SetFloat("cmrHalfFov", Camera.main.fieldOfView / 2);
            grassFreature.shader.SetFloat("offsetValue", grassFreature.offsetValue);
            var m = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false) * Camera.main.worldToCameraMatrix;

            //高版本 可用  computeShader.SetMatrix("matrix_VP", m); 代替 下面数组传入
            float[] mlist = new float[] {
                m.m00,m.m10,m.m20,m.m30,
                m.m01,m.m11,m.m21,m.m31,
                m.m02,m.m12,m.m22,m.m32,
                m.m03,m.m13,m.m23,m.m33
            };
            grassFreature.shader.SetFloats("matrix_VP", mlist);
            commandBuffer.DispatchCompute(grassFreature.shader, grassFreature.CSCullingID, 400 / 16, 400 / 16, 1);
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        //测试  16万棵草 computeshader 模式
        int count = 400 * 400;
        var terrain = FindObjectOfType<Terrain>();
        Vector3[] posList = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            int x = i % 400;
            int z = i / 400;
            posList[i] = new Vector3(x * 0.5f + StaticRandom(), 0, z * 0.5f + StaticRandom());
            posList[i].y = terrain != null ? terrain.SampleHeight(posList[i]) : 0;
        }

        args = new uint[] {drawMesh.GetIndexCount(0), 0, 0, 0, 0 };
        bufferWithArgs = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        bufferWithArgs.SetData(args);
        CSCullingID = shader.FindKernel("CSCulling");

        posBuffer = new ComputeBuffer(count, 4 * 3);
        posBuffer.SetData(posList);
        var posVisibleBuffer = new ComputeBuffer(count, 4 * 3);
        shader.SetBuffer(CSCullingID, "bufferWithArgs", bufferWithArgs);
        shader.SetBuffer(CSCullingID, "posAllBuffer", posBuffer);
        shader.SetBuffer(CSCullingID, "posVisibleBuffer", posVisibleBuffer);

        drawMat.SetBuffer("posVisibleBuffer", posVisibleBuffer);
        hzbDepth = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RHalf);
        hzbDepth.autoGenerateMips = false;

        hzbDepth.useMipMap = true;
        hzbDepth.filterMode = FilterMode.Point;
        hzbDepth.Create();

        m_ScriptablePass = new CustomRenderPass(this, hzbShader);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    static int staticRandomID = 0;
    float StaticRandom()
    {
        float v = 0;
        v = Mathf.Abs(Mathf.Sin(staticRandomID)) * 1000 + Mathf.Abs(Mathf.Cos(staticRandomID * 0.1f)) * 100;
        v -= (int)v;

        staticRandomID++;
        return v;
    }
}


