# 🧥 AutoClothingGenerator for Unity

**AutoClothingGenerator** is a Unity Editor tool built for Unturned modders and content creators. It streamlines the process of generating clothing items—complete with textures, animations, prefabs, and preview functionality.

---

## ✨ Features

- 🎨 **Batch or Single Texture Support** — Import clothing textures from a folder or single PNG.
- 🧵 **Multiple Clothing Types** — Shirt, Pants, Vest, Hat, Glasses, Mask, Backpack.
- ⚙️ **Auto Material Setup** — Transparent cutout material using Unity’s Standard Shader.
- 🔁 **Animation Integration** — Automatically include Equip and Use animations.
- 👀 **Live 3D Preview** — Preview selected texture and mesh in real time.
- 📁 **Auto Folder & Asset Generation** — Organized output with proper naming and structure.
- 🏷️ **Tag & Layer Management** — Automatically ensures required Unity tags and layers exist.

---

## 🧩 Installation

1. Download or clone this repository.
2. Place `AutoClothingGenerator.cs` inside an `Editor` folder in your Unity project.
3. Open Unity and go to **`Tools > Clothing Generator`** in the top menu.

---

## 🚀 Usage Guide

### 1. Open the Tool
`Tools > Clothing Generator` from the Unity menu bar.

### 2. Configure Settings
- **Clothing Type**: Choose from 7 categories.
- **Mode**:
  - `Multiple`: Generate items from all PNGs in a folder.
  - `Single`: Generate one item from a selected PNG texture.
- **Optional Assets**:
  - Mesh
  - Equip Animation
  - Use Animation

### 3. Preview (Single Mode Only)
Preview the item with 3D mesh and lighting.

### 4. Click `Generate Item`
The tool will:
- Create folders and assets
- Configure tags/layers
- Create prefabs
- Save all changes automatically

---

## 🛠️ Requirements

- Unity 2020 or later
- PNG format for textures
- Optional mesh and animation clips

---

## 📸 Preview

_3D preview window with drag-to-rotate functionality (only available in Single Mode)._

---

## ⚠️ Notes

- Mesh is optional; defaults to Unity's Quad if none is provided.
- Animations are not required but will generate logic prefabs if assigned.
- The tool modifies Unity tags and layers (`Item`, `Logic`, `Enemy`) if missing.

---

## 📜 License

MIT License  
Feel free to use, modify, and share!

---

## 🤝 Contributing

Pull requests and suggestions are welcome.  
If you find a bug or have an idea, open an issue.

---

