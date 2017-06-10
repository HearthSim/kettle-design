# Kettle design - WIP - Alpha

This repository contains all schemas which formalize the Kettle protocol. The schemas kinda mirror the [WIKI content](https://github.com/HearthSim/kettle-design/wiki). 

> The formalization is still an early work in progress. Any help on creating schemas is welcome! 

> Please file an issue if you notice any kind of problem with the protocol and/or possible implementation problems using the defined schemas.

[Kettle page on HearthSim.net](https://hearthsim.info/kettle/)

## Quickstart

The schemas are organized in a way the Kettle IRI is laid out.
Read [Kettle Home](https://github.com/HearthSim/kettle-design/wiki/1.-Kettle-Home) and [Kettle IRI](https://github.com/HearthSim/kettle-design/wiki/2.-Kettle-IRI) for an explanation about the philosophies for defining the protocol.

Both pages contain elaborate explanations to properly identify any piece of data communicated through the protocol.

### definitions folder

Contains schemas to explicitly define higher-level constructs in context of HearthStone. These schemas are re-used multiple times accross different Kettle payloads.

### enums folder

Contains schemas which explicitly define enumerations to force specific values for some properties. These schemas are re-used multiple times accross different Kettle payloads.

### types folder

Contains structural information of each Kettle payload. 

The most important one is `kettle:types/payload`, which translates to `/types/payload.json` relative to the repository location. Every Kettle payload has the structure defined by that schema. This structure defines exactly two properties;

- type

Acts as identifier of the entire payload.

- data

Contains the actual information that is being communicated. The structure of this data is defined by the (sibling) type property. The value of data is NEVER null, but can be empty.

## Remarks

### $ref issues

Currently any reference between schemas is made by utilising the Kettle IRI, but JSON implementations don't know how to resolve these resource identifiers to actual schemas. As a consequence, schema validators and JSON payload generators will complain.

We must check if JSON validator software can be setup to properly resolve schema documents through the IRI, else each reference needs to be changed to the hosted location of the schemas (which would be Github or HearthSim.net).

### Comments

No comments are allowed in a JSON document. Each schema in itself is also a JSON document so schemas which have C-style comments (`// example`) won't validate.