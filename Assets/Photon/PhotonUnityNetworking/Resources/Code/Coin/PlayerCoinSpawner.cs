using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerCoinSpawner : MonoBehaviourPun
{
    [Header("Configuración de Monedas")]
    public string prefabMonedaNombre = "Coin"; 
    public float separacionVertical = 1.5f; 
    public int monedasPorLineaMin = 4;
    public int monedasPorLineaMax = 8;

    //distancia en que se empieza a spawnear las monedas adelante del player
    
    public float distanciaVisibilidadAdelanto = 25f; 

    [Header("Filas de Generación (Relativas al Jugador)")]
    public float[] laneOffsets = { -2f, 0f, 2f }; 

    private float startX;
    private float currentSpawnWorldY; 

    void Start()
    {
        if (photonView.IsMine)
        {
            startX = transform.position.x;
            
            currentSpawnWorldY = transform.position.y + 5f; 

            StartCoroutine(SpawnCoinRoutine());
        }
    }

    IEnumerator SpawnCoinRoutine()
    {
        while (true)
        {
            
            while (currentSpawnWorldY < transform.position.y + distanciaVisibilidadAdelanto)
            {
                int chosenLane = Random.Range(0, laneOffsets.Length);
                float xPos = startX + laneOffsets[chosenLane];

                int monedasEnEstaLinea = Random.Range(monedasPorLineaMin, monedasPorLineaMax + 1);

                for (int i = 0; i < monedasEnEstaLinea; i++)
                {
                    Vector3 spawnPosition = new Vector3(xPos, currentSpawnWorldY, transform.position.z);
                    PhotonNetwork.Instantiate(prefabMonedaNombre, spawnPosition, Quaternion.identity);

                    // Avanzamos el puntero global de altura para la SIGUIENTE moneda individual
                    currentSpawnWorldY += separacionVertical;
                }

                
            }

            
            yield return new WaitForSeconds(0.1f);
        }
    }
}