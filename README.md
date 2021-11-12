# FreeSpace2TranslationTools

Set of tools to help to translate FreeSpace 2 and its mods.

## Why this program?

The main problem with existing FS mods is that most of them are not ready to be translated as is, because the support and interest for translations in this game has been low in the last 20 years.
So in most cases, the original mod files need to be modified in order to be able to translate them in a convenient way.
This is the main goal of this program. So basically it's an Internationalization tool for FS mods.

## What does it do?

First, the program will look for any 'hardcoded' text and try to put it into a 'XSTR' variable containing the translatable text.

Then, all XSTR variables are extracted to one or two plain files (tstrings.tbl and potentially xxx-tlc.tbm) that regroup all texts displayed in the mod.

From here you can start to manually translate these files.

If a mod you translated received an update with modified/new content, you might want to adapt your translation without starting again from scratch, so there is a feature to help you updating your translations.

## How does it work?

Regex, regex, regex, and more regex...

## How to get involved?

If you find bugs or have ideas to improve this program, you can create an issue on this project, or send a message to the dedicated HLP forum topic: https://www.hard-light.net/forums/index.php?topic=97658.0.
