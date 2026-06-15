# Isometric Pixel Game

An old project playing with 2D procedurally generated environments. RPG-inspired world exploration with dynamic map generation.
Everything is hand-drawn.
 
---
 
## Features
 
- Procedural map generation at runtime.
- Isometric pixel-art rendering with custom lighting system
- Movement, item throwing, tree chopping, torch placement
- 2 weapons: gun and daggers
- Multiplayer via Unity Netcode for GameObjects *(Some bugs may appear)*

## Custom Lighting
 
Unity's built-in 2D lighting doesn't support perspective projection, which makes it unusable for isometric games. The lighting system here is built from scratch using custom shaders. Lights cast properly along the isometric perspective instead of flat on the screen plane.
 
## Editor
 
Unity 2021.3.45f2

## Run
 
Open the project in Unity 2021.3.45f2 and hit Play. Multiplayer is experimental and may not work reliably.