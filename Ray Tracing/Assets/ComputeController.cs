using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets
{
    [CustomEditor(typeof(ComputeProgram))]
    class ComputeController : Editor
    {
        public override void OnInspectorGUI()
        {
            ComputeProgram program = target as ComputeProgram;
            GUILayout.Label("Raytracing Controller");

            if (GUILayout.Button("Regenerate"))
                program.SetUpScene();

            program.reflectionCount = EditorGUILayout.IntSlider("Reflection Count", program.reflectionCount, 1, 8);
            program.SphereCount = EditorGUILayout.IntSlider("Sphere Count", program.SphereCount / 100, 1, 4) * 100;
        }
    }
}