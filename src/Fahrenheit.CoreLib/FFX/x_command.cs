﻿namespace Fahrenheit.CoreLib.FFX;

[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 0x4)]
public struct PCommandData {
    [FieldOffset(0x00)] public byte ordering_idx;
    [FieldOffset(0x01)] public byte sphere_grid_role;
}

[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 0x60)]
public struct PCommand {
    [FieldOffset(0x00)] public Command      command;
    [FieldOffset(0x5C)] public PCommandData command_pdata;
}

[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 0x5C)]
public struct Command {
    [FieldOffset(0x00)] public  ushort name_offset;
    [FieldOffset(0x04)] public  ushort dash_offset;
    [FieldOffset(0x08)] public  ushort desc_offset;
    [FieldOffset(0x0C)] public  ushort misc_offset;
    [FieldOffset(0x10)] private ushort _anim_1;
    [FieldOffset(0x12)] private ushort _anim_2;
    [FieldOffset(0x14)] public  byte   icon;
    [FieldOffset(0x15)] public  byte   caster_anim;
    [FieldOffset(0x16)] public  byte   flags_menu;
    [FieldOffset(0x17)] public  byte   sub_menu_cat2;
    [FieldOffset(0x18)] public  byte   sub_menu_cat;
    [FieldOffset(0x19)] public  byte   user_id;
    [FieldOffset(0x1A)] public  byte   flags_target;
    [FieldOffset(0x1C)] public  uint   flags_misc;
    [FieldOffset(0x20)] public  byte   flags_damage;
    [FieldOffset(0x21)] public  bool   steals_gil;
    [FieldOffset(0x22)] public  bool   has_party_preview;
    [FieldOffset(0x23)] public  byte   flags_damage_class;
    [FieldOffset(0x24)] public  byte   ctb_rank;
    [FieldOffset(0x25)] public  byte   mp_cost;
    [FieldOffset(0x26)] public  byte   limit_cost;
    [FieldOffset(0x27)] public  byte   crit_bonus;
    [FieldOffset(0x28)] public  byte   dmg_formula;
    [FieldOffset(0x29)] public  byte   accuracy;
    [FieldOffset(0x2A)] public  byte   power;
    [FieldOffset(0x2B)] public  byte   hit_count;
    [FieldOffset(0x2C)] public  byte   shatter_chance;
    [FieldOffset(0x2D)] public  byte   flags_element;
    [FieldOffset(0x2E)] public  StatusMap         status_map;
    [FieldOffset(0x47)] public  StatusDurationMap status_duration_map;
    [FieldOffset(0x54)] public  ushort flags_status_extra;
    [FieldOffset(0x58)] public  byte   overdrive_category;
    [FieldOffset(0x59)] public  byte   buff_amount;
    [FieldOffset(0x5A)] public  byte   flags_buffs_mix;
    [FieldOffset(0x56)] public  byte   flags_buffs_stat;

    public  bool is_top_level_in_menu { get { return flags_menu.get_bit(0); } set { flags_menu.set_bit (0, value); } }
    private bool _menu_f4             { get { return flags_menu.get_bit(3); } set { flags_menu.set_bit (3, value); } }
    public  bool opens_sub_menu       { get { return flags_menu.get_bit(4); } set { flags_menu.set_bit (4, value); } }

    public  bool can_target        { readonly get { return flags_target.get_bit(0); } set { flags_target.set_bit(0, value); } }
    public  bool targets_enemies   { readonly get { return flags_target.get_bit(1); } set { flags_target.set_bit(1, value); } }
    public  bool targets_multiple  { readonly get { return flags_target.get_bit(2); } set { flags_target.set_bit(2, value); } }
    public  bool targets_self_only { readonly get { return flags_target.get_bit(3); } set { flags_target.set_bit(3, value); } }
    private bool _targets_f5       { readonly get { return flags_target.get_bit(4); } set { flags_target.set_bit(4, value); } }
    public  bool targets_team      { readonly get { return flags_target.get_bit(5); } set { flags_target.set_bit(5, value); } }
    public  bool targets_dead      { readonly get { return flags_target.get_bit(6); } set { flags_target.set_bit(6, value); } }
    private bool _targets_f8       { readonly get { return flags_target.get_bit(7); } set { flags_target.set_bit(7, value); } }

