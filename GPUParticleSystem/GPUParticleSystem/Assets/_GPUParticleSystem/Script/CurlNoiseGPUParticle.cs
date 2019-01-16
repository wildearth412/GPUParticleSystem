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
    private static string INIT_KERNEL_NAME = "CSInit";
    private static string UPDATE_KERNEL_NAME = "CSUpdate";

    // Kernal Buffers/Textures.
    private static string PARTICLE_BUFFER = "_ParticleBuffer";
    private static string VERTEX_POSITION_BUFFER = "_VertexPositionBuffer";
    private static string VERTEX_POSITION_TEX = "_VertexPosTex";

    // Kernal IDs.
    private int INIT_KERNEL_ID;
    private int UPDATE_KERNEL_ID;

    public ComputeShader particleComputeShader;
    public Shader particleShader;

    public Vector3 gravity = new Vector3(0, 0.1f, 0);
    public float speed = 1.0f;
    //public Vector3 areaSize = new Vector3(10.0f, 10.0f, 10.0f);

    public Texture2D particleTex;
    public float particleSize = 0.03f;
    public float startMaxLifespan = 5.0f;
    public float startMinLifespan = 3.0f;

    public bool useVertexAnimation = true;
    public Texture2D vertexPosTex;
    public float animeLength;
    public int animeTexSizeY;

    public Transform character;
    private Vector3 characterPosition;
    private Vector3 characterDirection;

    public GPUParticleSetting.EmitterType emitter = GPUParticleSetting.EmitterType.Plane;
    public float emitterSize = 1.0f;   
    public Mesh emitterMesh;
    private Mesh emitterMeshPrev;

    public GPUParticleSetting.NoiseType noise = GPUParticleSetting.NoiseType.None;
    public float noiseAmount = 1.0f;

    public Camera renderCam;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer vertexPositionBuffer;     // For mesh emitter vertex position.
    private ComputeBuffer uvBuffer;                 // For mesh emitter UV.
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
        characterPosition = character.position;
        characterDirection = character.forward;

        SetupComputeShader();

        int emtype = (int)emitter;    
        if(emtype == 2)
        {
            emitterMeshPrev = emitterMesh;
        }

        /*var pData = new ParticleDataAlpha[NUM_PARTICLES];
        for (int i = 0; i < pData.Length; i++)
        {
            if (emtype == 0)  // Plane.
            {
                pData[i].position = new Vector3((Random.value * 2.0f - 1.0f), 0, (Random.value * 2.0f - 1.0f)) * emitterSize;
                Vector3 n = GetNoise(pData[i].position);
                //n = new Vector3(n.x,Mathf.Abs(n.y),n.z);
                //pData[i].velocity = Vector3.Scale(new Vector3(Random.value, (1.0f - Random.value * 0.5f), Random.value), n);
                pData[i].velocity = new Vector3(Random.value, (1.0f - Random.value * 0.5f), Random.value) + n;
            }
            else if (emtype == 1)  // Sphere.
            {
                pData[i].position = Random.insideUnitSphere * emitterSize;
                //pData[i].velocity = Vector3.Scale(Random.insideUnitSphere, GetNoise(pData[i].position));
                pData[i].velocity = Random.insideUnitSphere + GetNoise(pData[i].position);
            }
            else if (emtype == 2)  // Mesh.
            {

            }
            
            startMaxLifespan = startMaxLifespan <= 0 ? 0.2f : startMaxLifespan;
            startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;           
            pData[i].lifespan = startMinLifespan + Random.value * (startMaxLifespan - startMinLifespan);
            pData[i].age = 1.0f;
        }
        particleBuffer.SetData(pData);
        pData = null;*/

        particleMat = new Material(particleShader);
        particleMat.hideFlags = HideFlags.HideAndDontSave;
    }

    private void SetupComputeShader()
    {
        // Create particleBuffer.
        particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleDataAlpha)));
        // Get the kernel ID.
        INIT_KERNEL_ID = particleComputeShader.FindKernel(INIT_KERNEL_NAME);
        UPDATE_KERNEL_ID = particleComputeShader.FindKernel(UPDATE_KERNEL_NAME);

        SetupComputeShaderParameters();
        particleComputeShader.SetFloat("_AnimLength", animeLength);
        particleComputeShader.SetInt("_AnimTexelSizeY", animeTexSizeY);

        if (emitterMesh != null)
        {
            int count = emitterMesh.vertexCount;
            vertexPositionBuffer = VerticesToComputeBuffer(emitterMesh.vertices);
            uvBuffer = UVsToComputeBuffer(emitterMesh.uv);

            particleComputeShader.SetInt("_VertexCount", count);
            particleComputeShader.SetBuffer(INIT_KERNEL_ID, VERTEX_POSITION_BUFFER, vertexPositionBuffer);            
        }

        particleComputeShader.Dispatch(INIT_KERNEL_ID, NUM_PARTICLES / NUM_THREAD_X, 1, 1);
    }

    private void SetupComputeShaderParameters()
    {
        int emtype = (int)emitter;
        particleComputeShader.SetInt("_EmitterType", emtype);
        particleComputeShader.SetFloat("_EmitterSize", emitterSize);
        particleComputeShader.SetBool("_UseVertexAnimation", useVertexAnimation);
        int ntype = (int)noise;
        particleComputeShader.SetInt("_NoiseType", ntype);
        particleComputeShader.SetFloat("_NoiseAmount", noiseAmount);
        particleComputeShader.SetFloat("_Time", Time.timeSinceLevelLoad);
        particleComputeShader.SetFloat("_TimeStep", Time.deltaTime);
        startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;
        startMaxLifespan = startMaxLifespan <= startMinLifespan ? startMinLifespan + 0.1f : startMaxLifespan;
        particleComputeShader.SetFloat("_MaxLifeSpan", startMaxLifespan);
        particleComputeShader.SetFloat("_MinLifeSpan", startMinLifespan);
        particleComputeShader.SetVector("_Gravity", gravity);
        particleComputeShader.SetFloat("_Speed", speed);
        particleComputeShader.SetVector("_CharPosition", characterPosition);
        particleComputeShader.SetVector("_CharDirection", characterDirection);
        //cs.SetFloats("_AreaSize", new float[3] { areaSize.x, areaSize.y, areaSize.z });
    }

    private ComputeBuffer VerticesToComputeBuffer(Vector3[] verts)
    {
        ComputeBuffer posBuff = new ComputeBuffer(verts.Length, Marshal.SizeOf(typeof(Vector3)));
        posBuff.SetData(verts);
        return posBuff;
    }

    private ComputeBuffer UVsToComputeBuffer(Vector2[] uvs)
    {
        ComputeBuffer uvBuff = new ComputeBuffer(uvs.Length,Marshal.SizeOf(typeof(Vector3)));
        uvBuff.SetData(uvs);
        return uvBuff;
    }

    private void Update()
    {
        characterPosition = character.position;
        characterDirection = character.forward;

        if (emitterMesh == null || particleMat == null) { return; }

        // Check if emitter mesh was updated.
        if(emitterMesh != emitterMeshPrev)
        {
            emitterMeshPrev = emitterMesh;
            ReleaseBuffers();
            SetupComputeShader();
        }

        // Set some interactions.
        //if (Input.GetMouseButton(0)) { }
    }

    private void OnRenderObject()
    {
        // Calculate the num of thread groups.
        int numThreadGroup = NUM_PARTICLES / NUM_THREAD_X;

        // Set parameters for compute shader.
        SetupComputeShaderParameters();

        ComputeShader cs = particleComputeShader;

        // Set compute buffer for compute shader.
        cs.SetBuffer(UPDATE_KERNEL_ID, PARTICLE_BUFFER, particleBuffer);
        cs.SetBuffer(UPDATE_KERNEL_ID, VERTEX_POSITION_BUFFER, vertexPositionBuffer);

        // Set vertex position texture for compute shader.
        cs.SetTexture(UPDATE_KERNEL_ID, VERTEX_POSITION_TEX, vertexPosTex);

        // Execude compute shader.
        cs.Dispatch(UPDATE_KERNEL_ID, numThreadGroup, 1, 1);

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

        if (particleMat != null)
        {
            DestroyImmediate(particleMat);
        }
    }

    private void ReleaseBuffers()
    {
        particleBuffer.Release();
        vertexPositionBuffer.Release();
        uvBuffer.Release();          
    }
}
