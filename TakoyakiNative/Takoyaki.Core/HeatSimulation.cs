using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class HeatSimulation
    {
        // Thermal Properties
        private const float ThermalConductivity = 0.4f; // Heat spread within ball
        private const float SurfaceHeatTransfer = 2.5f; // Heat from pan to surface
        private const float AirCoolingRate = 0.5f;

        private TakoyakiBall _ball;

        public HeatSimulation(TakoyakiBall ball)
        {
            _ball = ball;
        }

        public void Update(float dt, float panTemperature)
        {
            // 1. Calculate Heat Input based on Orientation
            // Ensure we know which side is touching the pan.
            // Pan is at World Y=0, Normal=Up(0,1,0).
            // We transform World Down (0,-1,0) into Local Space to see which local side is bottom.
            
            Quaternion invRot = Quaternion.Inverse(_ball.Rotation);
            Vector3 localDown = Vector3.Transform(new Vector3(0, -1, 0), invRot);

            // Directions corresponding to the 6 faces of the Heatmap
            // 0:Up, 1:Down, 2:Left, 3:Right, 4:Fwd, 5:Back
            Vector3[] directions = new Vector3[]
            {
                new Vector3(0, 1, 0),  // Up
                new Vector3(0, -1, 0), // Down
                new Vector3(-1, 0, 0), // Left
                new Vector3(1, 0, 0),  // Right
                new Vector3(0, 0, 1),  // Fwd
                new Vector3(0, 0, -1)  // Back
            };

            for (int i = 0; i < 6; i++)
            {
                // Dot product determines how close this face is to the bottom (Heat Source)
                float alignment = Vector3.Dot(localDown, directions[i]);
                
                // If aligned > 0, it's facing somewhat down. 
                // alignment=1 means it's directly at the bottom (Max heat).
                // If _ball.IsInHole is false, heat is much lower (air).
                
                float externalTemp = Constants.AmbientTemperature;
                float transferRate = AirCoolingRate;

                if (_ball.IsInHole && alignment > 0.5f) // Contact patch
                {
                    externalTemp = panTemperature;
                    // Scale transfer by contact quality (alignment)
                    transferRate = SurfaceHeatTransfer * (alignment - 0.5f) * 2f; 
                }

                // Apply Laws of Cooling/Heating: dT/dt = k * (T_env - T_obj)
                float currentFaceLevel = _ball.SurfaceCookLevels[i];
                
                // Convert Level (0..1) to Temp simulation (simplified)
                // Let's assume Level 1.0 = IdealTemp.
                float currentFaceTemp = MathCode.Lerp(Constants.AmbientTemperature, Constants.IdealTemperature * 1.5f, currentFaceLevel);

                float diff = externalTemp - currentFaceTemp;
                float change = diff * transferRate * dt * 0.01f; // Slow factor

                currentFaceTemp += change;

                // Convert back to Level
                _ball.SurfaceCookLevels[i] = (currentFaceTemp - Constants.AmbientTemperature) / (Constants.IdealTemperature * 1.5f - Constants.AmbientTemperature);
                _ball.SurfaceCookLevels[i] = Math.Max(0f, _ball.SurfaceCookLevels[i]);
            }

            // 2. Thermal Conductivity (Heat spreading between faces)
            // Simplified: Blend neighbors
            float[] newLevels = (float[])_ball.SurfaceCookLevels.Clone();
            float avg = 0;
            foreach (var l in _ball.SurfaceCookLevels) avg += l;
            avg /= 6f;

            for (int i = 0; i < 6; i++)
            {
                // Move towards average (entropy)
                float diff = avg - _ball.SurfaceCookLevels[i];
                newLevels[i] += diff * ThermalConductivity * dt;
            }
            _ball.SurfaceCookLevels = newLevels;

            // 3. Update Aggregate Cook Level
            _ball.CookLevel = avg;
        }
    }
}
