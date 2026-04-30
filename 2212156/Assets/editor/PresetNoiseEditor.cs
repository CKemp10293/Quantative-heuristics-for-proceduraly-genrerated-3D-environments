using UnityEditor;
using UnityEngine;
[CustomEditor (typeof (NoisePreset))]
public class PresetNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapgen = FindFirstObjectByType<MapGenerator>();
        if (DrawDefaultInspector())
        {
            mapgen.DrawMapInEditor();
        }

    }
}
