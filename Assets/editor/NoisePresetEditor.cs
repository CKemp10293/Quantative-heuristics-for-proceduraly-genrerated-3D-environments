using UnityEngine;
using UnityEditor; // CRITICAL: This allows us to modify the interface

// This tag tells Unity: "Use this script to draw the Inspector for NoisePreset files"
[CustomEditor(typeof(NoisePreset))] 
public class NoisePresetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default inspector so you can still see all your variables
        base.OnInspectorGUI();

        // Add a little visual padding
        GUILayout.Space(10);

        // 2. Draw the button and check if it was clicked
        if (GUILayout.Button("Generate Terrain", GUILayout.Height(30)))
        {
            // 3. Find the Map Generator currently sitting in your active scene
            MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
            
            if (mapGen != null)
            {
                // 4. Trigger the generation method you already wrote!
                mapGen.DrawMapInEditor();
            }
            else
            {
                Debug.LogWarning("Could not find a MapGenerator in the scene to trigger!");
            }
        }
    }
}