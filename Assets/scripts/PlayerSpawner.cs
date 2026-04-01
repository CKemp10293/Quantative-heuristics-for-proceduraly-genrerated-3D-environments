using System.Collections;
using UnityEngine;
using StarterAssets;

public class PlayerSpawner : MonoBehaviour
{
    private CharacterController characterController;
    private FirstPersonController firstPersonController;

    public float searchRadius = 100f;
    public int maxSearchAttempts = 50;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        firstPersonController = GetComponent<FirstPersonController>();

        StartCoroutine(CalculateLegalSpawn());
    }

    IEnumerator CalculateLegalSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        characterController.enabled = false;
        for (int i = 0; i < maxSearchAttempts; i++)
        {
            float randomX = transform.position.x + Random.Range(-searchRadius,searchRadius);
            float randomZ = transform.position.z + Random.Range(-searchRadius,searchRadius);

            Vector3 rayStartPoint = new Vector3(randomX,1000f,randomZ);

            if (Physics.Raycast(rayStartPoint,Vector3.down,out RaycastHit hit, 2000f))
            {
                bool isDryLand = !firstPersonController.oceanEnabled || hit.point.y > firstPersonController.waterLevel;

                if (isDryLand)
                {
                    transform.position = new Vector3(randomX,hit.point.y + 2f,randomZ);
                    break;
                }
            }
        }
        characterController.enabled = true;
    }
}
