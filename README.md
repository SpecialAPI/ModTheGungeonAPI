# Mod the Gungeon API
A plugin for BepInEx that adds some of Mod the Gungeon's functionality and also adds backwards compatibility for modders who want to port to BepInEx

# Features
Mod the Gungeon API includes:
 * Most of classic Mod the Gungeon's API features (adding items to loot tables, changing strings, replacing and adding textures, etc)
 * A debug/cheat console (F2, ~, /)
 * A debug log that records all Unity logging (F3)
 * A mods menu that shows all enabled plugins (F1)

# [Installation (Manual)](https://github.com/SpecialAPI/ModTheGungeonAPI/wiki/BepInEx-and-Mod-the-Gungeon-API-installation-guide)
# Installation (Automatic)
Install either [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager) or [r2modman](https://enter-the-gungeon.thunderstore.io/package/ebkr/r2modman/) if you don't already have a mod manager. After that, press "Install with Mod Manager" on the [thunderstore page](https://enter-the-gungeon.thunderstore.io/package/MtG_API/Mod_the_Gungeon_API/) to install this mod.
# [Porting your mod to BepInEx](https://github.com/SpecialAPI/ModTheGungeonAPI/wiki/Updating-your-Gungeon-mod-to-BepInEx)
# [Converting .json files to .jtk2d files](https://github.com/SpecialAPI/ModTheGungeonAPI/wiki/Converting-.json-files-to-.jtk2d-files)
# BepInEx Example Mod (Plain)
[Download Zip](https://github.com/SpecialAPI/BepInExExampleMod/archive/refs/heads/main.zip)  
[GitHub Repository](https://github.com/SpecialAPI/BepInExExampleMod)
# BepInEx Example Mod (Items)
[Download Zip](https://github.com/SpecialAPI/BepInExExampleModItems/archive/refs/heads/main.zip)  
[GitHub Repository](https://github.com/SpecialAPI/BepInExExampleModItems)
# [Publicized and Stripped Assembly-CSharp.dll](https://github.com/SpecialAPI/ModTheGungeonAPI/raw/main/Dependencies/Assembly-CSharp.dll)

# Credits:
 * SpecialAPI - main developer.
 * An3s - initial console + debug log port.
 * KubeRoot - updating SGUI to the latest version.
 * KyleTheScientist - base MTG example mod zips.

# Changelog:
 * 1.6.0: fixed possible save file corruption when using extended save flags, fixed the code for getting global tracked stats and maximums not working as intended with extended playable characters enum, added the UI table back to ETGMod.Databases.Strings and made it actually work, fixed ETGMod.Assets.TextureMap having the wrong names *again* and made ETGMod.Databases.Items.Add (the gun version) no longer be obsolete.
 * 1.5.8: hopefully fixed the ETGMod.Assets.TextureMap having the wrong names for the keys which caused sprite trimming to not work correctly, added the E key to the debug log that clears all messages except exceptions and removed ETGMod.Databases.Strings.UITable as it wasn't working.
 * 1.5.7: ACTUALLY fixed switching language breaking modded item names.
 * 1.5.6: fixed switching language breaking modded item names.
 * 1.5.5: MTG API now recognizes .jtk2d files as sprite definition metadata files, because .json files are recognized as config files by mod managers.
 * 1.5.3: fixed outlines breaking when using spritesheet replacements, made the console also log to the BepInEx console window and also fixed various other problems with sprite replacements
 * 1.5.2: fixed some sprite replacements not working, now you can enjoy your hunter recolors in peace.
 * 1.5.1: fixed more backwards compatibility issues, updated version in the title and also made DungeonStart actually work (don't worry i've improved the code for it so it shouldn't cause issues)
 * 1.5.0: fixed backwards some compatibility issues and added new overridable methods to GunBehaviour.
 * 1.4.1: fixed enums and guids potentially breaking savefiles and improved name to id conversion system.
 * 1.4.0: big update, added documentation, enum extensions, shared data, removed some unused or stupid methods and more.
 * 1.3.0: updated SGUI to the latest version thanks to KubeRoot.
 * 1.2.2: updated links *once more*, hopefully made the enabled mods menu bigger and updated version in the title screen.
 * 1.2.1: updated links, added credits section, restructured the page a little and added a dependency on BepInEx Pack.
 * 1.2.0: fixed the debug clear command, improved the console clear command and added a new debug key that clears all of the debug log, even errors.
 * 1.1.3: hopefully fixed the Newtonsoft.Json issue.
 * 1.1.2: updated the plugin version so that it displays correctly in the title screen.
 * 1.1.1: fixed the thumbnail to actually display the correct version.
 * 1.1.0: big update, be sure to uninstall BepInEx and reinstall it following the updated installation guide.
 * 1.0.1: fixed TextureMap being removed.
 * 1.0.0: release.