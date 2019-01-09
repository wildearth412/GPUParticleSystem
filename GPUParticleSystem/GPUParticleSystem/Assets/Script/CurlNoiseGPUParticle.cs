using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public enum EmitterType : int
{
    Plane = 0,
    Sphere = 1,
    Box = 2,
    Mesh = 3
}

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
    const int NUM_PARTICLES = 32768;

    // Num of threads in thread group.
    const int NUM_THREAD_X = 8;
    const int NUM_THREAD_Y = 1;
    const int NUM_THREAD_Z = 1;

    public ComputeShader particleComputeShader;
    public Shader particleShader;

    public Vector3 gravity = new Vector3(0, 0.1f, 0);
    //public Vector3 areaSize = new Vector3(10.0f, 10.0f, 10.0f);

    public Texture2D particleTex;
    public float particleSize = 0.03f;
    public float startMaxLifespan = 5.0f;
    public float startMinLifespan = 3.0f;

    public EmitterType emitter = EmitterType.Plane;
    public float emitterSize = 1.0f;

    public Camera renderCam;

    private ComputeBuffer particleBuffer;
    private Material particleMat;


    private void Start()
    {
        particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleDataAlpha)));

        int type = (int)emitter;
        var pData = new ParticleDataAlpha[NUM_PARTICLES];
        for (int i = 0; i < pData.Length; i++)
        {
            if (type == 0)
            {
                pData[i].velocity = new Vector3(0, Random.value, 0);
                pData[i].position = new Vector3((Random.value * 2.0f - 1.0f), 0, (Random.value * 2.0f - 1.0f)) * emitterSize;
            }
            else if (type == 1)
            {
                pData[i].velocity = Random.insideUnitSphere;
                pData[i].position = Random.insideUnitSphere * emitterSize;
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

        // Get the kernel ID.
        int kernelID = cs.FindKernel("CSMain");

        // Set parameters for compute shader.
        int type = (int)emitter;
        cs.SetInt("_EmitterType", type);
        cs.SetFloat("_EmitterSize", emitterSize);
        cs.SetFloat("_TimeStep", Time.deltaTime);
        startMaxLifespan = startMaxLifespan <= 0 ? 0.2f : startMaxLifespan;
        startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;
        cs.SetFloat("_MaxLifeSpan", startMaxLifespan);
        cs.SetFloat("_MinLifeSpan", startMinLifespan);
        cs.SetVector("_Gravity", gravity);
        //cs.SetFloats("_AreaSize", new float[3] { areaSize.x, areaSize.y, areaSize.z });

        // Set compute buffer for compute shader.
        cs.SetBuffer(kernelID, "_ParticleBuffer", particleBuffer);

        // Execude compute shader.
        cs.Dispatch(kernelID, numThreadGroup, 1, 1);

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
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }

        if (particleMat != null)
        {
            DestroyImmediate(particleMat);
        }
    }
}
