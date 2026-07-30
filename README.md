# VRC Unity Discord RPC

Standalone VPM package for Discord Rich Presence in Unity projects using the
VRChat SDK.

## Package

- Package ID: `dev.aitsys.vrc-discord-rpc`
- Unity: `2022.3.22f1`
- Platform: Windows Editor x86_64
- Settings: `AITSYS > VRC Unity > Discord RPC Settings`

The package detects World and Avatar SDK projects without changing project-wide
scripting define symbols. It reports Edit, Play, Build, and Upload state and
uses VRChat blueprint metadata when available.

## Development

The distributable VPM package lives at:

`Packages/dev.aitsys.vrc-discord-rpc`

The root Unity project is a minimal test host for package development. VRChat
Base is resolved through its VPM manifest; unrelated SDKs and Unity packages
are intentionally not included.

Run the `Build Release` GitHub Actions workflow to publish the version declared
in the package's `package.json`. The repository variable `PACKAGE_NAME` must
remain set to `dev.aitsys.vrc-discord-rpc`.

## License

The package source is MIT licensed. The bundled Discord RPC native library is
also MIT licensed; see `ThirdPartyNotices.md` in the package.
