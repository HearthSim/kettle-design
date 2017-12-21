# Kettle protocol

> To discuss this proposal, please see https://hearthsim.info/join/ about how to reach out to us.

## Philosophies

I. Tiny amount of data transferred.

II. Stateless. [note-1](#notes)

III. JSON, easy to decode, easy to encode; no protobuf dependency.

IV. Able to support the full HS state protocol or a subset.

V. As close to the original HS protocol as possible.

VI. Able to send game state deltas or full snapshots.

VII. Hard linked to hearthstoneJSON.com data.

IIX. Extensible enough to be handle other kinds of communication eg; Lobby server <-> Simulator, IPC

IX. Usable within webbrowsers.

X. Context is provided out of band. [note-1](#notes)

XI. Security is provided out of band. [note-1](#notes)

### Notes

1. The `Kettle Core Protocol` must not rely on state encoded within `Kettle Requests`. Usage of parameters should also be
minimized.  
Example: If state information must be polled for game A, 'A' is provided out of band and will not be included in `Kettle Packets`.
The intention is to let underlying protocols handle this eg; the PATH parameter contains 'A' when HTTP is a
carrier-protocol for Kettle. This keeps request/response structures simple.

2. Extension implementers are not bound to [note-1](#notes) because use cases vary and Kettle can also be used as
protocol directly on top of bare transport protocols.

## Definitions

* Kettle Endpoint

  A computer communicating with another computer through the use of the `Kettle Protocol`. Both computers are
  considered Kettle Endpoints.

* Kettle Packet

  A unit of communication which is transferred between endpoints in serialized form.
  Each packet consists of a header, referred to as `Kettle Header`, and optional payload. The latter is
  referred to as `Kettle Payload`.

* Kettle Request

  A `Kettle Packet` which is sent from endpoint X to endpoint Y, with the 'response flag' unset.  
  X activates Y to process the packet contents and sends a `Kettle Response` afterwards, if applicable.

* Kettle Extension

  Functionality added to the Kettle Protocol. An extension defines context about the transmitted `Kettle Payloads`.

* Kettle Core Protocol **!== Kettle Protocol**

  A `Kettle Extension` developed and maintained by the Kettle developer team.  
  It defines contexts regarding minimal player information, deck information and game state.

## Description

The idea is to provide an RPC system, inbetween sender X and receiver Y.  
Only transport and encoding is part of the `Kettle Protocol`. Relevant data for application developers can be found at
kettle-002.md.

1. X invokes remote methods by transmitting a request packet (Response flag is __UNSET__) targetting a specified `Kettle Producer`.
2. Y processes the request and responds accordingly.
3. X receives the response packet and handles its payload.

### Constraints

I. It's required to use a guaranteed and in-order delivery transport protocol (TCP) when communicating Kettle.

II. Endpoints are allowed to put restrictions on the subset of available `Kettle Producers` and the requests per minute it handles.

III. Endpoints are allowed to send responses asynchronously. If multiple `Kettle Requests` are sent within a batch, their
respective `Kettle Response` could have a different index than expected.

### Example

General structure of a **Kettle Packet**: [`Kettle Header`][`Kettle Payload`]

- **Kettle Header**: [`Kettle Producer`][Packet Size]  
  Examples of this object will be displayed in HEX-format.  
  The total size of `Kettle Header` is exactly __4 bytes__.

- **Kettle Payload**: [Packet Payload]
  Examples of this object will be displayed in STRING-format.  
  The total size of `Kettle Payload` is not defined by this document. Refer to the `Kettle Header` of the same
  `Kettle Packet` to find out the exact size.

  example; Kettle Core __Request__ **Block data** object, with size 0B
  E0-0A-00-00
  {}

  example; Kettle Core __Response__ **Block data** object, with size 51200B (50KB)
  E0-16-C8-00
  {}

## Kettle producer

This part of the `Kettle Packet` describes which functionality acts as context for the `Kettle Payload`.
A `Kettle Producer` consists of two parts: [Allocated Block][Allocated Method][Packet Flags].
Its' total size is exactly __2 bytes__.

> The purpose of a `Kettle Endpoint` becomes implicitly obvious through the Allocated Blocks it exposes.

### Allocated Block

The range of blocks which can be allocated go from 00 up until EF (inclusive). This takes up __1 byte__.
A maximum of XXX different extensions can be allocated, since the `Kettle Core Protocol` reserves blocks F0~FF. 
A `Kettle Extension` can be allowed to reserve more than one block. This decision is up to the `Kettle Core Team`.

Allocated block must have a default error payload structure, which is returned upon processing an invalid `Kettle Packet`.
Owners of multiple blocks, spanning a contiguous range, are also allowed to choose 1 error payload structure for the entire
block range.

> Owners can also choose to use the default error payload structure, defined on the `Kettle Core Protocol`, 
  if no custom structure is necessary.

### Allocated Method

Each Allocated Block holds 16 unique adresses. These addresses can be linked to a method. This takes up __4 bits__.  
A method references certain functionality and is linked to at most one `Kettle Request Payload` and `Kettle
Response Payload` structure.

* An 'Empty Payload' is assumed when there is a missing explicit reference between methods and payload
  structure. This object has size __0__.

* It's considered an **error** when a `Kettle Packet` holds a non-empty `Kettle Payload` when no payload structure is 
  referenced for the mention method and request/response combination!

* It's considered an **error** when the Empty Payload is expected and the Kettle Header holds a payload size greater than 0.

> In rare cases it's possible that the same payload structure can be useful for both request and response packet types. This
is allowed.

### Packet flags

Encodes properties of the `Kettle Packet` it's transmitted within. This takes up __4 bits__.  
See below for an explanation on each flag.

The Allocated Method and Packet Flags are strongly linked, always process both together like a tuple!  
The value of a flag is 0 when it's __UNSET__, 1 when __SET__.

##### Structure

Bit X | Name | Description
:----: | --- | ---
0	| Response flag | Makes the distinction between request/response packets.
1	| Invalid flag | Makes the distinction between valid/invalid requests (only applicable within response packets).
2	| Complete flag | Makes the distinction between partial/complete packets.
3	| *Reserved* |

#### Response flag

**The flag is 0 for requests, 1 for responses.**

The 'Empty Payload' is useful for performing a `Kettle Request`, since most of the time there is no data which needs to be 
provided (like a HTTP GET request).

#### Invalid flag

**The flag is 0 for a valid request, 1 for an invalid request.**
> This flag only matters when the Response flag is __SET__!

Endpoints indicate if the received `Kettle Request` was malformed by returning a `Kettle Response` with this flag set.
The endpoint is __not obligated__ to send back why a request was invalid and can leave the Payload Size of the packet at 0 bytes.
It __is recommended__ however to always return an error description!

**IF** a Payload Size is set, the default error payload structure for the reserved block is used to decode the payload!

#### Complete flag

**The flag is 0 indicating a partial packet, 1 for a complete packet.**

The `Kettle Protocol` doesn't dictate a way for splitting up `Kettle Payloads` to fit the allowed maximum amount of bytes.
It does however support an indication to the implementer that a `Kettle packet` contains __partial__ results.
The endpoint can be sure all data of a `Kettle Response` is received when this flag is __SET__ on the last `Kettle Packet`.

The payload **MUST ALWAYS BE** a valid UTF-8 JSON encoded object. The implementor itself is responsible for correctly splitting up it's payload contents!

## Packet Size

Identifies how many **bytes** are used to encode the payload. This takes up __2 bytes__.

The size part of the `Kettle Header` can hold a value up to 65536 (exclusive) bytes, which is a value around the maximum MTU for systems
following the IPv4 and v6 protocols.
Since the `Kettle Header` always has an encoded size of 4 bytes, we allow the payload to have a __maximum size__ of (65536-4) bytes per packet.  
The maximum payload size becomes **65532**, or **FF-FC**, bytes.

> The Packet Size is allowed to be 0! This means there will **NOT** follow a `Kettle Payload` and the next `Kettle Packet` will follow 
right after the current `Kettle Header`.

## Packet Payload

The Packet Payload holds the actual data being transmitted between `Kettle Endpoints`.  
The payload **MUST** be an __UTF-8 representation__ of a valid JSON object.

The structure of the payload is indicated by the `Kettle Producer`. Each Allocated Block owner must provide JSON schema's for each payload structure 
it encodes into Allocated Methods.

> The payload examples in this and following RFC's are prettified, the newline characters should **NOT** be sent between endpoints to save one space!

## PAYLOADS

Payloads will be defined in other RFC documents.  
Everybody is free to open a Pull-Request to allocate Blocks for themselves. An RFC Document is desired when
creating the PR, look at kettle-002.md for an example format.  
**ONLY WHEN** the PR is merged you'll officially own the requested blocks and nobody alse can claim them.

For any questions please file an issue or come meet us at Discord (see top of this document)!

> For the `Kettle Core Protocol` payload structures, see kettle-002.md.
