# Kettle core specification

> See Appendix A of this document for a complete communication example between endpoints.

# Block allocation

Block ID | Owner | Usage description
:---: | :--- | :---
F0~FF | Kettle Core Team | Blocks are in use for the `Kettle Core Protocol`

## Error structure

The error structure is defined on block **F0**, which becomes a valid error structure reference.

```javascript
{
  // [REQ] User friendly error message.
  "message": "I don't know this packet type `15` for block `F5`",
  // [OPT] Implementation related context.
  "context": "Producer: F5-F2"
}
```

## Block distribution

Block ID | Used for | Notes
:---: | :--- | :---
F0 | Endpoint information exchange | Used for polling supported blocks and operations.
F1 | Meta Game Information | Used for exchanging information about specific games. eg: Player accounts, mode, ranks, decks.
F2 | Game State requests | Used for exchanging information of a specific game. eg: Game history/Tag changes.

## Block addresses

Block ID | Address | Used for | Req | Resp
:---: | :---: | :--- | :---: | :---:
F0	| 0 | Pull brief information of blocks supported by the server. | N | Y
.		| 1 | Pull comprehensive data of specific blocks. | Y | Y
F1	| 0 | Pull game metadata. | N | Y
.		| 1 | ~~Pull full player data~~ | - | -
.		| 2 | Pull deck data of player 1. | N | Y
.		| 3 | Pull deck data of player 2. | N | Y
.   | 4 | Push deck data for authenticated player. | Y | N
F2	| 0 | Pull full game state for latest turn. | N | Y
.		| 1 | ~~Pull game state update starting from latest turn~~ | - | -
.		| 2 | Stream game state updates from latest turn [Server -> Client]. | N | Y
.		| 3 | Stream game state updates from latest turn [Player -> Server]. | Y | N


# F0-0

> No request structure

## Response

```javascript
{
  // [REQ] Identification of the server instance.
  "server_id": string,
  // [REQ] Version indication of server.
  "identification": string,

  // [REQ] Lists all supported blocks/block ranges.
  "blocks": [
    {
      // For single blocks the `range_start` and `range_end` contain the same values.

      // [REQ] Start of object range
      "range_start": F0,
      // [REQ] End of object range
      "range_end": FF,
      // [REQ]
      "owner": "Kettle Core",
      // [REQ] Value of the block which holds the error structure to be for decoding invalid Kettle
      // Packets.
      "error_struct_ref": F0,
      // [REQ] All supported addresses for the given block.
      "addresses": [
          F0-00, F0-01, F1-00, F1-01, F1-02, F1-03, F2-00, F2-01, F2-02, ..
      ]
    }
  ]
}
```

# F0-1

## Request

```javascript
{
  // [REQ] Start of the block range to fetch specific information for.
  "range_start": u32,
  // [REQ] End of the block range (inclusive).
  "range_end": u32,
}
```

## Response

Information is sent for one block per Kettle Packet. When a block is not supported/unknown it's skipped.

> There are at most as many Kettle Packets sent as the length of the requested range.

```javascript
{
  // [REQ] Identification of the block, for which information is sent.
  "block_id": u32,
  // [REQ]
  "owner": string,
  // [REQ]
  "version": string,
  // [REQ] Short/Medium description about the block.
  "description": string,
  // [REQ]
  "error_struct_ref": u32,
  // [REQ]
  "rfc_href": string,

  // [REQ] All supported addresses for the current block. This array contains at most 16 items (inclusive).
  "addresses": [
    {
      // [REQ] Id of the address, includes the
      "address_id": u32,
      // [REQ]
      "description": string,
      // [REQ] If this address has an explicit request structure
      "request_struct": bool,
      // [REQ] If this address has an explicit response structure
      "response_struct": bool,
    }
  ]
}
```

# F1-0

> No request structure

## Response

By default the player information can define a rank value and the amount of arena wins.
Optional information and/or internet links can be defined by extension blocks.

```javascript
{
  // [REQ]
  "game_format": FormatType::,
  // [REQ]
  "game_type": GameType::,

  // [REQ] Player information
  "players": [
    {
      // [REQ]
      "name": string,
      // [OPT]
      "rank": u32,
      // [OPT]
      "arena_wins": u32,
    }
  ]
}
```

# F1-1

# F1-2

> No request structure

## Response

```javascript
{
  // [REQ]
  "cards": [
    {
      // [OPT]
      "dbf_id": u32,
      // [OPT]
      "card_id": string,
    }
  ]
}
```

# F1-3

Identical to F1-2.

# F1-4

> No request structure

## Response

```javascript
{
  // [REQ]
  "cards": [
    {
      // [OPT]
      "dbf_id": u32,
      // [OPT]
      "card_id": string,
    }
  ]
}
```

# F2-0

> No request structure

## Response

```javascript
{
  // A clock value is necessary for the receivers to be able to properly sync full game updates and partial updates.
  // 'for_turn' is a monotone clock, in this case, which starts at 0 (pre-game) and increments every time a player starts his turn.
  // 0 is considered the mulligan turn, where updates reflect mulliganed cards.
  // [REQ]
  "for_turn": u32, // 0-indexed

  // [REQ] Contains a complete dump of all the entities in the game, including the game itself.
  "game_state": [ // Array of Full Entity structures!
    {
      // [REQ] Entity ID of the described entity
      "id": u32, // 1-indexed

      // keys dbf_id and card_id are both optional and can be ommitted.
      // The entity is considered 'hidden' if both keys have their default value (0 and empty string respectively) or ommitted.
      // dbf_id gets precedence above card_id!
      // [OPT]
      "dbf_id": u32, // 0-indexed
      // [OPT]
      "card_id": string,

      // [REQ]
      "tags": [
          {"key": GameTag::, "value": u32},
          /* [REPEATED SEQUENCE] */
          ]
    },
    /* [REPEATED SEQUENCE] */
  ]
}
```

# F2-1

This payload will always contain updates for exactly 1 turn.

> No request structure

## Response

```javascript
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
        // [REQ] Parameter of this meta data object.
        // The 'functionionality' which gets executed with this parameter is
        // defined by the `index` value.
        "data": 0,

        // [REQ] List of entity ID's which undergo the effect
        "info": [u32]
      }
    },

  ]
}
```

# F2-2

> No request structure

## Response

The responder WILL KEEP sending kettle response packets with the Complete flag UNSET.  
When the responder stops sending response packets for this packet type, the last packet will have the Complete flag SET.  

```javascript
{
  // [REQ] The turn from where the updates start.
  "from_turn": u32,
  // block_id is a monotone clock for each new top level power block.
  // nested blocks are NOT counted.
  // The clock is reset when the `turn` value increases
  // [REQ]
  "from_block_id": u32,

  // The history array holds the same objects as defined by E2-1's `history` key.
  // [REQ] Holds the updates starting from the offset defined by
  // `from_turn` and `from_block_id` up until the next top-level block (not included!)
  "history": []
}
```

# F2-3

> No request structure

## Response

Identical structure of F2-2's response.


# Notes

1. Both card_id and dbf_id are valid keys on entity details. Both keys are optional, but when both are provided the dbf_id takes precedence.

2. `PowerType::`, `MetaDataType::`, `GameTag::`, `FormatType::`, `GameType::` are integer types (u32), which range is limited to the
values contained by the enumerations, with corresponding name, defined by http://hearthstonejson.com
