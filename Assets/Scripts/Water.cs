using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private float fogDamage;
    [SerializeField] private float fogRate;
    private float timer;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer boat;
    [SerializeField] private SpriteRenderer turret;
    [SerializeField] private float inFogOpacity;
    [SerializeField] private float outFogOpacity;
    [SerializeField] private CameraMovement camMove;
    public bool playerInFog = false;

    private void Start()
    {
        timer = fogRate;
    }
    private void Update()
    {
        if (playerInFog == true)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                playerHealth.TakeDamage(fogDamage);
                timer = fogRate;
            }        
            Color boatColor = boat.color;
            Color turretColor = turret.color;
            boatColor.a = inFogOpacity;
            turretColor.a = inFogOpacity;
            boat.color = boatColor;
            turret.color = turretColor;
            camMove.playerInFog = true;
        }
        else
        {
            timer = fogRate;
            Color boatColor = boat.color;
            Color turretColor = turret.color;
            boatColor.a = outFogOpacity;
            turretColor.a = outFogOpacity;
            boat.color = boatColor;
            turret.color = turretColor;
            camMove.playerInFog = false;
        }
    }
}
