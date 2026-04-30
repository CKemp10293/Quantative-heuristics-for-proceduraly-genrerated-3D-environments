using System.Collections;
using UnityEngine;
using StarterAssets;

/// <summary>
/// Handles finding a valid, dry-land spawn position for the player upon scene start.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(FirstPersonController))]
public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The radius around the initial position to search for a valid spawn point.")]
    public float searchRadius = 100f;
    
    [Tooltip("Maximum number of raycast attempts to find dry land before giving up.")]
    public int maxSearchAttempts = 50;

    [Tooltip("Height offset applied to the spawn position to prevent clipping into the ground.")]
    public float verticalSpawnOffset = 2f;

    [Header("Raycast Physics")]
    [Tooltip("The layer mask used to detect the ground. Set this to your Terrain layer to avoid spawning on trees or triggers.")]
    public LayerMask groundLayer = ~0; // Default to 'Everything' to preserve original functionality
    private CharacterController characterController;
    private FirstPersonController firstPersonController;

    private readonly WaitForSeconds spawnDelay = new WaitForSeconds(0.5f);

    void Start()
    {
        // Safe fetching: RequireComponent guarantees these are present
        characterController = GetComponent<CharacterController>();
        firstPersonController = GetComponent<FirstPersonController>();

        StartCoroutine(CalculateLegalSpawn());
    }

    /// <summary>   
    /// Waits for the map to generate, then attempts to find a valid spawn point via vertical raycasting.
    /// </summary>
    IEnumerator CalculateLegalSpawn()
    {
        // Wait a brief moment to allow procedural terrain chunks to generate their meshes and colliders
        yield return spawnDelay;

        // Disable CharacterController to safely modify transform.position directly
        characterController.enabled = false;

        bool spawnFound = false;
        Vector3 startPos = transform.position;
        for (int i = 0; i < maxSearchAttempts; i++)
        {
            float randomX = startPos.x + Random.Range(-searchRadius,searchRadius);
            float randomZ = startPos.z + Random.Range(-searchRadius,searchRadius);

            // Start the raycast high above the map
            Vector3 rayStartPoint = new Vector3(randomX,1000f,randomZ);

            // Fire a raycast straight down, utilizing the layer mask
            if (Physics.Raycast(rayStartPoint,Vector3.down,out RaycastHit hit, 2000f,groundLayer))
            {
                // Validate if the hit point is above the water level, or if the ocean is simply disabled
                bool isDryLand = !firstPersonController.oceanEnabled || hit.point.y > firstPersonController.waterLevel;

                if (isDryLand)
                {
                    transform.position = new Vector3(randomX,hit.point.y + verticalSpawnOffset,randomZ);
                    spawnFound = true;
                    break;// Exit the loop early once a valid spawn is found
                }
            }
        }

        // Error handling if all 50 attempts fail
        if (!spawnFound)
        {
            Debug.LogWarning($"[PlayerSpawner] Failed to find a valid dry-land spawn point after {maxSearchAttempts} attempts. Player remains at origin.");
        }

        // Re-enable physics/movement control   
        characterController.enabled = true;
    }
}
