using UnityEngine;
using Photon.Pun;

public class CoinCollision : MonoBehaviourPun
{
    [Header("Valor de la Moneda")]
    public int puntosPorMoneda = 10;
    [Header("Rotacion de la Moneda")]
    public Vector3 velocidadRotacion = new Vector3(0f, 150f, 0f);
    
    private bool fueRecogida = false;

    void Update()
    {
        transform.Rotate(velocidadRotacion * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (fueRecogida) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        
        if (stats != null)
        {
            if (stats.photonView.IsMine)
            {
                fueRecogida = true;
                stats.AddScore(puntosPorMoneda); 
                
                photonView.RPC("DestruirMonedaEnRed", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void DestruirMonedaEnRed()
    {
        fueRecogida = true;
        
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}