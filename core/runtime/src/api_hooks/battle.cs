// SPDX-License-Identifier: MIT

using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Battle;
using Fahrenheit.Core.FFX.Events.Battle;
using Fahrenheit.Core.FFX.Ids;

namespace Fahrenheit.Core.Runtime;

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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate Command* MsGetRomPlyCommand(int id, byte* out_text);

    private readonly MsGetRomPlyCommand _MsGetRomPlyCommand = FhUtil.get_fptr<MsGetRomPlyCommand>(FhCall.__addr_MsGetRomPlyCommand);

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
        DamageFormula damage_formula = (DamageFormula)_damage_formula;
        bool should_vary = _should_vary != 0;

        DamageCalc.DamageCalcEventArgs args1 = new() {
            user = user,
            target = target,
            command = command,
            damage = damage,
            damage_formula = damage_formula,
            power = power,
            should_vary = should_vary,
            target_stat = target_stat,
            target_status__0x606 = target_status__0x606,
        };

        int chr_rng_idx = _MsGetRndChr(user->id, 0);

        byte defense = target->defense;
        byte magic_defense = target->magic_defense;

        int current_stat = target_stat switch {
            1 => target->current_hp,
            2 => target->current_mp,
            4 => target->current_ctb,
            _ => 0,
        };

        int max_stat = target_stat switch {
            1 => target->max_hp,
            2 => target->max_mp,
            3 => target->max_ctb,
            _ => 0,
        };

        DamageCalc.DamageCalcEventArgs2 args2 = DamageCalc.DamageCalcEventArgs2.upgrade(args1);
        args2.defense = defense;
        args2.magic_defense = magic_defense;
        args2.current_stat = current_stat;
        args2.max_stat = max_stat;

        DamageCalc.Invoke_PreApplyDefenseBreak(this, args2);

        if (command is not null && command->damages_hp) {
            // Armor Break
            if (args2.target_status__0x606.get_bit(6)) {
                if (DamageCalc.IsNull_OnApplyArmorBreak()) {
                    args2.defense = 0;
                } else {
                    DamageCalc.Invoke_OnApplyArmorBreak(this, args2);
                }
            }

            // Mental Break
            if (args2.target_status__0x606.get_bit(7)) {
                if (DamageCalc.IsNull_OnApplyMentalBreak()) {
                    args2.magic_defense = 0;
                } else {
                    DamageCalc.Invoke_OnApplyMentalBreak(this, args2);
                }
            }
        }

        DamageCalc.Invoke_PostApplyDefenseBreak(this, args2);

        DamageCalc.DamageCalcEventArgs3 args3 = DamageCalc.DamageCalcEventArgs3.upgrade(args2);
        args3.variance = 256;

        if (should_vary) {
            if (DamageCalc.IsNull_OnGetVariance()) {
                int variance_rng = _brnd(chr_rng_idx);
                args3.variance = (variance_rng & 31) + 240;
            } else {
                DamageCalc.Invoke_OnGetVariance(this, args3);
            }
        }

        DamageCalc.Invoke_PostGetVariance(this, args3);

        // Keeping all the integer division here makes this difficult to write more clearly
        // Maybe I'm being a bit overly careful when it comes to keeping all the base game rounding and it could've been simpler,
        // but I'm not going to test all that to make sure
        switch (args3.damage_formula) {
            case DamageFormula.StrVsDef: {
                int str = user->strength + user->cheer_stacks;
                int dr = (args3.defense * 51 - (args3.defense * args3.defense) / 11) / 10;
                int dmg = str * str * str / 32 + 30;
                dmg = (730 - dr) * dmg / 730;
                dmg *= (15 - target->cheer_stacks) / 15;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.StrIgnoreDef: {
                int str = user->strength + user->cheer_stacks;
                int dmg = str * str * str / 32 + 30;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.MagVsMDef: {
                int mag = user->magic + user->focus_stacks;
                int dr = (args3.magic_defense * 51 - (args3.magic_defense * args3.magic_defense) / 11) / 10;
                int dmg = ((mag * mag / 6) + args3.power) * args3.power;
                dmg = ((730 - dr) * dmg / 4) / 730;
                dmg *= (15 - target->focus_stacks) / 15;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.MagIgnoreMDef: {
                int mag = user->magic + user->focus_stacks;
                int dmg = ((mag * mag / 6) + args3.power) * args3.power;
                dmg /= 4;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.CurrentDiv16: {
                args3.damage = current_stat * args3.power / 16;
                break;
            }

            case DamageFormula.Multiple50: {
                args3.damage = 50 * args3.power;
                break;
            }

            case DamageFormula.Healing: {
                int mag = user->magic + user->focus_stacks;
                int dmg = (mag + args3.power) / 2 * power;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.MaxDiv16: {
                args3.damage = max_stat * args3.power / 16;
                break;
            }

            case DamageFormula.Multiple50WithVariance: {
                int dmg = 50 * args3.power;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.TargetMaxMpDiv16: {
                args3.damage = target->max_mp * args3.power / 16;
                break;
            }

            case DamageFormula.TargetMaxCtbDiv16: {
                args3.damage = target->max_ctb * args3.power / 16;
                break;
            }

            case DamageFormula.TargetMpDiv16: {
                args3.damage = target->current_mp * args3.power / 16;
                break;
            }

            case DamageFormula.TargetCtbDiv16: {
                args3.damage = target->current_ctb * args3.power / 16;
                break;
            }

            case (DamageFormula)0x0E: {
                int str = user->strength;
                int dmg = (str * str * str) / 32 + 30;
                dmg = dmg * args3.power / 16;
                args3.damage = dmg;
                return args3.damage;
            }

            case DamageFormula.MagSpecial: {
                int mag = user->magic + user->focus_stacks;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.UserMaxHpDiv10: {
                args3.damage = user->max_hp * args3.power / 10;
                break;
            }

            case DamageFormula.CelestialHighHp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = (user->hp * 100) / user->max_hp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.CelestialHighMp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = (user->mp * 100) / user->max_mp + 10;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 110;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case DamageFormula.CelestialLowHp: {
                int str = user->strength + user->cheer_stacks;
                int celestial_factor = 130 - (user->hp * 100) / user->max_hp;
                int dmg = (str * str * str / 32) + 30;
                dmg = celestial_factor * dmg / 60;
                dmg = dmg * args3.power / 16;
                dmg = dmg * args3.variance / 256;
                args3.damage = dmg;
                break;
            }

            case (DamageFormula)0x14: {
                int mag = user->magic;
                int dmg = (mag * mag * mag) / 32 + 30;
                dmg = dmg * power / 16;
                args3.damage = dmg;
                return args3.damage;
            }

            case DamageFormula.ChosenGilDiv10: {
                args3.damage = (int)Globals.Battle.btl->chosen_gil / 10;
                break;
            }

            case DamageFormula.TargetKills: {
                if (user->id < 0x12) {
                    args3.damage = (int)Globals.save_data->ply_arr[user->id].enemies_defeated * power;
                }

                break;
            }

            case DamageFormula.Multiple9999: {
                args3.damage = 9999 * power;
                break;
            }
        }

        DamageCalc.Invoke_PostCalcDamage(this, args3);

        // Command is healing and target isn't Zombied
        if (command is not null && command->is_heal && !target_status__0x606.get_bit(1)) {
            args3.damage = -args3.damage;
            DamageCalc.Invoke_PostApplyHealing(this, args3);
        }

        if (out_defense is not null) {
            *out_defense = args3.defense;
        }

        if (out_magic_defense is not null) {
            *out_magic_defense = args3.magic_defense;
        }

        return args3.damage;
    }
}
