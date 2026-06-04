using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class PlayerObstacleSpawner : MonoBehaviourPun
{
    [Header("Configuración de Obstáculos")]
    public string[] obstacleArbolPrefabs;
    public string[] obstacleLavaPrefabs;

    [Header("Filas de Generación (Relativas al Jugador)")]
    public float[] laneOffsets = { -2f, 0f, 2f }; 
    public float spawnHeightOffset = 15f; 

    [Header("Tiempos - Nivel Árbol")]
    public float minSpawnArbol = 2.5f;
    public float maxSpawnArbol = 4f;

    [Header("Tiempos - Nivel Lava")]
    public float minSpawnLava = 1.8f;
    public float maxSpawnLava = 2.5f;

    private string[] prefabsActuales;
    private float minSpawnTime;
    private float maxSpawnTime;

    private float startX;

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
            prefabsActuales = obstacleLavaPrefabs;
            Debug.Log("[ObstacleSpawner] Configurado en modo LAVA/VOLCÁN");
        }
        else
        {
            minSpawnTime = minSpawnArbol;
            maxSpawnTime = maxSpawnArbol;
            prefabsActuales = obstacleArbolPrefabs;
            Debug.Log("[ObstacleSpawner] Configurado en modo ÁRBOL");
        }
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            int obstaclesToSpawn = Random.Range(1, 3); 
            List<int> availableLanes = new List<int> { 0, 1, 2 };

            for (int i = 0; i < obstaclesToSpawn; i++)
            {
                int randomIndex = Random.Range(0, availableLanes.Count);
                int chosenLane = availableLanes[randomIndex];
                availableLanes.RemoveAt(randomIndex);

                float xPos = startX + laneOffsets[chosenLane];
                Vector3 spawnPosition = new Vector3(xPos, transform.position.y + spawnHeightOffset, transform.position.z);

                string randomPrefab = prefabsActuales[Random.Range(0, prefabsActuales.Length)];

                PhotonNetwork.Instantiate(randomPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}