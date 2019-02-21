using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using FluidSim3DProject;
//using Noise;


public struct ParticleDataAlpha
{
    public Vector3 velocity;
    public Vector3 position;
    public float lifespan;
    public float age;       // age : 0 ~ 1
}

public struct NoiseData
{
    public GPUParticleSetting.NoiseType noiseType;
    public float noiseAmount;
    public float noiseScale;
    public Vector3 noiseOffset;
}

[System.Serializable]
public class NoiseDataClass
{
    public GPUParticleSetting.NoiseType noiseType;
    public float noiseAmount;
    public float noiseScale;
    public Vector3 noiseOffset;
}

public class AdvancedGPUParticle : MonoBehaviour
{
    // Power 18 of 2.                    18       17         16        15
    private const int NUM_PARTICLES = 262144; //131072; //65536; //32768;

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
    private static string TRIANGLES_BUFFER = "_TrianglesBuffer";
    private static string TRIANGLE_INDICES_BUFFER = "_TriangleIndicesBuffer";
    private static string VERTEX_POSITION_TEX = "_VertexPosTex";
    private static string FLUID_VELOCITY_BUFFER = "_FluidVelocityBuffer";
    private static string NOISE_BUFFER = "_NoiseBuffer";

    // Kernal IDs.
    private int INIT_KERNEL_ID;
    private int UPDATE_KERNEL_ID;

    public ComputeShader particleComputeShader;
    public Shader particleShader;

    public Vector3 gravity = new Vector3(0, 0.8f, -1.0f);
    public float drag = 0;            // Air resistance.
    public float vortcity = 1.0f;     // Fluid vortcity.
    public bool useFluidVelocity = false;   // If apply fluid velocity field.
    public float fluidWeight = 0.3f;
    public Vector3 fluidOffset = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 fluidDirection = new Vector3(0.0f, 0.0f, 1.0f);
    public Vector3 fluidSize = new Vector3(3.0f,6.0f,3.0f);
    public float speed = 0.1f;
    //public Vector3 areaSize = new Vector3(10.0f, 10.0f, 10.0f);

    public SmokeFluidSim fluidSim;

    public Texture2D particleTex;
    public float particleSize = 0.002f;
    public float startMaxLifespan = 3.0f;
    public float startMinLifespan = 0.5f;

    public bool useVertexAnimation = true;
    public Texture2D vertexPosTex;
    public float animeLength;
    public int animeTexSizeY;

    public Transform character;
    public Vector3 characterOffset = new Vector3(0,-0.1f,-0.05f);
    private Vector3 characterPosition;
    private Vector3 characterDirection;

    public GPUParticleSetting.EmitterType emitter = GPUParticleSetting.EmitterType.Mesh;
    public float emitterSize = 1.0f;   
    public Mesh emitterMesh;
    private Mesh emitterMeshPrev;

    public bool bakeInRealtime = false;
    public SkinnedMeshRenderer smr;

    //public GPUParticleSetting.NoiseType noise = GPUParticleSetting.NoiseType.None;
    //public float noiseAmount = 1.0f;
    //public float noiseScale = 1.0f;
    //public Vector3 noiseOffset = Vector3.zero;

    public List<NoiseDataClass> noises = new List<NoiseDataClass>();
    private NoiseData[] noisesData;
    //public List<NoiseData> noiseList = new List<NoiseData>();

    public Camera renderCam;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer vertexPositionBuffer;     // For mesh emitter vertex position.
    private ComputeBuffer trianglesBuffer;          // For mesh emitter triangles.
    private ComputeBuffer triangleIndicesBuffer;    // For mesh emitter triangle indices.
    private ComputeBuffer uvBuffer;                 // For mesh emitter UV.
    private ComputeBuffer noiseBuffer;
    private Material particleMat;

    //private Vector3[] debugPos = new Vector3[3000];

