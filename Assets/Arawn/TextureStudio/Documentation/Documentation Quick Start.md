ONLINE DOCUMENTATION: https://arawn-software-publishing.gitbook.io/texture-studio
SUPPORT: mail@arawn.digital
Discord: https://discord.gg/MPhMKtSMUZ

# Quick Start Guide

Get up and running with Texture Studio & Material Converter in minutes.

## 1. Installation
Ensure the tool files are located in your project at:
`Assets/Arawn/TextureStudio/Editor/`

Once present, Unity compiles them automatically.

## 2. Open the Tool
Go to the Unity menu bar:
**Tools → Texture Studio → Material Converter**

## 3. Convert Materials
The core workflow consists of three steps:

1.  **Select Pipelines**
    *   **Source**: The pipeline your materials currently use (e.g., Built-in).
    *   **Target**: The pipeline you want to move to (e.g., URP or HDRP).

2.  **Choose Materials**
    *   **Selected Materials**: Converts only what you have highlighted in the Project window. (Recommended for testing).
    *   **Materials in Folder**: Converts everything in a specific folder recursively.
    *   **All Project Materials**: Converts every material in the project. (Use with caution!).

3.  **Convert**
    *   Check **Create Backup** (highly recommended).
    *   Click **Convert Shaders**.

## 4. Generate Missing Textures
If your materials lack specific maps (like Normal or Occlusion) required by the new pipeline:

1.  Expand the **Texture Generation Options** foldout in the tool window.
2.  Check **Auto-Generate Missing Textures**.
3.  Select the maps you need (e.g., Normal Map, Metallic/Mask Map).
4.  When you click **Convert Shaders**, the tool will automatically create and assign these textures based on the material's Base Map.

> **Note:** You can also generate textures for existing materials without converting them by clicking **Generate Textures for Existing Materials**.

## 5. Custom Shaders
If you use custom shaders that don't follow standard Unity naming conventions:

1.  Go to **Edit → Project Settings → Texture Studio**.
2.  Add a new mapping.
3.  Enter a substring of your shader's name (e.g., `MyToon`).
4.  List the property names your shader uses for each texture slot (e.g., `_MainTex, _Albedo`).
5.  The tool will now recognize and auto-assign textures for these materials.
