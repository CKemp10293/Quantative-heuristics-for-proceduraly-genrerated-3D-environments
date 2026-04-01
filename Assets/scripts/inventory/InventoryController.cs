using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class InventoryController : MonoBehaviour
{
    [Header("Inventory Data")]
    [Tooltip("Drop your ItemData assets here. 5 slots total.")]
    public ItemData[] inventorySlots = new ItemData[5];

    [Header("UI References")]
    [Tooltip("The 5 physical UI buttons in your Canvas")]
    public Button[] uiSlotButtons = new Button[5];

    public PhotoModeController photoController;

    private void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        for (int i = 0; i < uiSlotButtons.Length; i++)
        {
            int slotIndex = i; // Cache index for the click listener

            // Hook up the mouse click automatically
            uiSlotButtons[i].onClick.AddListener(() => UseItem(slotIndex));

            // Apply the visual icon if an item exists
            Image slotImage = uiSlotButtons[i].transform.GetChild(0).GetComponent<Image>();
            
            if (inventorySlots[i] != null && inventorySlots[i].itemIcon != null)
            {
                slotImage.sprite = inventorySlots[i].itemIcon;
                slotImage.color = Color.white; // Make icon visible
            }
            else
            {
                slotImage.color = Color.clear; // Hide icon if slot is empty
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) UseItem(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) UseItem(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) UseItem(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) UseItem(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) UseItem(4);
    }

    public void UseItem(int index)
    {
        if (index >= inventorySlots.Length || inventorySlots[index] == null)
        {
            Debug.Log($"Slot {index + 1} is empty!");
            return;
        }

        string id = inventorySlots[index].itemID;

        // Route the command to the correct mechanic based on the ID
        switch (id)
        {
            case "Camera":
                if (photoController != null) photoController.TogglePhotoMode();
                break;
            // Future tools go here! 
            // case "Flashlight": flashlightController.Toggle(); break;
            default:
                Debug.LogWarning($"Unknown Item ID: {id}");
                break;
        }
    }
}
