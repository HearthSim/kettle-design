# Kettle protocol

> To discuss this proposal, please see https://hearthsim.info/join/ about how to reach out to us.

## Philosophies

I. Tiny amount of data transferred

II. Stateless

III. JSON, easy to decode, easy to encode; no protobuf dependency

IV. Able to send game state deltas or full snapshots

V. Able to support a subset of the protocol or the full protocol

VI. Extensible enough to be used to communicate between stove and simulators, using extensions

VII. As close to the original HS protocol as possible (use real game enums, from hearthstonejson.com, etc)

### Example

General structure of a **Kettle Packet**: [Kettle Header][Kettle Payload]

|Kettle Header (HEX representation): [Kettle producer]-[packet identifier]-[Size High]-[Size Low]

|Kettle Payload (STR representation): [Payload]

	example, Kettle Core Request `Block data` object, with size 0kb
	E0-0A-00-00
	{}

	example, Kettle Core Response `Block data` object, with size 50kb
	E0-16-14-00
	{}

> More examples can be found at the bottom of this document.

## Description

The idea is to implement a simple RPC system.

1. The sender invokes remote methods by transmitting a request packet (Response flag is UNSET) for a specific packet type.
2. The receiver processes the request and responds accordingly.
3. The sender receives the response packet and performs operations on the payload data.

Endpoint processes each request in the order it was sent, but there is no guarantee that multiple requests with the same Packet type result in the same ordered responses.

Endpoints are allowed to put restrictions on the subset of available Packet types and the requests per minute it handles.

> The protocol assumes all packets are received in the order they were originally sent.

## Kettle producer

- Provides extensibility on the core protocol.

- All values above E0 are reserved for core protocol usage.

Extension implementers can reserve blocks which each hold 16 unique adresses. These addresses are implemented within the Message Type.
The range of blocks which can be allocated go from 00 up until DF (inclusive).
This provides the ability to allocate a maximum of 224 different extensions. Less if multiple blocks are allocated for one extension.

Each owner of an allocated block (or range of blocks) must choose a default error payload structure, which can be returned upon processing an invalid request.
The owner can also choose to use the default error payload structure, defined by the Kettle Core Team, if no custom structure is necessary.

### Current allocation

Block ID | Owner | Usage description
:---: | ---: | ---
E0~FF | Kettle Core Team | Blocks are in use for the core protocol

## Packet identifier

- Identifies the semantic meaning of the packet, can hold 16 addresses: 0 to 15 (inclusive). [4 High bits] ~= Packet Type

- Holds bit flags specifying meta information about the payload. [4 Low bits]

Bit X | Name | Description
:----: | --- | ---
0	| Response flag | Makes the distinction between request/response packets.
1	| Invalid flag | Makes the distinction between valid/invalid requests (only applicable within response packets).
2	| Complete flag | Makes the distinction between partial/complete packets.
3	| *Reserved* |

The payload types and flags are strongly linked, always process both together like a tuple!
The value of a flag is 0 when it's UNSET, 1 when SET.

### Response flag

**The flag is 0 for requests, 1 for responses.**

Each packet type is an indication for at least one explicit payload structure and one (or less) implicit payload structure: the empty payload.
Explicit structures are defined for performing a request **OR** a response for their corresponding packet type.
Implicit payload structures are useful when performing a request, since most of the time there is no data which needs to be provided (like a HTTP GET request).

It's considered an error when the empty payload is expected by the received and the Kettle Header holds a payload size greater than 0.

> In rare cases it's possible that the same payload structure can be useful for both request and response packet types.

### Invalid flag

**The flag is 0 for a valid request, 1 for an invalid request.**
> This flag only matters when the Response flag is SET!

Endpoints indicate if the received request was malformed by returning a response packet with this flag set.
The endpoint is not obligated to send back why a request was invalid, but the general suggestion is to always do this, and can leave the Payload Size of the packet at 0 bytes.

**IF** a size is set, the default error payload structure for the reserved block is used to decode the payload.

### Complete flag

**The flag is 0 indicating a partial packet, 1 for a complete packet.**

The core protocol is not responsible for splitting up payloads to fit the maximum amount of bytes. It does however support an indication to the implementer that a Kettle packet is only partially sent.
The endpoint can be sure all data for a Kettle Packet is transmitted when this flag is SET.

The payload **MUST ALWAYS BE** a valid UTF-8 JSON encoded object. The implementor itself is responsible for correctly splitting up it's payload contents!

## Size [High + Low]

- Identifies how many **bytes** are used to encode the payload.

