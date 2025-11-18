namespace Fahrenheit.Core.FFX.Battle;

public enum CheckCounterResult {
    NO_COUNTER = 0,
    COUNTER = 1,
    EVADE_N_COUNTER = 2,
}

public enum CheckHitResult {
    HIT = 0,
    MISS = 1,
    MISS_ALIVE = 2,
}

public enum TargetStat {
    HP = 1,
    MP = 2,
    CTB = 3,
}

[Flags]
public enum DamageClass : byte {
    NONE = 0,
    HP   = 1,
    MP   = 2,
    CTB  = 4,
}

public static partial class FhEnumExt {
    public static bool hp (this DamageClass flags) => flags.HasFlag(DamageClass.HP);
    public static bool mp (this DamageClass flags) => flags.HasFlag(DamageClass.MP);
    public static bool ctb(this DamageClass flags) => flags.HasFlag(DamageClass.CTB);
}

[Flags]
public enum DmgCalcFlags : ushort {
    DAMAGE_HP     = 0b001,
    DAMAGE_MP     = 0b010,
    DAMAGE_CTB    = 0b100,
    DAMAGE_CLASS  = 0b111,

    DEFEND        = 0x8,
    SENTINEL      = 0x10,
    SHELL         = 0x20,
    PROTECT       = 0x40,

    OVERKILL      = 0x80,
    CRIT          = 0x100,

    SLEEP         = 0x200,
    SILENCE       = 0x400,
    DARKNESS      = 0x800,

    ZOMBIE        = 0x2000,
    PETRIFICATION = 0x4000,

    SHIELD        = 0x8000,
}

public static partial class FhEnumExt {
    public static bool  damage_hp (this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.DAMAGE_HP);
    public static bool  damage_mp (this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.DAMAGE_MP);
    public static bool  damage_ctb(this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.DAMAGE_CTB);
    public static DamageClass damage_class(this DmgCalcFlags flags) => (DamageClass)(flags & DmgCalcFlags.DAMAGE_CLASS);

    public static bool shelled (this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.SHELL);
    public static bool protect (this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.PROTECT);
    public static bool crit    (this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.CRIT);
    public static bool shielded(this DmgCalcFlags flags) => flags.HasFlag(DmgCalcFlags.SHIELD);
}

[StructLayout(LayoutKind.Explicit, Size = 0x2C)]
public struct DamageInfo {
    [FieldOffset(0x6)]  public byte                 flags_buffs_mix;
    [FieldOffset(0x7)]  public StatusDurationMap    target_status_suffer_duration;
    [FieldOffset(0x14)] public StatusPermanentFlags target_status_suffer;
    [FieldOffset(0x16)] public StatusExtraFlags     target_status_suffer_extra;
    [FieldOffset(0x20)] public int                  out_damage_hp;
    [FieldOffset(0x24)] public int                  out_damage_mp;
    [FieldOffset(0x28)] public int                  out_damage_ctb;
}
