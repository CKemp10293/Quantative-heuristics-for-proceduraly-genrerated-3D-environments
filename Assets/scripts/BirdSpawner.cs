using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    [HideInInspector] public GameObject currentBirdPrefab;
    [HideInInspector] public float spawnRate = 5f;

    public float timer = 0f;

    void Update()
    {
        if(currentBirdPrefab == null) return;

        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            SpawnBird();

            timer = 0f;
        }
    }

    void SpawnBird()
    {
        Vector3 randomOffset = new Vector3(Random.Range(-100f,100f),70f,Random.Range(-100f,100f));

        Vector3 spawnPostion = transform.position + randomOffset;

        Quaternion randomRotation = Quaternion.Euler(0f,Random.Range(0f,360f),0f);

        Instantiate(currentBirdPrefab,spawnPostion,randomRotation);
    }
}