    public  bool is_usable_outside_combat { readonly get { return flags_misc.get_bit (0);    } set { flags_misc.set_bit (0,    value); } }
    public  bool is_usable_in_combat      { readonly get { return flags_misc.get_bit (1);    } set { flags_misc.set_bit (1,    value); } }
    public  bool display_move_name        { readonly get { return flags_misc.get_bit (2);    } set { flags_misc.set_bit (2,    value); } }
    public  uint accuracy_formula         { readonly get { return flags_misc.get_bits(3, 3); } set { flags_misc.set_bits(3, 3, value); } }
    public  bool is_affected_by_darkness  { readonly get { return flags_misc.get_bit (6);    } set { flags_misc.set_bit (6,    value); } }
    public  bool is_affected_by_reflect   { readonly get { return flags_misc.get_bit (7);    } set { flags_misc.set_bit (7,    value); } }
    public  bool absorbs_dmg              { readonly get { return flags_misc.get_bit (8);    } set { flags_misc.set_bit (8,    value); } }
    public  bool steals_item              { readonly get { return flags_misc.get_bit (9);    } set { flags_misc.set_bit (9,    value); } }
    public  bool is_in_use_menu           { readonly get { return flags_misc.get_bit (10);   } set { flags_misc.set_bit (10,   value); } }
    public  bool is_in_sub_menu           { readonly get { return flags_misc.get_bit (11);   } set { flags_misc.set_bit (11,   value); } }
    public  bool is_in_trigger_menu       { readonly get { return flags_misc.get_bit (12);   } set { flags_misc.set_bit (12,   value); } }
    public  bool inflicts_delay_weak      { readonly get { return flags_misc.get_bit (13);   } set { flags_misc.set_bit (13,   value); } }
    public  bool inflicts_delay_strong    { readonly get { return flags_misc.get_bit (14);   } set { flags_misc.set_bit (14,   value); } }
    public  bool targets_randomly         { readonly get { return flags_misc.get_bit (15);   } set { flags_misc.set_bit (15,   value); } }
    public  bool is_piercing              { readonly get { return flags_misc.get_bit (16);   } set { flags_misc.set_bit (16,   value); } }
    public  bool is_affected_by_silence   { readonly get { return flags_misc.get_bit (17);   } set { flags_misc.set_bit (17,   value); } }
    public  bool uses_weapon_properties   { readonly get { return flags_misc.get_bit (18);   } set { flags_misc.set_bit (18,   value); } }
    public  bool is_trigger_command       { readonly get { return flags_misc.get_bit (19);   } set { flags_misc.set_bit (19,   value); } }
    public  bool uses_tier1_cast_anim     { readonly get { return flags_misc.get_bit (20);   } set { flags_misc.set_bit (20,   value); } }
    public  bool uses_tier3_cast_anim     { readonly get { return flags_misc.get_bit (21);   } set { flags_misc.set_bit (21,   value); } }
    public  bool destroys_user            { readonly get { return flags_misc.get_bit (22);   } set { flags_misc.set_bit (22,   value); } }
    public  bool misses_living_targets    { readonly get { return flags_misc.get_bit (23);   } set { flags_misc.set_bit (23,   value); } }
    private bool _anim_f1                 { readonly get { return flags_misc.get_bit (24);   } set { flags_misc.set_bit (24,   value); } }
    private bool _anim_f2                 { readonly get { return flags_misc.get_bit (25);   } set { flags_misc.set_bit (25,   value); } }
    private bool _anim_f3                 { readonly get { return flags_misc.get_bit (26);   } set { flags_misc.set_bit (26,   value); } }
    private bool _anim_f4                 { readonly get { return flags_misc.get_bit (27);   } set { flags_misc.set_bit (27,   value); } }
    private bool _anim_f5                 { readonly get { return flags_misc.get_bit (28);   } set { flags_misc.set_bit (28,   value); } }
    private bool _anim_f6                 { readonly get { return flags_misc.get_bit (29);   } set { flags_misc.set_bit (29,   value); } }
    private bool _anim_f7                 { readonly get { return flags_misc.get_bit (30);   } set { flags_misc.set_bit (30,   value); } }
    private bool _anim_f8                 { readonly get { return flags_misc.get_bit (31);   } set { flags_misc.set_bit (31,   value); } }

    public bool deals_physical_damage       { readonly get { return flags_damage.get_bit(0); } set { flags_damage.set_bit(0, value); } }
    public bool deals_magical_damage        { readonly get { return flags_damage.get_bit(1); } set { flags_damage.set_bit(1, value); } }
    public bool can_crit                    { readonly get { return flags_damage.get_bit(2); } set { flags_damage.set_bit(2, value); } }
    public bool gives_crit_bonus            { readonly get { return flags_damage.get_bit(3); } set { flags_damage.set_bit(3, value); } }
    public bool is_heal                     { readonly get { return flags_damage.get_bit(4); } set { flags_damage.set_bit(4, value); } }
    public bool is_cleanse                  { readonly get { return flags_damage.get_bit(5); } set { flags_damage.set_bit(5, value); } }
    public bool ignores_break_damage_limit  { readonly get { return flags_damage.get_bit(6); } set { flags_damage.set_bit(6, value); } }
    public bool innate_break_damage_limit   { readonly get { return flags_damage.get_bit(7); } set { flags_damage.set_bit(7, value); } }

    public bool damages_hp  { readonly get { return flags_damage_class.get_bit(0); } set { flags_damage_class.set_bit(0, value); } }
    public bool damages_mp  { readonly get { return flags_damage_class.get_bit(1); } set { flags_damage_class.set_bit(1, value); } }
    public bool damages_ctb { readonly get { return flags_damage_class.get_bit(2); } set { flags_damage_class.set_bit(2, value); } }

