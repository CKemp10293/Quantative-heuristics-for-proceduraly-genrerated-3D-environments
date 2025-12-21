using UnityEditor;
using UnityEngine;
[CustomEditor (typeof (TreePreset))]
public class TreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapgen = FindFirstObjectByType<MapGenerator>();
        if (DrawDefaultInspector())
        {
            mapgen.GenerateMap();
        }

    }
}
