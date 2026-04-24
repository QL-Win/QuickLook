### Multilingual Support in QuickLook

**QuickLook Core Application (QL Main Program):**

- QuickLook implements multilingual support using separate XML files for each supported language.
- All UI strings, messages, and text displayed by QuickLook are stored in these XML language files.
- When adding a new feature, all supported language files must be updated; every string key must be translated in every language’s XML. Missing translations are not allowed.
- On startup, QuickLook loads the language file corresponding to the user’s system locale, ensuring a consistent and complete localized UI.

**QuickLook Plugins:**

- Plugins also implement localization using XML language files, following the same structure and requirements as the main program.
- Each plugin must maintain its own language files for every supported language, covering all displayed UI strings and messages.
- Any addition of new strings or features in a plugin requires translating every string into all supported languages and updating the XML files accordingly.
- The plugin loads the appropriate XML language file at runtime based on the system or QuickLook's selected language.

**Important Note:**
When implementing or updating multilingual support in QuickLook or its plugins, **all supported languages must be present and fully localized in the XML files**. Partial or missing translations are not permitted.