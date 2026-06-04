using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class PlayerBranchSpawner : MonoBehaviourPun
{
    [Header("Ramas Fijas")]
    public string[] branchPrefabs; 

    [Header("Configuración del Árbol")]
    public float distanciaEntreRamas = 6f; 
    public float distanciaDeRenderizado = 15f; 
    public float[] laneOffsets = { -2f, 0f, 2f }; 

    private class BranchData
    {
        public Vector3 position;
        public string prefabName;
        public int side; 
        public GameObject activeInstance; 
        
        public bool wasSpawned = false; 
        public bool isPermanentlyDestroyed = false; 
    }

    private List<BranchData> mapaDelArbol = new List<BranchData>();
    private float startX;
    private float nextSpawnY;

    void Start()
    {
        if (photonView.IsMine)
        {
            startX = transform.position.x;
            nextSpawnY = transform.position.y + 5f; 
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        while (transform.position.y + 20f > nextSpawnY)
        {
            GenerarDatosDeNuevaRama(nextSpawnY);
            nextSpawnY += distanciaEntreRamas; 
        }

        ActualizarObjetos3D();
    }

    void GenerarDatosDeNuevaRama(float alturaY)
    {
        int sideIndex = Random.Range(0, laneOffsets.Length);
        float offset = laneOffsets[sideIndex];
        
        int sideDir;
        if (offset < 0) sideDir = -1;
        else if (offset > 0) sideDir = 1;
        else sideDir = 0; 

        BranchData nuevaRama = new BranchData();
        nuevaRama.position = new Vector3(startX + offset, alturaY, transform.position.z);
        nuevaRama.prefabName = branchPrefabs[Random.Range(0, branchPrefabs.Length)];
        nuevaRama.side = sideDir;
        nuevaRama.activeInstance = null; 
        nuevaRama.wasSpawned = false;
        nuevaRama.isPermanentlyDestroyed = false;

        mapaDelArbol.Add(nuevaRama);
    }

    void ActualizarObjetos3D()
    {
        foreach (BranchData rama in mapaDelArbol)
        {
            if (rama.isPermanentlyDestroyed) continue;

            float distanciaAlJugador = Mathf.Abs(rama.position.y - transform.position.y);

            if (distanciaAlJugador <= distanciaDeRenderizado && rama.activeInstance == null && !rama.wasSpawned)
            {
                object[] initData = new object[1];
                initData[0] = rama.side; 

                rama.activeInstance = PhotonNetwork.Instantiate(rama.prefabName, rama.position, Quaternion.identity, 0, initData);
                rama.wasSpawned = true; 
            }
            else if (distanciaAlJugador > distanciaDeRenderizado && rama.activeInstance != null)
            {
                PhotonNetwork.Destroy(rama.activeInstance);
                rama.activeInstance = null; 
                rama.wasSpawned = false; 
            }

            else if (distanciaAlJugador <= distanciaDeRenderizado && rama.activeInstance == null && rama.wasSpawned)
            {
                rama.isPermanentlyDestroyed = true; 
            }
        }
    }
}