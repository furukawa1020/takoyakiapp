using UnityEngine;

namespace TakoyakiPhysics.Feedback
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sizzleSource;
        [SerializeField] private AudioSource ambientSource;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip sizzleClip;
        [SerializeField] private AudioClip turnClip;
        [SerializeField] private AudioClip perfectClip;
        [SerializeField] private AudioClip burntClip;
        
        private float _targetSizzleVolume = 0f;
        private bool _isSizzling = false;
        private bool _isPendingStop = false;

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
            
            // Create audio sources if they don't exist
            if (sizzleSource == null)
            {
                GameObject sizzleObj = new GameObject("SizzleSource");
                sizzleObj.transform.SetParent(transform);
                sizzleSource = sizzleObj.AddComponent<AudioSource>();
                sizzleSource.loop = true;
                sizzleSource.volume = 0f;
                sizzleSource.playOnAwake = false;
            }
            
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.volume = 0.3f;
                ambientSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            // Smoothly adjust sizzle volume
            if (sizzleSource != null)
            {
                sizzleSource.volume = Mathf.Lerp(sizzleSource.volume, _targetSizzleVolume, Time.deltaTime * 5f);
                
                // Auto-stop when faded out
                if (_isPendingStop && sizzleSource.volume < 0.01f)
                {
                    sizzleSource.Stop();
                    _isPendingStop = false;
                }
            }
        }

        public void StartSizzle(float intensity = 1f)
        {
            if (sizzleSource == null) return;
            
            _targetSizzleVolume = Mathf.Clamp01(intensity);
            
            if (!_isSizzling)
            {
                if (sizzleClip != null)
                {
                    sizzleSource.clip = sizzleClip;
                }
                sizzleSource.Play();
                _isSizzling = true;
            }
        }

        public void StopSizzle()
        {
            _targetSizzleVolume = 0f;
            
            if (_isSizzling)
            {
                // Mark for auto-stop when volume fades out
                _isPendingStop = true;
                _isSizzling = false;
            }
        }

        public void PlayTurn()
        {
            if (turnClip != null && sizzleSource != null)
            {
                sizzleSource.PlayOneShot(turnClip, 0.8f);
            }
        }

        public void PlayPerfect()
        {
            if (perfectClip != null && ambientSource != null)
            {
                ambientSource.PlayOneShot(perfectClip, 1f);
            }
        }

        public void PlayBurnt()
        {
            if (burntClip != null && sizzleSource != null)
            {
                sizzleSource.PlayOneShot(burntClip, 1f);
            }
        }
        
        public void SetAmbientVolume(float volume)
        {
            if (ambientSource != null)
            {
                ambientSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}
