[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/A4-PIxDB)

# Snake Game

A classic Snake game built with C# and the MonoGame framework.

## Project Description

This is a school assignment to create a Snake game using Visual Studio and MonoGame. The player controls a snake that moves around a grid, eating food to grow longer while avoiding collisions with walls and its own body.

## Goals

- Implement snake movement using arrow keys or WASD
- Detect collisions with walls and the snake's own body
- Spawn food at random positions on the grid
- Grow the snake when it eats food
- Track and display the player's score
- Handle game over and restart functionality

## How to Run

1. Open `Snake/Snake.sln` in Visual Studio
2. Set `Snake.DesktopGL` as the startup project
3. Build and run (F5)

## Controls

- **Arrow Keys / WASD** - Move the snake
- **ESC** - Exit the game

## Project Structure

- `Snake.Core/` - Shared game logic (SnakeGame, Snake, Food, GameBoard)
- `Snake.DesktopGL/` - Windows/Linux desktop build
- `Snake.Android/` - Android build
- `Snake.iOS/` - iOS build
