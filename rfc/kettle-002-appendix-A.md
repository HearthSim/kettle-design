# Kettle RFC 001 Appendix A

This document describes the communicated data between two endpoints; the server and client.

Actual size of the payloads is not given and is substituted by XX-XX when the payload length is larger than 0 bytes.

## Initiate connection from client to server

The protocol expects the transport layer to provide ordered delivery of sent Kettle packets.
The protocol data can be directly transmitted on top of any TCP-like transport layer. Wrapping in HTTP and WebSockets might be best.

```javascript
// [C->S] REQUEST blocks and addresses supported by the server.
F0-02-00-00 {}

// RESPOND with supported blocks.
F0-0A-XX-XX
{
  "server_id": "HSReplay frontend 15",
  "identification": "v0.0.1-ALPHA",
  "blocks": [
    {
      "range_start": F0,
      "range_end": FF,
      "owner": "Kettle Core",
      "error_struct_ref": F0,
      "addresses": [
          F0-00, F0-01, F1-00, F1-01, F1-02, F1-03, F2-00, F2-01, F2-02, ..
      ]
    }
  ]
}

// [C->S] REQUEST more information on specific block.
F0-12-XX-XX
{
  "range_start": F0,
  "range_end": F1,
}

// [S->C] RESPOND with specific block information.
F0-18-XX-XX
{
  "block_id": F0,
  "owner": "Kettle Core",
  "version": "v0.0.1-ALPHA",
  "description": "Used for polling supported blocks and operations",
  "error_struct_ref": F0,
  "rfc_href": "http://github.com/kettle-design/tree/dfsdffsd5554/rfc/kettle-002.md",
  "addresses": [
    {
      "address_id": F0-00,
      "description": "Used for pulling brief block data and supported addresses",
      "request_struct": false,
      "response_struct": true,
    },
    {
      "address_id": F0-10,
      "description": "Used for pulling comprehensive block data and supported addresses",
      "request_struct": true,
      "response_struct": true,
    },
  ]
}
F0-1A-XX-XX
{
  "block_id": F1,
  "owner": "Kettle Core",
  "version": "v0.0.1-ALPHA",
  "description": "Used for polling supported blocks and operations",
  "error_struct_ref": F0,
  "rfc_href": "http://github.com/kettle-design/tree/dfsdffsd5554/rfc/kettle-002.md",
  "addresses": [
    {
      "address_id": F1-00,
      "description": "Used for pulling meta game data",
      "request_struct": false,
      "response_struct": true,
    },
    {
      "address_id": F1-10,
      "description": "Used for pulling game account data",
      "request_struct": false,
      "response_struct": true,
    },
    {
      "address_id": F1-20,
      "description": "Used for pulling deck data of player with id 1",
      "request_struct": false,
      "response_struct": true,
    },
    {
      "address_id": F1-30,
      "description": "Used for pulling deck data of player with id 2",
      "request_struct": false,
      "response_struct": true,
    },
  ]
}
```

## Retrieve game state from server

```javascript
// [C->S] REQUEST meta game information.
F1-02-00-00 {}

// [S->C] RESPOND with meta game information.
F1-0A-XX-XX
{
  "game_format": 2, // FormatType::FT_STANDARD
  "game_type": 7, // GameType::GT_RANKED
  "players": [
    {
      "name": "Joffrey",
      "arena_wins": 4,
    },
    {
      "name": "Ramsay",
      "arena_wins": 8,
    }
  ]
}

// [C->S] REQUEST player decks.
F1-22-00-00 {}
F1-32-00-00 {}

// [S->C] RESPOND with player decks.
F1-2A-XX-XX
{
  "cards": [
    {"dbf_id": 1552141 },
    {"dbf_id": 1552141 }, // Second copy of the first card
    {"dbf_id": 1521443 },
    {"dbf_id": 854521 },
    {"dbf_id": 632254 },
    {"dbf_id": 12442 },
    {"dbf_id": 3356454 },
  ]
}

F1-3A-XX-XX
{
  "cards": [
    {"dbf_id": 1552141 },
    {"dbf_id": 1552141 },
    {"dbf_id": 1521443 },
    {"dbf_id": 854521 },
    {"dbf_id": 632254 },
    {"dbf_id": 12442 },
    {"dbf_id": 3356454 },
  ]
}

// [C->S] REQUEST latest complete game state.
F2-02-00-00 {}

// [S->C] RESPOND latest complete game state.
// First packet for the response.
F2-08-XX-XX
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
    /* [REPEATED SEQUENCE] */

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
    /* [REPEATED SEQUENCE] */
  ]
}
// second packet for the response.
F2-0A-XX-XX
{
  "for_turn": 4,
  "game_state": [
    {
      "entity_id": 160,
      "dbf_id": 1155424,
      "tags": [{"key": 47, "value": 5}] // GameTag::ATK = 5
    },
    /* [REPEATED SEQUENCE] */

  ]
}

// [C->S] REQUEST game state update stream.
F2-22-00-00 {}

// [S->C] RESPOND stream game state updates.
F2-28-XX-XX
{
  "from_turn": 5,
  "from_block_id": 20,
  "history": [
    /* START OF NEW TOP LEVEL BLOCK */
    {
      "type": "block",
      "index": 7, // BlockType::Play
      "data": {
        "start": true,
        "source_entity": 16, // Entity which is 'Played'
        "target_entity": 50,
      }
    },
        /* START OF NEW INNER BLOCK */
        {
          "type": "block",
          "index": 3, // BlockType::Power
          "data": {
            "start": true,
            "source_entity": 16, // Entity invoking a chain of actions
            "target_entity": 50,
          }
        }
        {
          "type": "meta",
          "index": 0, // MetaDataType::Meta_Target
          "data": { // MetaData structure
            "data": 0,
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
                "value": 2
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
                "value": 0
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
            "info": [50] // Target(s) receiving the heal
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
                "value": 0 // Entity got healed, which makes it become 'undamaged'
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

/* [REPEATED SEQUENCE] */

// [S->C] This is the last game update, the flag Complete is SET!
F2-2A-XX-XX
{
  "turn": 5,
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

    ////////////////////////////////////////////////////
    // Bunch of tag changes                           //
    // Meta data for damage                           //
    // More tag changes                               //
    // One of the player's heroes takes fatal damage  //
    ////////////////////////////////////////////////////

    {
      "type": "block",
      "index": 1, // BlockType::Attack
      "data": {
        "start": false,
        "source_entity": 0,
        "target_entity": 0,
      }
    }
    /* END OF TOP LEVEL BLOCK */

    //////////////////////////////////////////////
    // Game's (EID:1) state changes to finished //
    //////////////////////////////////////////////
  ]
}

```

## Push game state to server

The structures are actually built in a way they can be used in the context of 'client to server'
**AND** the other way around. For the latter case the server initiates the request.

Examples for pushing data can be constructed by reversing the Requester and Responder for each
operation:

1. Server requests full game state from the beginning of the game.
  1 Client responds with known state.
2. Server requests
