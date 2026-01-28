using Android.Content;
using Android.Hardware;
using Android.Runtime;
using System;
using System.Numerics;

namespace Takoyaki.Android
{
    public class TakoyakiSensor : Java.Lang.Object, ISensorEventListener
    {
        private SensorManager _sensorManager;
        private Sensor _accelerometer;
        private Sensor _gyroscope;

        public Vector2 CurrentTilt { get; private set; } // X, Y (-1 to 1)
        public Vector3 CurrentAcceleration { get; private set; }
        public Vector3 CurrentGyroVelo { get; private set; }

        // Simple Low-pass filter
        private float[] _gravity = new float[3];
        private float[] _linear_acceleration = new float[3];
        private const float ALPHA = 0.8f;

        public TakoyakiSensor(Context context)
        {
            _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService);
            _accelerometer = _sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            _gyroscope = _sensorManager.GetDefaultSensor(SensorType.Gyroscope);
        }

        public void Start()
        {
            if (_accelerometer != null)
                _sensorManager.RegisterListener(this, _accelerometer, SensorDelay.Game);
            
            if (_gyroscope != null)
                _sensorManager.RegisterListener(this, _gyroscope, SensorDelay.Game);
        }

        public void Stop()
        {
            _sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            // No-op
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e == null) return;

            if (e.Sensor.Type == SensorType.Gyroscope)
            {
                CurrentGyroVelo = new Vector3(e.Values[0], e.Values[1], e.Values[2]);
            }

            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                // Isolate Gravity vs Linear Acceleration
                _gravity[0] = ALPHA * _gravity[0] + (1 - ALPHA) * e.Values[0];
                _gravity[1] = ALPHA * _gravity[1] + (1 - ALPHA) * e.Values[1];
                _gravity[2] = ALPHA * _gravity[2] + (1 - ALPHA) * e.Values[2];

                _linear_acceleration[0] = e.Values[0] - _gravity[0];
                _linear_acceleration[1] = e.Values[1] - _gravity[1];
                _linear_acceleration[2] = e.Values[2] - _gravity[2];

                CurrentAcceleration = new Vector3(_linear_acceleration[0], _linear_acceleration[1], _linear_acceleration[2]);

                // Calculate Tilt from Gravity
                // Normalize gravity to -1..1 range approximately
                // Gravity is ~9.8
                
                // Phone Orientation:
                // X: Left/Right tilt (Landscape: Up/Down for Takoyaki?)
                // Y: Up/Down tilt
                
                // Assuming Landscape mode typical for games:
                // X axis points up (Short edge) -> Pitch
                // Y axis points right (Long edge) -> Roll
                
                // Let's normalize to +/- 1.0 (approx 45 degrees usually sufficient)
                float x = Math.Clamp(_gravity[0] / 5.0f, -1f, 1f); 
                float y = Math.Clamp(_gravity[1] / 5.0f, -1f, 1f);

                // Mapping depends on Screen Orientation, assuming fixed Landscape for now or simplified Logic
                // For a "Pan", usually holding flat (Z = -9.8).
                // Tilting away (Top goes down) -> Y changes
                // Tilting left/right -> X changes
                
                CurrentTilt = new Vector2(-x, y); 
            }
        }
    }
}
