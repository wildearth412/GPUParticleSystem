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
        PerlinNoise = 3
    }
}
