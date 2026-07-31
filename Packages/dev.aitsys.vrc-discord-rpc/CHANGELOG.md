# Changelog

## Unreleased

- Added SDK-independent Unity Edit/Play presence support.
- Moved VRChat descriptors, metadata, and build/upload hooks into an optional integration assembly.
- Added separate VCC/VPM and Asset Store release artifacts from the same source tree.
- Added configurable rotating scene statistics for Edit and Play modes.
- Added reporting for the most recent Unity or VRChat build artifact size.
- Added Discord activity text clamping and duplicate-update suppression.

## 1.0.1

- Fixed an accessibility error in the Editor test assembly.

## 1.0.0

- Initial standalone VPM package.
- Added project-local settings under `Project Settings > AITSYS > VRC Unity`.
- Added automatic World, Avatar, and unsupported project detection.
- Added migration from the legacy scene settings component.
- Restricted the native Discord RPC plugin to the Windows x86_64 Editor.
