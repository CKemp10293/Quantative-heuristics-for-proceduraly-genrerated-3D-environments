using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TriggerColliderBubble : MonoBehaviour
{
    // Bubble setting
    public float bubbleRadius = 4f;
    public LayerMask TreeDetectorLayer;

    // A dict to map objects to reusable collider objects
    private  Dictionary<GameObject,List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();

    // tracks which dummy colliders are holding which detailed collider
    private Dictionary<Transform,GameObject> activeDetailedColliders = new Dictionary<Transform, GameObject>();

    void Update()
    {
        // find all dummies inside bubble
        Collider[] nearbyDummies = Physics.OverlapSphere(transform.position,bubbleRadius,TreeDetectorLayer,QueryTriggerInteraction.Collide);

        // keep a list of all dummies seen this frame
        HashSet<Transform> currentFrameDummies = new HashSet<Transform>();

        foreach (Collider dummies in nearbyDummies)
        {
            currentFrameDummies.Add(dummies.transform);

            if (!activeDetailedColliders.ContainsKey(dummies.transform))
            {
                TreeColliderData data = dummies.GetComponent<TreeColliderData>();
                if (data != null && data.detailedColliderPrefab != null)
                {
                    GameObject detailedColl = GetFromPool(data.detailedColliderPrefab);

                    detailedColl.transform.position = dummies.transform.position;
                    detailedColl.transform.localScale = dummies.transform.localScale;
                    detailedColl.transform.rotation = dummies.transform.rotation;

                    detailedColl.SetActive(true);
                    activeDetailedColliders.Add(dummies.transform,detailedColl);
                }
            }
        }

        List<Transform> keysToRemove = new List<Transform>();
        foreach(var kvp in activeDetailedColliders)
        {
            if (!currentFrameDummies.Contains(kvp.Key))
            {
                ReturnToPool(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (Transform keys in keysToRemove)
        {
            activeDetailedColliders.Remove(keys);
        }
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if(!pool.ContainsKey(prefab)) pool[prefab] = new List<GameObject>();

        foreach(GameObject obj in pool[prefab])
        {
            if (!obj.activeSelf)
            {
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab);

        MeshRenderer[] renderers = newObj.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer mr in renderers)
        {
            Destroy(mr);
        }

        MeshFilter[] filters = newObj.GetComponentsInChildren<MeshFilter>();
        foreach(MeshFilter mf in filters)
        {
            Destroy(mf);
        }

        newObj.SetActive(false);
        pool[prefab].Add(newObj);
        return newObj;
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }
    
    // Draws a yellow sphere in the Scene view so you can see your bubble size
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bubbleRadius);
    }

}
