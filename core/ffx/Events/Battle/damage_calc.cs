// SPDX-License-Identifier: MIT

using System.Diagnostics;

using Fahrenheit.Core.FFX.Battle;

[assembly: InternalsVisibleTo("fhruntime")]
namespace Fahrenheit.Core.FFX.Events.Battle;

public unsafe class FhXEventsBattleDamageCalc {
    public class DamageCalcEventArgs {
        /// <summary>
        /// The attacker.
        /// </summary>
        public required Chr* user;

        /// <summary>
        /// The target of the attack.
        /// </summary>
        public required Chr* target;

        /// <summary>
        /// The id of the command used in the attack. Provided for ease of use.
        /// </summary>
        public required int command_id;

        /// <summary>
        /// The command used in the attack.
        /// </summary>
        public required Command* command;

        /// <summary>
        /// The base game's <c>DamageInfo</c> struct, similar to this <c>DamageCalcEventArgs</c> class.<br/>
        /// Only use this if you know what you're doing!
        /// </summary>
        public required DamageInfo* info;

        /// <summary>
        /// Whether the command should be countered or evade'n'countered.
        /// </summary>
        public CheckCounterResult? counter;

        /// <summary>
        /// Whether the command hit, missed, or missed because the target is alive.
        /// </summary>
        public CheckHitResult? hit;

        /// <summary>
        /// The damage formula to use for the base damage calculation.
        /// </summary>
        public required DamageFormula damage_formula;

        /// <summary>
        /// The power of the attack.
        /// </summary>
        public required int power;

        /// <summary>
        /// The elements of the attack.
        /// </summary>
        public required ElementFlags elements;

        /// <summary>
        /// The extra statuses to inflict.
        /// </summary>
        public required StatusExtraFlags status_inflict_extra;

        /// <summary>
        /// The damage calculation flags indicating what the attack is damaging and whether various events have occured.
        /// </summary>
        public required DmgCalcFlags dmg_calc_flags;

        /// <summary>
        /// How many times the attack has been nullified.
        /// </summary>
        // I don't know why this is a count either
        public required int immunity_count;

        /// <summary>
        /// How many statuses have been successfully inflicted or cleansed.
        /// Will always be <c>0</c> until the status infliction step.
        /// </summary>
        public required int status_hits;

        /// <summary>
        /// How many statuses have failed to be inflicted or cleansed.
        /// Will always be <c>0</c> until the status infliction step.
        /// </summary>
        public required int status_misses;

        /// <summary>
        /// How many statuses have been resisted.
        /// Will always be <c>0</c> until the status infliction step.
        /// </summary>
        public required int status_resists;

        // /// <summary>
        // /// How many statuses have been inflicted.
        // /// Will always be <c>0</c> until the status infliction step.
        // /// </summary>
        // public required int status_inflicts;
        //
        // /// <summary>
        // /// How many statuses have been cleansed.
        // /// Will always be <c>0</c> until the status infliction step.
        // /// </summary>
        // public required int status_cleanses;

        /// <summary>
        /// How many debuffs have been inflicted.
        /// Will always be <c>0</c> until the status infliction step.
        /// </summary>
        public required int debuff_inflicts;

        /// <summary>
        /// How many debuffs have been cleansed.
        /// Will always be <c>0</c> until the status infliction step.
        /// </summary>
        public required int debuff_cleanses;

        /// <summary>
        /// Whether the attack missed.
        /// </summary>
        public required bool missed;

        /// <summary>
        /// Whether the attack hit an armored target.
        /// </summary>
        //TODO: Set this for MP and CTB damage instead of just HP.
        //TODO: Set this before DmgCalc_Armored
        public required bool hit_armored;

        /// <summary>
        /// Whether the attack inflicted threaten.
        /// </summary>
        public required bool threatened;

        /// <summary>
        /// The target stat of the damage calculation. Only present when calculating damage.
        /// </summary>
        public TargetStat? target_stat;

        /// <summary>
        /// The user's current stat, depending on the target stat.
        /// </summary>
        public int? user_current_stat => target_stat switch {
            TargetStat.HP  => user->current_hp,
            TargetStat.MP  => user->current_mp,
            TargetStat.CTB => user->current_ctb,
            null => null,
            _ => throw new UnreachableException($"Unknown TargetStat: {(int)target_stat}"),
        };

        /// <summary>
        /// The user's max stat, depending on the target stat.
        /// </summary>
        public int? user_max_stat => target_stat switch {
            TargetStat.HP  => user->max_hp,
            TargetStat.MP  => user->max_mp,
            TargetStat.CTB => user->max_ctb,
            null => null,
            _ => throw new UnreachableException($"Unknown TargetStat: {(int)target_stat}"),
        };

        /// <summary>
        /// The target's current stat, depending on the target stat.
        /// </summary>
        public int? target_current_stat => target_stat switch {
            TargetStat.HP  => target->current_hp,
            TargetStat.MP  => target->current_mp,
            TargetStat.CTB => target->current_ctb,
            null => null,
            _ => throw new UnreachableException($"Unknown TargetStat: {(int)target_stat}"),
        };

        /// <summary>
        /// The target's max stat, depending on the target stat.
        /// </summary>
        public int? target_max_stat => target_stat switch {
            TargetStat.HP  => target->max_hp,
            TargetStat.MP  => target->max_mp,
            TargetStat.CTB => target->max_ctb,
            null => null,
            _ => throw new UnreachableException($"Unknown TargetStat: {(int)target_stat}"),
        };

        /// <summary>
        /// Whether the damage should include variance.
        /// </summary>
        public required bool should_vary;

