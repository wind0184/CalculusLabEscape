# Prefabs

All 57 prefab files in the main project are collected here. Models, materials, animations and data assets remain in their existing asset folders.

| Location | Contents |
| --- | --- |
| `LaboratoryAI.prefab` | Robot with nine motions, material feedback and the latest palm-up chin-support Think |
| `Resources/Production/` | Four production UI prefabs |
| `Studies/LightLab/` | Four saved light-theme UI studies |
| `AssetTrials/` | Factory, Modular Space and Sci-Fi MegaKit trial prefabs |
| `ThirdParty/Kenney/NatureKit/` | Prepared trees, rocks, grass and bridge samples |
| `ThirdParty/Kenney/SpaceStationKit/` | Prepared laboratory furniture and architecture samples |
| `ThirdParty/Quaternius/FarmAnimals/` | Prepared cow prefab |

Keep the `Resources/Production` nesting: the game loads those UI prefabs by the `Production/{name}` resource key. Question banks and introduction data remain under `Assets/_Project/Resources/Production/`.

Open this folder using **Calculus Lab > Assets > Open prefab folder**. The move preserved every prefab GUID and exact file content; existing scene instances and robot clip references remain valid. Migration evidence: `qa/prefab-organization-2026-10-23`.
