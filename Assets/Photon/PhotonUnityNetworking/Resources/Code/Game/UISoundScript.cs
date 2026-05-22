using UnityEngine;
using UnityEngine.UI;


public class UISoundScript : MonoBehaviour
{
    public static UISoundScript instance;

    public AudioClip buttonClickSound;
    public AudioClip HitSoudn;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayButtonSound()
    {
        audioSource.PlayOneShot(buttonClickSound);
    }
}