    public bool has_element_fire    { readonly get { return flags_element.get_bit(0); } set { flags_element.set_bit(0, value); } }
    public bool has_element_ice     { readonly get { return flags_element.get_bit(1); } set { flags_element.set_bit(1, value); } }
    public bool has_element_thunder { readonly get { return flags_element.get_bit(2); } set { flags_element.set_bit(2, value); } }
    public bool has_element_water   { readonly get { return flags_element.get_bit(3); } set { flags_element.set_bit(3, value); } }
    public bool has_element_holy    { readonly get { return flags_element.get_bit(4); } set { flags_element.set_bit(4, value); } }

    public bool inflicts_scan            { readonly get { return flags_status_extra.get_bit(0);  } set { flags_status_extra.set_bit(0, value); } }
    public bool inflicts_distill_power   { readonly get { return flags_status_extra.get_bit(1);  } set { flags_status_extra.set_bit(1, value); } }
    public bool inflicts_distill_mana    { readonly get { return flags_status_extra.get_bit(2);  } set { flags_status_extra.set_bit(2, value); } }
    public bool inflicts_distill_speed   { readonly get { return flags_status_extra.get_bit(3);  } set { flags_status_extra.set_bit(3, value); } }
    public bool inflicts_distill_move    { readonly get { return flags_status_extra.get_bit(4);  } set { flags_status_extra.set_bit(4, value); } }
    public bool inflicts_distill_ability { readonly get { return flags_status_extra.get_bit(5);  } set { flags_status_extra.set_bit(5, value); } }
    public bool inflicts_shield          { readonly get { return flags_status_extra.get_bit(6);  } set { flags_status_extra.set_bit(6, value); } }
    public bool inflicts_boost           { readonly get { return flags_status_extra.get_bit(7);  } set { flags_status_extra.set_bit(7, value); } }
    public bool inflicts_eject           { readonly get { return flags_status_extra.get_bit(8);  } set { flags_status_extra.set_bit(8, value); } }
    public bool inflicts_auto_life       { readonly get { return flags_status_extra.get_bit(9);  } set { flags_status_extra.set_bit(9, value); } }
    public bool inflicts_curse           { readonly get { return flags_status_extra.get_bit(10); } set { flags_status_extra.set_bit(0, value); } }
    public bool inflicts_defend          { readonly get { return flags_status_extra.get_bit(11); } set { flags_status_extra.set_bit(1, value); } }
    public bool inflicts_guard           { readonly get { return flags_status_extra.get_bit(12); } set { flags_status_extra.set_bit(2, value); } }
    public bool inflicts_sentinel        { readonly get { return flags_status_extra.get_bit(13); } set { flags_status_extra.set_bit(3, value); } }
    public bool inflicts_doom            { readonly get { return flags_status_extra.get_bit(14); } set { flags_status_extra.set_bit(4, value); } }

    public bool inflicts_cheer  { readonly get { return flags_buffs_stat.get_bit(0); } set { flags_buffs_stat.set_bit(0, value); } }
    public bool inflicts_aim    { readonly get { return flags_buffs_stat.get_bit(1); } set { flags_buffs_stat.set_bit(1, value); } }
    public bool inflicts_focus  { readonly get { return flags_buffs_stat.get_bit(2); } set { flags_buffs_stat.set_bit(2, value); } }
    public bool inflicts_reflex { readonly get { return flags_buffs_stat.get_bit(3); } set { flags_buffs_stat.set_bit(3, value); } }
    public bool inflicts_luck   { readonly get { return flags_buffs_stat.get_bit(4); } set { flags_buffs_stat.set_bit(4, value); } }
    public bool inflicts_jinx   { readonly get { return flags_buffs_stat.get_bit(5); } set { flags_buffs_stat.set_bit(5, value); } }

    public bool inflicts_double_hp     { readonly get { return flags_buffs_mix.get_bit(0); } set { flags_buffs_mix.set_bit(0, value); } }
    public bool inflicts_double_mp     { readonly get { return flags_buffs_mix.get_bit(1); } set { flags_buffs_mix.set_bit(1, value); } }
    public bool inflicts_spellspring   { readonly get { return flags_buffs_mix.get_bit(2); } set { flags_buffs_mix.set_bit(2, value); } }
    public bool inflicts_dmg_9999      { readonly get { return flags_buffs_mix.get_bit(3); } set { flags_buffs_mix.set_bit(3, value); } }
    public bool inflicts_always_crit   { readonly get { return flags_buffs_mix.get_bit(4); } set { flags_buffs_mix.set_bit(4, value); } }
    public bool inflicts_overdrive_150 { readonly get { return flags_buffs_mix.get_bit(5); } set { flags_buffs_mix.set_bit(5, value); } }
    public bool inflicts_overdrive_200 { readonly get { return flags_buffs_mix.get_bit(6); } set { flags_buffs_mix.set_bit(6, value); } }
}
