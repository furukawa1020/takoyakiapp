using Android.Content;
using Android.Media;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public class TakoyakiAudio
    {
        private SoundPool _soundPool;
        private int _sizzleSoundId;
        private int _tapSoundId;
        private int _sizzleStreamId;

        private float _currentSizzleVolume = 0f;

        public TakoyakiAudio(Context context)
        {
            var attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Game)
                .SetContentType(AudioContentType.Sonification)
                .Build();

            _soundPool = new SoundPool.Builder()
                .SetAudioAttributes(attributes)
                .SetMaxStreams(5)
                .Build();

            // Load assets (Assuming files exist in Assets, transparently handled by Xamarin/Maui usually via AssetManager, 
            // but SoundPool.Load(Context, ResId, Priority) is easier if mapped to Resources/raw)
            // For this prototype, we'll try to load from Assets via AssetFileDescriptor
            
            // To prevent crashes if files miss, we wrap in try/catch or just assume scaffolding
            // In a real scenario: context.Assets.OpenFd("sizzle.wav");
            
            // _sizzleSoundId = LoadSound(context, "sizzle.wav");
            // _tapSoundId = LoadSound(context, "tap.wav");
        }

        private int LoadSound(Context context, string filename)
        {
            try
            {
                var afd = context.Assets.OpenFd(filename);
                return _soundPool.Load(afd, 1);
            }
            catch
            {
                return 0; // Fail silently
            }
        }

        public void PlayTap()
        {
            if (_tapSoundId != 0)
            {
                // Play with random pitch for variety
                float pitch = 0.9f + (float)new System.Random().NextDouble() * 0.2f;
                _soundPool.Play(_tapSoundId, 1f, 1f, 1, 0, pitch);
            }
        }

        public void UpdateSizzle(float cookLevel, bool isCooking)
        {
            if (_sizzleSoundId == 0) return;

            if (isCooking)
            {
                float targetVol = 0f;
                if (cookLevel < 0.2f) targetVol = 0.2f; // Initial hiss
                else if (cookLevel < 0.8f) targetVol = 1.0f; // Roaring boil
                else targetVol = 0.5f; // Drying out

                // Simple Lerp
                _currentSizzleVolume = _currentSizzleVolume * 0.9f + targetVol * 0.1f;

                if (_sizzleStreamId == 0)
                {
                    _sizzleStreamId = _soundPool.Play(_sizzleSoundId, _currentSizzleVolume, _currentSizzleVolume, 1, -1, 1.0f);
                }
                else
                {
                    _soundPool.SetVolume(_sizzleStreamId, _currentSizzleVolume, _currentSizzleVolume);
                }
            }
            else
            {
                if (_sizzleStreamId != 0)
                {
                    _soundPool.Stop(_sizzleStreamId);
                    _sizzleStreamId = 0;
                }
            }
        }

        public void Release()
        {
            _soundPool.Release();
            _soundPool = null;
        }
    }
}
