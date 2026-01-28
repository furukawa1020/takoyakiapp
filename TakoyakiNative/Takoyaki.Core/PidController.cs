using System;

namespace Takoyaki.Core
{
    public class PidController
    {
        public float Kp { get; set; }
        public float Ki { get; set; }
        public float Kd { get; set; }

        private float _lastError;
        private float _integral;

        public PidController(float kp, float ki, float kd)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
        }

        public float Update(float setPoint, float actualValue, float dt)
        {
            if (dt <= 0) return 0;

            float error = setPoint - actualValue;
            _integral += error * dt;
            float derivative = (error - _lastError) / dt;

            _lastError = error;

            return (Kp * error) + (Ki * _integral) + (Kd * derivative);
        }

        public void Reset()
        {
            _lastError = 0;
            _integral = 0;
        }
    }
}
