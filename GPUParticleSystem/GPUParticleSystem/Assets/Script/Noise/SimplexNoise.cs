#region description
//
// Noise Shader Library for Unity - https://github.com/keijiro/NoiseShader
//
// Original work (webgl-noise) Copyright (C) 2011 Ashima Arts.
// Translation and modification was made by Keijiro Takahashi.
//
// This shader is based on the webgl-noise GLSL shader. For further details
// of the original shader, please see the following description from the
// original source code.
//

//
// Description : Array and textureless GLSL 2D/3D/4D simplex
//               noise functions.
//      Author : Ian McEwan, Ashima Arts.
//  Maintainer : ijm
//     Lastmod : 20110822 (ijm)
//     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//               Distributed under the MIT License. See LICENSE file.
//               https://github.com/ashima/webgl-noise
//
#endregion

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noise
{
    public class SimplexNoise
    {

        #region custom method

        Vector3 Floor(Vector3 v)
        {
            return new Vector3(Mathf.Floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z));
        }

        Vector4 Floor(Vector4 v)
        {
            return new Vector4(Mathf.Floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z), Mathf.Floor(v.w));
        }

        Vector3 AddV3F(Vector3 v, float f)
        {
            return new Vector3(v.x + f, v.y + f, v.z + f);
        }

        Vector4 AddV4F(Vector4 v, float f)
        {
            return new Vector4(v.x + f, v.y + f, v.z + f, v.w + f);
        }

        Vector3 MultV3(Vector3 v1, Vector3 v2)
        {
            return Vector3.Scale(v1, v2);
        }

        Vector4 MultV4(Vector4 v1, Vector4 v2)
        {
            return Vector4.Scale(v1, v2);
        }

        Vector3 EleV3(Vector3 v, int e1, int e2, int e3)
        {
            Vector3 result = v;
            switch (e1)
            {
                case 0:
                    result.x = v.x;
                    break;
                case 1:
                    result.x = v.y;
                    break;
                case 2:
                    result.x = v.z;
                    break;
            }
            switch (e2)
            {
                case 0:
                    result.y = v.x;
                    break;
                case 1:
                    result.y = v.y;
                    break;
                case 2:
                    result.y = v.z;
                    break;
            }
            switch (e3)
            {
                case 0:
                    result.z = v.x;
                    break;
                case 1:
                    result.z = v.y;
                    break;
                case 2:
                    result.z = v.z;
                    break;
            }
            return result;
        }

        Vector4 EleV4(Vector4 v, int e1, int e2, int e3, int e4)
        {
            Vector4 result = v;
            switch (e1)
            {
                case 0:
                    result.x = v.x;
                    break;
                case 1:
                    result.x = v.y;
                    break;
                case 2:
                    result.x = v.z;
                    break;
                case 3:
                    result.x = v.w;
                    break;
            }
            switch (e2)
            {
                case 0:
                    result.y = v.x;
                    break;
                case 1:
                    result.y = v.y;
                    break;
                case 2:
                    result.y = v.z;
                    break;
                case 3:
                    result.y = v.w;
                    break;
            }
            switch (e3)
            {
                case 0:
                    result.z = v.x;
                    break;
                case 1:
                    result.z = v.y;
                    break;
                case 2:
                    result.z = v.z;
                    break;
                case 3:
                    result.z = v.w;
                    break;
            }
            switch (e4)
            {
                case 0:
                    result.w = v.x;
                    break;
                case 1:
                    result.w = v.y;
                    break;
                case 2:
                    result.w = v.z;
                    break;
                case 3:
                    result.w = v.w;
                    break;
            }
            return result;
        }

        Vector3 StepV3(Vector3 a, Vector3 x)
        {
            return new Vector3(x.x < a.x ? 0 : 1, x.y < a.y ? 0 : 1, x.z < a.z ? 0 : 1);
        }

        Vector4 StepV4(Vector4 a, Vector4 x)
        {
            return new Vector4(x.x < a.x ? 0 : 1, x.y < a.y ? 0 : 1, x.z < a.z ? 0 : 1, x.w < a.w ? 0 : 1);
        }

        Vector3 AbsV3(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        Vector4 AbsV4(Vector4 v)
        {
            return new Vector4(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z), Mathf.Abs(v.w));
        }

        #endregion

        Vector3 mod289(Vector3 v)
        {
            return v - Floor((v / 289.0f) * 289.0f);
        }

        Vector4 mod289(Vector4 v)
        {
            return v - Floor((v / 289.0f) * 289.0f);
        }

        Vector4 permute(Vector4 v)
        {
            return MultV4((v * 34.0f + Vector4.one), v);
        }

        Vector4 taylorInvSqrt(Vector4 r)
        {
            return new Vector4(1.79284291400159f, 1.79284291400159f, 1.79284291400159f, 1.79284291400159f) - r * 0.85373472095314f;
        }

        public float snoise(Vector3 v)
        {
            Vector2 C = new Vector2(1.0f / 6.0f, 1.0f / 3.0f);

            //First Corner
            Vector3 i = Floor(AddV3F(v, Vector3.Dot(v, EleV3(C, 1, 1, 1))));
            Vector3 x0 = v - AddV3F(v, Vector3.Dot(i, EleV3(C, 0, 0, 0)));

            //Other Conner
            Vector3 g = StepV3(EleV3(x0, 1, 2, 0), EleV3(x0, 0, 1, 2));
            Vector3 l = Vector3.one - g;
            Vector3 i1 = Vector3.Min(EleV3(g, 0, 1, 2), EleV3(l, 2, 0, 1));
            Vector3 i2 = Vector3.Max(EleV3(g, 0, 1, 2), EleV3(l, 2, 0, 1));

            // x1 = x0 - i1  + 1.0 * C.xxx;
            // x2 = x0 - i2  + 2.0 * C.xxx;
            // x3 = x0 - 1.0 + 3.0 * C.xxx;
            Vector3 x1 = x0 - i1 + EleV3(C, 0, 0, 0);
            Vector3 x2 = x0 - i2 + EleV3(C, 1, 1, 1);
            Vector3 x3 = AddV3F(x0, 0.5f);

            //Permutations
            i = mod289(i);  // Avoid truncation effects in permutation
            Vector4 p =
                permute(permute(permute(AddV4F(new Vector4(0, i1.z, i2.z, 1.0f), i.z))
                                        + AddV4F(new Vector4(0, i1.z, i2.z, 1.0f), i.y))
                                        + AddV4F(new Vector4(0, i1.z, i2.z, 1.0f), i.y));

            // Gradients: 7x7 points over a square, mapped onto an octahedron.
            // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
            Vector4 j = p - Floor(p / 49.0f) * 49.0f;  // mod(p,7*7)

            Vector4 x_ = Floor(j / 7.0f);
            Vector4 y_ = Floor(j - 7.0f * x_);  // mod(j,N)

            Vector4 x = AddV4F(AddV4F(x_ * 2.0f, 0.5f) / 7.0f, -1.0f);
            Vector4 y = AddV4F(AddV4F(y_ * 2.0f, 0.5f) / 7.0f, -1.0f);

            Vector4 h = Vector4.one - AbsV4(x) - AbsV4(y);

            Vector4 b0 = new Vector4(x.x, x.y, y.x, y.y);
            Vector4 b1 = new Vector4(x.z, x.w, y.z, y.w);

            //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
            //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
            Vector4 s0 = Floor(b0) * 2.0f + Vector4.one;
            Vector4 s1 = Floor(b1) * 2.0f + Vector4.one;
            Vector4 sh = -StepV4(h, Vector4.zero);

            Vector4 a0 = EleV4(b0, 0, 2, 1, 3) + MultV4(EleV4(s0, 0, 2, 1, 3), EleV4(sh, 0, 0, 1, 1));
            Vector4 a1 = EleV4(b1, 0, 2, 1, 3) + MultV4(EleV4(s1, 0, 2, 1, 3), EleV4(sh, 2, 2, 3, 3));

            Vector3 g0 = new Vector3(a0.x, a0.y, h.x);
            Vector3 g1 = new Vector3(a0.z, a0.w, h.y);
            Vector3 g2 = new Vector3(a1.x, a1.y, h.z);
            Vector3 g3 = new Vector3(a1.z, a1.w, h.w);

            // Normalise gradients
            Vector4 norm = taylorInvSqrt(new Vector4(Vector4.Dot(g0, g0), Vector4.Dot(g1, g1), Vector4.Dot(g2, g2), Vector4.Dot(g3, g3)));
            g0 *= norm.x;
            g1 *= norm.y;
            g2 *= norm.z;
            g3 *= norm.w;

            // Mix final noise value
            Vector4 d = new Vector4(Vector3.Dot(x0, x0), Vector3.Dot(x1, x1), Vector3.Dot(x2, x2), Vector3.Dot(x3, x3));
            Vector4 m = Vector4.Max(Vector4.one * 0.6f - d, Vector4.zero);
            m = MultV4(m, m);
            m = MultV4(m, m);

            Vector4 px = new Vector4(Vector3.Dot(x0, g0), Vector3.Dot(x1, g1), Vector3.Dot(x2, g2), Vector3.Dot(x3, g3));
            return 42.0f * Vector4.Dot(m, px);
        }

        public Vector3 snoiseV3(Vector3 v)
        {
            float s = snoise(v);
            float s1 = snoise(new Vector3(v.y + Random.value, v.z + Random.value, v.x + Random.value));
            float s2 = snoise(new Vector3(v.z + Random.value, v.x + Random.value, v.y + Random.value));
            return new Vector3(s, s1, s2);
        }
    }
}
