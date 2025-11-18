// SPDX-License-Identifier: MIT

namespace Fahrenheit.Core.FFX.Battle;


[StructLayout(LayoutKind.Explicit, Size=0x48)]
public struct AttackInfo {
    [InlineArray(4)]
    public struct AttackCommandInfoArray {
        private AttackCommandInfo _data;
    }

    [FieldOffset(0x0)] public byte attacker_id;
    [FieldOffset(0x3)] public byte command_count;
    [FieldOffset(0x8)] public AttackCommandInfoArray commands_info;
}

[StructLayout(LayoutKind.Explicit, Size=0x10)]
public struct AttackCommandInfo {
    [InlineArray(2)]
    public struct CommandIdsArray {
        private T_XCommandId _id;
    }

    [FieldOffset(0x0)] public CommandIdsArray command_ids;
    [FieldOffset(0x8)] public int targets; // bitfield?
}
