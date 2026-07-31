#!/usr/bin/env python3
import argparse
import json
import shutil
import uuid
from pathlib import Path


ASSET_DESTINATION = "Assets/AITSYS/VRC Unity Discord RPC"
EXCLUDED_NAMES = {
    "Tests",
    "Tests.meta",
    "VRChatIntegration",
    "VRChatIntegration.meta",
}


def folder_guid(path: str) -> str:
    return uuid.uuid5(
        uuid.NAMESPACE_URL,
        "dev.aitsys.vrc-discord-rpc/asset-store/" + path,
    ).hex


def prepare(source: Path, output: Path) -> Path:
    source = source.resolve()
    output = output.resolve()
    if output.exists():
        shutil.rmtree(output)

    output.mkdir(parents=True)

    for child in source.iterdir():
        if child.name in EXCLUDED_NAMES:
            continue

        target = output / child.name
        if child.is_dir():
            shutil.copytree(
                child,
                target,
                ignore=shutil.ignore_patterns(*EXCLUDED_NAMES),
            )
        else:
            shutil.copy2(child, target)

    package_json_path = output / "package.json"
    package_json = json.loads(package_json_path.read_text(encoding="utf-8"))
    package_json["unityPackageDestinationFolder"] = ASSET_DESTINATION
    package_json["unityPackageDestinationFolderMetas"] = {
        "Assets/AITSYS": folder_guid("Assets/AITSYS"),
        ASSET_DESTINATION: folder_guid(ASSET_DESTINATION),
    }
    package_json.pop("vpmDependencies", None)
    package_json_path.write_text(
        json.dumps(package_json, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return output


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Stage the generic Unity Asset Store package layout."
    )
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    destination = prepare(args.source, args.output)
    print(destination)


if __name__ == "__main__":
    main()
