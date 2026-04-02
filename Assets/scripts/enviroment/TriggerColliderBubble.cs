using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Dynamically generates detailed colliders around a moving object within a specified radius,
/// using an object pool to maximize performance and eliminate memory allocation spikes.
/// </summary>
public class TriggerColliderBubble : MonoBehaviour
{
    [Header("Bubble Settings")]
    [Tooltip("The radius around this object where detailed colliders will be spawned.")]
    public float bubbleRadius = 4f;

    [Tooltip("The layer mask used to detect the dummy trigger colliders.")]
    [FormerlySerializedAs("TreeDetectorLayer")] // Prevents losing your existing Inspector assignment
    public LayerMask treeDetectorLayer;

    [Tooltip("Maximum number of colliders that can be processed at once. Increase if trees are extremely dense.")]
    public int maxConcurrentColliders = 128;

    // OPTIMIZATION: O(1) Object Pool using Stacks to hold only inactive objects
    private readonly Dictionary<GameObject, Stack<GameObject>> inactivePool = new Dictionary<GameObject, Stack<GameObject>>();

    // Tracks which dummy colliders are holding which detailed collider and from which prefab
    private readonly Dictionary<Transform, PooledCollider> activeDetailedColliders = new Dictionary<Transform, PooledCollider>();

    // OPTIMIZATION: Pre-allocated collections to eliminate Garbage Collection (GC) spikes in Update
    private Collider[] overlapResults;
    private readonly HashSet<Transform> currentFrameDummies = new HashSet<Transform>();
    private readonly List<Transform> keysToRemove = new List<Transform>();

    /// <summary>
    /// Struct to link an active instance back to its original prefab so it can be returned to the correct pool stack.
    /// </summary>
    private struct PooledCollider
    {
        public GameObject instance;
        public GameObject prefab;
    }

    private void Awake()
    {
        // Initialize the physics buffer once
        overlapResults = new Collider[maxConcurrentColliders];
    }
    void Update()
    {

        // Clear collections without reallocating memory
        currentFrameDummies.Clear();
        keysToRemove.Clear();

        // find all dummies inside bubble
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, bubbleRadius, overlapResults, treeDetectorLayer, QueryTriggerInteraction.Collide);

        // keep a list of all dummies seen this frame
        for (int i = 0; i < hitCount; i++)
        {
            Transform dummyTransform = overlapResults[i].transform;
            currentFrameDummies.Add(dummyTransform);

            // If this tree doesn't have an active detailed collider yet, spawn one
            if (!activeDetailedColliders.ContainsKey(dummyTransform))
            {
                if (dummyTransform.TryGetComponent(out TreeColliderData data) && data.detailedColliderPrefab != null)
                {
                    GameObject detailedColl = GetFromPool(data.detailedColliderPrefab);

                    // SetPositionAndRotation updates the transform matrix once
                    detailedColl.transform.SetPositionAndRotation(dummyTransform.position, dummyTransform.rotation);
                    detailedColl.transform.localScale = dummyTransform.localScale;

                    detailedColl.SetActive(true);

                    activeDetailedColliders.Add(dummyTransform, new PooledCollider
                    {
                        instance = detailedColl,
                        prefab = data.detailedColliderPrefab
                    });
                }
            }
        }

        // Find colliders that have fallen out of the bubble
        foreach (var kvp in activeDetailedColliders)
        {
            if (!currentFrameDummies.Contains(kvp.Key))
            {
                ReturnToPool(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }

        // Remove them from the active tracking dictionary
        foreach (Transform key in keysToRemove)
        {
            activeDetailedColliders.Remove(key);
        }
    }

    /// <summary>
    /// Retrieves an inactive collider from the pool in O(1) time, or instantiates a new one if empty.
    /// </summary>
    private GameObject GetFromPool(GameObject prefab)
    {
        if (!inactivePool.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            inactivePool[prefab] = stack;
        }

        if (stack.Count > 0)
        {
            return stack.Pop();
        }

        // Instantiate new if the pool stack is empty
        GameObject newObj = Instantiate(prefab);

        // Dynamically fetching and destroying components at runtime is very expensive.
        MeshRenderer[] renderers = newObj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in renderers)
        {
            Destroy(mr);
        }

        MeshFilter[] filters = newObj.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in filters)
        {
            Destroy(mf);
        }

        newObj.SetActive(false);
        return newObj;
    }

    /// <summary>
    /// Deactivates the collider and pushes it back onto its specific prefab stack.
    /// </summary>
    private void ReturnToPool(PooledCollider pooledColl)
    {
        pooledColl.instance.SetActive(false);
        inactivePool[pooledColl.prefab].Push(pooledColl.instance);
    }
    
    // Draws a yellow sphere in the Scene view so you can see your bubble size
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bubbleRadius);
    }

}
