using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class AtomosphericFeature : ScriptableRendererFeature
{
    //创建一个Pass类
    class AtomosphericRenderPass : ScriptableRenderPass
    {
        //pass属性
        public Material _Material;
        

        private CustomSettings _Settings;
        private RenderTargetHandle _TemporaryColorTexture;

        private RenderTargetIdentifier _Source; //源RT
        private RenderTargetHandle _Destination; //目标RT
        
        //构造函数 用于初始化 应该可以用初始化队列更快把 maybe
        public AtomosphericRenderPass(CustomSettings settings)
        {
            _Material = settings.GetMaterial();
            _Settings = settings;

        }
        //初始化原纹理
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            _Source = source;
            _Destination = destination;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
            _Settings.Sun = mainLight.light; // 将主光源分配给 sun
            
            _Settings.SetPPShaderParameters();
            
        }
        
        //这个方法在执行渲染通道之前被调用。
        //它可以用来配置渲染目标和它们的清除状态。同时创建临时渲染目标纹理。
        //当此渲染通道为空时，将渲染到活动的相机渲染目标。
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _TemporaryColorTexture.Init("_TemporaryColorTexture");
        }

        //这里执行渲染逻辑
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_Material == null) return;
            //初始化Shader参数

            
            CommandBuffer cmd = CommandBufferPool.Get("My Pass");

            if (_Destination == RenderTargetHandle.CameraTarget)
            {
                //创建一个临时RT
                cmd.GetTemporaryRT(_TemporaryColorTexture.id,renderingData.cameraData.cameraTargetDescriptor,FilterMode.Point);
                //指定材质，绘制到临时RT中
                cmd.Blit(_Source,_TemporaryColorTexture.Identifier(),_Material);
                //绘制回Source
                cmd.Blit(_TemporaryColorTexture.Identifier(), _Source);
            }
            else
            {
                cmd.Blit(_Source,_Destination.Identifier(),_Material);
            }
            //执行缓冲区
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        //清除缓存
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_Destination == RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(_TemporaryColorTexture.id);
            }
        }
    }
    //面板设置的参数
    [System.Serializable]
    public class CustomSettings
    {
        private Shader _Shader;
        private Material _Material;
        public RenderPassEvent PassEvent;
        [NonSerialized]
        public Light Sun;


        [Range(1, 64)]
        public int SampleCount = 16;
        public float MaxRayLength = 400;

        [ColorUsage(false, true)] // 不显示 Alpha 通道，启用 HDR
        public Color IncomingLight = new Color(4, 4, 4, 4);
        [Range(0, 10.0f)]
        public float RayleighScatterCoef = 1;
        [Range(0, 10.0f)]
        public float RayleighExtinctionCoef = 1;
        [Range(0, 10.0f)]
        public float MieScatterCoef = 1;
        [Range(0, 10.0f)]
        public float MieExtinctionCoef = 1;
        [Range(0.0f, 0.999f)]
        public float MieG = 0.76f;
        public float DistanceScale = 1;


        private Color _sunColor;

        private const float AtmosphereHeight = 80000.0f;
        private const float PlanetRadius = 6371000.0f;
        private readonly Vector4 DensityScale = new Vector4(7994.0f, 1200.0f, 0, 0);
        private readonly Vector4 RayleighSct = new Vector4(5.8f, 13.5f, 33.1f, 0.0f) * 0.000001f;
        private readonly Vector4 MieSct = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;
        
        public void SetPPShaderParameters()
        {

            _Material.SetFloat("_AtmosphereHeight", AtmosphereHeight);
            _Material.SetFloat("_PlanetRadius", PlanetRadius);
            _Material.SetVector("_DensityScaleHeight", DensityScale);

            Vector4 scatteringR = new Vector4(5.8f, 13.5f, 33.1f, 0.0f) * 0.000001f;
            Vector4 scatteringM = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;

            _Material.SetVector("_ScatteringR", RayleighSct * RayleighScatterCoef);
            _Material.SetVector("_ScatteringM", MieSct * MieScatterCoef);
            _Material.SetVector("_ExtinctionR", RayleighSct * RayleighExtinctionCoef);
            _Material.SetVector("_ExtinctionM", MieSct * MieExtinctionCoef);

            _Material.SetColor("_IncomingLight", IncomingLight);
            _Material.SetFloat("_MieG", MieG);
            _Material.SetFloat("_DistanceScale", DistanceScale);
            _Material.SetColor("_SunColor", _sunColor);

            //---------------------------------------------------

            _Material.SetVector("_LightDir", new Vector4(Sun.transform.forward.x, Sun.transform.forward.y, Sun.transform.forward.z, 1.0f / (Sun.range * Sun.range)));
            _Material.SetVector("_LightColor", Sun.color * Sun.intensity);
        }

        public Material GetMaterial()
        {
            _Shader = Shader.Find("Hidden/urp_atomospheric");
            if (_Shader)
            {
                _Material = new Material(_Shader);
                _Material.hideFlags = HideFlags.HideAndDontSave;
            }

            return _Material;
        }
    }
    
    public CustomSettings _CustomSettings = new CustomSettings();
    private AtomosphericRenderPass _ScriptablePass;
    
    //初始化
    public override void Create()
    {
        //调用构造函数
        _ScriptablePass = new AtomosphericRenderPass(_CustomSettings);
        //设在渲染队列中的位置
        _ScriptablePass.renderPassEvent = _CustomSettings.PassEvent;
    }
    
    //用于设置，执行渲染。
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _ScriptablePass.Setup(renderer.cameraColorTarget,RenderTargetHandle.CameraTarget);
        //渲染进入队列
        renderer.EnqueuePass(_ScriptablePass);
        
    }
}
