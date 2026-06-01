using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro; 

public class PlayerStats : MonoBehaviourPun
{
    public static PlayerStats LocalInstance; 

    [Header("Puntuación")]
    public int score = 0;
    [HideInInspector] public TextMeshProUGUI scoreText; 
    
    private Color originalScoreColor;
    private Coroutine rutinaScoreFlash;
    
    [HideInInspector] public bool multiplicadorActivo = false; 

    [Header("Efecto Visual (Daño y PowerUp)")]
    public Renderer[] playerRenderers; 
    public Material materialDeDano; 
    public Material materialMultiplicador; 

    private Material[][] originalMaterials; 
    private Coroutine rutinaDeDano; 

    void Awake()
    {
        if (photonView.IsMine)
        {
            LocalInstance = this;
        }
    }

    void Start()
    {
        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            originalMaterials = new Material[playerRenderers.Length][];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    originalMaterials[i] = playerRenderers[i].materials; 
                }
            }
        }

        if (photonView.IsMine)
        {
            bool soyJ1 = PhotonNetwork.IsMasterClient;
            
            string nombreMiTexto = soyJ1 ? "TextoPuntajeJ1" : "TextoPuntajeJ2";
            GameObject miTextoEnEscena = GameObject.Find(nombreMiTexto); 
            
            if (miTextoEnEscena != null)
            {
                scoreText = miTextoEnEscena.GetComponent<TextMeshProUGUI>();
                originalScoreColor = scoreText.color; 
                UpdateScoreUI(); 
            }

            string nombreOtroTexto = soyJ1 ? "TextoPuntajeJ2" : "TextoPuntajeJ1";
            GameObject otroTextoEnEscena = GameObject.Find(nombreOtroTexto);
            
            if (otroTextoEnEscena != null)
            {
                if (otroTextoEnEscena.transform.parent != null)
                {
                    otroTextoEnEscena.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    otroTextoEnEscena.SetActive(false);
                }
            }
        }
    }

    public void AddScore(int points)
    {
        if (!photonView.IsMine) return;
        
        if (multiplicadorActivo) points *= 2; 
        
        score += points;
        UpdateScoreUI(); 
        UpdateNetworkScore();
    }

    public void TakeDamage(int penalty)
    {
        if (!photonView.IsMine) return;

        PlayerInventory miInventario = GetComponent<PlayerInventory>();
        if (miInventario != null && miInventario.tieneEscudoActivo)
        {
            return; 
        }

        score -= penalty;
        if (score < 0) score = 0; 
        
        UpdateScoreUI(); 
        UpdateNetworkScore();

        if (rutinaScoreFlash != null)
        {
            StopCoroutine(rutinaScoreFlash);
            if (scoreText != null) scoreText.color = multiplicadorActivo ? Color.blue : originalScoreColor; 
        }
        rutinaScoreFlash = StartCoroutine(RutinaFlashPuntaje());

        photonView.RPC("ActivarColorRojoRPC", RpcTarget.All);
    }

    public void IniciarEfectoMultiplicador(float duracion)
    {
        if (!photonView.IsMine) return;
        StartCoroutine(RutinaColorAzulMultiplicador(duracion));
    }

    private IEnumerator RutinaColorAzulMultiplicador(float duracion)
    {
        multiplicadorActivo = true;
        if (scoreText != null) scoreText.color = Color.blue;
        
        photonView.RPC("RPC_ToggleColorAzul", RpcTarget.All, true);

        yield return new WaitForSeconds(duracion);
        
        multiplicadorActivo = false;
        if (scoreText != null) scoreText.color = originalScoreColor;

        photonView.RPC("RPC_ToggleColorAzul", RpcTarget.All, false);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "PUNTOS: " + score;
        }
    }

    private IEnumerator RutinaFlashPuntaje()
    {
        if (scoreText != null)
        {
            scoreText.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            scoreText.color = multiplicadorActivo ? Color.blue : originalScoreColor;
        }
        rutinaScoreFlash = null;
    }

    private void UpdateNetworkScore()
    {
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
        hash.Add("Score", score);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    [PunRPC]
    public void RPC_ToggleColorAzul(bool estado)
    {
        if (rutinaDeDano != null)
        {
            StopCoroutine(rutinaDeDano);
            rutinaDeDano = null;
        }

        if (estado)
        {
            AplicarMaterial(materialMultiplicador);
        }
        else
        {
            RestaurarMaterialOriginal();
        }
    }

    [PunRPC]
    public void ActivarColorRojoRPC()
    {
        if (rutinaDeDano != null)
        {
            StopCoroutine(rutinaDeDano);
            RestaurarMaterialOriginal(); 
        }

        rutinaDeDano = StartCoroutine(RutinaCambioDeColor());
    }

    private IEnumerator RutinaCambioDeColor()
    {
        int cantidadParpadeos = 4; 
        float tiempoRojo = 0.15f;  
        float tiempoNormal = 0.1f; 

        for (int i = 0; i < cantidadParpadeos; i++)
        {
            AplicarMaterial(materialDeDano); 
            yield return new WaitForSeconds(tiempoRojo);

            if (multiplicadorActivo) AplicarMaterial(materialMultiplicador);
            else RestaurarMaterialOriginal();
            
            yield return new WaitForSeconds(tiempoNormal);
        }

        rutinaDeDano = null; 
    }

    private void AplicarMaterial(Material nuevoMat)
    {
        if (nuevoMat != null)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    Material[] nuevosMats = new Material[playerRenderers[i].materials.Length];
                    for(int j = 0; j < nuevosMats.Length; j++) 
                    {
                        nuevosMats[j] = nuevoMat;
                    }
                    playerRenderers[i].materials = nuevosMats;
                }
            }
        }
    }

    private void RestaurarMaterialOriginal()
    {
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null && originalMaterials[i] != null)
            {
                playerRenderers[i].materials = originalMaterials[i];
            }
        }
    }
}