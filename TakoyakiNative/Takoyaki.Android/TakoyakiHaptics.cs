using Android.Content;
using Android.OS;
using Android.Media;

namespace Takoyaki.Android
{
    public class TakoyakiHaptics
    {
        private Vibrator _vibrator;
        private VibratorManager _vibratorManager; // Android 12+

        public TakoyakiHaptics(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                _vibratorManager = (VibratorManager)context.GetSystemService(Context.VibratorManagerService);
                _vibrator = _vibratorManager.DefaultVibrator;
            }
            else
            {
                _vibrator = (Vibrator)context.GetSystemService(Context.VibratorService);
            }
        }

        public void TriggerImpact(float intensity)
        {
            if (!_vibrator.HasVibrator) return;

            // Map intensity 0..1 to milliseconds or amplitude
            long duration = (long)(intensity * 50); // 10ms to 50ms
            int amplitude = (int)(intensity * 255);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // Crisp click effect
                var effect = VibrationEffect.CreateOneShot(duration, amplitude);
                _vibrator.Vibrate(effect);
            }
            else
            {
                _vibrator.Vibrate(duration);
            }
        }
        
        public void TriggerRolling()
        {
            // Subtle texture vibration
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                var effect = VibrationEffect.CreatePredefined(VibrationEffect.EffectTick);
                _vibrator.Vibrate(effect);
            }
        }
    }
}
