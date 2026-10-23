"""22 Oct: record the complete PixelLab enemy sets made since 9 Oct in LowlandsManifest.json (one-off; then run
download_lowlands_enemies.py). Each entry is an animation id per direction; frame URLs follow PixelLab's layout."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://backblaze.pixellab.ai/file/pixellab-characters/ed5ccd62-d29a-40ad-960a-d0686784d758"
DIRS = ["south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"]

# character -> slot -> (frames, {direction: animation id})
SETS = {
    "BriarWolf": ("7cd8cc09-2fa8-4559-acb8-4c0030d27215", {
        "Idle": (8, {"south-west": "3f7e9aac-cd8b-4024-ba07-14154f2c319a", "north-west": "c5db41d4-f187-4499-b397-dd7e69b02c22",
                     "west": "2eb340cf-0ff7-40b8-8dad-2e6d047254a4", "south": "256fe6e1-e79f-4dea-a0c7-24a5c2d35007",
                     "east": "ec7987fa-f00e-4be9-9d1e-584d8c35d744", "north": "1609e435-e475-4adf-9ee8-56175d87dd8b",
                     "north-east": "4fb6fd7a-1035-49ef-bc32-e8aafdc430da", "south-east": "3dfb9b70-d676-460e-9490-b506f80031f0"}),
        "Run": (8, {"south-west": "0fd29aeb-2707-4aaf-b498-6d478c97440c", "west": "64bc3335-e432-4195-bc46-405c7fe43040",
                    "north-west": "081ef0c3-e014-44c7-bd13-4b9514e6ab3a", "north": "057917db-9de3-4703-beaa-2cbeac604109",
                    "north-east": "5c22d023-8904-4407-a4f1-e3902916f264", "south-east": "7d9c3e85-f5b7-4bda-bf54-c243a1b3510e",
                    "south": "75496864-9d9a-470b-8595-723376d087b0", "east": "e9d0c9b9-e877-4fa0-bcc5-4d2f8f844520"}),
        "Walk": (8, {"north-west": "ecf33383-9a4b-4e95-9522-fd04bf6a72b2", "west": "f813cdd0-0c17-4a7b-b4d6-e15d6ef3ef0b",
                     "south-west": "c894be62-d134-42ba-a02a-c3fffa2a204c", "north": "c6b0b15a-0d29-4c0d-a451-ef513571c8e7",
                     "north-east": "ccbcc395-9723-4f5b-8621-d03514c26941", "south-east": "99292f63-76cb-4fe0-b96f-6f4f58232bd8",
                     "south": "db1e355d-0d6f-4ee9-b306-6cd0237ce0d5", "east": "3839b567-6021-47f3-9b2b-82b0d0b23493"}),
    }),
    "MossbackBoar": ("6b5f7ffe-dda1-4161-bd09-e81431c0f480", {
        "Idle": (8, {"north": "f924020b-8c3a-4698-a6f7-0c48bf0f763f", "south-west": "b3d8aacc-a6d1-4ba4-a421-28a222653f98",
                     "west": "4ba1d9ec-1556-49cc-a9eb-bd606248982e", "north-west": "56318646-9a70-4cc3-8eb9-7e2e7d897d12",
                     "north-east": "68aaf237-bb5c-4b37-8c1e-30c960adec04", "south-east": "e1c20f5e-da89-4b79-8f82-768efc76977b",
                     "south": "4630562f-d4db-4cc5-9a24-bae306ab9b9a", "east": "e99930de-fb53-4fc4-9398-511e2eeaf8ad"}),
        "Run": (8, {"west": "0d779057-ecbe-4d7a-8636-a1d26cec0cd4", "south-west": "ea74945d-6e33-4fcd-bd15-7ec8cca0b60d",
                    "north-west": "846b0a5f-62bc-4eb6-bb15-61d39f3389c4", "north-east": "fa38a469-7ee5-4485-b20e-5c0f887e6b4d",
                    "south-east": "b4060e2c-c476-4efb-9aaf-3fe75214f56e", "north": "c1f121bb-7ba7-4d76-9787-1f2e55d9d830",
                    "south": "80170da0-1978-43ca-9339-b3be6cd6e893", "east": "36d69fc9-79ef-4fd1-a1ce-5987598fc1c7"}),
    }),
    "RoadScavenger": ("35c0dd82-ce14-4151-9be5-52d9731c942a", {
        "Idle": (4, {"south-east": "ed71d296-24e7-499c-aeab-2776acaae6c3", "north": "3273960b-b41c-4ebf-897c-a23cf075fc28",
                     "south-west": "8f4b1fe3-6921-4869-865e-e5b16e0a4bee", "west": "226e3ab8-563a-48a7-b84e-59c7de14c213",
                     "north-east": "9590d993-aea8-41ac-af0a-6c5683b1e48d", "north-west": "c572e1f7-4322-43ec-92c0-ee40edc5a40e",
                     "east": "62dd6c38-5006-47b3-929f-6b76cafa4971", "south": "7c7c7d23-b0ae-4b7b-ad3d-d7ed5ec34e16"}),
        "Run": (8, {"south-east": "b5af0b6c-c7e7-4dbf-a7c2-a4ad8c082975", "north-east": "d0153d2b-26a8-4400-928e-42cf9f1d589c",
                    "west": "00413605-565d-46e1-b68e-4390ff5852c6", "south": "86bf9669-0e6f-49bd-843c-e67c01a679f2",
                    "north-west": "0ce6ffc6-e67b-4c07-b69c-6193674c12f9", "south-west": "632301c9-ee1f-4358-9f12-ecb048cea7a2",
                    "north": "fb444dfd-d5cb-4cf3-a2ef-5ca957761977", "east": "1e2133a2-6210-467c-a287-5a82205758ed"}),
    }),
    "ThornRat": ("21ff815c-ee63-4197-b8b8-6ad7394d23e6", {
        "Run": (8, {"west": "1f3b6f9a-ef4c-475f-a7bb-745ea43ded0f", "south-west": "1efb34d6-bf25-48b8-b170-e7a765b86f7e",
                    "north-west": "4413ca32-4619-4f21-b1ff-ab4cd243d72a", "north-east": "e25003eb-3d7a-484d-88df-21a9ec00f9d8",
                    "north": "cfe5c2fd-0914-404e-aac3-c46d09b2116b", "south-east": "540bacfd-58c6-4df6-a921-70ed250e27ac",
                    "south": "4c0751f2-40f7-4ab4-b2bf-9cd940bcf064", "east": "a57b6ab4-0c63-40c1-af39-c3da59a7ea71"}),
    }),
}

if __name__ == "__main__":
    path = ROOT / "ArtSource/Enemies/LowlandsManifest.json"
    manifest = json.loads(path.read_text())
    by_name = {c["name"]: c for c in manifest["characters"]}
    added = 0
    for name, (character_id, slots) in SETS.items():
        character = by_name[name]
        assert character["characterId"] == character_id, name
        present = {(f["slot"], f["file"]) for f in character["files"]}
        for slot, (frames, ids) in slots.items():
            assert sorted(ids) == sorted(DIRS), (name, slot)
            for direction in DIRS:
                for i in range(frames):
                    file = f"{direction}/frame_{i:03d}.png"
                    if (slot, file) in present: continue
                    character["files"].append({"slot": slot, "direction": direction,
                        "url": f"{BASE}/{character_id}/animations/{ids[direction]}/{direction}/{i}.png", "file": file})
                    added += 1
    manifest["retrieved"] = "2026-10-09; Idle/Run sets added 2026-10-22"
    path.write_text(json.dumps(manifest, indent=1) + "\n")
    print(f"Added {added} frame entries.")
