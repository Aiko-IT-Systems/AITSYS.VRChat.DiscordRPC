# VRC Unity Discord RPC

Discord Rich Presence for Unity projects using the VRChat SDK.

## Features

- Detects VRChat World and Avatar projects without changing global scripting symbols.
- Shows Edit, Play, Build, and Upload states in Discord.
- Uses VRChat blueprint metadata when available and quietly falls back to local details.
- Stores its enable state in `ProjectSettings/VRCUnityDiscordRPCSettings.asset`.
- Runs only in the Windows x86_64 Unity Editor and is excluded from client builds.

Open `AITSYS > VRC Unity > Discord RPC Settings` to view the detected project type, refresh
the presence, disable RPC, or clear it.
