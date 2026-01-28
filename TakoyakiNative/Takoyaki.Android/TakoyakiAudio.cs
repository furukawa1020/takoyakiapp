using Android.Content;
using Android.Media;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public class TakoyakiAudio : Takoyaki.Core.ITakoyakiAudio
    {
        private SoundPool _soundPool;
        private int _sizzleSoundId;
        private int _tapSoundId;
        private int _sizzleStreamId;

        private float _currentSizzleVolume = 0f;

        private ToneGenerator _toneGen;
        private int _dingSoundId;
        private int _turnSoundId;
        private int _serveSoundId;

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
            
            _toneGen = new ToneGenerator(global::Android.Media.Stream.Music, 100);

            // Placeholder loads (would fail silently if files missing)
            // _dingSoundId = LoadSound(context, "ding.wav");
            // _turnSoundId = LoadSound(context, "turn.wav");
            // _serveSoundId = LoadSound(context, "serve.wav");
        }

        public void PlayTap()
        {
            if (_tapSoundId != 0)
            {
                float pitch = 0.9f + (float)new System.Random().NextDouble() * 0.2f;
                _soundPool.Play(_tapSoundId, 1f, 1f, 1, 0, pitch);
            }
        }

        public void PlayDing()
        {
            if (_dingSoundId != 0) _soundPool.Play(_dingSoundId, 1f, 1f, 1, 0, 1.0f);
            else _toneGen.StartTone(Tone.CdmaAlertCallGuard, 150); // High pitch beep
        }

        public void PlayTurn()
        {
            if (_turnSoundId != 0) _soundPool.Play(_turnSoundId, 1f, 1f, 1, 0, 1.0f);
            else _toneGen.StartTone(Tone.PropBeep, 50); // Short blip
        }

        public void PlayServe()
        {
            if (_serveSoundId != 0) _soundPool.Play(_serveSoundId, 1f, 1f, 1, 0, 1.0f);
            else _toneGen.StartTone(Tone.CdmaConfirm, 300); // Longer confirmation
        }

        public void PlayChime()
        {
            // High-pitched crystal-clear chime for mastery beat
            _toneGen.StartTone(Tone.SupHighAck, 30); 
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
