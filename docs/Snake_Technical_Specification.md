# Technical Specification Document
## Snake Game

---

| Field | Value |
|-------|-------|
| **Project** | Snake Game - SE Final Project |
| **Platform** | MonoGame Framework (.NET) |
| **Author** | Aric |
| **Version** | 1.0 |
| **Date** | January 2026 |

---

## 1. Introduction

### 1.1 Purpose

This document defines the technical specification for a Snake game implementation using the MonoGame framework. It outlines all functional and non-functional requirements, system architecture, and design constraints necessary for successful development and evaluation.

### 1.2 Scope

The project encompasses a complete, playable Snake game featuring grid-based movement, collision detection, scoring mechanics, and multiple game states. The primary deliverable targets desktop platforms with potential Android portability as a secondary objective.

### 1.3 Definitions and Acronyms

| Term | Definition |
|------|------------|
| **Grid** | The discrete coordinate system where game entities exist |
| **Cell** | A single unit position within the grid |
| **Tick** | A fixed time interval at which game state updates occur |
| **Segment** | A single cell occupied by part of the snake body |
| **Head** | The leading segment of the snake that determines movement direction |
| **Tail** | The trailing segment of the snake removed during normal movement |

---

## 2. System Overview

### 2.1 System Architecture

The game follows the standard MonoGame application lifecycle with a main Game class inheriting from Microsoft.Xna.Framework.Game. The architecture uses a shared core library (`Snake.Core`) containing all game logic, with platform-specific entry points for Desktop (OpenGL), Android, and iOS. This separation ensures the core game logic remains platform-agnostic while allowing platform-specific initialization and input handling.

### 2.2 Technology Stack

- **Framework:** MonoGame 3.8+
- **Language:** C# (.NET 9.0)
- **IDE:** Visual Studio 2022 or JetBrains Rider
- **Graphics API:** OpenGL (Cross-platform via DesktopGL)
- **Build System:** MSBuild with MonoGame Content Pipeline
- **Target Platforms:** Windows/Linux (DesktopGL), Android, iOS

---

## 3. Functional Requirements

### 3.1 Grid System

| ID | Requirement |
|----|-------------|
| **FR-1** | The game SHALL define a grid with dimensions of 30 cells wide x 20 cells tall. |
| **FR-2** | The system SHALL calculate cell size dynamically based on screen/window dimensions and grid size to support varying display sizes (desktop windows, Android devices, iOS devices). |
| **FR-3** | The system SHALL provide coordinate translation between grid positions and screen pixel coordinates. |

### 3.2 Snake Entity

| ID | Requirement |
|----|-------------|
| **FR-4** | The snake SHALL be represented as an ordered collection of grid coordinates. |
| **FR-5** | The snake SHALL initialize with a minimum length of 3 segments. |
| **FR-6** | The snake head SHALL be defined as the first element in the collection. |
| **FR-7** | The snake tail SHALL be defined as the last element in the collection. |
| **FR-8** | On each movement tick, the system SHALL add a new head position in the current direction. |
| **FR-9** | On each movement tick (without food consumption), the system SHALL remove the tail segment. |
| **FR-10** | When food is consumed, the system SHALL skip tail removal for one tick, causing growth. |

### 3.3 Movement and Input

| ID | Requirement |
|----|-------------|
| **FR-11** | The system SHALL support four movement directions: Up, Down, Left, Right. |
| **FR-12** | The system SHALL accept keyboard input for direction changes (Arrow keys and/or WASD) on desktop platforms. |
| **FR-12a** | The system SHALL accept touch/swipe input for direction changes on Android. |
| **FR-13** | The system SHALL reject direction changes that would result in 180-degree reversal. |
| **FR-14** | The system SHALL implement input buffering to queue the next direction change between ticks. |
| **FR-15** | Movement SHALL occur at fixed time intervals independent of frame rate. |

### 3.4 Food System

| ID | Requirement |
|----|-------------|
| **FR-16** | The system SHALL spawn exactly one food item on the grid at any time. |
| **FR-17** | Food position SHALL be randomly generated within grid boundaries. |
| **FR-18** | Food SHALL NOT spawn on any cell currently occupied by the snake. |
| **FR-19** | When the snake head occupies the food cell, the food SHALL be consumed. |
| **FR-20** | Upon consumption, a new food item SHALL spawn immediately. |

### 3.5 Collision Detection

| ID | Requirement |
|----|-------------|
| **FR-21** | The system SHALL detect collision when the snake head position exceeds grid boundaries (wall collision). |
| **FR-22** | The system SHALL detect collision when the snake head position matches any body segment position (self collision). |
| **FR-23** | Upon any collision detection, the game SHALL transition to Game Over state. |

### 3.6 Scoring System

| ID | Requirement |
|----|-------------|
| **FR-24** | The system SHALL maintain a score counter initialized to zero. |
| **FR-25** | The score SHALL increment by a fixed value (default: 10 points) upon food consumption. |
| **FR-26** | The current score SHALL be displayed during gameplay. |
| **FR-27** | The final score SHALL be displayed on the Game Over screen. |

