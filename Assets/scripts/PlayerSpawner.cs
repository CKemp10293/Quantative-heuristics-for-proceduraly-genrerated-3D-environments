using System.Collections;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        StartCoroutine(CalcLegalSpawn());
    }

    IEnumerator CalcLegalSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        Vector3 rayStartPoint = new Vector3(transform.position.x,1000f,transform.position.z);

        if(Physics.Raycast(rayStartPoint,Vector3.down,out RaycastHit hit, 2000f))
        {
            controller.enabled = false;
            transform.position = new Vector3(transform.position.x,hit.point.y,transform.position.z);
            controller.enabled = true;
        }
        else
        {
            Debug.LogError("NEED TO ADD MESH COLLIDER");
        }
    }
}
