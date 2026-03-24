using UnityEditor;
using UnityEngine;
[CustomEditor (typeof (BiomePreset))]
public class PresetColourEditor : Editor
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
