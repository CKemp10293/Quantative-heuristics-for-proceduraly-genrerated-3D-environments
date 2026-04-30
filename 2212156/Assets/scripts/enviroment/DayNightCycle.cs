using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
   [Header("Time Settings")]
    [Tooltip("Current time in a 24-hour format (e.g., 12 = Noon, 18 = 6 PM)")]
    [Range(0f, 24f)] 
    public float timeOfDay = 8f; 
    
    [Tooltip("How fast time passes. 1 = 1 in-game hour per real second.")]
    public float timeSpeed = 0.5f;

    [Header("Celestial bodies")]
    public Transform sunTransform;
    public Transform moonTransform;
    
    [Tooltip("The compass direction the sun rises from (0 to 360)")]
    public float sunYRotation = 0f;

    void Update()
    {
        timeOfDay += Time.deltaTime * timeSpeed;

        // Loop 24hr
        if (timeOfDay >= 24f)
        {
            timeOfDay %= 24f;
        }

        // Calc sun rotation based on time
        float sunAngle = (timeOfDay / 24f) * 360f - 90f;

        // Moon is always opisite sun
        float moonAngle = sunAngle + 180f;

        // Apply rotation to sun

        if (sunTransform != null)
        {
            sunTransform.localRotation = Quaternion.Euler(sunAngle,sunYRotation,0f);
        }
        if (moonTransform != null)
        {
            moonTransform.localRotation = Quaternion.Euler(moonAngle,sunYRotation,0f);
        }
    }
}
