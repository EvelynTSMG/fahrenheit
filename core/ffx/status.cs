// SPDX-License-Identifier: MIT

namespace Fahrenheit.Core.FFX;

public struct StatusMap {
    public byte death;
    public byte zombie;
    public byte petrification;
    public byte poison;
    public byte power_break;
    public byte magic_break;
    public byte armor_break;
    public byte mental_break;
    public byte confuse;
    public byte berserk;
    public byte provoke;
    public byte threaten;
    public byte sleep;
    public byte silence;
    public byte darkness;
    public byte shell;
    public byte protect;
    public byte reflect;
    public byte nul_tide;
    public byte nul_blaze;
    public byte nul_shock;
    public byte nul_frost;
    public byte regen;
    public byte haste;
    public byte slow;

    public byte this[int i] {
        get {
            return i switch {
                0 => death,
                1 => zombie,
                2 => petrification,
                3 => poison,
                4 => power_break,
                5 => magic_break,
                6 => armor_break,
                7 => mental_break,
                8 => confuse,
                9 => berserk,
                10 => provoke,
                11 => threaten,
                12 => sleep,
                13 => silence,
                14 => darkness,
                15 => shell,
                16 => protect,
                17 => reflect,
                18 => nul_tide,
                19 => nul_blaze,
                20 => nul_shock,
                21 => nul_frost,
                22 => regen,
                23 => haste,
                24 => slow,
                _ => throw new IndexOutOfRangeException($"Index {i} does not exist in StatusMap"),
            };
        }

        set {
            _ = i switch {
                0 => death = value,
                1 => zombie = value,
                2 => petrification = value,
                3 => poison = value,
                4 => power_break = value,
                5 => magic_break = value,
                6 => armor_break = value,
                7 => mental_break = value,
                8 => confuse = value,
                9 => berserk = value,
                10 => provoke = value,
                11 => threaten = value,
                12 => sleep = value,
                13 => silence = value,
                14 => darkness = value,
                15 => shell = value,
                16 => protect = value,
                17 => reflect = value,
                18 => nul_tide = value,
                19 => nul_blaze = value,
                20 => nul_shock = value,
                21 => nul_frost = value,
                22 => regen = value,
                23 => haste = value,
                24 => slow = value,
                _ => throw new IndexOutOfRangeException($"Index {i} does not exist in StatusMap"),
            };
        }
    }
}

public struct StatusDurationMap {
    public byte sleep;
    public byte silence;
    public byte darkness;
    public byte shell;
    public byte protect;
    public byte reflect;
    public byte nul_tide;
    public byte nul_blaze;
    public byte nul_shock;
    public byte nul_frost;
    public byte regen;
    public byte haste;
    public byte slow;


    public byte this[int i] {
        get {
            return i switch {
                0 => sleep,
                1 => silence,
                2 => darkness,
                3 => shell,
                4 => protect,
                5 => reflect,
                6 => nul_tide,
                7 => nul_blaze,
                8 => nul_shock,
                9 => nul_frost,
                10 => regen,
                11 => haste,
                12 => slow,
                _ => throw new IndexOutOfRangeException($"Index {i} does not exist in StatusDurationMap"),
            };
        }

        set {
            _ = i switch {
                0 => sleep = value,
                1 => silence = value,
                2 => darkness = value,
                3 => shell = value,
                4 => protect = value,
                5 => reflect = value,
                6 => nul_tide = value,
                7 => nul_blaze = value,
                8 => nul_shock = value,
                9 => nul_frost = value,
                10 => regen = value,
                11 => haste = value,
                12 => slow = value,
                _ => throw new IndexOutOfRangeException($"Index {i} does not exist in StatusDurationMap"),
            };
        }
    }
}

[Flags]
public enum StatusPermanentFlags : ushort {
    NONE          =       0,
    DEATH         = 1 <<  0, // 10000000
    ZOMBIE        = 1 <<  1, // 11000000
    PETRIFICATION = 1 <<  2, // 10000000
    POISON        = 1 <<  3, // 11000000
    POWER_BREAK   = 1 <<  4, // 10000000
    MAGIC_BREAK   = 1 <<  5, // 10000000
    ARMOR_BREAK   = 1 <<  6, // 10000000
    MENTAL_BREAK  = 1 <<  7, // 10000000
    CONFUSE       = 1 <<  8, // 11000000
    BERSERK       = 1 <<  9, // 10000000
    PROVOKE       = 1 << 10, // 10000000
    THREATEN      = 1 << 11, // 10000000
}

