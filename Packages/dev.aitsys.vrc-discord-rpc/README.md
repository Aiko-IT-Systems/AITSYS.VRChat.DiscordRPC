# VRC Unity Discord RPC

Discord Rich Presence for Unity projects, with optional VRChat SDK integration.

## Features

- Works in standard Unity projects without requiring the VRChat SDK.
- Shows Edit and Play states in Discord.
- Detects VRChat World and Avatar projects without changing global scripting symbols.
- Adds VRChat Build and Upload states when the SDK integration is available.
- Optionally rotates cached scene complexity statistics while editing or testing.
- Reports the most recent Unity or VRChat build artifact size.
- Uses VRChat blueprint metadata when available and quietly falls back to local details.
- Stores its enable state in `ProjectSettings/VRCUnityDiscordRPCSettings.asset`.
- Runs only in the Windows x86_64 Unity Editor and is excluded from client builds.

Open `AITSYS > VRC Unity > Discord RPC Settings` to view the detected project type, refresh
the presence, disable RPC, or clear it.

The statistics rotation can include object, mesh, renderer, triangle, material, and light counts.
It defaults to a 15-second interval and pauses during Build and Upload states. Statistics are
recalculated only after the loaded scene hierarchy changes, rather than on every Discord update.

The `VRChatIntegration` editor assembly is enabled whenever `com.vrchat.base` is installed,
without enforcing a minimum SDK version. Standard Unity projects compile and run the core
package without any VRChat assemblies.

## Distribution

- The VPM zip and VCC Unity package include `package.json` and its VRChat Base dependency.
- The Asset Store Unity package installs below `Assets/AITSYS/VRC Unity Discord RPC` and does
  not install or require the VRChat SDK.
- If a compatible VRChat SDK is later added to an Asset Store installation, its optional
  integration assembly activates automatically.
