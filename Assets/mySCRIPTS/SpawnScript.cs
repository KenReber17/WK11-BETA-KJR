using UnityEngine;
using System.Collections.Generic;

public class SpawnScript : MonoBehaviour
{
    // Public variable to set the prefab to spawn
    public GameObject prefabToSpawn;
    
    // Public variable to set how many prefabs to spawn
    public int spawnCount = 1;
    
    // Public variable for rotation speed around central point (degrees per second)
    public float rotationSpeed = 30f;
    
    // Public variable for rotation direction around central point (true = counterclockwise, false = clockwise)
    public bool rotateCounterClockwise = true;
    
    // Public variable for spawn elevation (height above trigger position)
    public float spawnElevation = 10f;
    
    // Public variable for spin speed around each prefab's own Y-axis (degrees per second)
    public float spinSpeed = 20f; // Default spin speed
    
    // Fixed radius for the circular spawn pattern (20 units between objects)
    private float spawnRadius = 20f;
    
    // List to keep track of spawned objects
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // Track if spawning has been triggered
    private bool isTriggered = false;

    // Called when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player and hasn't been triggered yet
        if (other.CompareTag("Player") && !isTriggered)
        {
            // Mark as triggered
            isTriggered = true;

            // Clear any previously spawned objects (optional, remove if you want to keep adding)
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedObjects.Clear();

            // Calculate the angle between each spawn point
            float angleStep = 360f / spawnCount;
            
            // Spawn the specified number of prefabs
            for (int i = 0; i < spawnCount; i++)
            {
                // Calculate the angle for this spawn point
                float angle = i * angleStep * Mathf.Deg2Rad; // Convert to radians
                
                // Calculate spawn position in a circle
                float x = Mathf.Cos(angle) * spawnRadius;
                float z = Mathf.Sin(angle) * spawnRadius;
                Vector3 spawnPosition = transform.position + new Vector3(x, spawnElevation, z);
                
                // Instantiate the prefab and add to list
                GameObject spawned = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);
                spawnedObjects.Add(spawned);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Determine rotation direction around central point based on public variable
        float directionMultiplier = rotateCounterClockwise ? 1f : -1f;

        // Rotate all spawned objects around the central Y-axis and spin on their own Y-axis
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) // Check if object still exists
            {
                // Rotate around the trigger's position (Y-axis) with direction
                obj.transform.RotateAround(transform.position, Vector3.up, rotationSpeed * directionMultiplier * Time.deltaTime);
                
                // Spin the object around its own Y-axis
                obj.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
            }
        }
    }

    // Visualize the trigger area and spawn points in the editor
    private void OnDrawGizmos()
    {
        // Draw the trigger area
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        // Draw preview of spawn points
        if (spawnCount > 0)
        {
            Gizmos.color = Color.red;
            float angleStep = 360f / spawnCount;
            
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * spawnRadius;
                float z = Mathf.Sin(angle) * spawnRadius;
                Vector3 spawnPoint = transform.position + new Vector3(x, spawnElevation, z);
                Gizmos.DrawSphere(spawnPoint, 0.5f);
            }
        }
    }
}