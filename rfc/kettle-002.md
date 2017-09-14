# Kettle core specification

> See Appendix A of this document for a complete communication example between endpoints.

# Block allocation

Block ID | Owner | Usage description
:---: | :--- | :---
E0~FF | Kettle Core Team | Blocks are in use for the core protocol

## Error structure

The error structure is defined on block E0, which becomes a valid error structure reference.

```
{
	// [REQ] User friendly error message.
	"message": "I don't know this packet type `15` for block `E5`",
	// [OPT] Implementation related context.
	"context": "Packet identifier: E5-F2-00-15"
}
```

## Block distribution

Block ID | Used for | Notes
:---: | :--- | :---
E0 | Endpoint information exchange | Used for polling supported blocks and operations.
E1 | Meta Game Information | Used for exchanging information about specific games. eg: Player accounts, mode, ranks, decks.
E2 | Game State requests | Used for exchanging information of a specific game. eg: Game history/Tag changes.

## Block addresses

Block ID | Address | Used for | Req | Resp
:---: | :---: | :--- | :---: | :---:
E0	| 0 | Pull brief information of blocks supported by the server. | N | Y
.		| 1 | Pull comprehensive data of specific blocks. | Y | Y
E1	| 0 | Pull game metadata. | N | Y
.		| 1 | ~~Pull full? player data~~ | . | .
.		| 2 | Pull deck data of player 1. | N | Y
.		| 3 | Pull deck data of player 2. | N | Y
E2	| 0 | Pull game state. | N | Y
.		| 1 | Pull game state update. | N | Y
.		| 2 | Stream game state updates. | N | Y
.		| 3 | ~~Post game history.~~ | Y | N


# E00

# E01

# E10

# E11

# E12

# E13

# E20

> No request structure

## Response

```
{
	// A clock value is necessary for the receivers to be able to properly sync full game updates and partial updates.
	// 'for_turn' is a monotone clock, in this case, which starts at 0 (pre-game) and increments every time a player starts his turn.
	// 0 is considered the mulligan turn, where updates reflect mulliganed cards.
	// [REQ]
	"for_turn": u32, // 0-indexed

	// [REQ] Contains a complete dump of all the entities in the game, including the game itself.
	"game_state": [ // Array of Full Entity structures!
		{
			[REQ] Entity ID of the described entity
			"id": u32, // 1-indexed

			// keys dbf_id and card_id are both optional and can be ommitted.
			// The entity is considered 'hidden' if both keys have their default value (0 and empty string respectively) or ommitted.
			// dbf_id gets precedence above card_id!
			// [OPT]
			"dbf_id": u32, // 0-indexed
			// [OPT]
			"card_id": string,

			[REQ]
			"tags": [
					{"key": GameTag::, "value": u32},
					[REPEATED SEQUENCE]
					]
		},
		[REPEATED SEQUENCE]
	]
}
```

# E21

This packet will always contain updates for exactly 1 turn.
Updates spanning multiple turns are not supported!;

 - issue multiple requests with different turn numbers;
 - pull the full state from the desired turn.

## Request

```
{
	// [REQ] The updates have to encompass the entire turn.
	// See also E20::for_turn
	"from_turn": u32,
}
```

## Response

```
{
	// [REQ] This packet will provide updates which can be applied to the
	// full state at the beginning of the turn number.
	// See also E20::for_turn
	"from_turn": u32,

	// [REQ] The turn number indicating the range of turns for which these updates apply.
	// See also `from_turn`
	"until_turn": u32,

	// [REQ] Contains objects which mimic the power history format of the actual HS game.
	"history": [

		// There are 3 types of history objects:
		//	- Power
		//	- MetaData
		//	- Block
		//
		// Each type is identifiable by their `type` value.
		// The structure of the `data` key is different for each typed object.

		// Block objects are used to indicate the start and end of an event-chain. This chain consists
		// of entity updates and visual cues which are a result of a specific interaction
		// with/within the game.
		// Block objects contain Power and MetaData objects and can be nested.
		// The `data::start` value indicates the beginning or end of the chain.

		/* START OF BLOCK */
		{
			// [REQ]
			"type": "block",
			// [REQ]
			"index": PowerType::

			// [REQ]
			"data": {
				// [REQ] Indicates the beginning of the block chain.
				"start": true,
				// [REQ] The entity causing the chain.
				"source_entity": u32,
				// [REQ] The entity which will be influenced by this chain.
				"target_entity": u32,
			}
		},
		{
			// [REQ]
			"type": "block",
			// [REQ]
			"index": PowerType::

			// [REQ]
			"data": {
				// [REQ] Indicates the end of the chain.
				"start": false,
				// [REQ] Not interpreted when `start` is false.
				"source_entity": 0,
				// [REQ] Not interpreted when `start` is false.
				"target_entity": 0,
			}
		}
		/* END OF BLOCK */

		// Power objects represent a specific action which must be executed by the game's state machine.
		// These actions manipulate the game and/or it's containing entities.
		// Power objects CAN occur outside of block chains!
		{
			// [REQ]
			"type": "power",
			// [REQ]
			"index": PowerType::,

			// [REQ]
			"data": {
				// [REQ] Entity undergoing an effect
				"entity_id": u32,

				// Just like in E20, following keys are both optional.
				// If these keys are present they will override the actual value for the given entity, use the
				// default value to 'hide' the entity OR use PowerType::HIDE_ENTITY as `index` value.
				// Omitting these keys will not affect their visibility state or identity.
				// [OPT]
				"dbf_id": u32,
				// [OPT]
				"card_id": string,

				// [REQ]
				"tags": [
					{"key": GameTag::, "value": u32}
				]
			}
		},

		// MetaData objects are used to give visual cues to the client.
		// The semantic meaning of the `data` object is stronly dependant on the `index` value.
		// MetaData objects normally DO NOT occur outside of block chains!
		{
			// [REQ]
			"type": "meta",
			// [REQ]
			"index": MetaDataType::

			// [REQ]
			"data": {
				// [REQ] Parameter of this meta data object. The 'functionionality' which gets executed with this parameter is
				// defined by the `index` value.
				"data": 0,

				// [REQ] List of entity ID's which undergo the effect
				"info": [u32]
			}
		},

	]
}
```

# E22

# E23