### 3.7 Game State Management

| ID | Requirement |
|----|-------------|
| **FR-28** | The system SHALL implement a Start state displayed upon game launch. |
| **FR-29** | The system SHALL implement a Playing state where active gameplay occurs. |
| **FR-30** | The system SHALL implement a Game Over state triggered by collision. |
| **FR-30a** | The system SHALL implement a Paused state accessible during gameplay. |
| **FR-31** | The system SHALL allow transition from Start to Playing via user input (any key on desktop, tap on Android). |
| **FR-32** | The system SHALL allow transition from Game Over to Playing (restart) via user input. |
| **FR-33** | Upon restart, all game data (snake, score, food) SHALL reset to initial values. |

### 3.8 Rendering

| ID | Requirement |
|----|-------------|
| **FR-34** | The system SHALL render all snake segments as distinct visual elements. |
| **FR-35** | The system SHALL visually differentiate the snake head from body segments. |
| **FR-36** | The system SHALL render the food item with distinct visual appearance. |
| **FR-37** | The system SHALL render the current score during Playing state. |
| **FR-38** | The system SHALL render appropriate UI for Start and Game Over states. |

---

## 4. Non-Functional Requirements

### 4.1 Performance

| ID | Requirement |
|----|-------------|
| **NFR-1** | The game SHALL maintain a minimum frame rate of 60 FPS during normal operation. |
| **NFR-2** | Input latency SHALL NOT exceed 16ms (one frame at 60 FPS). |
| **NFR-3** | Memory usage SHALL NOT exceed 100MB during gameplay. |

### 4.2 Usability

| ID | Requirement |
|----|-------------|
| **NFR-4** | Game controls SHALL be intuitive and require no external documentation. |
| **NFR-5** | Visual feedback SHALL clearly indicate game state transitions. |
| **NFR-6** | The game SHALL display control instructions on the Start screen. |

### 4.3 Maintainability

| ID | Requirement |
|----|-------------|
| **NFR-7** | Code SHALL follow C# naming conventions and best practices. |
| **NFR-8** | Game parameters (grid size, tick rate, score values) SHALL be defined as configurable constants. |
| **NFR-9** | Logical components SHALL be separated into distinct classes or methods. |

### 4.4 Portability

| ID | Requirement |
|----|-------------|
| **NFR-10** | The core game logic SHALL be platform-agnostic. |
| **NFR-11** | Input handling SHALL be abstracted to support future platform extensions. |
| **NFR-12** | The codebase SHALL compile and run on Windows 10/11 without modification. |

### 4.5 Reliability

| ID | Requirement |
|----|-------------|
| **NFR-13** | The game SHALL NOT crash under any normal gameplay conditions. |
| **NFR-14** | The game SHALL handle window resize events gracefully. |
| **NFR-15** | The game SHALL handle focus loss/gain without state corruption. |

---

## 5. Data Structures

### 5.1 Core Data Types

| Structure | Type | Description |
|-----------|------|-------------|
| **Point** | Microsoft.Xna.Framework.Point | X and Y integer coordinates representing a grid cell (MonoGame built-in) |
| **Direction** | enum | Up, Down, Left, Right movement vectors |
| **GameState** | enum | Playing, GameOver, Paused state identifiers |
| **Snake Body** | List\<Point\> | Ordered collection of body segments (head at index 0) |

### 5.2 Project Structure

```
Snake/
├── Snake.Core/                    # Shared platform-agnostic game logic
│   ├── SnakeGame.cs              # Main MonoGame Game class (update, draw, input)
│   ├── GameState.cs              # Enum: Playing, GameOver, Paused
│   ├── Direction.cs              # Enum: Up, Down, Left, Right (TO IMPLEMENT)
│   ├── Snake.cs                  # Snake entity (body, movement, growth)
│   ├── Food.cs                   # Food spawning and position
│   ├── GameBoard.cs              # Grid constants and boundary logic
│   └── Content/
│       └── Fonts/                # SpriteFont for score/UI display
│
├── Snake.DesktopGL/              # Windows/Linux desktop executable
│   ├── Program.cs                # Entry point
│   └── Snake.DesktopGL.csproj
│
├── Snake.Android/                # Android executable
│   ├── MainActivity.cs           # Android activity entry point
│   └── Snake.Android.csproj
│
└── Snake.iOS/                    # iOS executable
    ├── Program.cs                # iOS entry point
    └── Snake.iOS.csproj
```

**Note:** Input handling is integrated into `SnakeGame.cs` rather than separated, following the starter repository pattern. Touch input for Android will be added to the shared core with platform detection.

---

## 6. Constraints and Assumptions

### 6.1 Constraints

- Development must use MonoGame framework exclusively (no Unity, Godot, etc.)
- Primary target platforms: Windows desktop (for grading) and Android (secondary deliverable)
- Core game logic must remain platform-agnostic in Snake.Core project

