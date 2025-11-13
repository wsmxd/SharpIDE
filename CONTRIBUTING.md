# Contributing to SharpIDE

## Run Steps
1. Ensure the .NET 10 SDK is installed
2. Download the latest version of Godot from [godotengine.org/download](https://godotengine.org/download)
3. Extract it, and put it somewhere, e.g. Documents/Godot/4.5.1
4. Run `Godot_v4.5.1-stable_mono_win64.exe` (or equivalent executable)
5. Import SharpIDE
<img width="345" height="180" alt="image" src="https://github.com/user-attachments/assets/6750d4fa-4ba9-427d-8481-99278baaa354" />
<img width="362" height="191" alt="image" src="https://github.com/user-attachments/assets/8a48d099-9d64-4e5b-9fbd-65e696b7f28d" />
<img width="252" height="115" alt="image" src="https://github.com/user-attachments/assets/b500dcae-1fa7-433b-bcf3-fba9bb23cc45" />
<hr>

6. Run from Godot  
<img width="206" height="50" alt="image" src="https://github.com/user-attachments/assets/e7043c77-a978-4599-8064-fb89c61962d4" />
<hr>

7. Run/Debug from Rider  
<img width="269" height="233" alt="image" src="https://github.com/user-attachments/assets/086de05a-c0bb-48d8-8e73-c0d4f8fca2da" />

Done! ✨

## Intro to Godot
I recommend reading the [brief overview](https://docs.godotengine.org/en/stable/getting_started/introduction/key_concepts_overview.html) of Godot from their docs, however most simply, a Godot game/app is composed of scenes and nodes. A scene is simply a reusable arrangement of nodes. The main scene of SharpIDE is `IdeRoot.tscn`:

<img width="1788" height="928" alt="image" src="https://github.com/user-attachments/assets/956e5694-c1b6-453a-8f7e-e663bc2d4aeb" />

Another important concept with Godot is Scripts. A single script can be attached to a Node.
You can tell that a script is attached to a node by the icon:

<img width="565" height="88" alt="image" src="https://github.com/user-attachments/assets/6ca1efcc-7aed-42d3-9c22-8ee9fcf0ab1e" />

And clicking the icon will open the script in Rider.

<img width="768" height="317" alt="image" src="https://github.com/user-attachments/assets/020cd402-4929-4e95-9fbb-7e430a81f668" />

Exploring the UI is easy - clicking anywhere on the actual UI Nodes will highlight the node/scene in the Scene Tree:

<img width="378" height="431" alt="image" src="https://github.com/user-attachments/assets/506a1d83-dc08-4438-af74-112413a30438" />

You can tell if a "node" is a scene by the icon:

<img width="391" height="60" alt="image" src="https://github.com/user-attachments/assets/f8a64b72-cc53-486e-a0d4-52fecc08e6c8" />

Clicking this icon will open the scene:

<img width="260" height="53" alt="image" src="https://github.com/user-attachments/assets/20aecd46-f137-49bd-8023-69ebae7fa7db" />
