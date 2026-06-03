using UnityEngine;
using Photon.Pun;

public class RisingLava : MonoBehaviour
{
    [Header("Velocidad en la que sube la lava")]
    public float scaleSpeed = 1f; 

    [Header("Tiempo de espera inicial")]
    public float tiempoEspera = 3f; 
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= tiempoEspera)
        {
            float crecimiento = scaleSpeed * Time.deltaTime;

            transform.localScale += new Vector3(0, crecimiento, 0);
            transform.position += new Vector3(0, crecimiento / 2f, 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Arbol")) return;

        if (other.name.Contains("EslabonFisico")) return;
        
        if (other.name.Contains("RopePivot")) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        
        if (player != null)
        {
            if (player.modoDiosActivo) return;
            
            if (player.photonView.IsMine)
            {
                PlayerTargetHud hud = FindObjectOfType<PlayerTargetHud>();
                if (hud != null)
                {
                    hud.JugadorEliminadoPorLava();
                }
            }
        }
        else
        {
            PhotonView pv = other.GetComponentInParent<PhotonView>();
            
            if (pv != null && pv.InstantiationId > 0)
            {
                if (pv.IsMine)
                {
                    PhotonNetwork.Destroy(pv.gameObject);
                }
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}