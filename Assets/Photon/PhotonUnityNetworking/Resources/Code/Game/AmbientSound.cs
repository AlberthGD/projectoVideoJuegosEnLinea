using UnityEngine;
using System.Collections;


public class AmbientSound : MonoBehaviour
{
    public AudioClip[] ambientClips;
    public AudioClip lavaSound;
    public AudioClip bubbleSound;


    public float minWaitTime = 5f;
    public float maxWaitTime = 15f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        StartCoroutine(PlayRandomSounds());

        audioSource.clip = lavaSound;
        audioSource.loop = true;
        audioSource.Play();
        audioSource.volume = 0.25f;

        audioSource.clip = bubbleSound;
        audioSource.loop = true;
        audioSource.Play();
        audioSource.volume = 0.25f;

    }

    private void Update()
    {
        
    }

    IEnumerator PlayRandomSounds()
    {
        while (true)
        {
            // Wait random amount of time
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // Pick random clip
            int randomIndex = Random.Range(0, ambientClips.Length);

            // Play it
            audioSource.PlayOneShot(ambientClips[randomIndex]);
        }
    }
}
