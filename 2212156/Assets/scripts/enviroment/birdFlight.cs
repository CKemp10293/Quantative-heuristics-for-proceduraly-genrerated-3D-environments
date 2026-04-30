using UnityEngine;

/// <summary>
/// Controls the automated flight movement and lifecycle of an atmospheric bird.
/// </summary>
public class birdFlight : MonoBehaviour
{
    public float flySpeed = 15f;
    public float lifeTime = 40f;

    void Start()
    {
        if (TryGetComponent(out AudioSource audioSource))
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f);
        }
        else
        {
            Debug.LogWarning($"[BirdFlight] Missing AudioSource component on {gameObject.name}.");
        }

        // Schedule the object for destruction 
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * (flySpeed * Time.deltaTime));
    }
}
