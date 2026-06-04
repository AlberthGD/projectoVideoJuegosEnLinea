using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class PlayerObstacleSpawner : MonoBehaviourPun
{
    [Header("Tipo de Generador")]
    public bool esPowerUp = false;

    [Header("Configuración de Obstáculos")]
    public string[] obstacleArbolPrefabs;
    public string[] obstacleLavaPrefabs;

    [Header("Filas de Generación (Relativas al Jugador)")]
    public float[] laneOffsets = { -2f, 0f, 2f }; 
    public float spawnHeightOffset = 9f; 

    [Header("Tiempos - Nivel Árbol")]
    public float minSpawnArbol = 2.5f;
    public float maxSpawnArbol = 4f;

    [Header("Tiempos - Nivel Lava")]
    public float minSpawnLava = 1.8f;
    public float maxSpawnLava = 2.5f;

    [Header("UI de Advertencias")]
    public GameObject[] uiWarnings; 
    public float tiempoDeAdvertencia = 1.5f;

    private string[] prefabsActuales;
    private float minSpawnTime;
    private float maxSpawnTime;
    private float startX;

    private int[] activeWarningsCount = new int[3]; 


    void Start()
    {
        if (photonView.IsMine)
        {
            startX = transform.position.x;
            ConfigurarParametrosPorNivel();

            string prefijoMio = PhotonNetwork.IsMasterClient ? "J1" : "J2";
            string prefijoRival = PhotonNetwork.IsMasterClient ? "J2" : "J1";

            uiWarnings = new GameObject[3]; 
            uiWarnings[0] = GameObject.Find("Advertencia_" + prefijoMio + "_Izq");
            uiWarnings[1] = GameObject.Find("Advertencia_" + prefijoMio + "_Centro");
            uiWarnings[2] = GameObject.Find("Advertencia_" + prefijoMio + "_Der");

            foreach (GameObject warning in uiWarnings)
            {
                if (warning != null) warning.SetActive(false);
            }

            GameObject rivalIzq = GameObject.Find("Advertencia_" + prefijoRival + "_Izq");
            GameObject rivalCentro = GameObject.Find("Advertencia_" + prefijoRival + "_Centro");
            GameObject rivalDer = GameObject.Find("Advertencia_" + prefijoRival + "_Der");

            if (rivalIzq != null) rivalIzq.SetActive(false);
            if (rivalCentro != null) rivalCentro.SetActive(false);
            if (rivalDer != null) rivalDer.SetActive(false);

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
                string randomPrefab = prefabsActuales[Random.Range(0, prefabsActuales.Length)];

                StartCoroutine(LanzarObstaculoConAdvertencia(xPos, randomPrefab, chosenLane));
            }
        }
    }

    IEnumerator LanzarObstaculoConAdvertencia(float posicionX, string nombrePrefab, int laneIndex)
    {
        if (!esPowerUp)
        {
            if (uiWarnings != null && uiWarnings.Length > laneIndex && uiWarnings[laneIndex] != null)
            {
                activeWarningsCount[laneIndex]++;
                uiWarnings[laneIndex].SetActive(true);
            }

            yield return new WaitForSeconds(tiempoDeAdvertencia);

            if (uiWarnings != null && uiWarnings.Length > laneIndex && uiWarnings[laneIndex] != null)
            {
                activeWarningsCount[laneIndex]--;
                if (activeWarningsCount[laneIndex] <= 0)
                {
                    activeWarningsCount[laneIndex] = 0;
                    uiWarnings[laneIndex].SetActive(false);
                }
            }
        }

        Vector3 finalSpawnPosition = new Vector3(posicionX, transform.position.y + spawnHeightOffset, transform.position.z);
        PhotonNetwork.Instantiate(nombrePrefab, finalSpawnPosition, Quaternion.identity);
    }
}