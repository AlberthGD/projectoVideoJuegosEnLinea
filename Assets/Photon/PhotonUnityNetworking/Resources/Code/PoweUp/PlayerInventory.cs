using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections; 

public class PlayerInventory : MonoBehaviourPun
{
    [Header("Item Guardado")]
    public TipoItem itemActual = TipoItem.Ninguno;

    [Header("Icono UI")]
    public Sprite iconoProyectil;
    public Sprite iconoEscudo;
    public Sprite iconoMultiplicador;

    [Header("Configuración PowerUps")]
    public GameObject escudoVisual; 
    public string nombrePrefabProyectil = "ProyectilRival"; 
    public float duracionPowerUp = 10f;
    public float multiplicadorVelocidad = 2f;

    [HideInInspector] public bool tieneEscudoActivo = false;
    [HideInInspector] public bool powerUpEnUso = false; 
    
    private Image iconoUI; 
    private PlayerMovement miMovimiento;

    void Start()
    {
        if (photonView.IsMine)
        {
            miMovimiento = GetComponent<PlayerMovement>();
            bool soyJ1 = PhotonNetwork.IsMasterClient;
            
            string nombreMiIcono = soyJ1 ? "IconoItemUI_J1" : "IconoItemUI_J2";
            GameObject miIconoEnEscena = GameObject.Find(nombreMiIcono);
            
            if (miIconoEnEscena != null)
            {
                iconoUI = miIconoEnEscena.GetComponent<Image>();
                ActualizarUI();
            }

            string nombreIconoRival = soyJ1 ? "IconoItemUI_J2" : "IconoItemUI_J1";
            GameObject iconoRivalEnEscena = GameObject.Find(nombreIconoRival);
            
            if (iconoRivalEnEscena != null)
            {
                iconoRivalEnEscena.SetActive(false);
            }
        }

        if (escudoVisual != null) escudoVisual.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Space) && itemActual != TipoItem.Ninguno && !powerUpEnUso)
        {
            UsarPowerUp(itemActual);
        }
    }

    public void RecogerItem(TipoItem nuevoItem)
    {
        if (!photonView.IsMine) return;

        itemActual = nuevoItem;
        Debug.Log("¡Recogiste un: " + itemActual.ToString() + "!");
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (iconoUI == null) return;

        if (itemActual == TipoItem.Ninguno)
        {
            iconoUI.enabled = false;
            return;
        }

        iconoUI.enabled = true;

        switch (itemActual)
        {
            case TipoItem.Proyectil:
                iconoUI.sprite = iconoProyectil;
                break;
            case TipoItem.Escudo:
                iconoUI.sprite = iconoEscudo;
                break;
            case TipoItem.Multiplicador:
                iconoUI.sprite = iconoMultiplicador;
                break;
        }
    }

    private void UsarPowerUp(TipoItem item)
    {
        switch (item)
        {
            case TipoItem.Escudo:
                StartCoroutine(RutinaEscudo());
                break;
            case TipoItem.Multiplicador:
                StartCoroutine(RutinaMultiplicador());
                break;
            case TipoItem.Proyectil:
                LanzarProyectilARival(); 
                break;
        }

        itemActual = TipoItem.Ninguno;
        ActualizarUI();
    }

    private IEnumerator RutinaEscudo()
    {
        powerUpEnUso = true; 
        tieneEscudoActivo = true;
        photonView.RPC("RPC_ToggleEscudo", RpcTarget.All, true);

        yield return new WaitForSeconds(duracionPowerUp);

        tieneEscudoActivo = false;
        photonView.RPC("RPC_ToggleEscudo", RpcTarget.All, false);
        powerUpEnUso = false;
    }

    [PunRPC]
    private void RPC_ToggleEscudo(bool estado)
    {
        if (escudoVisual != null) escudoVisual.SetActive(estado);
    }

    private IEnumerator RutinaMultiplicador()
    {
        powerUpEnUso = true;

        PlayerStats misStats = GetComponent<PlayerStats>();
        if (misStats != null)
        {
            misStats.IniciarEfectoMultiplicador(duracionPowerUp);
        }

        yield return new WaitForSeconds(duracionPowerUp);

        powerUpEnUso = false; 
    }

    private void LanzarProyectilARival()
    {
        PlayerMovement[] todosLosPlayers = FindObjectsOfType<PlayerMovement>();
        Transform rivalTransform = null;

        foreach (PlayerMovement p in todosLosPlayers)
        {
            if (!p.photonView.IsMine) 
            {
                rivalTransform = p.transform;
                break;
            }
        }

        if (rivalTransform != null)
        {
            Vector3 spawnPos = rivalTransform.position + new Vector3(0, 8f, 0); 
            PhotonNetwork.Instantiate(nombrePrefabProyectil, spawnPos, Quaternion.identity);
        }
    }
}