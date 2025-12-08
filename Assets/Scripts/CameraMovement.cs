using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform island;

    [SerializeField] private float minCameraSize;
    [SerializeField] private float maxCameraSize;
    [SerializeField] private float sizeScaleFactor;

    [SerializeField] private float islandCircleRadMin;
    [SerializeField] private float islandCircleRadMax;
    [SerializeField] private float islandCircleScaleFactor;
    private float islandCircleRadius;

    private float distance;
    private new Camera camera;
    public bool playerInFog;

    private void Start()
    {
        camera = GetComponent<Camera>();
    }
    private void Update()
    {
        distance = Vector2.Distance(island.position, player.position);
        if (playerInFog == true)
        {
            FocusOnPlayer();
        }
        else
        {
            
            ///*
            float midPointForX = MidPoint(player.position, IslandCircleLocation(AngleOfPlayerRelIsland(player.position, island.position))).x;
            float midpointForY = MidPoint(player.position, IslandCircleLocation(AngleOfPlayerRelIsland(player.position, island.position))).y;
            transform.position = new Vector3(midPointForX, midpointForY, transform.position.z);
            //*/
            //transform.position = new Vector3(MidPoint(player.position, island.position).x, MidPoint(player.position, island.position).y, transform.position.z);
            camera.orthographicSize = CameraSize(distance, sizeScaleFactor, minCameraSize, maxCameraSize);
        }
    }

    private Vector2 MidPoint(Vector2 obj1, Vector2 obj2)
    {
       
        float midpointX = (obj1.x + obj2.x) / 2;
        
        float midpointY = (obj1.y + obj2.y) / 2;
        
        Vector2 midpoint = new Vector2(midpointX, midpointY);

        return midpoint;
    }

    private void FocusOnPlayer()
    {
        transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
        camera.orthographicSize = 10;
    }

    private float CameraSize(float distance, float scaleFactor, float minSize, float maxSize)
    {
        float cameraSize = distance * scaleFactor + 3;

        if(cameraSize <= minSize)
        {
            cameraSize = minSize;
        }
        if(cameraSize >= maxSize)
        {
            cameraSize = maxSize;
        }

        return cameraSize;
    }
 
    private Vector2 IslandCircleLocation(float angle)
    {

        float x = ChangingRadius(distance) * Mathf.Cos(angle);
        float y = ChangingRadius(distance) * Mathf.Sin(angle);
        
        Vector2 circlePos = new Vector2(x, y);
        return circlePos;
    }

    private float AngleOfPlayerRelIsland(Vector2 player, Vector2 island)
    {
        Vector2 playerPosRelIsland = player - island;
        float angle = Mathf.Atan(playerPosRelIsland.y / playerPosRelIsland.x);
        if(playerPosRelIsland.x < 0)
        {
            angle += 180 * Mathf.Deg2Rad;
        }

        return angle;
    }
    private float ChangingRadius(float distance)
    {
        float radius = distance * islandCircleScaleFactor;
        if(radius >= islandCircleRadMax)
        {
            radius = islandCircleRadMax;
        }
        if(radius <= islandCircleRadMin)
        {
            radius = islandCircleRadMin;
        }
        return radius;
    }
}
