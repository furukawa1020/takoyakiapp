using UnityEngine;

namespace TakoyakiPhysics.Feedback
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        public AudioClip sizzleSound;
        public AudioClip turnSound;
        public AudioClip pourSound;
        public AudioClip perfectSound;
        public AudioClip burnSound;

        private AudioSource _audioSource;
        private AudioSource _sizzleSource;
        private bool _isSizzling = false;

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
                return;
            }

            _audioSource = GetComponent<AudioSource>();
            
            // Create a second audio source for continuous sizzle
            GameObject sizzleObj = new GameObject("SizzleAudioSource");
            sizzleObj.transform.SetParent(transform);
            _sizzleSource = sizzleObj.AddComponent<AudioSource>();
            _sizzleSource.loop = true;
            _sizzleSource.volume = 0.5f;
            
            GenerateProceduralSounds();
        }

        private void GenerateProceduralSounds()
        {
            // Initialize random with a seed for consistent audio generation
            Random.InitState(12345);
            
            // Generate simple procedural sounds if clips are not assigned
            if (sizzleSound == null)
            {
                sizzleSound = GenerateWhiteNoise(2.0f, 0.3f);
            }
            if (turnSound == null)
            {
                turnSound = GenerateTone(0.1f, 440f, 0.5f);
            }
            if (pourSound == null)
            {
                pourSound = GenerateTone(0.3f, 220f, 0.3f);
            }
            if (perfectSound == null)
            {
                perfectSound = GenerateTone(0.5f, 880f, 0.6f);
            }
            if (burnSound == null)
            {
                burnSound = GenerateWhiteNoise(0.8f, 0.5f);
            }
            
            // Reset random state after generation
            Random.InitState(System.Environment.TickCount);
        }

        private AudioClip GenerateWhiteNoise(float duration, float volume)
        {
            int sampleRate = 22050;
            int samples = (int)(duration * sampleRate);
            AudioClip clip = AudioClip.Create("WhiteNoise", samples, 1, sampleRate, false);
            
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                data[i] = (Random.value * 2f - 1f) * volume;
            }
            
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateTone(float duration, float frequency, float volume)
        {
            int sampleRate = 22050;
            int samples = (int)(duration * sampleRate);
            AudioClip clip = AudioClip.Create("Tone", samples, 1, sampleRate, false);
            
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume;
                // Apply envelope for smooth start/end
                float envelope = 1f;
                if (i < sampleRate * 0.01f) // Fade in
                    envelope = (float)i / (sampleRate * 0.01f);
                else if (i > samples - sampleRate * 0.01f) // Fade out
                    envelope = (float)(samples - i) / (sampleRate * 0.01f);
                data[i] *= envelope;
            }
            
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayPour()
        {
            if (pourSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(pourSound);
            }
        }

        public void PlayTurn()
        {
            if (turnSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(turnSound);
            }
        }

        public void PlayPerfect()
        {
            if (perfectSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(perfectSound);
            }
        }

        public void PlayBurn()
        {
            if (burnSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(burnSound);
            }
        }

        public void StartSizzle(float intensity = 0.5f)
        {
            if (_sizzleSource == null) return;

            if (!_isSizzling)
            {
                if (sizzleSound != null)
                {
                    _sizzleSource.clip = sizzleSound;
                    _sizzleSource.Play();
                    _isSizzling = true;
                }
            }
            
            _sizzleSource.volume = Mathf.Clamp01(intensity);
        }

        public void StopSizzle()
        {
            if (_sizzleSource != null && _isSizzling)
            {
                _sizzleSource.Stop();
                _isSizzling = false;
            }
        }

        public void PlayOneShot(AudioClip clip, float volume = 1.0f)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }

        public void PlayToppingSound()
        {
            // Play a pleasant chime/ding for adding toppings
            if (perfectSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(perfectSound, 0.5f);
            }
        }
    }
}
