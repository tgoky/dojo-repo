// Generated manually as temporary fix until dojo-bindgen supports events. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;
using BigInteger = System.Numerics.BigInteger;

// Event definition for `dojo_starter::events::GameTimeUpdated` event
public class dojo_starter_GameTimeUpdated : ModelInstance {
    [ModelField("player")]
    public FieldElement player;

    [ModelField("remaining")]
    public uint remaining;

    void Start() {}
    void Update() {}
}