        /// <summary>
        /// The variance to be used in the damage calculation. Normally, only affects base damage.<br/>
        /// Defaults to <c>256</c>, which is the average. Vanilla variance ranges from <c>240</c> to <c>271</c>.
        /// </summary>
        public required int variance;

        /// <summary>
        /// The damage that would be dealt absent modifiers, including variance and statuses.
        /// </summary>
        public int? expected_damage;

        /// <summary>
        /// Base damage for the current calculation step. Updated to equal <see cref="damage"/> before every step.
        /// </summary>
        public required int base_damage;

        /// <summary>
        /// The damage value being used in the calculation steps. May not be final.
        /// Use the <see cref="damage_hp"/>/<see cref="damage_mp">mp</see>/<see cref="damage_ctb">ctb</see> fields after the damage calculation is done.
        /// </summary>
        public required int damage;

        /// <summary>
        /// The damage value to be dealt to HP.
        /// Equal to <c>0</c> if no damage is to be dealt or if the damage calculation is not done.
        /// </summary>
        public required int damage_hp;

        /// <summary>
        /// The damage value to be dealt to MP.
        /// Equal to <c>0</c> if no damage is to be dealt or if the damage calculation is not done.
        /// </summary>
        public required int damage_mp;

        /// <summary>
        /// The damage value to be dealt to CTB.
        /// Equal to <c>0</c> if no damage is to be dealt or if the damage calculation is not done.
        /// </summary>
        public required int damage_ctb;

        /// <summary>
        /// The strength to be used in place of the user's usual strength stat.
        /// </summary>
        public required byte strength;

        /// <summary>
        /// The magic to be used in place of the user's usual magic stat.
        /// </summary>
        public required byte magic;

        /// <summary>
        /// The defense to be used in place of the target's usual defense stat.
        /// </summary>
        public required byte defense;

        /// <summary>
        /// The magic defense to be used in place of the target's usual defense stat.
        /// </summary>
        public required byte magic_defense;

        /// <summary>
        /// The permanent status bitfield to be used in place of the user's usual permanent status.
        /// </summary>
        public required StatusPermanentFlags user_status_suffer;

        /// <summary>
        /// The temporary status duration map to be used in place of the user's usual temporary status.
        /// </summary>
        public required StatusDurationMap user_status_suffer_duration;

        /// <summary>
        /// The extra status bitfield to be used in place of the user's usual extra status.
        /// </summary>
        public required StatusExtraFlags user_status_suffer_extra;

        /// <summary>
        /// The permanent status bitfield to be used in place of the target's usual permanent status.
        /// </summary>
        public required StatusPermanentFlags target_status_suffer;

        /// <summary>
        /// The temporary status duration map to be used in place of the target's usual temporary status.
        /// </summary>
        public required StatusDurationMap target_status_suffer_duration;

        /// <summary>
        /// The extra status bitfield to be used in place of the target's usual extra status.
        /// </summary>
        public required StatusExtraFlags target_status_suffer_extra;
    }

    /// <summary>
    /// Raised before the Armor/Mental Break calculations.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? PreApplyDefenseBreak;

    /// <summary>
    /// Raised over the vanilla Armor Break calculation. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if the command does not damage HP.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? OnApplyArmorBreak;

    /// <summary>
    /// Raised over the vanilla Mental Break calculation. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if the command does not damage HP.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? OnApplyMentalBreak;

    /// <summary>
    /// Raised after the Armor/Mental Break calculations.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? PostApplyDefenseBreak;

    /// <summary>
    /// Raised over the vanilla variance-setting code. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if <c>should_vary</c> is <c>false</c>.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? OnGetVariance;

    /// <summary>
    /// Raised after the variance is set.<br/>
    /// Will be raised even if <c>should_vary</c> is <c>false</c>.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? PostGetVariance;

    /// <summary>
    /// Raised after the damage is calculated.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? PostCalcDamage;

    /// <summary>
    /// Raised after, and only if, the damage is negated for healing.
    /// </summary>
    public event EventHandler<DamageCalcEventArgs>? PostApplyHealing;


    // Internal methods for runtime to invoke the events
    internal void Invoke_OnApplyArmorBreak(object sender, DamageCalcEventArgs e) {
        OnApplyArmorBreak?.Invoke(sender, e);
    }

    internal void Invoke_OnApplyMentalBreak(object sender, DamageCalcEventArgs e) {
        OnApplyMentalBreak?.Invoke(sender, e);
    }

    internal void Invoke_PreApplyDefenseBreak(object sender, DamageCalcEventArgs e) {
        PreApplyDefenseBreak?.Invoke(sender, e);
    }

    internal void Invoke_PostApplyDefenseBreak(object sender, DamageCalcEventArgs e) {
        PostApplyDefenseBreak?.Invoke(sender, e);
    }

    internal void Invoke_OnGetVariance(object sender, DamageCalcEventArgs e) {
        OnGetVariance?.Invoke(sender, e);
    }

    internal void Invoke_PostGetVariance(object sender, DamageCalcEventArgs e) {
        PostGetVariance?.Invoke(sender, e);
    }

    internal void Invoke_PostCalcDamage(object sender, DamageCalcEventArgs e) {
        PostCalcDamage?.Invoke(sender, e);
    }

    internal void Invoke_PostApplyHealing(object sender, DamageCalcEventArgs e) {
        PostApplyHealing?.Invoke(sender, e);
    }

    // Internal methods for runtime to check if events have subscribers
    internal bool IsNull_OnApplyArmorBreak() {
        return OnApplyArmorBreak == null;
    }

    internal bool IsNull_OnApplyMentalBreak() {
        return OnApplyMentalBreak == null;
    }

    internal bool IsNull_OnGetVariance() {
        return OnGetVariance == null;
    }
}
