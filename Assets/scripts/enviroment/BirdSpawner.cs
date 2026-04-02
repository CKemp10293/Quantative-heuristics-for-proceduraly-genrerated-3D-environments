using UnityEngine;

/// <summary>
/// Periodically spawns bird prefabs in the sky around the player to enhance atmospheric depth.
/// </summary>
public class BirdSpawner : MonoBehaviour
{
    public GameObject currentBirdPrefab;
    [HideInInspector] public float spawnRate = 5f;

    private float timeSinceLastSpawn = 0f;

    // Extracted magic numbers into constants for easier tuning
    private const float SpawnRadius = 100f;
    private const float SpawnHeightOffset = 70f;

    void Update()
    {
        // Fail-safe: Do nothing if the environment hasn't assigned a bird prefab yet
        if (currentBirdPrefab == null) return;

        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnRate)
        {
            SpawnBird();
            timeSinceLastSpawn = 0f;
        }
    }

    /// <summary>
    /// Calculates a random sky position and rotation, then instantiates the bird.
    /// </summary>
    void SpawnBird()
    {
        // Calculate a random offset within a 200x200 square, elevated into the sky
        Vector3 randomOffset = new Vector3(
            Random.Range(-SpawnRadius, SpawnRadius),
            SpawnHeightOffset,
            Random.Range(-SpawnRadius, SpawnRadius)
        );

        Vector3 spawnPosition = transform.position + randomOffset;

        // Randomize yaw (Y-axis) rotation for varied flight paths
        Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Instantiate(currentBirdPrefab, spawnPosition, randomRotation);
    }
}