public static partial class FhEnumExt {
    public static bool death        (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.DEATH);
    public static bool zombie       (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.ZOMBIE);
    public static bool petrification(this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.PETRIFICATION);
    public static bool poison       (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.POISON);
    public static bool confuse      (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.CONFUSE);
    public static bool berserk      (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.BERSERK);
    public static bool provoke      (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.PROVOKE);
    public static bool threaten     (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.THREATEN);

    public static bool power_break (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.POWER_BREAK);
    public static bool magic_break (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.MAGIC_BREAK);
    public static bool armor_break (this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.ARMOR_BREAK);
    public static bool mental_break(this StatusPermanentFlags flags) => flags.HasFlag(StatusPermanentFlags.MENTAL_BREAK);
}

[Flags]
public enum StatusTemporaryFlags : ushort {
    NONE        =       0,
    SLEEP       = 1 <<  0, // 10101100
    SILENCE     = 1 <<  1, // 11001100
    DARKNESS    = 1 <<  2, // 11001100
    SHELL       = 1 <<  3, // 00010100
    PROTECT     = 1 <<  4, // 00010100
    REFLECT     = 1 <<  5, // 00010100
    NUL_WATER   = 1 <<  6, // 00000000
    NUL_FIRE    = 1 <<  7, // 00000000
    NUL_THUNDER = 1 <<  8, // 00000000
    NUL_ICE     = 1 <<  9, // 00000000
    REGEN       = 1 << 10, // 00000011
    HASTE       = 1 << 11, // 00001100
    SLOW        = 1 << 12, // 10001100
}

public static partial class FhEnumExt {
    public static bool sleep   (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.SLEEP);
    public static bool silence (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.SILENCE);
    public static bool darkness(this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.DARKNESS);

    public static bool shell  (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.SHELL);
    public static bool protect(this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.PROTECT);
    public static bool reflect(this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.REFLECT);
    public static bool regen  (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.REGEN);
    public static bool haste  (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.HASTE);
    public static bool slow   (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.SLOW);

    public static bool nul_water  (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.NUL_WATER);
    public static bool nul_fire   (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.NUL_FIRE);
    public static bool nul_thunder(this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.NUL_THUNDER);
    public static bool nul_ice    (this StatusTemporaryFlags flags) => flags.HasFlag(StatusTemporaryFlags.NUL_ICE);
}

[Flags]
public enum StatusExtraFlags : ushort {
    NONE            =       0,
    SCAN            = 1 <<  0, // 00000000
    DISTILL_POWER   = 1 <<  1, // 00000000
    DISTILL_MANA    = 1 <<  2, // 00000000
    DISTILL_SPEED   = 1 <<  3, // 00000000
    DISTILL_MOVE    = 1 <<  4, // 00000000
    DISTILL_ABILITY = 1 <<  5, // 00000000
    SHIELD          = 1 <<  6, // 00000000
    BOOST           = 1 <<  7, // 00000000
    EJECT           = 1 <<  8, // 10000000
    AUTO_LIFE       = 1 <<  9, // 00000000
    CURSE           = 1 << 10, // 10000000
    DEFEND          = 1 << 11, // 00010000
    GUARD           = 1 << 12, // 00000000
    SENTINEL        = 1 << 13, // 00010000
    DOOM            = 1 << 14, // 10000000

    DISTILLS =
        DISTILL_POWER
      | DISTILL_MANA
      | DISTILL_SPEED
      | DISTILL_MOVE
      | DISTILL_ABILITY,
}

public static partial class FhEnumExt {
    public static bool distill_power  (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DISTILL_POWER);
    public static bool distill_mana   (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DISTILL_MANA);
    public static bool distill_speed  (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DISTILL_SPEED);
    public static bool distill_move   (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DISTILL_MOVE);
    public static bool distill_ability(this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DISTILL_ABILITY);
    public static bool distills       (this StatusExtraFlags flags) => (flags & StatusExtraFlags.DISTILLS) != 0;

    public static bool shield   (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.SHIELD);
    public static bool boost    (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.BOOST);
    public static bool scan     (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.SCAN);
    public static bool eject    (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.EJECT);
    public static bool auto_life(this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.AUTO_LIFE);
    public static bool curse    (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.CURSE);
    public static bool defend   (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DEFEND);
    public static bool guard    (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.GUARD);
    public static bool sentinel (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.SENTINEL);
    public static bool doom     (this StatusExtraFlags flags) => flags.HasFlag(StatusExtraFlags.DOOM);
}

[StructLayout(LayoutKind.Explicit, Size = 0x4)]
public struct StatusData {
    [FieldOffset(0x0)] public byte __0x0;
    [FieldOffset(0x1)] public byte __0x1;
    [FieldOffset(0x2)] public byte __0x2;
    [FieldOffset(0x3)] public byte __0x3;
}
