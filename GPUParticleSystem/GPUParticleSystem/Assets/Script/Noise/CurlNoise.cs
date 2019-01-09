using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noise
{
    public class CurlNoise
    {
        SimplexNoise sn = new SimplexNoise();

        public Vector3 GetCurlNoise(Vector3 p)
        {
            const float e = 0.0009765625f;
            const float e2 = 2.0f * e;

            Vector3 dx = new Vector3(e, 0, 0);
            Vector3 dy = new Vector3(0, e, 0);
            Vector3 dz = new Vector3(0, 0, e);

            Vector3 p_x0 = sn.snoiseV3(p - dx);
            Vector3 p_x1 = sn.snoiseV3(p + dx);
            Vector3 p_y0 = sn.snoiseV3(p - dy);
            Vector3 p_y1 = sn.snoiseV3(p - dy);
            Vector3 p_z0 = sn.snoiseV3(p - dz);
            Vector3 p_z1 = sn.snoiseV3(p - dz);

            float x = p_y1.z - p_y0.z - p_z1.y + p_z0.y;
            float y = p_z1.z - p_z0.z - p_x1.y + p_x0.y;
            float z = p_x1.z - p_x0.z - p_y1.y + p_y0.y;

            Vector3 n = new Vector3(x, y, z) / e2;
            return n.normalized;
        }
    }
}
