using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GPUParticleSetting
{
    public enum EmitterType : int
    {
        Plane = 0,
        Sphere = 1,
        Mesh = 2
    }

    public enum NoiseType : int
    {
        None = 0,
        CurlNoise = 1,
        SimplexNoise = 2,
        ClassicPerlinNoise = 3,
        PeriodicPerlinNoise = 4
    }

    public enum TotalParticlesNum : int
    {
        Pow7Of2 = 128,
        Pow8Of2 = 256,
        Pow9Of2 = 512,
        Pow10Of2 = 1024,
        Pow11Of2 = 2048,
        Pow12Of2 = 4096,
        Pow13Of2 = 8192,
        Pow14Of2 = 16384,
        Pow15Of2 = 32768,
        Pow16Of2 = 65536,
        Pow17Of2 = 131072,
        Pow18Of2 = 262144
    }
}
