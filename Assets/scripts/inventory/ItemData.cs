using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Adventure/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("The ID used by the code to trigger the mechanic (e.g., 'Camera', 'Flashlight')")]
    public string itemID;
    
    [Tooltip("The UI Icon displayed in the inventory bar")]
    public Sprite itemIcon;
}
