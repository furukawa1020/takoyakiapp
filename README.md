# One Ball Soul Takoyaki - Unity Project Guide

## Overview
This project is designed with a "Code-First" approach. Most of the logic is decoupled from the Unity Editor, but there are a few setup steps required to connect the visual assets.

## Quick Setup (Automation)
We have included a custom Editor Tool to set up the scene for you.
1. In Unity, go to the Top Menu bar.
2. Click **Takoyaki > Setup > Create Game Scene**.
3. This will create the Main Camera, Lights, Managers, UI, and the Takoyaki player object with all necessary scripts attached.

## Key Components

### physics & Controls
- **InputManager**: Handles Gyro and Accelerometer. Call `InputManager.Instance.Calibrate()` to center the gyro.
- **TakoyakiController**: Main physics logic.
- **SoftBodyJiggle**: Simulates the "gooey" internal physics using perlin noise vertex displacement.

### Visuals (Shader)
- **Material Setup**:
    - Create a new Material (e.g., `Mat_Takoyaki`).
    - Set Shader to `Takoyaki/TakoyakiSurface`.
    - Assign Textures:
        - `Raw Batter Texture`: Creamy white/yellow noise.
        - `Cooked Texture`: Golden brown baked texture.
        - `Burnt Texture`: Black/Dark brown charred texture.
        - `Noise Map`: Any grayscale noise texture (for uneven cooking).

### Game Loop
- **GameManager**: Controls the state (Title -> Pouring -> Cooking -> Result).
- **ScoreManager**: Tracks the shape quality and cooking level.

## Folder Structure
- `Scripts/`: C# Logic
- `Scripts/Visuals/`: Shaders and visual controllers
- `Editor/`: Automation tools
