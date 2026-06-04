using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class PlayerLateralSpawner : MonoBehaviourPun
{
    [Header("Enemigos Voladores")]
    public string[] prefabsArbolEnemigosFly;
    public string[] prefabsLavaEnemigosFly;

    [Header("Filas de Generación (Alturas)")]
    public float[] laneYOffsets = { 2f, 4f, 6f }; 
    public float spawnXDistance = 5f; 

    [Header("Tiempos - Nivel Árbol")]
    public float minSpawnArbol = 3f;
    public float maxSpawnArbol = 4.5f;

    [Header("Tiempos - Nivel Lava")]
    public float minSpawnLava = 2f;
    public float maxSpawnLava = 3.5f;

    private string[] prefabsActuales;
    private float minSpawnTime;
    private float maxSpawnTime;

    private float startX;
    private int lastLane = -1; 

    void Start()
    {
        if (photonView.IsMine)
        {
            startX = transform.position.x;
            ConfigurarParametrosPorNivel();
            StartCoroutine(SpawnRoutine());
        }
    }

    void ConfigurarParametrosPorNivel()
    {
        string escenaActual = SceneManager.GetActiveScene().name;

        if (escenaActual.Contains("Volcan") || escenaActual.Contains("Lava"))
        {
            minSpawnTime = minSpawnLava;
            maxSpawnTime = maxSpawnLava;
            prefabsActuales = prefabsLavaEnemigosFly;
            Debug.Log("[LateralSpawner] Configurado en modo LAVA/VOLCÁN");
        }
        else
        {
            minSpawnTime = minSpawnArbol;
            maxSpawnTime = maxSpawnArbol;
            prefabsActuales = prefabsArbolEnemigosFly;
            Debug.Log("[LateralSpawner] Configurado en modo ÁRBOL");
        }
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(3f); 

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            int chosenLane = Random.Range(0, laneYOffsets.Length);
            
            if (chosenLane == lastLane) 
            {
                chosenLane = (chosenLane + 1) % laneYOffsets.Length;
            }
            lastLane = chosenLane; 

            int side = Random.Range(0, 2) == 0 ? -1 : 1; 
            
            float spawnX = startX + (spawnXDistance * side);
            float spawnY = transform.position.y + laneYOffsets[chosenLane];
            Vector3 spawnPosition = new Vector3(spawnX, spawnY, transform.position.z);

            int flightDirection = side * -1; 
            string randomPrefab = prefabsActuales[Random.Range(0, prefabsActuales.Length)];

            object[] initData = new object[1]; 
            initData[0] = flightDirection; 

            PhotonNetwork.Instantiate(randomPrefab, spawnPosition, Quaternion.identity, 0, initData);
        }
    }
}