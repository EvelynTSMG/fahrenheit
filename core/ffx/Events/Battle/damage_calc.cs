// SPDX-License-Identifier: MIT

using Fahrenheit.Core.FFX.Battle;

[assembly: InternalsVisibleTo("fhruntime")]
namespace Fahrenheit.Core.FFX.Events.Battle;

public unsafe static class DamageCalc {
    // Most of MsCalcDamageCommand's parameters
    public class DamageCalcEventArgs {
        public Chr* user;
        public Chr* target;
        public Command* command;
        public DamageFormula damage_formula;
        public int power;
        public byte target_status__0x606;
        public int target_stat;
        public bool should_vary;
        public int damage;
    }

    //TODO: Think of more appropriate names for these
    //TODO: Or ideally a better way to *do* this tbh
    public class DamageCalcEventArgs2 : DamageCalcEventArgs {
        internal static DamageCalcEventArgs2 upgrade(DamageCalcEventArgs e) {
            return new() {
                user = e.user,
                target = e.target,
                command = e.command,
                damage_formula = e.damage_formula,
                power = e.power,
                target_status__0x606 = e.target_status__0x606,
                target_stat = e.target_stat,
                should_vary = e.should_vary,
                damage = e.damage,
            };
        }

        public byte defense;
        public byte magic_defense;
        public int current_stat;
        public int max_stat;
    }

    //TODO: Think of more appropriate names for these
    public class DamageCalcEventArgs3 : DamageCalcEventArgs2 {
        internal static DamageCalcEventArgs3 upgrade(DamageCalcEventArgs2 e) {
            return new() {
                user = e.user,
                target = e.target,
                command = e.command,
                damage_formula = e.damage_formula,
                power = e.power,
                target_status__0x606 = e.target_status__0x606,
                target_stat = e.target_stat,
                should_vary = e.should_vary,
                damage = e.damage,
                defense = e.defense,
                magic_defense = e.magic_defense,
                current_stat = e.current_stat,
                max_stat = e.max_stat,
            };
        }

        public int variance;
    }

    public delegate void DamageCalcEventHandler(object sender, DamageCalcEventArgs e);

    //TODO: Think of more appropriate names for these
    public delegate void DamageCalcEventHandler2(object sender, DamageCalcEventArgs2 e);

    //TODO: Think of more appropriate names for these
    public delegate void DamageCalcEventHandler3(object sender, DamageCalcEventArgs3 e);

    /// <summary>
    /// Raised before the Armor/Mental Break calculations.
    /// </summary>
    public static event DamageCalcEventHandler2? PreApplyDefenseBreak;

    /// <summary>
    /// Raised over the vanilla Armor Break calculation. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if the command does not damage HP.
    /// </summary>
    public static event DamageCalcEventHandler2? OnApplyArmorBreak;

    /// <summary>
    /// Raised over the vanilla Mental Break calculation. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if the command does not damage HP.
    /// </summary>
    public static event DamageCalcEventHandler2? OnApplyMentalBreak;

    /// <summary>
    /// Raised after the Armor/Mental Break calculations.
    /// </summary>
    public static event DamageCalcEventHandler2? PostApplyDefenseBreak;

    /// <summary>
    /// Raised over the vanilla variance-setting code. If nothing is subscribed, vanilla code will run.<br/>
    /// Will not be raised if <c>should_vary</c> is <c>false</c>.
    /// </summary>
    public static event DamageCalcEventHandler3? OnGetVariance;

    /// <summary>
    /// Raised after the variance is set.<br/>
    /// Will be raised even if <c>should_vary</c> is <c>false</c>.
    /// </summary>
    public static event DamageCalcEventHandler3? PostGetVariance;

    /// <summary>
    /// Raised after the damage is calculated.
    /// </summary>
    public static event DamageCalcEventHandler3? PostCalcDamage;

    /// <summary>
    /// Raised after, and only if, the damage is negated for healing.
    /// </summary>
    public static event DamageCalcEventHandler3? PostApplyHealing;


    // Internal methods for runtime to invoke the events
    internal static void Invoke_OnApplyArmorBreak(object sender, DamageCalcEventArgs2 e) {
        OnApplyArmorBreak?.Invoke(sender, e);
    }

    internal static void Invoke_OnApplyMentalBreak(object sender, DamageCalcEventArgs2 e) {
        OnApplyMentalBreak?.Invoke(sender, e);
    }

    internal static void Invoke_PreApplyDefenseBreak(object sender, DamageCalcEventArgs2 e) {
        PreApplyDefenseBreak?.Invoke(sender, e);
    }

    internal static void Invoke_PostApplyDefenseBreak(object sender, DamageCalcEventArgs2 e) {
        PostApplyDefenseBreak?.Invoke(sender, e);
    }

    internal static void Invoke_OnGetVariance(object sender, DamageCalcEventArgs3 e) {
        OnGetVariance?.Invoke(sender, e);
    }

    internal static void Invoke_PostGetVariance(object sender, DamageCalcEventArgs3 e) {
        PostGetVariance?.Invoke(sender, e);
    }

    internal static void Invoke_PostCalcDamage(object sender, DamageCalcEventArgs3 e) {
        PostCalcDamage?.Invoke(sender, e);
    }

    internal static void Invoke_PostApplyHealing(object sender, DamageCalcEventArgs3 e) {
        PostApplyHealing?.Invoke(sender, e);
    }

    // Internal methods for runtime to check if events have subscribers
    internal static bool IsNull_OnApplyArmorBreak() {
        return OnApplyArmorBreak == null;
    }

    internal static bool IsNull_OnApplyMentalBreak() {
        return OnApplyMentalBreak == null;
    }

    internal static bool IsNull_OnGetVariance() {
        return OnGetVariance == null;
    }
}
