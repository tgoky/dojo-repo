// Generated manually as temporary fix until dojo-bindgen supports events. Do not modify this file manually.
using System;
using Dojo;
using Dojo.Starknet;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Enum = Dojo.Starknet.Enum;
using BigInteger = System.Numerics.BigInteger;

// Event definition for `dojo_starter::events::MoleMissed` event
public class dojo_starter_MoleMissed : ModelInstance {
    [ModelField("player")]
    public FieldElement player;

    [ModelField("is_mole")]
    public bool is_mole;

    void Start() {}
    void Update() {}
}
