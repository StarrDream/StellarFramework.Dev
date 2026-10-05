# Unity project agent guidance

- Before implementing or reviewing StellarFramework architecture, Kit, ResKit, localization, UI adaptation, hot-update, or ToolsHub work, follow [the project skill](.agents/skills/stellarframework-unity/SKILL.md).
- For Unity Editor operations, follow the UnitySkills `unity-skills` guidance when installed and use its REST API when available. Confirm `/health` identifies this Dev project before making Editor changes. If that skill is unavailable, use the available Unity MCP guidance. The framework skill supplies StellarFramework contracts; UnitySkills supplies Editor operations.
- The Dev project's installed source, `KitDistributionCatalog.json`, and matching release manifest define the current APIs and package boundaries. Do not copy API assumptions from another StellarFramework version.
