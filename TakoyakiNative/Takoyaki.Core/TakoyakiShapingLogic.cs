using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class TakoyakiShapingLogic : IDisposable
    {
        private PidController _pid;
        private IntPtr _rustEngine = IntPtr.Zero;
        private bool _useRust = false;

        public float ShapingProgress { get; private set; } = 1.0f; 
        public float MasteryLevel { get; private set; } = 0.0f;
        public float RhythmPulse { get; private set; } = 0.0f;
        public int ComboCount { get; private set; } = 0;
        public bool IsPerfect { get; private set; } = false;
        public bool TriggerHapticTick { get; set; } = false; 
        public IntPtr NativeEngine => _rustEngine;
        public bool IsNativeEnabled => _useRust;

        public float P_Term { get; private set; }
        public float I_Term { get; private set; }
        public float D_Term { get; private set; }
        
        public const float TARGET_GYRO_MAG = 6.0f;
        private const float SHAPING_SPEED = 0.4f;
        private float _pulseTimer = 0.0f;
        private float _stabilityTimer = 0.0f;

        // --- Native Rust Interface ---
        [System.Runtime.InteropServices.DllImport("takores")]
        private static extern IntPtr tako_init();
        [System.Runtime.InteropServices.DllImport("takores")]
        private static extern float tako_update(IntPtr engine, float gx, float gy, float gz, float dt, float target);
        [System.Runtime.InteropServices.DllImport("takores")]
        private static extern float tako_get_mastery(IntPtr engine);
        [System.Runtime.InteropServices.DllImport("takores")]
        private static extern void tako_free(IntPtr engine);
        [System.Runtime.InteropServices.DllImport("takores")]
        private unsafe static extern void tako_smooth_mesh(IntPtr engine, Vector3* vertices, Vector3* base_vertices, int count, float dt);
        [System.Runtime.InteropServices.DllImport("takores")]
        public unsafe static extern void tako_step_physics(IntPtr engine, void* states, int count, void* params_ptr, void* g, void* a, float dt);
        [System.Runtime.InteropServices.DllImport("takores")]
        private unsafe static extern void tako_get_pid_terms(IntPtr engine, float* p, float* i, float* d);

        public TakoyakiShapingLogic()
        {
            _pid = new PidController(1.2f, 0.3f, 0.15f); 
            
            try {
                _rustEngine = tako_init();
                _useRust = (_rustEngine != IntPtr.Zero);
            } catch {
                _useRust = false;
            }
        }

        public void Update(float dt, Vector3 angularVelocity)
        {
            float currentMag;
            float pidOutput;
            
            if (_useRust) {
                // High-performance Rust path (Kalman + PID)
                currentMag = tako_update(_rustEngine, angularVelocity.X, angularVelocity.Y, angularVelocity.Z, dt, TARGET_GYRO_MAG);
                MasteryLevel = tako_get_mastery(_rustEngine);
                
                unsafe {
                    float p, i, d;
                    tako_get_pid_terms(_rustEngine, &p, &i, &d);
                    P_Term = p; I_Term = i; D_Term = d;
                }
                
                // For the HUD Analyzer, we use the residual error to show activity
                pidOutput = TARGET_GYRO_MAG - currentMag; 
            }
            else
            {
                // Standard C# Path
                currentMag = angularVelocity.Length();
                pidOutput = _pid.Update(TARGET_GYRO_MAG, currentMag, dt);
                P_Term = _pid.P_Contribution;
                I_Term = _pid.I_Contribution;
                D_Term = _pid.D_Contribution;
            }
            
            _pulseTimer += dt * (TARGET_GYRO_MAG / (float)Math.PI); 
            float lastPulse = RhythmPulse;
            RhythmPulse = (float)Math.Abs(Math.Sin(_pulseTimer)); 

            if (RhythmPulse > 0.9f && lastPulse <= 0.9f && currentMag > 1.0f) {
                TriggerHapticTick = true;
            }

            if (currentMag > 2.0f)
            {
                float harmony = Math.Clamp(1.0f - Math.Abs(pidOutput) / TARGET_GYRO_MAG, 0.0f, 1.0f);
                IsPerfect = harmony > 0.85f;

                if (IsPerfect) {
                    _stabilityTimer += dt;
                    if (_stabilityTimer > 0.5f) {
                        ComboCount++;
                        _stabilityTimer = 0.0f;
                    }
                    MasteryLevel = Math.Min(1.0f, MasteryLevel + dt * 0.5f);
                } else {
                    _stabilityTimer = 0.0f;
                    if (harmony < 0.5f) ComboCount = 0;
                    MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 0.2f);
                }

                float pressure = harmony * (1.0f + MasteryLevel * 2.0f); 
                ShapingProgress = Math.Max(0.0f, ShapingProgress - pressure * SHAPING_SPEED * dt);
            }
            else
            {
                ComboCount = 0;
                MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 1.0f);
                ShapingProgress = Math.Min(1.0f, ShapingProgress + dt * 0.05f);
            }
        }

        public unsafe void ApplyNativeMeshShaping(Vector3[] deformed, Vector3[] original, float dt)
        {
            if (!_useRust || _rustEngine == IntPtr.Zero) return;

            fixed (Vector3* pDeformed = deformed)
            fixed (Vector3* pOriginal = original)
            {
                tako_smooth_mesh(_rustEngine, pDeformed, pOriginal, deformed.Length, dt);
            }
        }

        public void Dispose()
        {
            if (_rustEngine != IntPtr.Zero) {
                tako_free(_rustEngine);
                _rustEngine = IntPtr.Zero;
            }
        }
    }
}
