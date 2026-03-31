using UnityEngine;

public class birdFlight : MonoBehaviour
{
    public float flySpeed = 15f;
    public float lifeTime = 40f;

    void Start()
    {
        GetComponent<AudioSource>().pitch = Random.Range(0.8f, 1.2f);
        Destroy(gameObject,lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.one * flySpeed * Time.deltaTime);
    }
}
