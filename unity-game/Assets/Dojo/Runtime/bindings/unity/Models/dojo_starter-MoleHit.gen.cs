// Generated manually as temporary fix until dojo-bindgen supports events. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;
using BigInteger = System.Numerics.BigInteger;

// Event definition for `dojo_starter::events::MoleHit` event
public class dojo_starter_MoleHit : ModelInstance {
    [ModelField("player")]
    public FieldElement player;

    [ModelField("points")]
    public uint points;

    [ModelField("score")]
    public uint score;

    void Start() {}
    void Update() {}
}
