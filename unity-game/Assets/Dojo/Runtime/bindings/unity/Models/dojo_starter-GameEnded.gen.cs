// Generated manually as temporary fix until dojo-bindgen supports events. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;
using BigInteger = System.Numerics.BigInteger;

// Event definition for `dojo_starter::events::GameEnded` event
public class dojo_starter_GameEnded : ModelInstance {
    [ModelField("player")]
    public FieldElement player;

    [ModelField("score")]
    public uint score;

    [ModelField("reason")]
    public byte reason; // 0: time expired, 1: bomb hit

    void Start() {}
    void Update() {}
}
