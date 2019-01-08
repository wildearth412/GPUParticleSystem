using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public struct ParticleData
{
    public Vector3 velocity;
    public Vector3 position;
}

public class SimpleGPUParticle : MonoBehaviour
{
    // Power 15 of 2.
    const int NUM_PARTICLES = 32768;

    // Num of threads in thread group.
    const int NUM_THREAD_X = 8;
    const int NUM_THREAD_Y = 1;
    const int NUM_THREAD_Z = 1;

    public ComputeShader simpleParticleComputeShader;
    public Shader simpleParticleShader;

    public Vector3 gravity = new Vector3(0,-1.0f,0);
    public Vector3 areaSize = new Vector3(10.0f,10.0f,10.0f);

    public Texture2D particleTex;
    public float particleSize = 0.05f;

    public Camera renderCam;

    private ComputeBuffer particleBuffer;
    private Material particleMat;

	private void Start ()
    {
        particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleData)));

        var pData = new ParticleData[NUM_PARTICLES];
        for(int i = 0; i < pData.Length; i++)
        {
            pData[i].velocity = Random.insideUnitSphere;
            pData[i].position = Random.insideUnitSphere;
        }
        particleBuffer.SetData(pData);
        pData = null;

        particleMat = new Material(simpleParticleShader);
        particleMat.hideFlags = HideFlags.HideAndDontSave;
	}
	
	private void Update ()
    {
		
	}

    private void OnRenderObject()
    {
        ComputeShader cs = simpleParticleComputeShader;

        // Calculate the num of thread groups.
        int numThreadGroup = NUM_PARTICLES / NUM_THREAD_X;

        // Get the kernel ID.
        int kernelID = cs.FindKernel("CSMain");

        // Set parameters for compute shader.
        cs.SetFloat("_TimeStep",Time.deltaTime);
        cs.SetVector("_Gravity",gravity);
        cs.SetFloats("_AreaSize",new float[3] { areaSize.x,areaSize.y,areaSize.z});

        // Set compute buffer for compute shader.
        cs.SetBuffer(kernelID,"_ParticleBuffer",particleBuffer);

        // Execude compute shader.
        cs.Dispatch(kernelID,numThreadGroup,1,1);

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
        if(particleBuffer != null)
        {
            particleBuffer.Release();
        }

        if(particleMat != null)
        {
            DestroyImmediate(particleMat);
        }
    }
}