The size part of the Kettle header can hold a value up to 65536 (exclusive) bytes, which is a value around the maximum MTU for systems following the IPv4 and v6 protocols.
Since the header has (always) an encoded size of 4 bytes, we allow the payload to have a maximum size of (65536-4) bytes per packet. The maximum payload size becomes 65532, or FF-FC, bytes.

> The payload is allowed to have a size of 0 bytes! This means the next Kettle Packet will follow right after the current Kettle Header.

## Payload

The payload holds the actual data being transmitted to endpoints. The payload must be an UTF-8 representation of a valid JSON object.

The structure of the payload is indicated by the Packet Identifier. Each block owner must provide schema's for each payload structure it encodes into block addresses.

> Even though the packet examples in this document have a prettified representation, the newline characters should not be sent between endpoints!

### Kettle Core

> C = the client application; S = the server application

#### Block distribution

Block ID | Used for | Notes
:---: | ---: | ---
E0 | Endpoint information exchange | Used for polling supported blocks and types (~ operations).
E1 | Meta Game Information | Used for exchanging information about specific games. eg: Player accounts, mode, ranks, decks.
E2 | Game State requests | Used for exchanging information of a specific game. eg: Game history/Tag changes.

#### Error payload

The following is an example of the default error payload under the reserved blocks for Kettle Core.
The error payload does not have an explicit Packet Type, because the structure is implicitly used when the invalid flag is set.

```
E5-FA-00-6B
{
	// [REQ] User friendly error message.
	"message": "I don't know this packet type `15` for block `E5`",
	// [OPT] Implementation related context.
	"context": "Packet identifier: E5-F2-00-15"
}
```

#### Endpoint information

Type ID | Used for
:---: | ---
0 | Pull supported blocks data, includes block addresses. eg: Block range, owner name, website
1 | Pull full block data.

#### Meta Game information

Type ID | Used for
:---: | ---
0 | Pull highlevel metadata. eg: Game id, game mode, player 1+2 account id.
1 | Pull full player data.
2 | Pull player 1 deck data.
3 | Pull player 2 deck data.

#### Game State information

Type ID | Used for | Notes
:---: | --- | ---
0 | Pull game history | A complete state from the beginning of the last started turn is returned.
1 | Pull game updates | *stateless is a bit of an issue here*, State changes starting from the chosen turn up until the 'live' state are returned.
2 | Stream game updates | Instructs the server to keep sending live state updates.
3 | Post game history

##### Pull Game history

C -> S
```
// Pull game history, Flags: (request)+(valid)+complete, size: 0
E2-02-00-00
```

S -> C
```
// Pull game history, Flags: response+(valid)+(partial), size: 65532
E2-08-FF-FC
{
	// A clock value is necessary for the receivers to be able to properly sync
	// full game updates and partial updates.
	// 'for_turn' is a monotone clock, in this case, which starts at 0 and increments every time
	// a player ends his turn.
	// [REQ]
	"for_turn": 0, // This is the state at the BEGINNING of the game!

	// [REQ] Contains a complete dump of all the entities in the game, including the game itself.
	"game_state": [ // Array of Full Entity structures!
		{
			[REQ] Entity ID of the described entity
			"id": 1, // This is the game entity

			// keys dbf_id and card_id are both optional and can be ommitted.
			// The entity is considered 'hidden' if both keys are omitted or have their default
			// value (0 and empty string respectively)
			// dbf_id gets precedence above card_id!
			// [OPT]
			"dbf_id": 0, // Unknown DBF ID -> The game itself has no CARD.
			// [OPT]
			"card_id": "", // Unknown CARD ID

			[REQ]
			"tags": [
					{"key": 49, "value": 1}, // Zone: Play
					{"key": 202, "value": 1} // CardType: Game
					]
		},

		{ // This is controller 1 entity
			// [REQ]
			"id": 2,
			// [OPT]
			"dbf_id": 0,
			// [OPT]
			"card_id": "",
			// [REQ]
			"tags": [],
		},
		...
	]
}

// Pull game history, Flags: response+(valid)+complete, size: 5
E2-0A-00-05
{
	// [REQ]
	"for_turn": 0, // Same clock value for all response parts of E2-0 request

	// [REQ]
	"game_state: [ // Array of Full Entity structures!
		{
			// [REQ]
			"id": 160,
			// [OPT]
			"dbf_id": 1155424,
			// [REQ]
			"tags": [ // All tag keys are derived from HearthStoneJSON.com
				{
					"key": 47, // Tag 'Atk'
					"value": 5
				}
			]
		}
	]
}
```

##### Pull game updates

C -> S
```
// Pull game updates, Flags: (request)+(valid)+complete, size: 0
E2-12-00-00
```