    private void Start()
    {
        characterPosition = character.position + characterOffset;
        //characterPosition = characterOffset;
        characterDirection = character.forward + characterOffset;

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

        // Initialize vertexPositionBuffer.
        vertexPositionBuffer = new ComputeBuffer(emitterMesh.vertexCount, Marshal.SizeOf(typeof(Vector3)));

        // Max 10 noise buffers created.
        noiseBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(NoiseData)));

        SetupComputeShaderParameters();
        particleComputeShader.SetFloat("_AnimLength", animeLength);
        particleComputeShader.SetInt("_AnimTexelSizeY", animeTexSizeY);
        particleComputeShader.SetBuffer(INIT_KERNEL_ID, NOISE_BUFFER, noiseBuffer);
        //noiseBuffer.Release();

        if (emitterMesh != null)
        {
            int count = emitterMesh.vertexCount;
            int count2 = 0;

            if(!bakeInRealtime)
            {
                VerticesToComputeBuffer(emitterMesh.vertices);
            }
            else
            {
                VerticesToComputeBuffer();
            }
            
            trianglesBuffer = TrianglesToComputeBuffer(emitterMesh.triangles);
            triangleIndicesBuffer = TriangleIndicesToComputeBuffer(emitterMesh.vertices, emitterMesh.triangles, out count2);
            uvBuffer = UVsToComputeBuffer(emitterMesh.uv);

            particleComputeShader.SetInt("_VertexCount", count);
            particleComputeShader.SetInt("_TriangleIndicesCount", count2);
            particleComputeShader.SetBuffer(INIT_KERNEL_ID, VERTEX_POSITION_BUFFER, vertexPositionBuffer);
            particleComputeShader.SetBuffer(INIT_KERNEL_ID, TRIANGLES_BUFFER, trianglesBuffer);
            particleComputeShader.SetBuffer(INIT_KERNEL_ID, TRIANGLE_INDICES_BUFFER, triangleIndicesBuffer);
        }

        particleComputeShader.Dispatch(INIT_KERNEL_ID, NUM_PARTICLES / NUM_THREAD_X, 1, 1);
    }

    private void SetupComputeShaderParameters()
    {
        int emtype = (int)emitter;
        particleComputeShader.SetInt("_EmitterType", emtype);
        particleComputeShader.SetFloat("_EmitterSize", emitterSize);
        particleComputeShader.SetBool("_UseVertexAnimation", useVertexAnimation);

        if(noises.Count > 0)
        {
            NoiseToComputeBuffer(noises);
            int nn = noises.Count > 10 ? 10 : noises.Count;
            particleComputeShader.SetInt("_NoiseCount",nn);
        }
        
        particleComputeShader.SetFloat("_Time", Time.timeSinceLevelLoad);
        particleComputeShader.SetFloat("_TimeStep", Time.deltaTime);
        startMinLifespan = startMinLifespan <= 0 ? 0.1f : startMinLifespan;
        startMaxLifespan = startMaxLifespan <= startMinLifespan ? startMinLifespan + 0.1f : startMaxLifespan;
        particleComputeShader.SetFloat("_MaxLifeSpan", startMaxLifespan);
        particleComputeShader.SetFloat("_MinLifeSpan", startMinLifespan);
        particleComputeShader.SetVector("_Gravity", gravity);
        particleComputeShader.SetFloat("_Drag",drag);
        particleComputeShader.SetFloat("_Vorticity", vortcity);
        particleComputeShader.SetBool("_UseFluidVelocity", useFluidVelocity);
        particleComputeShader.SetFloat("_FluidWeight", fluidWeight);
        particleComputeShader.SetVector("_FluidOffset", fluidOffset);
        particleComputeShader.SetVector("_FluidDirection", fluidDirection);
        particleComputeShader.SetVector("_FluidSize", fluidSize);
        particleComputeShader.SetFloat("_Speed", speed);
        particleComputeShader.SetVector("_CharPosition", characterPosition);
        particleComputeShader.SetVector("_CharDirection", characterDirection);
        //cs.SetFloats("_AreaSize", new float[3] { areaSize.x, areaSize.y, areaSize.z });
    }

    private void VerticesToComputeBuffer()
    {
        Mesh me = new Mesh();
        smr.BakeMesh(me);
        Vector3[] verts = me.vertices;
        vertexPositionBuffer.SetData(verts);
        verts = null;
        me = null;
    }

    private void VerticesToComputeBuffer(Vector3[] verts)
    {
        var vData = verts;
        vertexPositionBuffer.SetData(vData);
        vData = null;
    }

    private ComputeBuffer TrianglesToComputeBuffer(int[] tris)
    {
        ComputeBuffer triBuff = new ComputeBuffer(tris.Length, Marshal.SizeOf(typeof(int)));
        triBuff.SetData(tris);
        return triBuff;
    }

    private ComputeBuffer TriangleIndicesToComputeBuffer(Vector3[] verts, int[] tris, out int n)
    {
        float[] areas = new float[tris.Length/3];       
        for(int i = 0; i < areas.Length; i++)
        {
            // Use the Heron's formula to calculate the area of triangle.
            int ii = i * 3;
            float a = Vector3.Distance(verts[tris[ii]], verts[tris[ii + 1]]);
            float b = Vector3.Distance(verts[tris[ii]], verts[tris[ii + 2]]);
            float c = Vector3.Distance(verts[tris[ii + 1]], verts[tris[ii + 2]]);
            float s = (a + b + c) / 2.0f;
            areas[i] = Mathf.Sqrt(s*(s-a)*(s-b)*(s-c));
        }
        float mina = areas[0];
        for (int i = 1; i < areas.Length; i++)
        {
            if(areas[i] < mina)
            {
                mina = areas[i];
            }
        }
        mina *= 10.0f;
        int[] nums = new int[areas.Length];
        for(int j = 0; j < nums.Length; j++)
        {
            nums[j] = Mathf.CeilToInt(areas[j] / mina);
        }
        List<int> trids = new List<int>();
        for (int k = 0; k < nums.Length; k++)
        {
            for (int l = 0; l < nums[k]; l++)
            {
                int num = k;
                trids.Add(num);
            }
        }

        int[] tridsa;
        tridsa = trids.ToArray();
        n = tridsa.Length;
        
        ComputeBuffer tridBuff = new ComputeBuffer(tridsa.Length, Marshal.SizeOf(typeof(int)));
        tridBuff.SetData(tridsa);

        // For sampling debug.
        //debugPos = SamplingDebug(verts,tris, tridsa);

        return tridBuff;
    }

    private ComputeBuffer UVsToComputeBuffer(Vector2[] uvs)
    {
        ComputeBuffer uvBuff = new ComputeBuffer(uvs.Length,Marshal.SizeOf(typeof(Vector3)));
        uvBuff.SetData(uvs);
        return uvBuff;
    }

    private void NoiseToComputeBuffer(List<NoiseDataClass> list)
    {
        var nData = new NoiseData[list.Count];
        for (int i = 0; i < nData.Length; i++)
        {
            if (i > 9) { break; }
            nData[i].noiseType = noises[i].noiseType;
            nData[i].noiseAmount = noises[i].noiseAmount;
            nData[i].noiseScale = noises[i].noiseScale;
            nData[i].noiseOffset = noises[i].noiseOffset;
        }
        noiseBuffer.SetData(nData);
        nData = null;
        //return nBuffer;
    }

    private void Update()
    {
        characterPosition = character.position + characterOffset;
        characterDirection = character.forward + characterOffset;

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

        if (!bakeInRealtime)
        {
            VerticesToComputeBuffer(emitterMesh.vertices);
        }
        else
        {
            VerticesToComputeBuffer();
        }

        cs.SetBuffer(UPDATE_KERNEL_ID, VERTEX_POSITION_BUFFER, vertexPositionBuffer);

        if(useFluidVelocity)
        {
            cs.SetBuffer(UPDATE_KERNEL_ID, FLUID_VELOCITY_BUFFER, fluidSim.m_velocity[0]);
        }

        cs.SetBuffer(UPDATE_KERNEL_ID, TRIANGLES_BUFFER, trianglesBuffer);
        cs.SetBuffer(UPDATE_KERNEL_ID, TRIANGLE_INDICES_BUFFER, triangleIndicesBuffer);
        cs.SetBuffer(UPDATE_KERNEL_ID, NOISE_BUFFER, noiseBuffer);
        //noiseBuffer.Release();

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
        trianglesBuffer.Release();
        triangleIndicesBuffer.Release();
        uvBuffer.Release();
        noiseBuffer.Release();
    }

    private Vector3[] SamplingDebug(Vector3[] verts, int[] tris, int[] idcs)
    {
        Vector3[] pos = new Vector3[3000];
        int count = idcs.Length;
        for (int i = 0; i < 3000; i++)
        {           
            int r = Mathf.FloorToInt( count * Random.value );
            r = idcs[r];
            Vector3 p1 = verts[tris[r * 3]];
            Vector3 p2 = verts[tris[r * 3 + 1]];
            Vector3 p3 = verts[tris[r * 3 + 2]];

            float u = Random.value;
            float v = Random.value * (1.0f - u);
            float w = 1.0f - (u + v);
            Vector3 rp = u * p1 + v * p2 + w * p3;

            pos[i] = rp;
        }
        return pos;
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Vector3 size = Vector3.one * 0.009f;
    //    for (int i = 0; i < debugPos.Length; i++)
    //    {
    //        Gizmos.DrawCube(debugPos[i],size);
    //    }
    //}
}
