using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AdvancedGPUParticle))]
public class AdvancedGPUParticleEditor : Editor
{
    private bool isInitialized = false;
    private bool foldingList = false;
    private List<bool> foldings = new List<bool>();  // To record the foldouting status.

    //SerializedProperty noiseListProp;

    //private void OnEnable()
    //{
    //    noiseListProp = serializedObject.FindProperty("noiseList");
    //}

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        AdvancedGPUParticle targetInstance = (AdvancedGPUParticle)target;

        EditorGUILayout.LabelField("< Shader >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        targetInstance.particleComputeShader = EditorGUILayout.ObjectField("Compute Shader", targetInstance.particleComputeShader, typeof(ComputeShader), true) as ComputeShader;
        targetInstance.particleShader = EditorGUILayout.ObjectField("Render Shader", targetInstance.particleShader, typeof(Shader), true) as Shader;
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("< Overall >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        targetInstance.gravity = EditorGUILayout.Vector3Field("Gravity", targetInstance.gravity);
        targetInstance.speed = EditorGUILayout.FloatField("Simulation Speed", targetInstance.speed);
        targetInstance.character = EditorGUILayout.ObjectField("Whole Object Transform", targetInstance.character, typeof(Transform), true) as Transform;
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("< Particle Looks >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        targetInstance.renderCam = EditorGUILayout.ObjectField("Render Camera", targetInstance.renderCam, typeof(Camera), true) as Camera;
        targetInstance.particleTex = EditorGUILayout.ObjectField("Particle Texture", targetInstance.particleTex, typeof(Texture2D), true) as Texture2D;
        targetInstance.particleSize = EditorGUILayout.FloatField("Particle Size", targetInstance.particleSize);
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("< Lifespan >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        targetInstance.startMaxLifespan = EditorGUILayout.FloatField("Start Max Lifespan", targetInstance.startMaxLifespan);
        targetInstance.startMinLifespan = EditorGUILayout.FloatField("Start Min Lifespan", targetInstance.startMinLifespan);
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("< Emitter >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        targetInstance.emitter = (GPUParticleSetting.EmitterType)EditorGUILayout.EnumPopup("Emitter Type", targetInstance.emitter);
        targetInstance.emitterSize = EditorGUILayout.FloatField("Emitter Size", targetInstance.emitterSize);
        if (targetInstance.emitter == GPUParticleSetting.EmitterType.Mesh)
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;
            targetInstance.emitterMesh = EditorGUILayout.ObjectField("Emitter Mesh", targetInstance.emitterMesh, typeof(Mesh), true) as Mesh;
            targetInstance.useVertexAnimation = EditorGUILayout.Toggle("Use Vertex Animation", targetInstance.useVertexAnimation);
            if (targetInstance.useVertexAnimation == true)
            {
                targetInstance.vertexPosTex = EditorGUILayout.ObjectField("Vertex Position Texture", targetInstance.vertexPosTex, typeof(Texture2D), true) as Texture2D;
                targetInstance.animeLength = EditorGUILayout.FloatField("Animation Length", targetInstance.animeLength);
                targetInstance.animeTexSizeY = EditorGUILayout.IntField("Animation Texture Height", targetInstance.animeTexSizeY);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("< Noise >", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var list = targetInstance.noises;
        if (!isInitialized)
        {
            foldings = new List<bool>();
            for (int i = 0; i < list.Count; i++)
            {
                bool b = true;
                foldings.Add(b);
            }
        }

        if (foldingList = EditorGUILayout.Foldout(foldingList, "Noise List")) // If the main foldout is folding out.
        {
            isInitialized = true;

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUI.indentLevel++;
                var nData = new NoiseDataClass();
                nData.noiseType = list[i].noiseType;
                nData.noiseAmount = list[i].noiseAmount;
                nData.noiseScale = list[i].noiseScale;
                nData.noiseOffset = list[i].noiseOffset;
                if (foldings[i] = EditorGUILayout.Foldout(foldings[i], "Noise" + (i + 1))) // If the sub foldout is folding out.
                {
                    var t = list[i].noiseType;
                    t = (GPUParticleSetting.NoiseType)EditorGUILayout.EnumPopup("Type", list[i].noiseType);
                    var a = list[i].noiseAmount;
                    a = EditorGUILayout.FloatField("Amount", list[i].noiseAmount);
                    var s = list[i].noiseScale;
                    s = EditorGUILayout.FloatField("Scale", list[i].noiseScale);
                    var o = list[i].noiseOffset;
                    o = EditorGUILayout.Vector3Field("Offset", list[i].noiseOffset);
                    nData.noiseType = t;
                    nData.noiseAmount = a;
                    nData.noiseScale = s;
                    nData.noiseOffset = o;
                }
                list[i] = nData;
                EditorGUI.indentLevel--;
            }

        }
        if (GUILayout.Button("Add Noise"))
        {
            var n = new NoiseDataClass();
            n.noiseType = GPUParticleSetting.NoiseType.None;
            n.noiseAmount = 1.0f;
            n.noiseScale = 1.0f;
            n.noiseOffset = Vector3.zero;
            list.Add(n);
            var b = true;
            foldings.Add(b);
        }

        if (GUILayout.Button("Remove Noise"))
        {
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
            if (foldings.Count > 0)
            {
                foldings.RemoveAt(foldings.Count - 1);
            }
        }
    }

}

