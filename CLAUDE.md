# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6000.0.48f1 project implementing **Schottentotten**, a card-based strategy game. The project uses Photon PUN2 for multiplayer networking and includes both local AI and online multiplayer gameplay modes.

## Core Architecture

### Main Game Systems

- **Game Controller**: `Assets/EasyCardGame/Scripts/Game.cs` - Central game manager that orchestrates all gameplay
- **Card System**: `Assets/EasyCardGame/Scripts/Card.cs` - Individual card logic with health, attack, and special abilities
- **Layout System**: `Assets/EasyCardGame/Scripts/Layouts/` - Manages card placement areas (decks, tables, graveyard)
- **Player System**: `Assets/EasyCardGame/Scripts/Players/` - Handles LocalPlayer, AIPlayer, and NetworkedPlayer
- **Networking**: `Assets/EasyCardGame/Scripts/Networking/Gateway.cs` - Photon PUN2 integration layer
- **Animation**: `Assets/EasyCardGame/Scripts/Animation/` - Card movement and visual effects system

### Key Directories

- `Assets/EasyCardGame/Scripts/` - Core game logic (120+ C# files)
- `Assets/Photon/` - Photon PUN2 networking framework
- `Assets/Plugins/` - Third-party tools and extensions
- `ProjectSettings/` - Unity project configuration

### Game Flow

1. **Initialization**: Game.cs sets up players, decks, layouts, and networking
2. **Card Distribution**: Players receive cards from their selected decks
3. **Turn-Based Gameplay**: Players alternate placing/targeting cards across multiple rounds
4. **Win Conditions**: Game tracks scores and determines winners after configured rounds

### Networking Architecture

- Uses Photon PUN2 for real-time multiplayer
- Gateway.cs abstracts networking calls for local AI and online play
- Supports both AI opponents and human players
- Room-based matchmaking system

## Development Commands

### Building
```bash
# Unity builds are typically done through the Unity Editor:
# File -> Build Settings -> Build
# Or use Unity's command-line interface for CI/CD
```

### Testing
```bash
# Unity Test Framework is included in the project
# Run tests through: Window -> General -> Test Runner in Unity Editor
```

### Package Management
- Unity packages managed via `Packages/manifest.json`
- Third-party assets included directly in Assets/

## Important Development Notes

### Multiplayer Testing
- Use `DebugRoomCreator.cs` for automated room creation during development
- Game supports both local AI testing and networked multiplayer
- AI difficulty configurable via AIMode enum

### Code Organization
- Namespace: `CardGame` with subnamespaces (Animation, Input, Players, etc.)
- Event-driven architecture using custom GameEvents system
- Object pooling implemented for cards and effects

### Key Configuration Files
- `GameSettings.cs` - Core gameplay parameters (rounds, card counts, etc.)
- Photon settings in Unity Editor under Photon PUN2
- Unity packages defined in `Packages/manifest.json`

### Visual Effects & Animation
- Custom animation system in `Animation/` directory
- Card placement and movement animations
- Particle effects for card interactions
- UI animations for smooth gameplay experience