// SPDX-License-Identifier: MIT

using System.Diagnostics;

using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Battle;
using Fahrenheit.Core.FFX.Events;
using Fahrenheit.Core.FFX.Events.Battle;
using Fahrenheit.Core.FFX.Ids;

namespace Fahrenheit.Core.Runtime;

using DmgEventArgs = FhXEventsBattleDamageCalc.DamageCalcEventArgs;

//devnote: I believe this will be the first non-trivial 1k+ lines file
//TODO: Think of a more appropriate name for this
[FhLoad(FhGameId.FFX)]
public unsafe partial class FhBattleAPIs : FhModule {
    private FhModContext _context = null!;
    private FileStream _global_state = null!;

    // Dirty workarounds for FhCall not being up-to-date
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsCalcDamage(
        int user_id,
        Chr* user,
        int target_id,
        Chr* target,
        Command* command,
        int command_id,
        byte* p7,
        int* p8,
        int* p9,
        int* p10,
        int p11
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DmgCalc(
        int user_id,
        Chr* user,
        int target_id,
        Chr* target,
        Command* command,
        int command_id,
        DamageInfo* info,
        int* param_8,
        int* param_9,
        int* param_10,
        int* param_11
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetUpDamageInfo(DamageInfo* info, DamageInfo* parent, Chr* target);

    private readonly SetUpDamageInfo _SetUpDamageInfo = FhUtil.get_fptr<SetUpDamageInfo>(0x38dba0);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsCheckWrongStatus(Command* command);

    private readonly MsCheckWrongStatus _MsCheckWrongStatus = FhUtil.get_fptr<MsCheckWrongStatus>(FhCall.__addr_MsCheckWrongStatus);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsGetRomCTBcount(int agility);

    private readonly MsGetRomCTBcount _MsGetRomCTBcount = FhUtil.get_fptr<MsGetRomCTBcount>(FhCall.__addr_MsGetRomCTBcount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CheckDebugInvincibility(int target_id);

    private readonly CheckDebugInvincibility _CheckDebugInvincibility = FhUtil.get_fptr<CheckDebugInvincibility>(0x38d460);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsAddLoveParam(int chr_id, int amount);

    private readonly MsAddLoveParam _MsAddLoveParam = FhUtil.get_fptr<MsAddLoveParam>(FhCall.__addr_MsAddLoveParam);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsGetRndChr(int chr_id, int p2);

    private readonly MsGetRndChr _MsGetRndChr = FhUtil.get_fptr<MsGetRndChr>(FhCall.__addr_MsGetRndChr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int brnd(int rng_idx);

    private readonly brnd _brnd = FhUtil.get_fptr<brnd>(FhCall.__addr_brnd);

    // Method Handles
    private FhMethodHandle<MsCalcDamage> _mh_MsCalcDamage = null!;
    private FhMethodHandle<DmgCalc> _mh_DmgCalc = null!;

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _context = mod_context;
        _global_state = global_state_file;

        _mh_MsCalcDamage = new(this, "FFX.exe", FhCall.__addr_MsCalcDamage, h_MsCalcDamage);
        _mh_DmgCalc = new(this, "FFX.exe", 0x38e680, h_DmgCalc);

        _mh_MsCalcDamage.hook();
        _mh_DmgCalc.hook();

        return true;
    }

    private int h_MsCalcDamage(
        int user_id,
        Chr* user,
        int target_id,
        Chr* target,
        Command* command,
        int command_id,
        byte* p7,
        int* p8,
        int* p9,
        int* p10,
        int p11
    ) {
        // Prepare target fields for damage calculation
        target->current_hp = target->hp;
        target->current_mp = target->mp;
        target->current_ctb = target->ctb;
        target->damage_hp = 0;
        target->damage_mp = 0;
        target->damage_ctb = 0;

        int local1 = 0;
        int total_damage = 0;
        int damage = 0;

        DamageInfo* dmg_infos = (DamageInfo*)((int)p7 + 18);
        int current_info_idx = *p7;

        DamageInfo* parent_info = null;
        if (current_info_idx > 0) {
            parent_info = &dmg_infos[current_info_idx - 1];
        }

        for (int i = current_info_idx; i < current_info_idx + p11; i++) {
            DamageInfo* info = &dmg_infos[i];
            _SetUpDamageInfo(info, parent_info, target);

            damage =
                _mh_DmgCalc.hook_fptr(
                    user_id,
                    user,
                    target_id,
                    target,
                    command,
                    command_id,
                    info,
                    p8,
                    p9,
                    p10,
                    &local1
                );

            total_damage += damage;

            if (*(byte*)((int)user + 0x50) != 0) continue;

            if (damage >= 9999) {
                // Set the 9999 damage flag for achievement
                FhUtil.set_at(0xD33390, 1);
            }

            if (damage >= 99999) {
                // Set the 99999 damage flag for achievement
                FhUtil.set_at(0xD33394, 1);
            }
        }

        bool is_invincible = _CheckDebugInvincibility(target_id) == 0;
        if (is_invincible || target->hp > total_damage) {
            target->stat_will_die = 0;
        } else {
            target->stat_will_die = 1;
        }

        if (user_id == PlySaveId.PC_TIDUS && target_id < PlySaveId.PC_VALEFOR) {
            int affection_amount =
                total_damage switch {
                    < 0 =>  1, // Healing increases affection
                    > 0 => -1, // Damage reduces affection
                    0 =>  0,
                };

            if (affection_amount != 0) {
                _MsAddLoveParam(target_id, affection_amount);
            }
        }

        *(byte*)((int)p7 + 0x16) = (byte)local1;
        *(byte*)((int)p7 + 0x1) = (byte)(current_info_idx + p11);

        return damage;
    }

    private int h_DmgCalc(
        int user_id,
        Chr* user,
        int target_id,
        Chr* target,
        Command* command,
        int command_id,
        DamageInfo* info,
        int* p8,
        int* p9,
        int* p10,
        int* p11
    ) {
        DamageFormula damage_formula = command->dmg_formula;
        byte power = command->power;
        ElementFlags elements = command->element;
        StatusExtraFlags status_inflict_extra = command->flags_status_extra;

        if (command->uses_weapon_properties) {
            damage_formula = user->wpn_dmg_formula;
            power = user->wpn_power;
            elements |= user->elem_wpn;

            // If the command inflicts any distills, ignore the user's innate distills
            if (command->flags_status_extra.distills()) {
                status_inflict_extra |= user->status_inflict_extra & ~StatusExtraFlags.DISTILLS;
            } else {
                status_inflict_extra |= user->status_inflict_extra;
            }
        }

        DmgEventArgs e = new() {
            user = user,
            target = target,
            command = command,
            command_id = command_id,
            info = info,
            damage_formula = damage_formula,
            power = power,
            elements = elements,
            status_inflict_extra = status_inflict_extra,
            dmg_calc_flags = (DmgCalcFlags)command->flags_damage_class,
            immunity_count = 0,
            status_hits = 0,
            status_misses = 0,
            status_resists = 0,
            debuff_inflicts = 0,
            debuff_cleanses = 0,
            missed = false,
            hit_armored = false,
            threatened = false,
            should_vary = !Globals.Battle.btl->debug.no_variance,
            variance = 256,
            base_damage = 0,
            damage = 0,
            damage_hp = 0,
            damage_mp = 0,
            damage_ctb = 0,
            strength = user->strength,
            magic = user->magic,
            defense = target->defense,
            magic_defense = target->magic_defense,
            user_status_suffer = user->status_suffer,
            user_status_suffer_duration = user->status_suffer_turns_left,
            user_status_suffer_extra = user->status_suffer_extra,
            target_status_suffer = target->status_suffer,
            target_status_suffer_duration = target->status_suffer_turns_left,
            target_status_suffer_extra = target->status_suffer_extra,
        };

        DmgEventArgs e_for_expected = new() {
            user = user,
            target = target,
            command = command,
            command_id = command_id,
            info = info,
            damage_formula = damage_formula,
            power = power,
            elements = elements,
            status_inflict_extra = status_inflict_extra,
            dmg_calc_flags = (DmgCalcFlags)command->flags_damage_class,
            immunity_count = 0,
            status_hits = 0,
            status_misses = 0,
            status_resists = 0,
            debuff_inflicts = 0,
            debuff_cleanses = 0,
            missed = false,
            hit_armored = false,
            threatened = false,
            should_vary = false,
            variance = 256,
            base_damage = 0,
            damage = 0,
            damage_hp = 0,
            damage_mp = 0,
            damage_ctb = 0,
            strength = user->strength,
            magic = user->magic,
            defense = target->defense,
            magic_defense = target->magic_defense,
            user_status_suffer = StatusPermanentFlags.NONE,
            user_status_suffer_duration = new(),
            user_status_suffer_extra = StatusExtraFlags.NONE,
            target_status_suffer = StatusPermanentFlags.NONE,
            target_status_suffer_duration = new(),
            target_status_suffer_extra = StatusExtraFlags.NONE,
        };

        e.counter = DmgCalc_CheckCounter(false, e);

        bool attack_nullified = DmgCalc_CheckNuls(e);
        if (attack_nullified) {
            e.immunity_count += 1;
            local_8c = 2;
            DmgCalc_78c330(e);
            if (local_64 == NO_COUNTER) goto LAB_0078ecb4;
        } else {
            e.hit = DmgCalc_CheckHit(e);

            if (e.hit == CheckHitResult.MISS) {
                local_8c = 1;
                *(byte*)p10 += 1;

                if (user_id != target_id) {
                    local_60 = 1;
                } else {
                    // Original makes sure the command doesn't deal physical damage
                    // This is undesired for mods
                    local_60 = e.command->deals_magical_damage ? 17 : 1;
                }

                e.missed = true;
            } else if (e.hit == CheckHitResult.MISS_ALIVE) {
                local_60 = 0;
                *(byte*)p10 += 1;
                e.missed = true;
                goto LAB_0078ecb4;
            } else {
                *local_90 += 1;

                if (e.power != 0) {
                    if (command->flags_damage_class.hp()) {
                        e.target_stat = TargetStat.HP;

                        DmgCalc_QueueRemoveStatusOnHit(e);

                        DmgCalc_Base(e);
                        DmgCalc_ShieldBoost(e);
                        DmgCalc_Shell(e);
                        DmgCalc_Protect(e);
                        if (!Globals.Battle.btl->debug.never_crit)
                            DmgCalc_Crit(e);
                        DmgCalc_Berserk(e);
                        DmgCalc_MagicBooster(e);
                        DmgCalc_Alchemy(e);
                        DmgCalc_StatBoostAbility(e);
                        DmgCalc_GravImmunity(e);
                        DmgCalc_ApplyZombieDrainInteraction(e);
                        DmgCalc_Elem(e);
                        DmgCalc_Armored(e);
                        DmgCalc_Defend(e);
                        DmgCalc_PowerMagicBreak(e);
                        DmgCalc_DmgImmunity(e);

                        e.damage_hp = FUN_0078be50(user, e.damage);
                        e.target_stat = null;
                        e.damage = 0;
                        local_8c = local_6c;
                    }

                    if (command->flags_damage_class.mp()) {
                        e.target_stat = TargetStat.MP;

                        DmgCalc_Base(e);
                        DmgCalc_MagicBooster(e);
                        DmgCalc_Alchemy(e);

                        // Original: if (e.damage > 0 && e.damage > e.target->current_mp)
                        //               e.damage = e.target->current_mp
                        // Replaced to make negative damage work for mods
                        if (e.damage > 0) {
                            e.damage = Math.Min(e.damage, e.target->current_mp);
                        } else if (e.damage < 0) {
                            e.damage = Math.Max(e.damage, -e.user->mp);
                        }

                        DmgCalc_ApplyZombieDrainInteraction(e);
                        e.damage_mp = e.damage;
                        e.target_stat = null;
                        e.damage = 0;
                    }

                    if (command->flags_damage_class.ctb()) {
                        e.target_stat = TargetStat.CTB;

                        // ???? To prevent multihits inflicting delay multiple times?
                        e.dmg_calc_flags &= ~DmgCalcFlags.DAMAGE_CTB;

                        DmgCalc_Base(e);
                        DmgCalc_ApplyZombieDrainInteraction(e);

                        e.damage_ctb = e.damage;
                        e.target_stat = null;
                        e.damage = 0;
                    }
                }

                DmgCalc_Delay(e);
                DmgCalc_Threaten(e);

                if (!Globals.Battle.btl->debug.never_inflict_status) {
                    DmgCalc_InflictStatus(e);
                    DmgCalc_InflictStatus_Extra(e);

                    if (!e.target_status_suffer.petrification()) {
                        e.info->flags_buffs_mix |= e.command->flags_buffs_mix;
                    }
                }

                DmgCalc_ShatterFail(e);
                DmgCalc_DelayImmunity(e);
                DmgCalc_NoDamageDeath(e);

                DmgCalc_ApplyStatBuffs(e);
                DmgCalc_Bribe(e);

                DmgCalc_78c330(e);
            }
        }

        DmgCalc_CheckCounter(true, e);

        // Damage debug flags
        if (Globals.Battle.btl->debug.always_1_dmg) {
            if (e.dmg_calc_flags.damage_hp()) e.damage_hp = 1;
            if (e.dmg_calc_flags.damage_mp()) e.damage_mp = 1;
        }

        if (Globals.Battle.btl->debug.always_9999_dmg) {
            if (e.dmg_calc_flags.damage_hp()) e.damage_hp = 10000;
            if (e.dmg_calc_flags.damage_mp()) e.damage_mp = 10000;
        }

        if (Globals.Battle.btl->debug.always_99999_dmg) {
            if (e.dmg_calc_flags.damage_hp()) e.damage_hp = 100000;
            if (e.dmg_calc_flags.damage_mp()) e.damage_mp = 100000;
        }

        int damage_limit = e.user->auto_ability_effects.has_break_damage_limit ? 99999 : 9999;

        if (e.command->innate_break_damage_limit) damage_limit = 99999;
        else if (e.command->ignores_break_damage_limit) damage_limit = 9999;

        if ((*FhUtil.ptr_at<byte>(e.user, 0x640)).get_bit(3) && e.dmg_calc_flags.damage_hp()) {
            if (Math.Abs(e.damage_hp) < 9999) {
                e.damage_hp = Math.Sign(e.damage_hp) * 9999;
            }
        }

        for (int i = 3; i > 0; i--) {
            int* current_stat = i switch {
                3 => &e.target->current_hp,
                2 => &e.target->current_mp,
                1 => &e.target->current_ctb,
                _ => throw new UnreachableException(),
            };

            int* damage_stat = i switch {
                3 => &e.target->damage_hp,
                2 => &e.target->damage_mp,
                1 => &e.target->damage_ctb,
                _ => throw new UnreachableException(),
            };

            int* out_param = i switch {
                3 => p8,
                2 => p9,
                1 => p10,
                _ => throw new UnreachableException(),
            };

            int* out_info = i switch {
                3 => &e.info->out_damage_hp,
                2 => &e.info->out_damage_mp,
                1 => &e.info->out_damage_ctb,
                _ => throw new UnreachableException(),
            };

            int damage = i switch {
                3 => e.damage_hp,
                2 => e.damage_mp,
                1 => e.damage_ctb,
                _ => throw new UnreachableException(),
            };

            // I presume this is correct?
            damage = Math.Clamp(damage, -damage_limit, damage_limit);

            // Original code (I think correctly written?) below:
            // if (-damage_limit <= damage && damage_limit < damage) {
            //     damage = damage_limit;
            // }

            *out_info = damage;
            *damage_stat += damage;
            *current_stat -= damage;
            if (out_param != null) {
                *out_param -= damage;
            }

            *current_stat = Math.Max(*current_stat, 0);
        }

        if (e.target->overkill_threshold <= e.info->out_damage_hp) {
            e.dmg_calc_flags |= DmgCalcFlags.OVERKILL;
        }

        e.info->__0x0 = local_60;
        e.info->__0x1 = DmgCalc_78c700(local_a4, &local_84);
        e.info->__0x18 = e.dmg_calc_flags; // ??? I'm not sure how these differ.
        e.info->__0x1C = e.dmg_calc_flags; // ??? Do we need to keep track of two separate bitfields?
        e.info->__0x2 = (byte)local_a4; // extraout_DL
        e.info->__0x3 = (byte)local_8c;

        DmgCalc_Base(e_for_expected);
        e.expected_damage = e_for_expected.damage;

        piVar11 = local_78;
        e.info->__0x4 = (byte)local_18;

        DmgCalc_78c010(e);

        if (local_48 != 0) {
            user->field_0xdea = 7;
        }

        if (p11 != null) {
            *p11 = e.status_hits;
        }

        return *piVar11;
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private CheckCounterResult DmgCalc_CheckCounter(bool check_status, DmgEventArgs e) {
        if (e.command->targets_multiple
         || e.command->targets_self_only
         || *(bool*)((int)e.user + 0xDF1)
         || e.user->id == e.target->id
         || !*(bool*)((int)Globals.Battle.btl + 0x2142)
           )  return CheckCounterResult.NO_COUNTER;

        var result = CheckCounterResult.NO_COUNTER;

        if (e.command->deals_physical_damage && !e.command->deals_magical_damage) {
            if (e.target->auto_ability_effects.has_evade_and_counter) {
                result = CheckCounterResult.EVADE_N_COUNTER;
            } else if (e.target->auto_ability_effects.has_counter_attack) {
                result = CheckCounterResult.COUNTER;
            }
        } else if (e.command->deals_magical_damage) {
            if (e.target->auto_ability_effects.has_magic_counter) {
                result = CheckCounterResult.COUNTER;
            }
        }

        if (!check_status || result == CheckCounterResult.NO_COUNTER) return result;

        if (_MsCheckWrongStatus(e.command) != 0) {
            *(byte*)((int)e.target + 0x6DC) = 1;
        }

        return result;
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private bool DmgCalc_CheckNuls(DmgEventArgs e) {
        int elements = 0;
        int elements_nullified = 0;

        byte nul_blaze = e.target_status_suffer_duration.nul_blaze;
        byte nul_shock = e.target_status_suffer_duration.nul_shock;
        byte nul_frost = e.target_status_suffer_duration.nul_frost;
        byte nul_tide  = e.target_status_suffer_duration.nul_tide;

        if (e.elements.HasFlag(ElementFlags.FIRE)) {
            elements += 1;
            if (nul_blaze != 0) {
                elements_nullified += 1;
            }
        }

        if (e.elements.HasFlag(ElementFlags.THUNDER)) {
            elements += 1;
            if (nul_shock != 0) {
                elements_nullified += 1;
            }
        }

        if (e.elements.HasFlag(ElementFlags.ICE)) {
            elements += 1;
            if (nul_frost != 0) {
                elements_nullified += 1;
            }
        }

        if (e.elements.HasFlag(ElementFlags.WATER)) {
            elements += 1;
            if (nul_tide != 0) {
                elements_nullified += 1;
            }
        }

        if (elements_nullified == 0 || elements > elements_nullified) return false;

        if (nul_blaze != 0 && nul_blaze < 254) e.target_status_suffer_duration.nul_blaze -= 1;
        if (nul_shock != 0 && nul_shock < 254) e.target_status_suffer_duration.nul_shock -= 1;
        if (nul_frost != 0 && nul_frost < 254) e.target_status_suffer_duration.nul_frost -= 1;
        if (nul_tide  != 0 && nul_tide  < 254) e.target_status_suffer_duration.nul_tide  -= 1;

        return true;
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    CheckHitResult DmgCalc_CheckHit(DmgEventArgs e) {
        bool target_is_dead = e.target_status_suffer.zombie() || e.target_status_suffer.death();

        if (e.command->misses_living_targets && !target_is_dead) {
            return CheckHitResult.MISS_ALIVE;
        }

        uint accuracy_formula = e.command->accuracy_formula;

        if (accuracy_formula == 0) return CheckHitResult.HIT;

        int base_hit_chance = accuracy_formula switch {
            1 or 2 => e.command->accuracy,
            3 or 4 => e.user->accuracy,
            5      => e.user->accuracy * 5 / 2,
            6      => e.user->accuracy * 3 / 2,
            7      => e.user->accuracy     / 2,
            _      => 0,
        };

        if (e.target_status_suffer_duration.sleep > 0) return CheckHitResult.HIT;
        if (e.target_status_suffer.petrification()) return CheckHitResult.HIT;
        if (Globals.Battle.btl->debug.never_hit) return CheckHitResult.MISS;

        int rng_idx = _MsGetRndChr(e.user->id, 1);
        int rng = _brnd(rng_idx);

        byte* HIT_TABLE = FhUtil.ptr_at<byte>(0x8421E0);

        int hit_chance;
        int luck_adjustment = e.user->luck + e.user->luck_stacks + e.target->jinx_stacks - e.target->luck;
        int aim_adjustment = (e.user->aim_stacks - e.target->reflex_stacks) * 10;

        switch (accuracy_formula) {
            case 1:
            case 3:
            case 5:
            case 6:
            case 7: {
                int hit_table_idx = (base_hit_chance * 2 / 5 - e.target->evasion) + 10;
                hit_table_idx = Math.Clamp(hit_table_idx, 0, 8);
                hit_chance = HIT_TABLE[hit_table_idx];

                if (e.command->is_affected_by_darkness && e.user_status_suffer_duration.darkness > 0) {
                    hit_chance /= 10;
                }

                hit_chance += luck_adjustment;
                hit_chance += aim_adjustment;

                break;
            }

            case 2:
            case 4: {
                hit_chance = base_hit_chance - e.target->evasion;

                if (e.command->is_affected_by_darkness && e.user_status_suffer_duration.darkness > 0) {
                    hit_chance /= 10;
                }

                hit_chance += luck_adjustment;
                hit_chance += aim_adjustment;

                break;
            }

            default: {
                hit_chance = 0;
                break;
            }
        }

        if (hit_chance <= rng % 101) return CheckHitResult.MISS;
        if (e.counter == CheckCounterResult.EVADE_N_COUNTER) return CheckHitResult.MISS;
        if (Globals.Battle.btl->debug.always_hit) return CheckHitResult.MISS;

        return CheckHitResult.HIT;
    }

    // MsCalcDamageCommand is the only function here that is actually called outside of regular damage calculation,
    // so we hook that.
    //TODO: Hook that

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Base(DmgEventArgs e) {
        // Shorthand for method calls
        FhXEventsBattleDamageCalc dmg_calc = FhXEvents.battle.damage_calc;

        int chr_rng_idx = _MsGetRndChr(e.user->id, 0);

        dmg_calc.Invoke_PreApplyDefenseBreak(this, e);

        if (e.command is not null && e
                                    .dmg_calc_flags.damage_class()
                                    .hp()) {
            if (e.target_status_suffer.armor_break()) {
                if (dmg_calc.IsNull_OnApplyArmorBreak()) {
                    e.defense = 0;
                } else {
                    dmg_calc.Invoke_OnApplyArmorBreak(this, e);
                }
            }

            if (e.target_status_suffer.mental_break()) {
                if (dmg_calc.IsNull_OnApplyMentalBreak()) {
                    e.magic_defense = 0;
                } else {
                    dmg_calc.Invoke_OnApplyMentalBreak(this, e);
                }
            }
        }

        dmg_calc.Invoke_PostApplyDefenseBreak(this, e);

        if (e.should_vary) {
            if (dmg_calc.IsNull_OnGetVariance()) {
                int variance_rng = _brnd(chr_rng_idx);
                e.variance = (variance_rng & 31) + 240;
            } else {
                dmg_calc.Invoke_OnGetVariance(this, e);
            }
        }

        dmg_calc.Invoke_PostGetVariance(this, e);

        // Keeping all the integer division here makes this difficult to write more clearly
        // Maybe I'm being a bit overly careful when it comes to keeping all the base game rounding and it could've been simpler,
        // but I'm not going to test all that to make sure
        switch (e.damage_formula) {
            case DamageFormula.StrVsDef: {
                int str = e.strength + e.user->cheer_stacks;
                int dr = (e.defense * 51 - (e.defense * e.defense) / 11) / 10;
                int dmg = str * str * str / 32 + 30;
                dmg = (730 - dr) * dmg / 730;
                dmg *= (15 - e.target->cheer_stacks) / 15;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.StrIgnoreDef: {
                int str = e.strength + e.user->cheer_stacks;
                int dmg = str * str * str / 32 + 30;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MagVsMDef: {
                int mag = e.magic + e.user->focus_stacks;
                int dr = (e.magic_defense * 51 - (e.magic_defense * e.magic_defense) / 11) / 10;
                int dmg = ((mag * mag / 6) + e.power) * e.power;
                dmg = ((730 - dr) * dmg / 4) / 730;
                dmg *= (15 - e.target->focus_stacks) / 15;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MagIgnoreMDef: {
                int mag = e.magic + e.user->focus_stacks;
                int dmg = ((mag * mag / 6) + e.power) * e.power;
                dmg /= 4;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CurrentDiv16: {
                e.damage = (int)e.target_current_stat! * e.power / 16;
                break;
            }

            case DamageFormula.Multiple50: {
                e.damage = 50 * e.power;
                break;
            }

            case DamageFormula.Healing: {
                int mag = e.magic + e.user->focus_stacks;
                int dmg = (mag + e.power) / 2 * e.power;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MaxDiv16: {
                e.damage = (int)e.target_max_stat! * e.power / 16;
                break;
            }

            case DamageFormula.Multiple50WithVariance: {
                int dmg = 50 * e.power;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.TargetMaxMpDiv16: {
                e.damage = e.target->max_mp * e.power / 16;
                break;
            }

            case DamageFormula.TargetMaxCtbDiv16: {
                e.damage = e.target->max_ctb * e.power / 16;
                break;
            }

            case DamageFormula.TargetMpDiv16: {
                e.damage = e.target->current_mp * e.power / 16;
                break;
            }

            case DamageFormula.TargetCtbDiv16: {
                e.damage = e.target->current_ctb * e.power / 16;
                break;
            }

            case (DamageFormula)0x0E: {
                int str = e.strength;
                int dmg = (str * str * str) / 32 + 30;
                dmg = dmg * e.power / 16;
                e.damage = dmg;
                return;
            }

            case DamageFormula.MagSpecial: {
                int mag = e.magic + e.user->focus_stacks;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.UserMaxHpDiv10: {
                e.damage = e.user->max_hp * e.power / 10;
                break;
            }

            case DamageFormula.CelestialHighHp: {
                int str = e.strength + e.user->cheer_stacks;
                int celestial_factor = (e.user->hp * 100) / e.user->max_hp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CelestialHighMp: {
                int str = e.strength + e.user->cheer_stacks;
                int celestial_factor = (e.user->mp * 100) / e.user->max_mp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CelestialLowHp: {
                int str = e.strength + e.user->cheer_stacks;
                int celestial_factor = 130 - (e.user->hp * 100) / e.user->max_hp;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 60;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case (DamageFormula)0x14: {
                int mag = e.magic;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * e.power / 16;
                e.damage = dmg;
                return;
            }

            case DamageFormula.ChosenGilDiv10: {
                e.damage = (int)Globals.Battle.btl->chosen_gil / 10;
                break;
            }

            case DamageFormula.TargetKills: {
                if (e.user->id < 0x12) {
                    e.damage = (int)Globals.save_data->ply_arr[e.user->id].enemies_defeated * e.power;
                }

                break;
            }

            case DamageFormula.Multiple9999: {
                e.damage = 9999 * e.power;
                break;
            }
        }

        dmg_calc.Invoke_PostCalcDamage(this, e);

        // Command is healing and target isn't Zombied
        if (e.command is not null && e.command->is_heal && !e.target_status_suffer.zombie()) {
            e.damage = -e.damage;
            dmg_calc.Invoke_PostApplyHealing(this, e);
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_ShieldBoost(DmgEventArgs e) {
        e.base_damage = e.damage;

        if (e.target_status_suffer_extra.shield()) {
            *FhUtil.ptr_at<byte>(e.target, 0x6da) = 1;
            e.damage /= 4;
            e.dmg_calc_flags |= DmgCalcFlags.SHIELD;
            return;
        }

        if (e.target_status_suffer_extra.boost()) {
            e.damage = e.damage * 3 / 2;
            return;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Shell(DmgEventArgs e) {
        e.base_damage = e.damage;

        // Original makes sure the command doesn't deal physical damage
        // This is undesired for mods, and not doing so doesn't have any impact on vanilla
        if (e.command->deals_magical_damage && e.target_status_suffer_duration.shell > 0) {
            *FhUtil.ptr_at<byte>(e.info, 3) = 2;
            e.dmg_calc_flags |= DmgCalcFlags.SHELL;
            e.damage /= 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Protect(DmgEventArgs e) {
        e.base_damage = e.damage;

        // Original makes sure the command doesn't deal magical damage
        // This is undesired for mods, and not doing so doesn't have any impact on vanilla
        if (e.command->deals_physical_damage && e.target_status_suffer_duration.protect > 0) {
            *FhUtil.ptr_at<byte>(e.info, 3) = 2;
            e.dmg_calc_flags |= DmgCalcFlags.PROTECT;
            e.damage /= 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Crit(DmgEventArgs e) {
        if (!e.command->can_crit) return;

        e.base_damage = e.damage;

        int rng_idx = _MsGetRndChr(e.user->id, 0);
        int rng = _brnd(rng_idx);

        byte crit_bonus = e.command->gives_crit_bonus ? e.command->crit_bonus : e.user->wpn_crit_bonus;

        int chance = (e.user->luck - e.target->luck) + e.user->luck_stacks + e.target->jinx_stacks + crit_bonus;

        bool hero_drink = (*FhUtil.ptr_at<byte>(e.user, 0x640) & 0x10) == 0;

        if (Globals.Battle.btl->debug.always_crit || hero_drink || chance > rng % 101) {
            e.dmg_calc_flags |= DmgCalcFlags.CRIT;
            e.damage *= 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Berserk(DmgEventArgs e) {
        e.base_damage = e.damage;

        if (e.user_status_suffer.berserk()
         && *FhUtil.ptr_at<short>(e.user, 0xf5c) == *FhUtil.ptr_at<short>(e.user, 0x6c6)) {
            e.damage = e.damage * 3 / 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    // Notably, we do not have to hook the original here despite it being called outside of DmgCalc,
    // as it is only called with no care for the damage. (see: MsGetCommandMP)
    private void DmgCalc_MagicBooster(DmgEventArgs e) {
        e.base_damage = e.damage;

        if (e.command_id == PlayerCommandId.PCOM_AUTO_LIFE_EX) {
            byte* user__0x5ca = FhUtil.ptr_at<byte>(e.user, 0x5ca);
            if (*user__0x5ca == 0) return;

            *user__0x5ca = 0;
            e.damage = e.damage * 3 / 2;
            return;
        }

        bool black_or_white_magic = e.command->sub_menu_cat2 == 1 || e.command->sub_menu_cat2 == 2;
        if (e.user->auto_ability_effects.has_magic_booster && black_or_white_magic) {
            e.damage = e.damage * 3 / 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Alchemy(DmgEventArgs e) {
        e.base_damage = e.damage;

        bool alchemy = e.user->auto_ability_effects.has_alchemy;
        bool is_item = e.command_id >> 0xC == 2;
        bool is_heal = e.command->is_heal;

        // This is probably undesired for mods, as they may want to add healing items with different formulas.
        bool good_formula = e.damage_formula is DamageFormula.Multiple50 or DamageFormula.MaxDiv16;

        if (alchemy && is_item && is_heal && good_formula) {
            e.damage *= 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_StatBoostAbility(DmgEventArgs e) {
        e.base_damage = e.damage;

        StatBoostData user_boosts = Globals.Battle.param_boost[e.user->id];
        StatBoostData target_boosts = Globals.Battle.param_boost[e.target->id];

        // Original makes sure the command doesn't deal magical damage
        // This is undesired for mods, and not doing so doesn't have any impact on vanilla
        if (e.command->deals_physical_damage) {
            if (user_boosts.strength != 0) {
                e.damage += e.damage * user_boosts.strength / 100;
            }

            if (target_boosts.defense != 0) {
                e.damage -= e.damage * target_boosts.defense / 100;
            }
        }

        // Original makes sure the command doesn't deal physical damage
        // This is undesired for mods, and not doing so doesn't have any impact on vanilla
        if (e.command->deals_magical_damage) {
            if (user_boosts.magic != 0) {
                e.damage += e.damage * user_boosts.magic / 100;
            }

            if (target_boosts.magic_defense != 0) {
                e.damage -= e.damage * target_boosts.magic_defense / 100;
            }
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_GravImmunity(DmgEventArgs e) {
        e.base_damage = e.damage;

        bool is_grav_formula = e.damage_formula is DamageFormula.CurrentDiv16 or DamageFormula.MaxDiv16;

        if (is_grav_formula && e.target->immune_gravity_dmg) {
            // In vanilla, this can be any damage class (HP, MP, CTB), but the function is only ever called with HP
            e.dmg_calc_flags &= ~DmgCalcFlags.DAMAGE_HP;
            e.immunity_count += 1;
            e.damage = 0;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_ApplyZombieDrainInteraction(DmgEventArgs e) {
        e.base_damage = e.damage;

        bool one_zombie = e.user_status_suffer.zombie() ^ e.target_status_suffer.zombie();
        if (e.command->is_draining && one_zombie) {
            e.damage = -e.damage;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Elem(DmgEventArgs e) {
        if (e.elements == ElementFlags.NONE) return;

        e.base_damage = e.damage;

        ElementFlags elem_weak   = e.target->elem_weak;
        ElementFlags elem_resist = e.target->elem_resist;
        ElementFlags elem_immune = e.target->elem_ignore;
        ElementFlags elem_absorb = e.target->elem_absorb;

        // Stack weaknesses
        bool weak_proc = false;
        for (int elem_idx = 0; elem_idx < sizeof(ElementFlags); elem_idx++) {
            if (!e.elements.HasFlag((ElementFlags)elem_idx)) continue;
            if (!elem_weak.HasFlag((ElementFlags)elem_idx)) continue;

            weak_proc = true;
            e.damage = e.damage * 3 / 2;
        }

        if (weak_proc) return;

        // Return early if there's any neutral element
        for (int elem_idx = 0; elem_idx < sizeof(ElementFlags); elem_idx++) {
            if (!e.elements.HasFlag((ElementFlags)elem_idx)) continue;
            if (elem_resist.HasFlag((ElementFlags)elem_idx)) continue;
            if (elem_immune.HasFlag((ElementFlags)elem_idx)) continue;
            if (elem_absorb.HasFlag((ElementFlags)elem_idx)) continue;

            return;
        }

        // Return early if there's any resistance, and only apply one
        for (int elem_idx = 0; elem_idx < sizeof(ElementFlags); elem_idx++) {
            if (!e.elements.HasFlag((ElementFlags)elem_idx)) continue;
            if (!elem_resist.HasFlag((ElementFlags)elem_idx)) continue;

            e.damage /= 2;
            return;
        }

        // Return early if there's any immunity
        for (int elem_idx = 0; elem_idx < sizeof(ElementFlags); elem_idx++) {
            if (!e.elements.HasFlag((ElementFlags)elem_idx)) continue;
            if (!elem_immune.HasFlag((ElementFlags)elem_idx)) continue;

            e.damage = 0;
            return;
        }

        // Account for absorption
        for (int elem_idx = 0; elem_idx < sizeof(ElementFlags); elem_idx++) {
            if (!e.elements.HasFlag((ElementFlags)elem_idx)) continue;
            if (!elem_absorb.HasFlag((ElementFlags)elem_idx)) continue;

            e.damage = -e.damage;
            return;
        }

        // This shouldn't be reachable
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Armored(DmgEventArgs e) {
        e.base_damage = e.damage;

        bool any_piercing =
            e.command->is_piercing
         || e.user->auto_ability_effects.has_piercing
         || e.target_status_suffer.armor_break();
        if (e.target->is_armored && any_piercing) {
            e.hit_armored = true;
            e.damage /= 3;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Defend(DmgEventArgs e) {
        if (!e.command->deals_physical_damage) return;

        e.base_damage = e.damage;

        if (e.target_status_suffer_extra.defend() || e.target_status_suffer_extra.sentinel()) {
            e.damage /= 2;
            *FhUtil.ptr_at<byte>(e.target, 0x6da) = 1;

            if (e.target_status_suffer_extra.sentinel()) {
                e.dmg_calc_flags |= DmgCalcFlags.SENTINEL;
            } else {
                e.dmg_calc_flags |= DmgCalcFlags.DEFEND;
            }
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_PowerMagicBreak(DmgEventArgs e) {
        e.base_damage = e.damage;

        if (e.command->deals_physical_damage && e.user_status_suffer.power_break()) {
            e.damage = e.base_damage / 2;
        }

        if (e.command->deals_magical_damage && e.user_status_suffer.magic_break()) {
            e.damage = e.base_damage / 2;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_DmgImmunity(DmgEventArgs e) {
        e.base_damage = e.damage;

        bool immune = false;
        if (e.command->deals_physical_damage) {
            immune |= e.target->immune_physical_dmg;
        }

        if (e.command->deals_magical_damage) {
            immune |= e.target->immune_magical_dmg;
        }

        if (immune || e.target->immune_all_dmg) {
            e.damage = 0;
            e.dmg_calc_flags &= ~(DmgCalcFlags)(1 << ((int)e.target_stat! - 1));
            e.immunity_count += 1;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Delay(DmgEventArgs e) {
        int delay_strength;

        if (e.command->inflicts_delay_strong) {
            delay_strength = 2;
        } else if (e.command->inflicts_delay_weak) {
            delay_strength = 1;
        } else {
            return;
        }

        int ctb_count = _MsGetRomCTBcount(e.target->agility);
        e.damage_ctb += (ctb_count & 0xFF) * 3 * delay_strength / 2;
        e.dmg_calc_flags |= DmgCalcFlags.DAMAGE_CTB;
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Threaten(DmgEventArgs e) {
        if (!e.dmg_calc_flags.damage_ctb()) return;
        if (!e.target_status_suffer.threaten()) return;

        e.damage_ctb = 0;
        e.dmg_calc_flags &= ~DmgCalcFlags.DAMAGE_CTB;

        // I believe this is correct? might be nullified instead but I think it's missed
        e.missed = true;
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_ShatterFail(DmgEventArgs e) {
        // If we were already petrified, and are still petrified, but failed to shatter
        if (e.target->status_suffer.petrification() && e.target_status_suffer.petrification() && !e.target_status_suffer_extra.eject()) {
            *p4 = 0;
            e.damage_hp = 0;
            e.damage_mp = 0;
            e.damage_ctb = 0;
            e.status_misses += 1;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_DelayImmunity(DmgEventArgs e) {
        if (!e.dmg_calc_flags.damage_ctb()) return;

        if (e.target->immune_ctb_damage && !e.threatened) {
            e.status_resists += 1;
            e.damage_ctb = 0;
        }

        if (e.damage_ctb != 0) {
            e.status_hits += 1;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_NoDamageDeath(DmgEventArgs e) {
        // If we just applied death
        if (!e.target->status_suffer.death() && e.target_status_suffer.death()) {
            e.damage_hp = 0;
            e.damage_mp = 0;
            e.damage_ctb = 0;
            e.dmg_calc_flags &= ~DmgCalcFlags.DAMAGE_CLASS;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_ApplyStatBuffs(DmgEventArgs e) {
        for (int i = 0; i < 6; i++) {
            int mask = 1 << i;

            if (!e.command->flags_buffs_stat.get_bit(mask)) continue;

            int buff_amount = e.command->buff_amount;
            if (buff_amount == 0) buff_amount = 1;

            byte new_amount = (byte)Math.Clamp(e.target->stat_up_stacks[i] + buff_amount, 0, 5);
            e.target->stat_up_stacks[i] = new_amount;
        }
    }

    // Replacement for the game's original function, so we can pass in the event args instead
    private void DmgCalc_Bribe(DmgEventArgs e) {
        if (!e.command->attempts_bribe) return;

        uint total_bribe = e.target->bribe_gil_spent + Globals.Battle.btl->chosen_gil;
        int score = (int)total_bribe * 256 / e.target->max_hp / 20 - 64;

        int rng_idx = _MsGetRndChr(e.target->id, 2);
        int rng = _brnd(rng_idx);

        if (FhGlobal.lang_id == FhLangId.Debug && *FhUtil.ptr_at<short>(e.target, 0xe) == 4335) {
            if (total_bribe < 157000) {
                e.status_misses += 1;
                return;
            }

            e.target->bribe_gil_spent += 18000;
        }

        if (e.target->immune_bribe || e.target_status_suffer_duration.sleep > 0) {
            e.status_resists += 1;
            e.target->bribe_gil_spent = Math.Clamp(e.target->bribe_gil_spent, 0, 999999999);
            return;
        }

        bool should_bribe = Globals.Battle.btl->chosen_gil > 0 && (rng & 0xFF) < score;
        if (should_bribe || Globals.Battle.btl->debug.always_hit) {
            *FhUtil.ptr_at<byte>(e.target, 0x701) = 2; // Probably signals the monster should walk away?
            e.target->bribe_score = score;
            e.target_status_suffer_extra |= StatusExtraFlags.EJECT;
            e.status_hits += 1;
            // Only path that'd return `1` is here, but the return value is never used by the game
        } else {
            e.status_misses += 1;
        }

        e.target->bribe_gil_spent = Math.Clamp(e.target->bribe_gil_spent, 0, 999999999);
    }

    private void DmgCalc_78c330(DmgEventArgs e) {
        if (e.status_hits == 0) {
            if (e.status_misses == 0) {
                if (e.status_resists != 0) {
                    p15[0] += 1;
                }
            } else {
                p15[1] += 1;
            }
        }

        int higher_damage = Math.Max(e.damage_hp, e.damage_mp);
        int lower_damage  = Math.Min(e.damage_hp, e.damage_mp);

        if (higher_damage < 1) {
            if (e.command->flags_damage_class == DamageClass.HP && e.dmg_calc_flags.damage_class() != DamageClass.NONE) {
                return e.target->stat_inv_motion.get_bit(0) ? 4 : 5;
            }

            if (p8->__0x4 != 0) return 8;
            if (p8->__0x5 != 0) return 11;
            if (p9->__0x8 != 0) return 12;
            if (p9->__0xC != 0) return 2;

            if (lower_damage < 0) {
                if (e.user->id == e.target->id) return 0;
                if (e.target_status_suffer_duration.sleep > 0) return 0;

                return 2;
            }

            if (e.command->flags_damage_class == DamageClass.MP) {
                if (e.dmg_calc_flags.damage_class() == DamageClass.NONE) goto LAB_0078c4e2;

                if (e.target->stat_inv_motion.get_bit(1)) return 3;
            }

            if (e.dmg_calc_flags.damage_class() != DamageClass.NONE) return 0;

        LAB_0078c4e2:
            if (e.damage_ctb != 0) return 0;

            if (p9->__0x4 == 0) {
                if (p8->__0x0 != 0) return 9;

                if (p9->__0x0 != 0) return 10;
            } else if (e.user->id != e.target->id && e.target_status_suffer_duration.sleep == 0) {
                return 7;
            }

            return 0;
        }

        int max_stat = _ switch {
            X => _MsGetRamChrHPmax(),
            Y => _MsGetRamChrMPmax(),
            Z => _MsGetRamChrCTBmax(),
        };

        bVar4 = 0;
        uVar5 = 5;
        if (e.hit_armored) {
        LAB_0078c3f4:
            uVar7 = uVar5;
            if (e.command->flags_damage_class == DamageClass.MP) goto LAB_0078c3f9;
        } else {
            if (magic_defense < 100 || e.command->flags_damage_class != DamageClass.MP) {
                uVar7 = 5;
                if (defense < 100) {
                    uVar7 = uVar5;
                    if (e.command->flags_damage_class != DamageClass.HP) goto LAB_0078c424;
                }

                goto LAB_0078c3f4;
            }

        LAB_0078c3f9:
            uVar7 = 5;
            if (bVar1.get_bit(1)) {
                uVar7 = uVar5;
                if (!e.target_status_suffer.mental_break()) {
                    bVar4 = 1;
                    uVar7 = 3;
                }
            }
        }

        if (e.command->flags_damage_class == DamageClass.HP && bVar1.get_bit(0) && !e.target_status_suffer.armor_break()) {
            uVar7 = 4;
            bVar4 = 1;
        }

    LAB_0078c424:
        if (!e.dmg_calc_flags.crit()) return uVar7;

        return 6 - bVar4;
    }

    private void DmgCalc_78c700(DmgEventArgs e) {
        uVar2 = 0;
        if ((param_1 == 0) && (param_2[1] != 0)) {
            uVar2 = 1;
        }
        if ((param_2[1] != 0) || (uVar1 = 2, *param_2 == 0)) {
            uVar1 = uVar2;
        }
        return uVar1;
    }

    private void DmgCalc_78c010(DmgEventArgs e) {
        bool did_damage_or_healed;

        int hp_damage  = e.info->out_damage_hp;
        int mp_damage  = e.info->out_damage_mp;
        int ctb_damage = e.info->out_damage_ctb;

        if (e.command->is_heal) {
            did_damage_or_healed = hp_damage < 0 || mp_damage < 0 || ctb_damage < 0;
        } else {
            did_damage_or_healed = hp_damage > 0 || mp_damage > 0 || ctb_damage > 0;
        }

        if (e.status_hits == 0) {
            if (did_damage_or_healed)
                *FhUtil.ptr_at<byte>(e.user, 0x715) = 1;
            return did_damage_or_healed; // Whether the attack had an effect?
        }

        *FhUtil.ptr_at<byte>(e.user, 0x715) = 1;
        return 1; // Whether the attack had an effect?
    }
}
