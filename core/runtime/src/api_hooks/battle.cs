// SPDX-License-Identifier: MIT

using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Battle;
using Fahrenheit.Core.FFX.Events;
using Fahrenheit.Core.FFX.Events.Battle;
using Fahrenheit.Core.FFX.Ids;

namespace Fahrenheit.Core.Runtime;

using DmgEventArgs = FhXEventsBattleDamageCalc.DamageCalcEventArgs;

//TODO: Think of a more appropriate name for this
[FhLoad(FhGameType.FFX)]
public unsafe class FhBattleAPIs : FhModule {
    private FhModContext _context = null!;
    private FileStream _global_state = null!;

    // Dirty workarounds for FhCall not being up-to-date
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsCalcDamageCommand(
        Chr* user,
        Chr* target,
        Command* command,
        byte damage_formula,
        int power,
        byte target_status__0x606,
        int target_stat,
        int should_vary,
        int* out_defense,
        int* out_magic_defense,
        int damage
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MsGetRndChr(int chr_id, int p2);

    private readonly MsGetRndChr _MsGetRndChr = FhUtil.get_fptr<MsGetRndChr>(FhCall.__addr_MsGetRndChr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int brnd(int rng_idx);

    private readonly brnd _brnd = FhUtil.get_fptr<brnd>(FhCall.__addr_brnd);

    // Method Handles
    private FhMethodHandle<MsCalcDamageCommand> _mh_MsCalcDamageCommand = null!;

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _context = mod_context;
        _global_state = global_state_file;

        _mh_MsCalcDamageCommand = new(this, "FFX.exe", FhCall.__addr_MsCalcDamageCommand, h_MsCalcDamageCommand);
        _mh_MsCalcDamageCommand.hook();

        return true;
    }

    private int h_MsCalcDamageCommand(
        Chr* user,
        Chr* target,
        Command* command,
        byte _damage_formula,
        int power,
        byte target_status__0x606,
        int target_stat,
        int _should_vary,
        int* out_defense,
        int* out_magic_defense,
        int damage
    ) {
        // Convert parameters to more accurate types
        var damage_formula = (DamageFormula)_damage_formula;
        bool should_vary = _should_vary != 0;

        // Shorthand for method calls
        FhXEventsBattleDamageCalc dmg_calc = FhXEvents.battle.damage_calc;

        DmgEventArgs e = new() {
            user = user,
            target = target,
            command = command,
            damage = damage,
            damage_formula = damage_formula,
            power = power,
            should_vary = should_vary,
            target_stat = target_stat,
            target_status__0x606 = target_status__0x606,
            defense = target->defense,
            magic_defense = target->magic_defense,
            current_stat = target_stat switch {
                1 => target->current_hp,
                2 => target->current_mp,
                4 => target->current_ctb,
                _ => 0,
            },
            max_stat = target_stat switch {
                1 => target->max_hp,
                2 => target->max_mp,
                3 => target->max_ctb,
                _ => 0,
            },
            variance = 256,
        };

        int chr_rng_idx = _MsGetRndChr(user->id, 0);

        dmg_calc.Invoke_PreApplyDefenseBreak(this, e);

        if (command is not null && command->damages_hp) {
            // Armor Break
            if (e.target_status__0x606.get_bit(6)) {
                if (dmg_calc.IsNull_OnApplyArmorBreak()) {
                    e.defense = 0;
                } else {
                    dmg_calc.Invoke_OnApplyArmorBreak(this, e);
                }
            }

            // Mental Break
            if (e.target_status__0x606.get_bit(7)) {
                if (dmg_calc.IsNull_OnApplyMentalBreak()) {
                    e.magic_defense = 0;
                } else {
                    dmg_calc.Invoke_OnApplyMentalBreak(this, e);
                }
            }
        }

        dmg_calc.Invoke_PostApplyDefenseBreak(this, e);

        if (should_vary) {
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
                int str = user->strength + user->cheer_stacks;
                int dr = (e.defense * 51 - (e.defense * e.defense) / 11) / 10;
                int dmg = str * str * str / 32 + 30;
                dmg = (730 - dr) * dmg / 730;
                dmg *= (15 - target->cheer_stacks) / 15;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.StrIgnoreDef: {
                int str = user->strength + user->cheer_stacks;
                int dmg = str * str * str / 32 + 30;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MagVsMDef: {
                int mag = user->magic + user->focus_stacks;
                int dr = (e.magic_defense * 51 - (e.magic_defense * e.magic_defense) / 11) / 10;
                int dmg = ((mag * mag / 6) + e.power) * e.power;
                dmg = ((730 - dr) * dmg / 4) / 730;
                dmg *= (15 - target->focus_stacks) / 15;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MagIgnoreMDef: {
                int mag = user->magic + user->focus_stacks;
                int dmg = ((mag * mag / 6) + e.power) * e.power;
                dmg /= 4;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CurrentDiv16: {
                e.damage = e.current_stat * e.power / 16;
                break;
            }

            case DamageFormula.Multiple50: {
                e.damage = 50 * e.power;
                break;
            }

            case DamageFormula.Healing: {
                int mag = user->magic + user->focus_stacks;
                int dmg = (mag + e.power) / 2 * power;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.MaxDiv16: {
                e.damage = e.max_stat * e.power / 16;
                break;
            }

            case DamageFormula.Multiple50WithVariance: {
                int dmg = 50 * e.power;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.TargetMaxMpDiv16: {
                e.damage = target->max_mp * e.power / 16;
                break;
            }

            case DamageFormula.TargetMaxCtbDiv16: {
                e.damage = target->max_ctb * e.power / 16;
                break;
            }

            case DamageFormula.TargetMpDiv16: {
                e.damage = target->current_mp * e.power / 16;
                break;
            }

            case DamageFormula.TargetCtbDiv16: {
                e.damage = target->current_ctb * e.power / 16;
                break;
            }

            case (DamageFormula)0x0E: {
                int str = user->strength;
                int dmg = (str * str * str) / 32 + 30;
                dmg = dmg * e.power / 16;
                e.damage = dmg;
                return e.damage;
            }

            case DamageFormula.MagSpecial: {
                int mag = user->magic + user->focus_stacks;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.UserMaxHpDiv10: {
                e.damage = user->max_hp * e.power / 10;
                break;
            }

            case DamageFormula.CelestialHighHp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = (user->hp * 100) / user->max_hp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CelestialHighMp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = (user->mp * 100) / user->max_mp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case DamageFormula.CelestialLowHp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = 130 - (user->hp * 100) / user->max_hp;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 60;
                dmg = dmg * e.power / 16;
                dmg = dmg * e.variance / 256;
                e.damage = dmg;
                break;
            }

            case (DamageFormula)0x14: {
                int mag = user->magic;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * power / 16;
                e.damage = dmg;
                return e.damage;
            }

            case DamageFormula.ChosenGilDiv10: {
                e.damage = (int)Globals.Battle.btl->chosen_gil / 10;
                break;
            }

            case DamageFormula.TargetKills: {
                if (user->id < 0x12) {
                    e.damage = (int)Globals.save_data->ply_arr[user->id].enemies_defeated * power;
                }

                break;
            }

            case DamageFormula.Multiple9999: {
                e.damage = 9999 * power;
                break;
            }
        }

        dmg_calc.Invoke_PostCalcDamage(this, e);

        // Command is healing and target isn't Zombied
        if (command is not null && command->is_heal && !target_status__0x606.get_bit(1)) {
            e.damage = -e.damage;
            dmg_calc.Invoke_PostApplyHealing(this, e);
        }

        if (out_defense is not null) {
            *out_defense = e.defense;
        }

        if (out_magic_defense is not null) {
            *out_magic_defense = e.magic_defense;
        }

        return e.damage;
    }
}
