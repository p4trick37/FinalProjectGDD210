using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform island;

    [SerializeField] private float minCameraSize;
    [SerializeField] private float maxCameraSize;

    [SerializeField] private float sizeScaleFactor;
    [SerializeField] private float posScaleFactor;

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
        if(playerInFog == true)
        {
            FocusOnPlayer();
        }
        else
        {
            transform.position = new Vector3(MidPoint(player.position, island.position).x, MidPoint(player.position, island.position).y, transform.position.z);
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
}
