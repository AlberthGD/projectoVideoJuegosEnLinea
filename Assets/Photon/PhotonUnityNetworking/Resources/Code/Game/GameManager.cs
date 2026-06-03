using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviourPunCallbacks 
{
    [Header("Spawn")]
    public Transform spawnPointJ1; 
    public Transform spawnPointJ2; 

    [Header("PrefabsCharacters")]
    public string[] playerPrefabNames;

    [Header("Menú de Pausa / Salida")]
    public GameObject pausePanel; 
    public string nombreEscenaLobby = "Lobby"; 

    void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(!pausePanel.activeSelf);
            }
        }
    }

    void SpawnPlayer()
    {
        int index = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("AvatarIndex"))
        {
            index = (int)PhotonNetwork.LocalPlayer.CustomProperties["AvatarIndex"];
        }

        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.identity;

        if (PhotonNetwork.IsMasterClient) // Jugador 1 (Izquierda)
        {
            spawnPos = spawnPointJ1.position;
        }
        else // Jugador 2 (Derecha)
        {
            spawnPos = spawnPointJ2.position;
        }

        string prefabToSpawn = playerPrefabNames[index];
        GameObject myPlayer = PhotonNetwork.Instantiate(prefabToSpawn, spawnPos, spawnRot);

        SetupLocalCamera(myPlayer);
    }

    void SetupLocalCamera(GameObject player)
    {
        Camera camJ1 = GameObject.Find("Camara_J1").GetComponent<Camera>();
        Camera camJ2 = GameObject.Find("Camara_J2").GetComponent<Camera>();

        camJ1.gameObject.SetActive(true);
        camJ2.gameObject.SetActive(true);
    }

    
    public void BotonSalirPartida()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); 
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaLobby);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(nombreEscenaLobby);
    }
}