using UnityEngine;
using Photon.Pun;

public class ObstacleCollision : MonoBehaviourPun
{
    [HideInInspector] public bool hasHitPlayer = false;
    [HideInInspector] public bool wasPunched = false; 

    void OnTriggerEnter(Collider other)
    {
        if (hasHitPlayer || wasPunched) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        
        if (stats != null)
        {
            if (stats.photonView.IsMine)
            {
                stats.TakeDamage(10); 
                photonView.RPC("RegistrarChoqueYDestruir", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void RegistrarChoqueYDestruir()
    {
        hasHitPlayer = true; 

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public void RecibirPunetazoRPC()
    {
        wasPunched = true;
        hasHitPlayer = true; 

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}