### 6.2 Assumptions

- Desktop: User has a keyboard for input
- Android: User has touch screen for swipe-based input
- Desktop display resolution is at least 800x600 pixels
- System supports OpenGL 3.0+ (DesktopGL configuration)

---

## 7. Future Enhancements (Optional)

The following features are out of scope for the initial release but documented for potential future development:

- [ ] Sound effects (movement, eating, collision)
- [ ] High score persistence (local file storage)
- [ ] Difficulty scaling (increasing speed over time)
- [ ] Visual themes/skins
- [ ] Wrap-around mode (no wall collision)
- [ ] iOS deployment and testing

---

## 8. Implementation Status

This section tracks what has been implemented in the starter repository versus what remains to be built.

### 8.1 Implemented (Starter Repository)

| Component | Status | Notes |
|-----------|--------|-------|
| Project structure | ✓ Complete | Snake.Core + platform-specific projects |
| Window/graphics setup | ✓ Complete | 750x550 fixed window (desktop) |
| Grid rendering | ✓ Complete | 30x20 grid with 25px cells |
| Snake rendering | ✓ Complete | Head (green) and body (light green) |
| Food rendering | ✓ Complete | Red squares |
| Score display | ✓ Complete | Top of screen during gameplay |
| Game Over screen | ✓ Complete | Overlay with final score |
| GameState enum | ✓ Complete | Playing, GameOver, Paused defined |
| Font loading | ✓ Complete | With fallback handling |
| Android project | ✓ Complete | Entry point and configuration ready |

### 8.2 To Be Implemented

| Component | Priority | Related Requirements |
|-----------|----------|---------------------|
| Direction enum | High | FR-11 |
| Snake movement logic | High | FR-8, FR-9, FR-15 |
| Keyboard input handling | High | FR-12, FR-14 |
| 180-degree reversal prevention | High | FR-13 |
| Wall collision detection | High | FR-21, FR-23 |
| Self collision detection | High | FR-22, FR-23 |
| Food consumption logic | High | FR-19, FR-10 |
| Food spawn validation (BUG FIX) | High | FR-18 |
| Game state transitions | High | FR-31, FR-32 |
| Game reset on restart | High | FR-33 |
| Start screen | Medium | FR-28, FR-31 |
| Dynamic cell sizing | Medium | FR-2 (for Android) |
| Android touch input | Medium | Platform support |
| Pause functionality | Low | GameState.Paused exists |

### 8.3 Known Bugs in Starter Repository

| Location | Issue | Impact |
|----------|-------|--------|
| Food.cs:66 | `while (false)` loop never executes | Food can spawn on snake body |
| SnakeGame.cs:145-148 | Movement/collision logic is only TODO comments | Game is not playable |
| SnakeGame.cs:HandleInput() | Method body is empty | No input response |

---

## Appendix A: Requirements Traceability Matrix

| Req ID | Requirement Summary | Test Method |
|--------|---------------------|-------------|
| FR-1 | Grid dimensions configurable | Code review / Manual test |
| FR-4 | Snake as ordered collection | Unit test / Debug inspection |
| FR-13 | 180-degree reversal rejected | Manual input test |
| FR-15 | Movement at fixed intervals | Frame timing analysis |
| FR-18 | Food avoids snake position | Stress test with long snake |
| FR-21 | Wall collision detection | Boundary test cases |
| FR-22 | Self collision detection | Intentional self-collision test |
| FR-33 | Reset on restart | State inspection after restart |
| NFR-1 | 60 FPS minimum | FPS counter / Profiling |
| NFR-13 | No crashes | Extended play testing |

---

## Appendix B: State Diagram

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│    ┌─────────┐    Press Any Key    ┌─────────┐         │
│    │  START  │ ──────────────────► │ PLAYING │         │
│    └─────────┘                     └─────────┘         │
│         ▲                               │              │
│         │                               │              │
│         │      Press R/Enter            │ Collision    │
│         │                               ▼              │
│         │                         ┌───────────┐        │
│         └──────────────────────── │ GAME OVER │        │
│                                   └───────────┘        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Appendix C: Movement Logic Pseudocode

```
OnTick():
    if currentState != Playing:
        return
    
    // Apply buffered input
    if bufferedDirection != null AND isValidDirection(bufferedDirection):
        currentDirection = bufferedDirection
        bufferedDirection = null
    
    // Calculate new head position
    newHead = snake.Head + directionVector[currentDirection]
    
    // Check collisions
    if isOutOfBounds(newHead) OR snake.Contains(newHead):
        currentState = GameOver
        return
    
    // Add new head
    snake.InsertAt(0, newHead)
    
    // Check food collision
    if newHead == foodPosition:
        score += POINTS_PER_FOOD
        SpawnFood()
        // Skip tail removal (snake grows)
    else:
        snake.RemoveLast()
```

---

*Document generated for SE Final Project - Snake Game*
