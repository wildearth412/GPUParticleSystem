using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Noise;


public struct ParticleDataAlpha
{
    public Vector3 velocity;
    public Vector3 position;
    public float lifespan;
    public float age;       // age : 0 ~ 1
}

public class CurlNoiseGPUParticle : MonoBehaviour
{
    // Power 15 of 2.
    private const int NUM_PARTICLES = 32768;

    // Num of threads in thread group.
    private const int NUM_THREAD_X = 8;
    private const int NUM_THREAD_Y = 1;
    private const int NUM_THREAD_Z = 1;

    // Kernal Names.
    private static string INIT_KERNAL_NAME = "CSInit";
    private static string UPDATE_KERNAL_NAME = "CSUpdate";

    // Kernal Buffers/Textures.
    private static string PARTICLE_BUFFER = "_ParticleBuffer";
    private static string VERTEX_POSITION = "_VertexPos";

    // Kernal IDs.
    private int INIT_KERNAL_ID;
    private int UPDATE_KERNAL_ID;

    public ComputeShader particleComputeShader;
    public Shader particleShader;

    public Vector3 gravity = new Vector3(0, 0.1f, 0);
    //public Vector3 areaSize = new Vector3(10.0f, 10.0f, 10.0f);

    public Texture2D particleTex;
    public float particleSize = 0.03f;
    public float startMaxLifespan = 5.0f;
    public float startMinLifespan = 3.0f;

    public GPUParticleSetting.EmitterType emitter = GPUParticleSetting.EmitterType.Plane;
    public float emitterSize = 1.0f;
    public Mesh emitterMesh;
    private Mesh emitterMeshPrev;

    public GPUParticleSetting.NoiseType noise = GPUParticleSetting.NoiseType.None;
    public float noiseAmount = 1.0f;

    public Camera renderCam;

    private ComputeBuffer particleBuffer;
    private Material particleMat;

    private CurlNoise cn = new CurlNoise();

    private Vector3 GetNoise(Vector3 v)
    {
        int ntype = (int)noise;
        Vector3 noiseFactor = Vector3.one;
        switch (ntype)
        {
            case 0:             
                break;
            case 1:
                noiseFactor = cn.GetCurlNoise(v) * noiseAmount;
                break;
            case 2:

                break;
            case 3:

                break;
        }
        return noiseFactor;
    }

    private void Start()
    {
        // Create particleBuffer.
        particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleDataAlpha)));
        // Get the kernel ID.
        UPDATE_KERNAL_ID = particleComputeShader.FindKernel(UPDATE_KERNAL_NAME);

        int emtype = (int)emitter;    
        if(emtype == 2)
        {
            emitterMeshPrev = emitterMesh;
        }
        var pData = new ParticleDataAlpha[NUM_PARTICLES];
        for (int i = 0; i < pData.Length; i++)
        {
            if (emtype == 0)
            {
                pData[i].position = new Vector3((Random.value * 2.0f - 1.0f), 0, (Random.value * 2.0f - 1.0f)) * emitterSize;
                Vector3 n = GetNoise(pData[i].position);
                //n = new Vector3(n.x,Mathf.Abs(n.y),n.z);
                //pData[i].velocity = Vector3.Scale(new Vector3(Random.value, (1.0f - Random.value * 0.5f), Random.value), n);
                pData[i].velocity = new Vector3(Random.value, (1.0f - Random.value * 0.5f), Random.value) + n;
            }
            else if (emtype == 1)
            {
                pData[i].position = Random.insideUnitSphere * emitterSize;
                //pData[i].velocity = Vector3.Scale(Random.insideUnitSphere, GetNoise(pData[i].position));
                pData[i].velocity = Random.insideUnitSphere + GetNoise(pData[i].position);
            }
            else if (emtype == 2)
            {

            }
            
            startMaxLifespan = startMaxLifespan <= 0 ? 0.2f : startMaxLifespan;
            startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;           
            pData[i].lifespan = startMinLifespan + Random.value * (startMaxLifespan - startMinLifespan);
            pData[i].age = 1.0f;
        }
        particleBuffer.SetData(pData);
        pData = null;

        particleMat = new Material(particleShader);
        particleMat.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Update()
    {

    }

    private void OnRenderObject()
    {
        ComputeShader cs = particleComputeShader;

        // Calculate the num of thread groups.
        int numThreadGroup = NUM_PARTICLES / NUM_THREAD_X;

        // Set parameters for compute shader.
        int emtype = (int)emitter;
        cs.SetInt("_EmitterType", emtype);
        cs.SetFloat("_EmitterSize", emitterSize);
        int ntype = (int)noise;
        cs.SetInt("_NoiseType", ntype);
        cs.SetFloat("_NoiseAmount", noiseAmount);
        cs.SetFloat("_TimeStep", Time.deltaTime);
        startMaxLifespan = startMaxLifespan <= 0 ? 0.2f : startMaxLifespan;
        startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;
        cs.SetFloat("_MaxLifeSpan", startMaxLifespan);
        cs.SetFloat("_MinLifeSpan", startMinLifespan);
        cs.SetVector("_Gravity", gravity);
        //cs.SetFloats("_AreaSize", new float[3] { areaSize.x, areaSize.y, areaSize.z });

        // Set compute buffer for compute shader.
        cs.SetBuffer(UPDATE_KERNAL_ID, PARTICLE_BUFFER, particleBuffer);

        // Execude compute shader.
        cs.Dispatch(UPDATE_KERNAL_ID, numThreadGroup, 1, 1);

        // Calculate the inverse view matrix.
        var inverseViewMatrix = renderCam.worldToCameraMatrix.inverse;

        Material m = particleMat;
        // Set parameters for render material.
        m.SetPass(0);
        m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
        m.SetTexture("_MainTex", particleTex);
        m.SetFloat("_ParticleSize", particleSize);

        // Set compute buffer for material.
        m.SetBuffer("_ParticleBuffer", particleBuffer);

        // Render the particles.
        Graphics.DrawProcedural(MeshTopology.Points, NUM_PARTICLES);
    }

    private void OnDestroy()
    {
        ReleaseBuffers();

        if (particleMat != null)
        {
            DestroyImmediate(particleMat);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }            
    }
}
