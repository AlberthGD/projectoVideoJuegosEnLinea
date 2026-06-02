using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Modo Desarrollo")]
    public bool modoPruebaRapida = false;
    public string nombreEscenaJuego = "GameScene";
    
    [Header("Selección de Personaje (Prueba)")]
    [Tooltip("Índice del personaje (0, 1, 2...) que coincide con tu array en el GameManager.")]
    public int avatarIndexPrueba = 0; 

    [Header("Elementos de la UI")]
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    
    public TextMeshProUGUI statusText; 
    public TMP_InputField codeInputField; 

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        statusText.text = "Conectando al servidor...";
    }

    public override void OnConnectedToMaster()
    {
        if (modoPruebaRapida)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("AvatarIndex", avatarIndexPrueba);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            statusText.text = "Modo Prueba: Entrando directo...";
            RoomOptions opcionesTest = new RoomOptions { MaxPlayers = 1 };
            PhotonNetwork.JoinOrCreateRoom("SalaDeDesarrollo", opcionesTest, TypedLobby.Default);
        }
        else
        {
            PhotonNetwork.JoinLobby();
            statusText.text = "Conectado. Elige una opción.";
        }
    }

    public void CreateRoom()
    {
        string roomCode = Random.Range(1000, 9999).ToString(); 
        RoomOptions options = new RoomOptions { MaxPlayers = 2 }; 
        PhotonNetwork.CreateRoom(roomCode, options);
        statusText.text = "Creando sala...";
    }

    public void JoinRoomWithCode()
    {
        string joinCode = codeInputField.text;
        if (!string.IsNullOrEmpty(joinCode))
        {
            PhotonNetwork.JoinRoom(joinCode);
            statusText.text = "Buscando sala " + joinCode + "...";
        }
        else
        {
            statusText.text = "Por favor, ingresa un código válido.";
        }
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Sala: " + PhotonNetwork.CurrentRoom.Name;
        
        if (modoPruebaRapida || PhotonNetwork.CurrentRoom.MaxPlayers == 1)
        {
            Debug.Log("¡Cargando escena con el personaje índice: " + avatarIndexPrueba + "!");
            PhotonNetwork.LoadLevel(nombreEscenaJuego);
            return; 
        }

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Error: Código incorrecto o sala llena.";
    }

    public void BotonSalirDeSala()
    {
        if (PhotonNetwork.InRoom)
        {
            statusText.text = "Saliendo de la sala...";
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        statusText.text = "Has salido de la sala. Elige una opción.";
        
        roomPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        
        if (codeInputField != null)
        {
            codeInputField.text = "";
        }
    }
}