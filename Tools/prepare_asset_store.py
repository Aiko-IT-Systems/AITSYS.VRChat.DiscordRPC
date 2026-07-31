#!/usr/bin/env python3
import argparse
import shutil
import uuid
from pathlib import Path


ASSET_ROOT = Path("Assets") / "AITSYS" / "VRC Unity Discord RPC"
EXCLUDED_NAMES = {
    "Tests",
    "Tests.meta",
    "package.json",
    "package.json.meta",
}


def folder_meta(relative_path: Path) -> str:
    guid = uuid.uuid5(
        uuid.NAMESPACE_URL,
        "dev.aitsys.vrc-discord-rpc/asset-store/" + relative_path.as_posix(),
    ).hex
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def write_folder_meta(project_root: Path, folder: Path) -> None:
    relative = folder.relative_to(project_root)
    meta_path = folder.with_name(folder.name + ".meta")
    meta_path.write_text(folder_meta(relative), encoding="utf-8", newline="\n")


def prepare(source: Path, output: Path) -> Path:
    source = source.resolve()
    output = output.resolve()
    if output.exists():
        shutil.rmtree(output)

    destination = output / ASSET_ROOT
    destination.mkdir(parents=True)

    for child in source.iterdir():
        if child.name in EXCLUDED_NAMES:
            continue

        target = destination / child.name
        if child.is_dir():
            shutil.copytree(child, target)
        else:
            shutil.copy2(child, target)

    write_folder_meta(output, output / "Assets" / "AITSYS")
    write_folder_meta(output, destination)
    return destination


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Stage the SDK-optional Asset Store Unity package layout."
    )
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    destination = prepare(args.source, args.output)
    print(destination)


if __name__ == "__main__":
    main()
