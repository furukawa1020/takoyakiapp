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

        public void StartPouring() 
        {
            // Placeholder for pouring sound
            // In a real app, use a liquid sound
             if (pourClip != null) 
             {
                 sfxSource.PlayOneShot(pourClip);
             }
             else 
             {
                 // Haptic fallback
                 HapticManager.Instance?.TriggerLightImpact();
             }
        }
    
        public void PlayToppingSound()
        {
            // Procedural "Pop" / "Shake" sound
            if (sfxSource != null)
            {
                // Create a temporary clip for a "Pop"
                AudioClip clip = AudioClip.Create("Pop", 441, 1, 44100, false);
                float[] data = new float[441]; // 0.01 sec very short
                for (int i = 0; i < data.Length; i++)
                {
                    float t = (float)i / 44100f;
                    data[i] = Mathf.Sin(2 * Mathf.PI * 800 * t) * (1f - (float)i/data.Length);
                }
                clip.SetData(data, 0);
                sfxSource.PlayOneShot(clip, 0.5f);
            }
        }

        public void StopPouring() { }
    }
}
