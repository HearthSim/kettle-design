# Kettle protocol

> To discuss this proposal, please see https://hearthsim.info/join/ about how to reach out to us.

## Philosophies

I. Tiny amount of data transferred

II. Stateless

III. JSON, easy to decode, easy to encode; no protobuf dependency

IV. Able to send game state deltas or full snapshots

V. Able to support a subset of the protocol or the full protocol

VI. Extensible enough to be used to communicate between stove and simulators, using extensions

VII. As close to the original HS protocol as possible

VIII. Hard linked to hearthstoneJSON.com data

### To be discussed

1. Accessible through browsers (implies WebSockets).

2. Context is provided out of band.
eg: the user has to use a certain path (hsreplay.com/live_games/[GAME_ID]). The listening server communicates Kettle only in the context of the GAME_ID, which is NOT SELECTED and a constant value within the protocol structure.

3. Security is handled out of band. See previous example, but utilise a token to authorize access to the resource.

4. Visibility of card data.
	- Should the protocol send all known state (which could include card ID's which are hidden from the current player) when both players upload their live plays?
	- Should the client open 2 streams per game ID, one for each player's 'view' of the visible entities?

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
:---: | ---: | :---
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

## PAYLOADS

Payloads will be defined in seperate documents. For the core payload structures, see kettle-002.md.

[TODO; move this into kettle-002.md]
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
