# Unity Clothing Generator

A powerful Unity Editor tool for quickly generating and previewing in-game clothing and accessory items.

## Overview

The Auto Clothing Generator is a comprehensive Unity Editor extension designed to streamline the creation of various clothing items and accessories for your games. This tool automates the process of generating properly configured meshes, materials, prefabs, and animations for different types of wearable items.

## Features

- **Multiple Item Types**: Generate various item types including shirts, pants, vests, hats, glasses, masks, and backpacks
- **Batch Processing**: Process multiple textures at once from a specified folder
- **Advanced Preview System**: 
  - Multi-view mode to see your items from different angles
  - Perspective view with rotate, pan, and zoom controls
  - Custom background colors and images
  - Preview export functionality
- **Material Configuration**: Automatically sets up materials with proper transparency settings
- **Animation Support**: Associate equip and use animations with your items
- **Prefab Generation**: Creates properly tagged and layered prefabs ready for game implementation
- **Custom Icon Placement**: Position item icons based on camera view

## Installation

1. Copy the `AutoClothingGenerator.cs` script into your Unity project's `Editor/Assembly-CSharp-Editor/Tools` folder
2. In Unity, navigate to `Tools > Clothing Generator` to open the tool

## Usage

### Basic Workflow

1. Select the item type (shirt, pants, hat, etc.)
2. Choose between single or multiple selection mode
3. Select your texture(s)
4. Assign a mesh (or use the default quad)
5. Optional: Add equip and use animations
6. Click "Generate Item" to create your clothing assets

### Selection Modes

- **Single Mode**: Process one specific texture
- **Multiple Mode**: Batch process all textures in a selected folder

### Preview Controls

- **Perspective View**: 
  - Left-click and drag to rotate
  - Middle-click and drag to pan
  - Scroll wheel to zoom
- **View Modes**:
  - Single View: Shows one selected angle
  - Multi View: Shows the item from multiple angles simultaneously

## Generated Assets

For each processed item, the tool creates:
- A properly configured texture
- A material with appropriate transparency settings
- A prefab with the correct tags and layers
- Animation prefabs (if animations are provided)
- Special type prefabs for appropriate item types

## Structure

The generated assets are organized into the following folder structure:
```
Assets/Clothing/
  ├── Shirts/
  │   └── [ItemName]/
  │       ├── shirt.png
  │       ├── [ItemName]_Mat.mat
  │       ├── Item.prefab
  │       └── Animations.prefab (if animations provided)
  ├── Pants/
  │   └── [ItemName]/
  │       └── ...
  └── ...
```

## Requirements

- Unity 2019.4 or higher
- The tool uses Unity's built-in rendering pipeline with the Standard shader

## Tips

- For best results, use transparent PNG files with alpha channels
- Organize your source textures in a dedicated folder for easy batch processing
- Use the preview system to check how items will look in-game before generating them

## License

This tool is available under the MIT License. See the LICENSE file for details.
