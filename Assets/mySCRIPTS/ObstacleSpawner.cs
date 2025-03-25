using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    // Player reference for GroundSpawn to follow
    public Transform player;

    // Prefabs for obstacles
    public GameObject groundSpawnPrefab; // GroundSpawn obstacle
    public GameObject skyFallSpawnPrefab; // SkyFallSpawn obstacle

    // GroundSpawn settings
    public float groundSpawnHeight = 1f; // Hover height above ground
    public float initialFollowSpeed = 2f; // Initial speed at which GroundSpawn follows player
    public float followSpeedIncrease = 0.05f; // Rate at which follow speed increases per second
    private float currentFollowSpeed; // Tracks current follow speed

    // SkyFallSpawn settings
    public float skySpawnHeight = 20f; // Initial height for SkyFallSpawn
    public float fallSpeed = 5f; // Speed at which SkyFallSpawn falls

    // Spawn rate settings
    public float initialGroundSpawnRate = 2f; // Initial delay between GroundSpawn spawns (seconds)
    public float initialSkySpawnRate = 3f;   // Initial delay between SkyFallSpawn spawns (seconds)
    public float minSpawnRate = 0.5f; // Minimum delay between spawns for both
    public float groundSpawnRateIncrease = 0.1f; // Rate at which GroundSpawn delay decreases per second
    public float skySpawnRateIncrease = 0.05f;   // Rate at which SkyFallSpawn delay decreases per second

    // 3D Bounding box for spawn area
    public Vector3 minSpawnBounds = new Vector3(-10f, 0f, -10f); // Min x, y, z coordinates
    public Vector3 maxSpawnBounds = new Vector3(10f, 20f, 10f);  // Max x, y, z coordinates

    private float currentGroundSpawnRate;
    private float currentSkySpawnRate;
    private float groundSpawnTimer;
    private float skySpawnTimer;
    private float timeElapsed;

    // Terrain layer for ground detection
    public LayerMask terrainLayer;

    void Start()
    {
        currentGroundSpawnRate = initialGroundSpawnRate; // Initialize with separate ground rate
        currentSkySpawnRate = initialSkySpawnRate;       // Initialize with separate sky rate
        currentFollowSpeed = initialFollowSpeed;         // Initialize follow speed
        groundSpawnTimer = 0f;
        skySpawnTimer = 0f;
        timeElapsed = 0f;

        if (player == null)
        {
            player = FindObjectOfType<ThirdPersonController>().transform;
            if (player == null)
            {
                Debug.LogError("Player not found! Please assign the player Transform in the Inspector.");
            }
        }
    }

    void Update()
    {
        // Increase spawn rates and follow speed over time
        timeElapsed += Time.deltaTime;
        currentGroundSpawnRate = Mathf.Max(minSpawnRate, initialGroundSpawnRate - (groundSpawnRateIncrease * timeElapsed));
        currentSkySpawnRate = Mathf.Max(minSpawnRate, initialSkySpawnRate - (skySpawnRateIncrease * timeElapsed));
        currentFollowSpeed = initialFollowSpeed + (followSpeedIncrease * timeElapsed); // Increase follow speed

        // Only spawn if player is within bounds (optional)
        if (IsPlayerInBounds())
        {
            // GroundSpawn spawning
            groundSpawnTimer += Time.deltaTime;
            if (groundSpawnTimer >= currentGroundSpawnRate)
            {
                SpawnGroundObstacle();
                groundSpawnTimer = 0f;
            }

            // SkyFallSpawn spawning
            skySpawnTimer += Time.deltaTime;
            if (skySpawnTimer >= currentSkySpawnRate)
            {
                SpawnSkyFallObstacle();
                skySpawnTimer = 0f;
            }
        }
    }

    // Spawn GroundSpawn obstacle
    void SpawnGroundObstacle()
    {
        Vector3 spawnPos = GetRandomGroundSpawnPosition();
        if (GetTerrainHeight(spawnPos, out float terrainHeight))
        {
            spawnPos.y = terrainHeight; // Start at ground level
            GameObject groundSpawn = Instantiate(groundSpawnPrefab, spawnPos, Quaternion.identity);
            StartCoroutine(MoveGroundSpawn(groundSpawn));
        }
    }

    // Spawn SkyFallSpawn obstacle
    void SpawnSkyFallObstacle()
    {
        Vector3 spawnPos = GetRandomSkySpawnPosition();
        GameObject skyFallSpawn = Instantiate(skyFallSpawnPrefab, spawnPos, Quaternion.identity);
        StartCoroutine(MoveSkyFallSpawn(skyFallSpawn));
    }

    // Move GroundSpawn: rise to hover height and follow player
    System.Collections.IEnumerator MoveGroundSpawn(GameObject obstacle)
    {
        Vector3 targetPos = obstacle.transform.position;
        targetPos.y += groundSpawnHeight; // Rise to hover height
        float riseTime = 0.5f;

        // Rise to hover height
        while (obstacle != null && Vector3.Distance(obstacle.transform.position, targetPos) > 0.1f)
        {
            obstacle.transform.position = Vector3.Lerp(obstacle.transform.position, targetPos, Time.deltaTime / riseTime);
            yield return null;
        }

        // Follow player with current follow speed
        while (obstacle != null)
        {
            Vector3 direction = (player.position - obstacle.transform.position).normalized;
            direction.y = 0f; // Keep height constant
            obstacle.transform.position += direction * currentFollowSpeed * Time.deltaTime;
            yield return null;
        }
    }

    // Move SkyFallSpawn: fall to ground
    System.Collections.IEnumerator MoveSkyFallSpawn(GameObject obstacle)
    {
        while (obstacle != null)
        {
            obstacle.transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            if (GetTerrainHeight(obstacle.transform.position, out float terrainHeight) &&
                obstacle.transform.position.y <= terrainHeight)
            {
                Destroy(obstacle); // Destroy when it hits the ground
                yield break;
            }
            yield return null;
        }
    }

    // Get random spawn position for GroundSpawn (x/z in bounds, y from terrain)
    Vector3 GetRandomGroundSpawnPosition()
    {
        float x = Random.Range(minSpawnBounds.x, maxSpawnBounds.x);
        float z = Random.Range(minSpawnBounds.z, maxSpawnBounds.z);
        return new Vector3(x, minSpawnBounds.y, z); // y adjusted by terrain later
    }

    // Get random spawn position for SkyFallSpawn (x/z in bounds, y at sky height)
    Vector3 GetRandomSkySpawnPosition()
    {
        float x = Random.Range(minSpawnBounds.x, maxSpawnBounds.x);
        float z = Random.Range(minSpawnBounds.z, maxSpawnBounds.z);
        return new Vector3(x, maxSpawnBounds.y, z); // y starts at max height
    }

    // Check if player is within the 3D spawn bounds (optional)
    bool IsPlayerInBounds()
    {
        Vector3 playerPos = player.position;
        return playerPos.x >= minSpawnBounds.x && playerPos.x <= maxSpawnBounds.x &&
               playerPos.y >= minSpawnBounds.y && playerPos.y <= maxSpawnBounds.y &&
               playerPos.z >= minSpawnBounds.z && playerPos.z <= maxSpawnBounds.z;
    }

    // Raycast to get terrain height at position
    private bool GetTerrainHeight(Vector3 position, out float height)
    {
        Ray ray = new Ray(new Vector3(position.x, 1000f, position.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2000f, terrainLayer))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f; // Default fallback if raycast fails
        return false;
    }

    // Visualize the 3D spawn area in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = (minSpawnBounds + maxSpawnBounds) / 2f;
        Vector3 size = maxSpawnBounds - minSpawnBounds;
        Gizmos.DrawWireCube(center, size);
    }
}