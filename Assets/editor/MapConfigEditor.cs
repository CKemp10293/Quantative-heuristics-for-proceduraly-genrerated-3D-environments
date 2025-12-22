using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.TerrainTools;

[CustomEditor(typeof(MapGenerator))]
public class MapConfigEditor : Editor
{
    public override void OnInspectorGUI(){
        
        MapGenerator mapGen = (MapGenerator)target;

        EditorGUI.BeginChangeCheck();

        if (mapGen.availablePresets != null && mapGen.availablePresets.Count > 0)
        {
            // Create a list of names from your configs
            string[] options = mapGen.availablePresets
                .Select((x, i) => x != null ? x.configName : $"Element {i}")
                .ToArray();

            // Draw the Popup (Dropdown)
            // It uses the 'activePresetIndex' to know which one is selected
            mapGen.activePresetIndex = EditorGUILayout.Popup("Active Preset", mapGen.activePresetIndex, options);
        }
        else
        {
            EditorGUILayout.HelpBox("Add Map Configs to the 'Available Presets' list to see the dropdown!", MessageType.Info);
        }

        if (EditorGUI.EndChangeCheck())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap();
            }
        }

        // Generate Button
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap();
        }

    }
    


}