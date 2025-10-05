### 1.0.3
 - Fixed client version string forcing the use of a discontinued client version.

### 1.0.2
 - Updated Archipelago.MultiClient.Net to 6.6.0.
 - Fixed crash that would sometimes occur on a deathlink during act 2.
 - Fixed basic cards in act 2 randomizing when they shouldn't with randomize by type enabled.
 - Fixed deathcard getting skipped if dying normally and the last choice was to skip with "deathlink only" option.

### 1.0.1
 - Updated Archipelago.MultiClient.Net to 6.5.0 which fixes disconnect issues after hinting.

### 1.0.0
 - Fixed a bug that would sometimes cause act 2 battles to break on start when randomizing sigils.

### 0.3.1
 - Aded support for updated options from the latest apworld.
 - The hoarder sigil can no longer appear in act 2 with randomized sigils on as it was not implemented in this act.
 - Death link no longer applies during the epilogue.
 - Fixed a bug that gave all epitaph pieces for free in act 2 if they were grouped as one item.

### 0.3.0
 - Added support for new options:
	- Added option to skip the epilogue.
	- Added option to adjust the 2nd and 3rd painting checks. They can be moved to later spheres or be forced to only contain filler items.
	- Added a new choice for the "Randomize Deck" option to only randomize starter decks.
	- Added a new choice for the "Randomize Sigils" option to randomize all sigils on cards.
 - The "Randomize Abilities" option has been renamed to "Randomize Sigils".
 - Basic cards in act 2 (squirrels, skeletons, mox cards) are no longer randomized when randomizing the deck by type.
 - A message appears after completing acts.
 - Card pools for deck randomization and sigil pools for sigil randomization have been adjusted.
 - The Mycobot card reward from the Mycologists boss is now included in the random card pool when receiving it.
 - Dates in the chapter select screen have been replaced with act titles that turn green when completed.
 - Fixed all candles extinguishing in act 1 combats after receiving deathlink even with the single candle option selected.
 - Fixed multiple deathlink errors and moved to a failsafe approach in case more errors occur during deathlink.
 - Fixed Grimora's ghouls not fighting the player if the Grimora puzzle was completed before fighting them.
 - Fixed modded sigils being randomized in act 3 even with the option disabled when randomizing the deck.
 - Fixed modded sigils not randomizing even with the option enabled when not randomizing the deck.
 - Fixed the Hrokkall card unintentionally appearing in act 1 after starting act 2.

### 0.2.2
 - The caged wolf card will always appear in your deck if you own it when randomizing your deck.
 - The oil painting puzzles in act 1 will now contain a bee instead of a squirrel if you have the bee figurine.
 - Fixed the camera replica check being impossible to get if all 3 of Leshy's subordinates were defeated before ever talking to Leshy in act 2.
 - Fixed a lag spike that would occur when receiving a card item after connecting to the server.
 - Fixed some items that would wrongfully be marked as "failed to apply".

### 0.2.1
 - Single candle deathlink no longer forces a view switch towards the candles if more lives are still remaining.
 - Fixed single candle deathlink soft-locking the game in certain situations.
 - Fixed game freeze during screen transitions in act 3 when the deck is randomized.
 - Fixed certain items that were being reapplied by mistake on connect.

### 0.2.0
 - A new save file system allows you to create and select save files on startup to facilitate playing in multiple multiworlds.
 - Added a new setting for how deathlink behaves in act 1. You can now choose to only lose a single candle when receiving a deathlink and send a deathlink when losing a candle.
 - Added a new setting for how epitaph pieces are randomized. You can now choose to group them in groups of three or group them all as a single item.
 - Wizard pillars in act 2 now have a random code when randomizing codes.
 - Custom built cards in act 3 are now added to the randomization pool when randomizing cards.
 - Moved the femur pedestal further left in the Bone Lord's lair in act 2 to avoid a double pickup glitch.
 - Luke's file entry locations are now consistent with the place they were found in.
 - Fixed custom built cards acting as a card modification which would randomize into another card with the custom card's stats, abilities and cost on top.
 - Fixed randomized cards in act 3 which could end up with more than the maximum amount of four sigils.
 - Fixed act 2 card items not appearing in your collection if received before first starting act 2.
 - Fixed common act 2 cards sometimes randomizing into rare cards when randomizing cards within the same type.
 - Fixed pelt cards in act 1 randomizing into other rare cards when randomizing cards within same type.
 - Fixed the chapter select option for act 3 sending the player in the starting area where they cannot leave if the epilogue was chosen in the past in the chapter select menu.
 - Fixed some issues with item verification.

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