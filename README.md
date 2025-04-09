Auto Clothing Generator for Unity
This Unity editor tool allows users to generate clothing items (such as shirts and pants) for characters, along with optional animations. The tool simplifies the process of creating clothing items by supporting both single and batch generation from texture files. The clothing items can be automatically organized into their respective folders (Shirts or Pants) and are generated with the appropriate material and prefab configurations.

Features:
Single or Multiple Texture Generation: Choose to generate clothing from a single PNG file or from all PNG files in a selected folder.

Custom Mesh Support: Optionally specify a custom mesh for the clothing items, or use a default primitive (Quad) for basic items.

Cutout Material: Automatically generates a material for the clothing items with a Cutout shader for transparent effects.

Animation Support: Attach up to two optional animations to the clothing items for use in character movements.

Organized Folder Structure: Generated items are saved into a structured folder system (Shirts or Pants), making it easy to manage your assets.

Installation:
Download or clone the repository.

Place the AutoClothingGenerator.cs script inside the Editor folder of your Unity project.

Access the tool through Tools > Clothing Generator in the Unity Editor menu.

Usage:
Select the texture or folder containing your texture files (PNG format).

Choose between generating a single clothing item or processing multiple items from a folder.

Optionally, configure a custom mesh and animations for the clothing item.

Click Generate Clothing to create your item, which will automatically be placed in the appropriate folder based on your selection (Shirts or Pants).

License:
This project is licensed under the MIT License, meaning you are free to use, modify, and distribute the code with proper attribution. However, the software is provided "as is" without any warranty of any kind, express or implied. Use it at your own risk.
