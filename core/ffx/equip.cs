// SPDX-License-Identifier: MIT

namespace Fahrenheit.Core.FFX;

public enum DamageFormula : byte {
    StrVsDef               = 0x1,
    StrIgnoreDef           = 0x2,
    MagVsMDef              = 0x3,
    MagIgnoreMDef          = 0x4,
    CurrentDiv16           = 0x5,
    Multiple50             = 0x6,
    Healing                = 0x7,
    MaxDiv16               = 0x8,
    Multiple50WithVariance = 0x9,
    TargetMaxMpDiv16       = 0xa,
    TargetMaxCtbDiv16      = 0xb,
    TargetMpDiv16          = 0xc,
    TargetCtbDiv16         = 0xd,
    MagSpecial             = 0xf,
    UserMaxHpDiv10         = 0x10,
    CelestialHighHp        = 0x11,
    CelestialHighMp        = 0x12,
    CelestialLowHp         = 0x13,
    ChosenGilDiv10         = 0x15,
    TargetKills            = 0x16,
    Multiple9999           = 0x17,
}

[InlineArray(4)]
public struct EquipmentAbilityArray {
    private ushort _u;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Equipment {
    public ushort                name_id;
    public bool                  exists;
    public byte                  flags;
    public byte                  owner;
    public byte                  type;
    public byte                  equipped_by;
    public byte                  __0x7;
    public DamageFormula         dmg_formula;
    public byte                  power;
    public byte                  crit_bonus;
    public byte                  slot_count;
    public ushort                model_id;
    public EquipmentAbilityArray abilities;

    public bool is_hidden       { readonly get { return flags.get_bit(1); } set { flags.set_bit(1, value); } }
    public bool is_celestial    { readonly get { return flags.get_bit(2); } set { flags.set_bit(2, value); } }
    public bool is_brotherhood  { readonly get { return flags.get_bit(3); } set { flags.set_bit(3, value); } }

    public readonly bool is_weapon { get { return (type & 1) == 0; } }
    public readonly bool is_armor  { get { return (type & 1) != 0; } }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UnownedEquipment {
    public byte                  flags;
    public byte                  owner;
    public byte                  type;
    public byte                  __0x3;
    public DamageFormula         dmg_formula;
    public byte                  power;
    public byte                  crit_bonus;
    public byte                  slot_count;
    public EquipmentAbilityArray abilities;

    public bool is_hidden       { readonly get { return flags.get_bit(1); } set { flags.set_bit(1, value); } }
    public bool is_celestial    { readonly get { return flags.get_bit(2); } set { flags.set_bit(2, value); } }
    public bool is_brotherhood  { readonly get { return flags.get_bit(3); } set { flags.set_bit(3, value); } }

    public readonly bool is_weapon { get { return (type & 1) == 0; } }
    public readonly bool is_armor  { get { return (type & 1) != 0; } }
}
