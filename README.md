# Archipelago Multiworld Randomizer integration for Inscryption

The ArchipelagoMod is a randomizer mod that will change how you play the story of Inscryption. With all items used to progress the game now shuffled, the order in which you complete the game won't be familiar.

This mod is meant to be used alongside Archipelago. To know more about Archipelago, you can visit their website [here](https://archipelago.gg/). To give you a quick rundown, Archipelago allows you to connect to a multiworld server which will shuffle items from different players and their respective game. This means that you can find items from other games belonging to your friends. Once found, these items will be sent to the appropriate player. Likewise, your friends can find your items in their own game and send them to you to help you progress. This essentially turns your singleplayer game into a coop experience. It can also be used with only one player if you just want to shuffle items within your own world.

## Where is the settings page?
You can configure your player options with the Inscryption options page. [Click here](https://archipelago.gg/games/Inscryption/player-options) to start configuring them to your liking.

## What does randomization do to this game?
Due to the nature of the randomizer, you are allowed to return to a previous act you've previously completed if there are location checks you've missed. The "New Game" option is replaced with a "Chapter Select" option and is enabled after you beat act 1. If you prefer, you can also make all acts available from the start by changing the goal option. All items that you can find lying around, in containers, or from puzzles are randomized and replaced with location checks. Boss fights from all acts and battles from act 2 also count as location checks.

## What is the goal of Inscryption when randomized?
By default, the goal is considered reached once you open the OLD_DATA file. This means playing through all three acts in order and the epilogue. You can change the goal option to instead complete all acts in any order or simply complete act 1.

## Which items can be in another player's world?
All key items necessary for progression such as the film roll, the dagger, Grimora's epitaphs, etc. Unique cards that aren't randomly found in the base game (e.g. talking cards) are also included. For filler items, you can receive currency which will be added to every act's bank or card packs that you can open at any time when inspecting your deck.

## What does another world's item look like in Inscryption?
Items from other worlds usually take the appearance of a normal card from the current act you're playing. The card's name contains the item that will be sent when picked up and its portrait is the Archipelago logo (a ring of six circles). Picking up these cards does not add them to your deck.

## When the player receives an item, what happens?
The item is instantly granted to you. A yellow message appears in the Archipelago logs at the top-right of your screen. An audio cue is also played. If the item received is a holdable item (wardrobe key, inspectometer battery, gems module), the item will be placed where you would usually collect it in a vanilla playthrough (safe, inspectometer, drone).

## How many items can I find or receive in my world?
By default, if all three acts are played, there are **100** randomized locations in your world and **100** of your items shuffled in the multiworld. There are **17** locations in act 1 (this will be the total amount if you decide to only play act 1), **52** locations in act 2, and **31** locations in act 3.

# Setup Guide

## Required Software

- [Inscryption](https://store.steampowered.com/app/1092790/Inscryption/)
- For easy setup (recommended):
  - [r2modman](https://inscryption.thunderstore.io/package/ebkr/r2modman/) OR [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
- For manual setup:
  - [BepInEx pack for Inscryption](https://inscryption.thunderstore.io/package/BepInEx/BepInExPack_Inscryption/)
  - [ArchipelagoMod](https://inscryption.thunderstore.io/package/Ballin_Inc/ArchipelagoMod/)

## Installation
Before starting the installation process, here's what you should know:
- Only install the mods mentioned in this guide if you want a guaranteed smooth experience! Other mods were NOT tested with ArchipelagoMod and could cause unwanted issues.
- The ArchipelagoMod uses its own save file system when playing, but for safety measures, back up your save file by going to your Inscryption installation directory and copy the `SaveFile.gwsave` file to another folder.
- It is strongly recommended to use a mod manager if you want a quicker and easier installation process, but if you don't like installing extra software and are comfortable moving files around, you can refer to the manual setup guide instead.

### Easy setup (mod manager)
1. Download [r2modman](https://inscryption.thunderstore.io/package/ebkr/r2modman/) using the "Manual Download" button, then install it using the executable in the downloaded zip package (You can also use [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager) which works the same, but it requires [Overwolf](https://www.overwolf.com/))
2. Open the mod manager and select Inscryption in the game selection screen.
3. Select the default profile or create a new one.
4. Open the `Online` tab on the left, then search for `ArchipelagoMod`.
5. Expand ArchipelagoMod and click the `Download` button to install the latest version and all its dependencies.
6. Click `Start Modded` to open the game with the mods (a console should appear if everything was done correctly).

### Manual setup
1. Download the following mods using the `Manual Download` button:
   - [BepInEx pack for Inscryption](https://inscryption.thunderstore.io/package/BepInEx/BepInExPack_Inscryption/)
   - [ArchipelagoMod](https://inscryption.thunderstore.io/package/Ballin_Inc/ArchipelagoMod/)
2. Open your Inscryption installation directory. On Steam, you can find it easily by right-clicking the game and clicking `Manage` > `Browse local files`.
3. Open the BepInEx pack zip file, then open the `BepInExPack_Inscryption` folder.
4. Drag all folders and files located inside the `BepInExPack_Inscryption` folder and drop them in your Inscryption directory.
5. Open the `BepInEx` folder in your Inscryption directory.
6. Open the ArchipelagoMod zip file.
7. Drag and drop the `plugins` folder in the `BepInEx` folder to fuse with the existing `plugins` folder.
8. Open the game normally to play with mods (if BepInEx was installed correctly, a console should appear).

## Creating a MultiWorld
Before playing, you or another player needs to generate and host a MultiWorld. For a detailed explanation, visit the [Archipelago setup guide](https://archipelago.gg/tutorial/Archipelago/setup/en).

## Joining a new MultiWorld Game
1. After opening the game, you should see a new menu for browsing and creating save files.
2. Click on the `New Game` button, then write a unique name for your save file.
3. On the next screen, enter the information needed to connect to the MultiWorld server, then press the `Connect` button.
4. If successful, the status on the top-right will change to "Connected". If not, a red error message will appear.
5. After connecting to the server and receiving items, the game menu will appear.

## Continuing a MultiWorld Game
1. After opening the game, you should see a list of your save files and a button to add a new one.
2. Find the save file you want to use, then click its `Play` button.
3. On the next screen, the input fields will be filled with the information you've written previously. You can adjust some fields if needed, then press the `Connect` button.
4. If successful, the status on the top-right will change to "connected". If not, a red error message will appear.
5. After connecting to the server and receiving items, the game menu will appear.

## Troubleshooting
### The game opens normally without the new menu.
If the new menu mentioned previously doesn't appear, it can be one of two issues:
 - If there was no console appearing when opening the game, this means the mods didn't load correctly. Here's what you can try:
   - If you are using the mod manager, make sure to open it and press `Start Modded`. Opening the game normally from Steam won't load any mods.
   - Check if the mod manager correctly found the game path. In the mod manager, click `Settings` then go to the `Locations` tab. Make sure the path listed under `Change Inscryption directory` is correct. You can verify the real path if you right-click the game on steam and click `Manage` > `Browse local files`. If the path is wrong, click that setting and change the path.
   - If you installed the mods manually, this usually means BepInEx was not correctly installed. Make sure to read the installation guide carefully.
   - If there is still no console when opening the game modded, try asking in the [Archipelago Discord Server](https://discord.gg/8Z65BR2) for help.
 - If there is a console, this means the mods loaded but the ArchipelagoMod wasn't found or had errors while loading.
   - Look in the console and make sure you can find a message about ArchipelagoMod being loaded.
   - If you see any red text, there was an error. Report the issue in the [Archipelago Discord Server](https://discord.gg/8Z65BR2) or create an issue in our [GitHub](https://github.com/DrBibop/Archipelago_Inscryption/issues).

### I'm getting a different issue.
You can ask for help in the [Archipelago Discord Server](https://discord.gg/8Z65BR2) or, if you think you've found a bug with the mod, create an issue in our [GitHub](https://github.com/DrBibop/Archipelago_Inscryption/issues).

# Credits
Developed by Ballin Inc. :
 - DrBibop
 - Glowbuzz