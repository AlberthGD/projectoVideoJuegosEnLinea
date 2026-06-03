using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerTargetHud : MonoBehaviourPunCallbacks
{
    [Header("UI Progreso - Jugador 1 (Izquierda)")]
    public GameObject contenedorProgresoJ1;
    public RectTransform progressBarBgJ1;
    public RectTransform heightIndicatorJ1; 
    public RectTransform puntoMinimoJ1; 
    public RectTransform puntoMaximoJ1;
    public RectTransform opponentTargetJ1;  
    public Image opponentTargetImageJ1;     

    [Header("UI Progreso - Jugador 2 (Derecha)")]
    public GameObject contenedorProgresoJ2; 
    public RectTransform progressBarBgJ2;
    public RectTransform heightIndicatorJ2; 
    public RectTransform puntoMinimoJ2; 
    public RectTransform puntoMaximoJ2;
    public RectTransform opponentTargetJ2;  
    public Image opponentTargetImageJ2;  

    [Header("Configuración del Nivel")]
    public float alturaTotalDelNivel = 100f;
    public float distanciaRadar = 15f; 
    public float attackVerticalRange = 2f; 
    public Color targetMatchColor = Color.cyan; 

    [Header("UI Fin de Partida - Jugador 1")]
    public GameObject panelVictoriaJ1;
    public TextMeshProUGUI textoNombreJ1;
    public TextMeshProUGUI textoPuntajeJ1;
    public TextMeshProUGUI textoPosicionJ1;
    public TextMeshProUGUI textoResultadoFinalJ1;

    [Header("UI Fin de Partida - Jugador 2")]
    public GameObject panelVictoriaJ2;
    public TextMeshProUGUI textoNombreJ2;
    public TextMeshProUGUI textoPuntajeJ2;
    public TextMeshProUGUI textoPosicionJ2;
    public TextMeshProUGUI textoResultadoFinalJ2;

    [Header("Votaciones")]
    public GameObject contenedorBotones; 
    public GameObject panelVotacion;
    public TextMeshProUGUI textoEstadisticas;
    public TextMeshProUGUI textoEleccionVotos;
    public TextMeshProUGUI textoTemporizador;

    [Header("Configuración Votación Niveles")]
    public string[] nombresDeEscenasDeNiveles; 
    public float tiempoDeVotacion = 15f;

    // --- VARIABLES INTERNAS ---
    private bool miJuegoTerminado = false;    
    private static int jugadoresTerminados = 0;
    private static int metaAlcanzadaCount = 0; 
    private static int puntajeFinalJ1 = 0;
    private static int puntajeFinalJ2 = 0;

    private PlayerMovement localPlayer;
    private PlayerMovement opponentPlayer;
    private Color originalTargetColor;

    private bool votacionIniciada = false;
    private float temporizadorActual = 0f;
    private int votoJ1 = -1;
    private int votoJ2 = -1;
    private bool decidiendoNivel = false;
    
    private string nombreVotoJ1 = "Pensando...";
    private string nombreVotoJ2 = "Pensando...";

    void Start()
    {
        jugadoresTerminados = 0;
        metaAlcanzadaCount = 0;
        puntajeFinalJ1 = 0;
        puntajeFinalJ2 = 0;
        votoJ1 = -1;
        votoJ2 = -1;
        nombreVotoJ1 = "Pensando...";
        nombreVotoJ2 = "Pensando...";

        if (panelVotacion != null) panelVotacion.SetActive(false);
        if (contenedorBotones != null) contenedorBotones.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            if (contenedorProgresoJ1 != null) contenedorProgresoJ1.SetActive(true);
            if (contenedorProgresoJ2 != null) contenedorProgresoJ2.SetActive(false);
            if (opponentTargetImageJ1 != null) originalTargetColor = opponentTargetImageJ1.color;
            
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("PartidasJugadas"))
            {
                Hashtable initialProps = new Hashtable() { { "PartidasJugadas", 0 }, { "VictoriasJ1", 0 }, { "VictoriasJ2", 0 } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(initialProps);
            }
        }
        else
        {
            if (contenedorProgresoJ1 != null) contenedorProgresoJ1.SetActive(false);
            if (contenedorProgresoJ2 != null) contenedorProgresoJ2.SetActive(true);
            if (opponentTargetImageJ2 != null) originalTargetColor = opponentTargetImageJ2.color;
        }
    }

    void Update()
    {
        if (localPlayer == null || opponentPlayer == null)
        {
            FindPlayersInScene();
            if (localPlayer == null || opponentPlayer == null) return;
        }

        UpdateHeightIndicator();
        UpdateOpponentTarget();
        CheckAttackProximity();

        if (votacionIniciada && PhotonNetwork.IsMasterClient && !decidiendoNivel)
        {
            temporizadorActual -= Time.deltaTime;
            photonView.RPC("RPC_ActualizarTemporizadorUI", RpcTarget.All, temporizadorActual);

            if (temporizadorActual <= 0 || (votoJ1 != -1 && votoJ2 != -1))
            {
                decidiendoNivel = true;
                ElegirNivelYCambiarEscena();
            }
        }
    }

    void FindPlayersInScene()
    {
        PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (PlayerMovement p in allPlayers)
        {
            if (p.photonView.IsMine) localPlayer = p;
            else opponentPlayer = p;
        }
    }

    void UpdateHeightIndicator()
    {
        RectTransform activeHeightIndicator = PhotonNetwork.IsMasterClient ? heightIndicatorJ1 : heightIndicatorJ2;
        RectTransform activeMin = PhotonNetwork.IsMasterClient ? puntoMinimoJ1 : puntoMinimoJ2;
        RectTransform activeMax = PhotonNetwork.IsMasterClient ? puntoMaximoJ1 : puntoMaximoJ2;

        if (activeHeightIndicator == null || activeMin == null || activeMax == null) return;

        float currentHeight = localPlayer.totalClimbedDistance; 
        float normalizedHeight = Mathf.Clamp01(currentHeight / alturaTotalDelNivel);

        float targetY = Mathf.Lerp(activeMin.anchoredPosition.y, activeMax.anchoredPosition.y, normalizedHeight);
        activeHeightIndicator.anchoredPosition = new Vector2(activeHeightIndicator.anchoredPosition.x, targetY);

        if (currentHeight >= alturaTotalDelNivel && !miJuegoTerminado)
        {
            miJuegoTerminado = true;
            localPlayer.enabled = false; 
            int misPuntosFinales = PlayerStats.LocalInstance != null ? PlayerStats.LocalInstance.score : 0;
            bool soyJugador1 = PhotonNetwork.IsMasterClient;
            
            photonView.RPC("RPC_JugadorTermino", RpcTarget.All, PhotonNetwork.NickName, misPuntosFinales, soyJugador1, false);
        }
    }

    public void JugadorEliminadoPorLava()
    {
        if (miJuegoTerminado) return; 

        miJuegoTerminado = true;
        localPlayer.enabled = false; 

        int misPuntosFinales = 0;
        if (PlayerStats.LocalInstance != null)
        {
            PlayerStats.LocalInstance.TakeDamage(300); 
            misPuntosFinales = PlayerStats.LocalInstance.score; 
        }

        bool soyJugador1 = PhotonNetwork.IsMasterClient;
        photonView.RPC("RPC_JugadorTermino", RpcTarget.All, PhotonNetwork.NickName, misPuntosFinales, soyJugador1, true);
    }

    [PunRPC]
    void RPC_JugadorTermino(string nombreJugador, int puntajeBase, bool esJugador1, bool murioPorLava)
    {
        jugadoresTerminados++;
        int puntajeCalculado = puntajeBase;
        string textoPosicion = "";

        if (string.IsNullOrEmpty(nombreJugador)) nombreJugador = esJugador1 ? "Jugador 1" : "Jugador 2";

        if (murioPorLava) textoPosicion = "Eliminado por Lava (-300 pts)";
        else
        {
            metaAlcanzadaCount++;
            if (metaAlcanzadaCount == 1) { puntajeCalculado += 300; textoPosicion = "Meta: 1er Lugar (+300 Bonus)"; }
            else textoPosicion = "Meta: 2do Lugar (Sin Bonus)";
        }

        if (esJugador1) puntajeFinalJ1 = puntajeCalculado;
        else puntajeFinalJ2 = puntajeCalculado;

        if (esJugador1)
        {
            if (panelVictoriaJ1 != null) panelVictoriaJ1.SetActive(true);
            if (textoNombreJ1 != null) textoNombreJ1.text = "Jugador: " + nombreJugador;
            if (textoPuntajeJ1 != null) textoPuntajeJ1.text = "Puntaje Final: " + puntajeCalculado.ToString();
            if (textoPosicionJ1 != null) { textoPosicionJ1.text = textoPosicion; textoPosicionJ1.color = murioPorLava ? Color.red : Color.white; }
            if (textoResultadoFinalJ1 != null) { textoResultadoFinalJ1.text = "Esperando al rival..."; textoResultadoFinalJ1.color = Color.white; }
        }
        else
        {
            if (panelVictoriaJ2 != null) panelVictoriaJ2.SetActive(true);
            if (textoNombreJ2 != null) textoNombreJ2.text = "Jugador: " + nombreJugador;
            if (textoPuntajeJ2 != null) textoPuntajeJ2.text = "Puntaje Final: " + puntajeCalculado.ToString();
            if (textoPosicionJ2 != null) { textoPosicionJ2.text = textoPosicion; textoPosicionJ2.color = murioPorLava ? Color.red : Color.white; }
            if (textoResultadoFinalJ2 != null) { textoResultadoFinalJ2.text = "Esperando al rival..."; textoResultadoFinalJ2.color = Color.white; }
        }

        if (jugadoresTerminados >= 2) DecidirGanador();
    }

    void DecidirGanador()
    {
        int ganadorIndex = 0; 
        
        if (puntajeFinalJ1 > puntajeFinalJ2)
        {
            ganadorIndex = 1;
            if (textoResultadoFinalJ1 != null) { textoResultadoFinalJ1.text = "¡HAS GANADO!"; textoResultadoFinalJ1.color = Color.green; }
            if (textoResultadoFinalJ2 != null) { textoResultadoFinalJ2.text = "HAS PERDIDO"; textoResultadoFinalJ2.color = Color.red; }
        }
        else if (puntajeFinalJ2 > puntajeFinalJ1)
        {
            ganadorIndex = 2;
            if (textoResultadoFinalJ2 != null) { textoResultadoFinalJ2.text = "¡HAS GANADO!"; textoResultadoFinalJ2.color = Color.green; }
            if (textoResultadoFinalJ1 != null) { textoResultadoFinalJ1.text = "HAS PERDIDO"; textoResultadoFinalJ1.color = Color.red; }
        }
        else 
        {
            if (textoResultadoFinalJ1 != null) { textoResultadoFinalJ1.text = "¡ES UN EMPATE!"; textoResultadoFinalJ1.color = Color.yellow; }
            if (textoResultadoFinalJ2 != null) { textoResultadoFinalJ2.text = "¡ES UN EMPATE!"; textoResultadoFinalJ2.color = Color.yellow; }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            int partidasJugadas = (int)PhotonNetwork.CurrentRoom.CustomProperties["PartidasJugadas"] + 1;
            int victoriasJ1 = (int)PhotonNetwork.CurrentRoom.CustomProperties["VictoriasJ1"];
            int victoriasJ2 = (int)PhotonNetwork.CurrentRoom.CustomProperties["VictoriasJ2"];

            if (ganadorIndex == 1) victoriasJ1++;
            else if (ganadorIndex == 2) victoriasJ2++;

            Hashtable newProps = new Hashtable() { {"PartidasJugadas", partidasJugadas}, {"VictoriasJ1", victoriasJ1}, {"VictoriasJ2", victoriasJ2} };
            PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);
            
            photonView.RPC("RPC_MostrarBotones", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_MostrarBotones()
    {
        if (contenedorBotones != null) contenedorBotones.SetActive(true);
    }

    public void BotonSalir()
    {
        PhotonNetwork.LeaveRoom(); 
    }

    public void BotonContinuar()
    {
        photonView.RPC("RPC_IrAVotacion", RpcTarget.All);
    }

    [PunRPC]
    void RPC_IrAVotacion()
    {
        // Apagar UI de victoria anterior
        if (panelVictoriaJ1 != null) panelVictoriaJ1.SetActive(false);
        if (panelVictoriaJ2 != null) panelVictoriaJ2.SetActive(false);
        if (contenedorBotones != null) contenedorBotones.SetActive(false);

        // Encender Panel Compartido Central
        if (panelVotacion != null) panelVotacion.SetActive(true);

        ActualizarTextosEstadisticas();
        ActualizarTextoVotosUi();

        if (PhotonNetwork.IsMasterClient && !votacionIniciada)
        {
            photonView.RPC("RPC_IniciarVotacion", RpcTarget.All);
        }
    }

    void ActualizarTextosEstadisticas()
    {
        int pJugadas = (int)PhotonNetwork.CurrentRoom.CustomProperties["PartidasJugadas"];
        int vJ1 = (int)PhotonNetwork.CurrentRoom.CustomProperties["VictoriasJ1"];
        int vJ2 = (int)PhotonNetwork.CurrentRoom.CustomProperties["VictoriasJ2"];

        string liderazgo = "¡Están empatados!";
        if (vJ1 > vJ2) liderazgo = "¡Jugador 1 va ganando!";
        else if (vJ2 > vJ1) liderazgo = "¡Jugador 2 va ganando!";

        string statsTexto = $"Partidas Jugadas: {pJugadas}\n\nVictorias Jugador 1: {vJ1}  \nVictorias Jugador 2: {vJ2}\n\n<color=yellow>{liderazgo}</color>";

        if (textoEstadisticas != null) textoEstadisticas.text = statsTexto;
    }

    void ActualizarTextoVotosUi()
    {
        if (textoEleccionVotos != null)
        {
            textoEleccionVotos.text = $"Jugador 1: {nombreVotoJ1}\nJugador 2: {nombreVotoJ2}";
        }
    }

    [PunRPC]
    void RPC_IniciarVotacion()
    {
        if (!votacionIniciada)
        {
            votacionIniciada = true;
            temporizadorActual = tiempoDeVotacion;
        }
    }

    public void VotarPorNivel(int indiceNivel)
    {
        bool soyJ1 = PhotonNetwork.IsMasterClient;
        photonView.RPC("RPC_RegistrarVoto", RpcTarget.All, soyJ1, indiceNivel);
    }

    [PunRPC]
    void RPC_RegistrarVoto(bool fueJ1, int indiceNivel)
    {
        string nombreNivel = nombresDeEscenasDeNiveles[indiceNivel];

        if (fueJ1) 
        {
            votoJ1 = indiceNivel;
            nombreVotoJ1 = nombreNivel;
        }
        else 
        {
            votoJ2 = indiceNivel;
            nombreVotoJ2 = nombreNivel;
        }
        
        ActualizarTextoVotosUi();
    }

    [PunRPC]
    void RPC_ActualizarTemporizadorUI(float tiempoRestante)
    {
        int segundos = Mathf.CeilToInt(tiempoRestante);
        if (textoTemporizador != null) 
        {
            textoTemporizador.text = "Tiempo para elegir: " + segundos.ToString();
        }
    }

    void ElegirNivelYCambiarEscena()
    {
        int nivelGanador = 0;

        if (votoJ1 == votoJ2 && votoJ1 != -1) nivelGanador = votoJ1;
        else if (votoJ1 != -1 && votoJ2 == -1) nivelGanador = votoJ1; 
        else if (votoJ2 != -1 && votoJ1 == -1) nivelGanador = votoJ2; 
        else 
        {
            nivelGanador = Random.Range(0, nombresDeEscenasDeNiveles.Length);
        }

        string escenaDestino = nombresDeEscenasDeNiveles[nivelGanador];

        if (panelVotacion != null) panelVotacion.SetActive(false);

        StartCoroutine(RutinaCambioNivel(escenaDestino));
    }

    private System.Collections.IEnumerator RutinaCambioNivel(string escenaDestino)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();

            yield return new WaitForSeconds(0.5f);

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == escenaDestino)
            {
                photonView.RPC("RPC_ForzarRecargaEscena", RpcTarget.All, escenaDestino);
            }
            else
            {
                PhotonNetwork.LoadLevel(escenaDestino);
            }
        }
    }

    [PunRPC]
    void RPC_ForzarRecargaEscena(string escenaDestino)
    {
        // Apagamos los mensajes de red mientras carga para ignorar colisiones "fantasmas"
        PhotonNetwork.IsMessageQueueRunning = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(escenaDestino);
    }

    // UI RADAR
    void UpdateOpponentTarget()
    {
        RectTransform activeProgressBarBg = PhotonNetwork.IsMasterClient ? progressBarBgJ1 : progressBarBgJ2;
        RectTransform activeOpponentTarget = PhotonNetwork.IsMasterClient ? opponentTargetJ1 : opponentTargetJ2;
        Image activeOpponentTargetImage = PhotonNetwork.IsMasterClient ? opponentTargetImageJ1 : opponentTargetImageJ2;

        if (activeProgressBarBg == null || activeOpponentTarget == null) return;

        float heightDifference = opponentPlayer.totalClimbedDistance - localPlayer.totalClimbedDistance;
        
        float normalizedDiff = Mathf.Clamp(heightDifference / distanciaRadar, -1f, 1f); 
        float barHeight = activeProgressBarBg.rect.height;
        
        float targetY = normalizedDiff * (barHeight / 2f); 
        activeOpponentTarget.anchoredPosition = new Vector2(activeOpponentTarget.anchoredPosition.x, targetY);

        if (activeOpponentTargetImage != null)
        {
            Color c = activeOpponentTargetImage.color;
            c.a = 0.3f + (Mathf.Sin(Time.time * 5f) + 1f) * 0.3f;
            activeOpponentTargetImage.color = c;
        }
    }

    void CheckAttackProximity()
    {
        Image activeOpponentTargetImage = PhotonNetwork.IsMasterClient ? opponentTargetImageJ1 : opponentTargetImageJ2;
        float heightDifference = Mathf.Abs(opponentPlayer.totalClimbedDistance - localPlayer.totalClimbedDistance);

        if (activeOpponentTargetImage != null)
        {
            if (heightDifference <= attackVerticalRange)
            {
                activeOpponentTargetImage.color = targetMatchColor; 
            }
            else
            {
                activeOpponentTargetImage.color = originalTargetColor; 
            }
        }
    }
}