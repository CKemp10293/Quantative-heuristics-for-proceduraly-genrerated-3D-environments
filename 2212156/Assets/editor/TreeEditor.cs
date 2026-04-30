using UnityEditor;
using UnityEngine;
[CustomEditor (typeof (TreePreset))]
public class TreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector first
        if (DrawDefaultInspector())
        {
            // Try to find the MapGenerator
            MapGenerator mapgen = FindFirstObjectByType<MapGenerator>();
            
            // Only attempt to draw if we actually found one in the scene!
            if (mapgen != null)
            {
                mapgen.DrawMapInEditor();
            }
            else
            {
                // Optional: A gentle reminder in the console instead of a red error
                Debug.LogWarning("TreePreset modified, but no MapGenerator was found in the scene to update.");
            }
        }
    }
}
