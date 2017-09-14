# Kettle RFC 001 Appendix A

This document describes the communicated data between two endpoints; the server and client.

Actual size of the payloads is not given and is substituted by XX-XX when the payload length is larger than 0 bytes.

## Initiate connection from client to server

The protocol expects the transport layer to provide ordered delivery of sent Kettle packets.
The protocol data can be directly transmitted on top of any TCP-like transport layer. Wrapping in HTTP and WebSockets might be best.

```
// REQUEST blocks and addresses supported by the server.
E0-00-00-00 {}

// RESPOND with supported blocks.
E0-0A-XX-XX
{
	"server_id": "HSReplay frontend 15",
	"identification": "v0.0.1-ALPHA",
	"blocks": [
		{
			"range_start": E0,
			"range_end": FF,
			"owner": "Kettle Core",
			"error_struct_ref": E0,
			"addresses": [
					E0-00, E0-10, E1-00, E1-10, E1-20, E1-30, E2-00, E2-10, E2-20, ..
			]
		}
	]
}

// REQUEST more information on specific block.
E0-10-XX-XX
{
	"range_start": E0,
	"range_end": E1,
}

// RESPOND with specific block information.
E0-18-XX-XX
{
	"block_id": E0,
	"owner": "Kettle Core",
	"version": "v0.0.1-ALPHA",
	"description": "Used for polling supported blocks and operations",
	"error_struct_ref": E0,
	"rfc_href": "http://github.com/kettle-design/tree/dfsdffsd5554/rfc/kettle-002.md"
	"addresses": [
		{
			"address_id": E0-00,
			"description": "Used for pulling brief block data and supported addresses",
			"request_struct": false,
			"response_struct": true,
		},
		{
			"address_id": E0-10,
			"description": "Used for pulling comprehensive block data and supported addresses",
			"request_struct": true,
			"response_struct": true,
		},
	]
}
E0-1A-XX-XX
{
	"block_id": E1,
	"owner": "Kettle Core",
	"version": "v0.0.1-ALPHA",
	"description": "Used for polling supported blocks and operations",
	"error_struct_ref": E0,
	"rfc_href": "http://github.com/kettle-design/tree/dfsdffsd5554/rfc/kettle-002.md"
	"addresses": [
		{
			"address_id": E1-00,
			"description": "Used for pulling meta game data",
			"request_struct": false,
			"response_struct": true,
		},
		{
			"address_id": E1-10,
			"description": "Used for pulling game account data",
			"request_struct": false,
			"response_struct": true,
		},
		{
			"address_id": E1-20,
			"description": "Used for pulling deck data of player with id 1",
			"request_struct": false,
			"response_struct": true,
		},
		{
			"address_id": E1-30,
			"description": "Used for pulling deck data of player with id 2",
			"request_struct": false,
			"response_struct": true,
		},
	]
}
```

## Push game state to server


## Retrieve game state from server

```
// REQUEST meta game information.
E1-00-00-00 {}

// RESPOND with meta game information.
E1-0A-XX-XX
{
	"game_id": 5512744121151,
	"game_format": 2, // FormatType::FT_STANDARD
	"game_type": 7, "GameType::GT_RANKED
	"players": [
		{
			"name": "Joffrey",
			"rank": 15,
			"arena_wins": 4,
		},
		{
			"name": "Ramsay",
			"rank": 25,
			"arena_wins": 8,
		}
	],
	"player_view": 1
}

// REQUEST player decks.
E1-20-00-00 {}
E1-30-00-00 {}

// RESPOND with player decks.
E1-2A-XX-XX
{
	"cards": [
		{"dbf_id": 1552141, "card_id": 0 },
		{"dbf_id": 1552141, "card_id": 0 },
		{"dbf_id": 1521443, "card_id": 0 },
		{"dbf_id": 854521, "card_id": 0 },
		{"dbf_id": 632254, "card_id": 0 },
		{"dbf_id": 12442, "card_id": 0 },
		{"dbf_id": 3356454, "card_id": 0 },
	]
}

E1-3A-XX-XX
{
	"cards": [
		{"dbf_id": 1552141, "card_id": 0 },
		{"dbf_id": 1552141, "card_id": 0 },
		{"dbf_id": 1521443, "card_id": 0 },
		{"dbf_id": 854521, "card_id": 0 },
		{"dbf_id": 632254, "card_id": 0 },
		{"dbf_id": 12442, "card_id": 0 },
		{"dbf_id": 3356454, "card_id": 0 },
	]
}

// REQUEST latest complete game state.
E2-00-00-00 {}

// RESPOND latest complete game state.
// First packet for the response.
E2-08-XX-XX
{
	"for_turn": 4,
	"game_state": [
		{
			"entity_id": 1,
			"tags": [
				// Keys coming from GameTag enum
				{"key": 49, "value": 1}, // GameTag::ZONE = Zone::PLAY
				{"key": 202, "value": 1}, // GameTag::CARDTYPE = CardType::GAME
			]
		},
		..

		{
			"entity_id": 50,
			"dbf_id": 114212141,
			"tags": []
		},
		{
			"entity_id": 51,
			"card_id": "EXH_414h",
			"tags": []
		},
		..
	]
}
// second packet for the response.
E2-0A-XX-XX
{
	"for_turn": 4,
	"game_state": [
		{
			"entity_id": 160,
			"dbf_id": 1155424,
			"tags": [{"key": 47, "value": 5}] // GameTag::ATK = 5
		},
		..

	]
}

// REQUEST game state update.
E2-10-XX-XX
{
	"from_turn": 4
}

// RESPOND game state update to requested range.
E2-1A-XX-XX
{
	"from_turn": 4,
	"until_turn": 5,
	"history": [
		{
			"type": "power",
			"index": 4, // PowerType::TAG_CHANGE
			"data": {
				"entity_id": 1,
				"tags": [
					{"key": 204, "value": 2} // GameTag::STATE = State::Running
				]
			}
		},
		{
			"type": "power",
			"index": 4, // PowerType::TAG_CHANGE
			"data": {
				"entity_id": 2,
				"tags": [
					{"key": 17, "value": 1} // GameTag::PLAYSTATE = PlayState::PLAYING
				]
			}
		},

		{
			"type": "block",
			"index": 5, // PowerType::TRIGGER
			"data": {
				"start": true,
				"source_entity": 51,
				"target_entity": 0,
			}
		},
		{
			"type": "power",
			"index": 4, // PowerType::TAG_CHANGE
			"data": {
				"entity_id": 2,
				"dbf_id": 521441,
				"tags": [
					{"key": 49, "value": 3} // GameTag::Zone = PlayState::HAND
				]
			}
		},
		{
			"type": "block",
			"index": 5, // PowerType::TRIGGER
			"data": {
				"start": false,
				"source_entity": 0,
				"target_entity": 0,
			}
		}
	]
}

```
