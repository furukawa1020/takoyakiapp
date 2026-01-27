using UnityEngine;

namespace TakoyakiPhysics.Feedback
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource loopSource; // For sizzling
        
        [Header("Clips")]
        public AudioClip pourClip;
        public AudioClip sizzleLoop;
        public AudioClip turnClip;
        public AudioClip serveClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (loopSource == null) loopSource = gameObject.AddComponent<AudioSource>();
        }

        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip != null) sfxSource.PlayOneShot(clip, volume);
        }

        public void StartSizzle(float intensity)
        {
            if (sizzleLoop != null && !loopSource.isPlaying)
            {
                loopSource.clip = sizzleLoop;
                loopSource.loop = true;
                loopSource.Play();
            }
            loopSource.volume = Mathf.Clamp01(intensity);
        }

        public void StopSizzle()
        {
            loopSource.Stop();
        }
    }
}
