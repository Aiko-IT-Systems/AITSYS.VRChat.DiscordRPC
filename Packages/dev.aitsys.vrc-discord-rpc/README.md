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

The VPM distribution includes a `VRChatIntegration` editor assembly which is enabled whenever
`com.vrchat.base` is installed, without enforcing a minimum SDK version. The Asset Store
distribution contains only the generic Unity core and has no VRChat assembly references.

## Distribution

- The VPM zip includes the optional VRChat integration and declares its VRChat Base dependency.
- The Asset Store Unity package installs below `Assets/AITSYS/VRC Unity Discord RPC`, contains
  generic Unity Edit/Play support, and does not install or reference the VRChat SDK.
- Test assemblies remain in the source repository but are omitted from release artifacts.
