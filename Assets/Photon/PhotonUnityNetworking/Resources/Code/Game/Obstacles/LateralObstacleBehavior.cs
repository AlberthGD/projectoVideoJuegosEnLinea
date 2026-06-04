using UnityEngine;
using Photon.Pun;

public class LateralObstacleBehavior : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [Header("Movimiento Base")]
    public float horizontalSpeed = 5f; 
    
    [Header("Efecto ZigZag")]
    public float zigzagAmplitude = 1.5f; 
    public float zigzagFrequency = 3f;   

    [Header("Zona de Destrucción")]
    public float destroyDistance = 25f; 

    private int direction = 1; 
    private float baseY; 
    private float spawnX; 
    private float flightTimer = 0f;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data != null && data.Length >= 1)
        {
            direction = (int)data[0];
        }

        baseY = transform.position.y;
        spawnX = transform.position.x; 

        if (direction == -1) transform.rotation = Quaternion.Euler(0, -90, 0); 
        else transform.rotation = Quaternion.Euler(0, 90, 0);  
    }

    void Update()
    {
        flightTimer += Time.deltaTime;

        float newX = transform.position.x + (direction * horizontalSpeed * Time.deltaTime);
        float newY = baseY + (Mathf.Sin(flightTimer * zigzagFrequency) * zigzagAmplitude);

        transform.position = new Vector3(newX, newY, transform.position.z);

        if (photonView.IsMine)
        {
            if (Mathf.Abs(transform.position.x - spawnX) > destroyDistance)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}