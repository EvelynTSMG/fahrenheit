// SPDX-License-Identifier: MIT

using Fahrenheit.Core.FFX.Battle;

[assembly: InternalsVisibleTo("fhruntime")]
namespace Fahrenheit.Core.FFX.Events.Battle;

public unsafe class FhXEventsBattleDamageCalc {
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
        public byte defense;
        public byte magic_defense;
        public int current_stat;
        public int max_stat;
        public int variance;
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