S -> C
```
// Pull game updates, Flags: response+(valid)+complete, size: 65532
E2-1A-00-00
{
	// [REQ] See above for explanation about for_turn.
	"for_turn": 0,
	// [OPT] Possible upper limit?, maybe not necessary.
	"until_turn": 7,

	// [REQ] Holds game history data in a structure which resembles the HS Power format.
	"history": [
		{
			// [REQ] Indicates which kind of history object is encoded into `data`
			"type": "power", // Can be power, block, or meta

			// `Index` value interpretation changes according to `type`.
			// power => PowerType enum
			// block => BlockType enum
			// meta => MetaDataType enum
			// [REQ] int value for corresponding enums
			"index": 4, // PowerType::Tag_Change

			// [REQ] Holds structured data
			"data": { // Power type structure

				// [REQ] The entity ID who's data is described.
				"id": 1, // Game entity

				// keys dbf_id and card_id are both optional and can be ommitted.
				// Entities are considered 'shown' when their `dbf_id` or `card_id` is known.
				// dbf_id gets precedence above card_id!
				// [OPT]
				"dbf_id": 0,
				// [OPT]
				"card_id": "",

				// [REQ] Collection of tags which are updated.
				"tags": [
					{
						// [REQ] Integer value for corresponding GameTag value.
						"key": 204, // GameTag::State
						// [REQ] (Updated) value mapped to the GameTag key.
						"value": 2 // State::Running
					}
				]
			}
		},
		{
			// [REQ]
			"type": "power",
			// [REQ]
			"index": 4, // PowerType::Tag_Change

			// [REQ]
			"data": {
				// [REQ]
				"id": 2, // Player 1

				// [OPT]
				"dbf_id": 0,
				// [OPT]
				"card_id": "",

				// [REQ]
				"tags": [
					{
						// [REQ]
						"key": 17, // GameTag::PlayState
						// [REQ]
						"value": 1 // PlayState::Playing
					}
				]
			}
		},

		/* START OF NEW TOP LEVEL BLOCK */
		{
			// [REQ]
			"type": "block",
			// [REQ]
			"index": 5, // BlockType::trigger

			// [REQ]
			"data": { // Block type structure

				// Blocks can be nested! To accomodate this we prepend and append each block collection
				// with a block object specifically formed block object.
				// The first has `start` set to true, the last has `start` set to false -> indicating the
				// end of the collection.
				// `source_entity` and `target_entity` are ignored when `start` is set to false.

				// [REQ] Indicates if this object indicates the start of a block.
				"start": true, // Indicates the trigger block opened

				// [REQ] Entity ID which is causing the tag changes contained within this block.
				"source_entity": 51,

				// [REQ] Entity ID which is the target of the tag changes contained within this block.
				"target_entity": 0,
			}
		},
		{
			// [REQ]
			"type": "power",
			// [REQ]
			"index": 4, // PowerType::Tag_Change
			// [REQ]
			"data": {
				"id": 51,

				// [OPT]
				"dbf_id": 521441, // Reveal entity
				// [OPT]
				"card_id": "",

				"tags": [
					{
						"key": 49, // GameTag::Zone
						"value": 1 // Zone::Play
					}
				]
			}
		},

				/* START OF NEW NESTED BLOCK */
				// Power blocks CAN be nested!
				// This block is nested within the parent trigger block
				{
					// [REQ]
					"type": "block",
					// [REQ]
					"index": 9, // BlockType::Ritual

					// [REQ]
					"data": {
						// [REQ] Indicates if this object indicates the start of a block.
						"start": true, // Indicates the trigger block opened

						// [REQ] Entity ID which is causing the tag changes contained within this block.
						"source_entity": 51,

						// [REQ] Entity ID which is the target of the tag changes contained within this block.
						"target_entity": 81, // Proxy C'Thun is target of ritual
					}
				}

				{
					// [REQ]
					"type": "power",
					// [REQ]
					"index": 4, // PowerType::Tag_Change
					// [REQ]
					"data": {
						"id": 81, // Proxy C'Thun
						"tags": [
							{"key": 47, "value": 20 }, // Buff attack
							{"key": 45, "value": 20 } // Buff health
						]
					}
				}

				{ // End of ritual block
					// [REQ]
					"type": "block",
					// [REQ]
					"index": 9, // BlockType::Ritual
					// [REQ]
					"data": {
						// [REQ]
						"start": false,

						// `source_entity` and `target_entity` aren't processed, so the values default to zero.
						// [REQ]
						"source_entity": 0,
						// [REQ]
						"target_entity": 0,
					}
				}
				/* END OF NESTED BLOCK */


		{ // End of trigger block
			// [REQ]
			"type": "block",
			// [REQ]
			"index": 5, // BlockType::trigger
			// [REQ]
			"data": {
				// [REQ]
				"start": false,

				// `source_entity` and `target_entity` aren't processed, so the values default to zero.
				// [REQ]
				"source_entity": 0,
				// [REQ]
				"target_entity": 0,
			}
		}
		/* END OF TOP LEVEL BLOCK */
	]
}
```
##### Stream Game history

