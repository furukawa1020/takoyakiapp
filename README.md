# One Ball Soul Takoyaki - Unity Project Guide

## Overview
This project is designed with a "Code-First" approach. Most of the logic is decoupled from the Unity Editor, but there are a few setup steps required to connect the visual assets.

## Quick Setup (Automation)
We have included a custom Editor Tool to set up the scene for you.
1. In Unity, go to the Top Menu bar.
2. Click **Takoyaki > Setup > Create Game Scene**.
3. This will create the Main Camera, Lights, Managers, UI, and the Takoyaki player object with all necessary scripts attached.

## Key Components

### Physics & Controls
- **InputManager**: Handles Gyro and Accelerometer. Call `InputManager.Instance.Calibrate()` to center the gyro.
- **TakoyakiController**: Main physics logic.
- **TakoyakiSoftBody**: **[NEW]** Mass-Spring-Damper system. Simulates inertia, gravity sag, and impact jitter on a per-vertex basis.

### Visuals (Shader)
- **Material Setup**:
    - Create a new Material.
    - Set Shader to **`Takoyaki/TakoyakiCinematic`**.
    - **SSS Settings**: Simulates the translucent batter look. High intensity = raw, Low = cooked.
    - **Oil Settings**: Control the `Fresnel Power` and `Roughness` to get that perfect "wet glaze" look.
    - **Displacement**: Uses the noise map to "puff up" the surface as it cooks.

### Game Loop
- **GameManager**: Controls the state (Title -> Pouring -> Cooking -> Result).
- **ScoreManager**: Tracks the shape quality and cooking level.

## Folder Structure
- `Scripts/`: C# Logic
- `Scripts/Visuals/`: Shaders and visual controllers
- `Editor/`: Automation tools
