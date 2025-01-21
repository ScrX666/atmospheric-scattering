using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphericScattering : MonoBehaviour
{
    protected Shader m_shader;
    protected Material m_material;


    public Light Sun;


    [Range(1, 64)]
    public int SampleCount = 16;
    public float MaxRayLength = 400;

    [ColorUsage(false, true, 0, 10, 0, 10)]
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
    // Start is called before the first frame update
    void Start()
    {
        InitPPMaterial();
    }
    void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (m_shader != null)
        {
            SetPPShaderParameters();
            Graphics.Blit(sourceTexture, destTexture, m_material);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
    
    protected virtual void InitPPMaterial()
    {
        m_shader = Shader.Find("Hidden/AtmosphereScattering");
        m_material = new Material(m_shader);
        m_material.hideFlags = HideFlags.HideAndDontSave;
    }

    void SetPPShaderParameters()
    {

        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.current.projectionMatrix, false);

        m_material.SetMatrix("_InverseViewMatrix", Camera.current.worldToCameraMatrix.inverse);
        m_material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);

        m_material.SetFloat("_AtmosphereHeight", AtmosphereHeight);
        m_material.SetFloat("_PlanetRadius", PlanetRadius);
        m_material.SetVector("_DensityScaleHeight", DensityScale);

        Vector4 scatteringR = new Vector4(5.8f, 13.5f, 33.1f, 0.0f) * 0.000001f;
        Vector4 scatteringM = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;

        m_material.SetVector("_ScatteringR", RayleighSct * RayleighScatterCoef);
        m_material.SetVector("_ScatteringM", MieSct * MieScatterCoef);
        m_material.SetVector("_ExtinctionR", RayleighSct * RayleighExtinctionCoef);
        m_material.SetVector("_ExtinctionM", MieSct * MieExtinctionCoef);

        m_material.SetColor("_IncomingLight", IncomingLight);
        m_material.SetFloat("_MieG", MieG);
        m_material.SetFloat("_DistanceScale", DistanceScale);
        m_material.SetColor("_SunColor", _sunColor);

        //---------------------------------------------------

        m_material.SetVector("_LightDir", new Vector4(Sun.transform.forward.x, Sun.transform.forward.y, Sun.transform.forward.z, 1.0f / (Sun.range * Sun.range)));
        m_material.SetVector("_LightColor", Sun.color * Sun.intensity);
    }
}
