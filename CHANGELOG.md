### 0.1.4
 - Fixed broken obol cards not staying in the randomized deck in act 2 if the obol check wasn't done and the obol object was received.
 - Fixed campfire buffs in act 1 applying to multiple cards in rare occasions with the randomized deck.
 - Fixed a bug that added gems related cards to the randomized deck pool in act 3 if the gems module item was received but not fetched.

### 0.1.3
 - Removed grizzly scripted deaths in act 1 (for real this time). Now only removed if the tutorial is skipped or deathlink is on.

### 0.1.2
 - The randomize type option now works in act 2 within the same temple and rarity.
 - Removed grizzly scripted deaths in act 1.
 - Card packs are no longer available while Leshy displays the starting deck in act 1.
 - You can now push your luck in campfires if the skip tutorial option is enabled.
 - Fixed currency item not applying correctly on every act.
 - Fixed Ouroboros card not appearing with deck randomization in act 1.

### 0.1.1
 - Added skip tutorial setting.
 - Fixed a bug that showed the wrong clock clue in act 3 with randomized codes.

### 0.1.0
 - Removed API dependency.
 - Changed internal saving system to use json instead of the API.
 - Added the Ourobot card to the item pool.
 - Added goal setting.
 - Some items are now double-checked when connecting to a server to prevent potential issues.
 - Cards from the Archipelago item pool can now only appear in randomized decks if unlocked.
 - The map now disappears properly before the optional death card choice in act 1.
 - Receiving a deathlink now waits for the player to unpause.
 - Receiving a deathlink in act 2 now sends the player to the world map.
 - The left and right side of the broken obol now always appear in the randomized deck in act 2 if the obol check isn't completed.
 - The pause button is now disabled while dying from deathlink.
 - Talking cards can now only appear once in randomized decks.
 - Card mods now properly stay when randomizing the deck in act 3.
 - The card pool for deck randomization in act 3 has been expanded.
 - Fixed a bug where the holo map appeared out of the player's view after opening a card pack in act 3.
 - Fixed the card pack pile not appearing in act 3 after acquiring the gems module.
 - Fixed a bug where deathlink wouldn't work in certain areas of act 2.
 - Fixed a bug where the act 1 deck wouldn't reset properly on a new run started right after completing act 3.
 - Fixed a bug where the optional death card choice was given when not dying from deathlink instead of the other way around when that setting was chosen.
 - Fixed a bug where deathcards were empty in randomized decks.
 - Fixed an error that occured when receiving a card pack while the pack pile was visible on screen in act 1 and 3.

### 0.0.2
 - The card pack button is now disabled in the act 2 world map (we weren't lazy, the pack opening UI just doesn't exist in that scene lol).
 - Check cards found around the cabin/factory now only grant the check when the card leaves the screen in an attempt to fix some crashes.
 - Death cards can now be found in randomized decks.
 - Fixed a bug that locked the camera in the wrong room when quitting act 2 in a different room than the entrance.
 - Fixed a bug that reverted some received items when first starting act 2.
 - Fixed a bug that locked the chapter select button after starting act 3.
 - Fixed a bug that prevented card modifications from saving when randomizing the deck.
 - Fixed a bug that showed blank names on the first item received after connecting.

### 0.0.1
 - First test build