Special type which can be used accross web sockets. The receiver of this request keeps pushing partial response objects when they are available.

C -> S
```
// Stream game updates, Flags: (request)+(valid)+complete, size: 0 bytes
E2-22-00-00
```

S -> C
```
// Stream game updates, Flags: response+(valid)+(partial), size: 160 bytes
E2-28-00-70
{
	// The timing of this update is positioned at turn 5.
	"turn": 5,
	// block_id is a monotone clock for each new top level power block.
	// nested blocks are NOT counted
	"block_id": 20,
	"history": [
		/* START OF NEW TOP LEVEL BLOCK */
		{
			"type": "block",
			"index": 7, // BlockType::Play
			"data": {
				"start": true,
				"source_entity": 16, // Entity which is 'Played'
				"target_entity": 50, // There is no play target
			}
		},
				/* START OF NEW INNER BLOCK */
				{
					"type": "block",
					"index": 3, // BlockType::Power
					"data": {
						"start": true,
						"source_entity": 16, // Entity which is 'Played'
						"target_entity": 50, // There is no play target
					}
				}
				{
					"type": "meta",
					"index": 0, // MetaDataType::Meta_Target
					"data": { // MetaData structure
						// [REQ] Parameter of this meta data object; in case of MetaDataType::Meta_Healing this would
						// hold the amount to be healed (PRE_HEAL)
						"data": 0,
						// [REQ] List of entity ID's which undergo the effect
						"info": [50]
					}
				},
				{
					"type": "power",
					"index": 4, // PowerType::Tag_Change
					"data": {
						"id": 50,
						"tags": [
							{
								"key": 425, // GameTag::Prehealing
								"value": 2 // Entity got healed, which gives it an actual health up to it's total health.
							}
						]
					}
				},
				{
					"type": "power",
					"index": 4, // PowerType::Tag_Change
					"data": {
						"id": 50,
						"tags": [
							{
								"key": 425, // GameTag::Prehealing
								"value": 0 // Entity got healed, which gives it an actual health up to it's total health.
							}
						]
					}
				},
				{
					"type": "meta",
					"index": 2, // MetaDataType::Meta_Healing
					"data": { // MetaData structure
						// [REQ]
						"data": 2, // Amount of healing
						// [REQ]
						"info": [50]
					}
				},
				{
					"type": "power",
					"index": 4, // PowerType::Tag_Change
					"data": {
						"id": 50,
						"tags": [
							{
								"key": 44, // GameTag::Damage
								"value": 0 // Entity got healed, which makes it become 'undamaged'.
							}
						]
					}
				},

				{ // End of Power Block
					"type": "block",
					"index": 3, // BlockType::Power
					"data": {
						"start": false,
						"source_entity": 0,
						"target_entity": 0,
					}
				}
				/* END OF INNER BLOCK */

		{ // End of Play Block
			"type": "block",
			"index": 7, // BlockType::Play
			"data": {
				"start": false,
				"source_entity": 0,
				"target_entity": 0,
			}
		}

		/* END OF TOP LEVEL BLOCK */
	]
}

// Stream game updates, Flags: response+(valid)+complete, size: 80 bytes
E2-28-00-50
{
	// The timing of this update is positioned at turn 5.
	"turn": 5,
	// block_id is a monotone clock for each new top level power block.
	// nested blocks are NOT counted
	"block_id": 21, // -> Top level block idx incremented.
	"history": [
		/* START OF TOP LEVEL BLOCK */
		{
			"type": "block",
			"index": 1, // BlockType::Attack
			"data": {
				"start": true,
				"source_entity": 81, // Proposed attacker
				"target_entity": 131, // Proposed defender
			}
		},

		//////////////////////////////////
		// Bunch of tag changes			//
		// Meta data for damage			//
		// More tag changes				//
		//////////////////////////////////

		{ // End of Play Block
			"type": "block",
			"index": 7, // BlockType::Play
			"data": {
				"start": false,
				"source_entity": 0,
				"target_entity": 0,
			}
		}

		/* END OF TOP LEVEL BLOCK */
	]
}
```
