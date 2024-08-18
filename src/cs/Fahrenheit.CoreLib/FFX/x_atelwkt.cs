﻿namespace Fahrenheit.CoreLib.FFX;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x4C)]
public unsafe struct AtelWorkThread
{
    [FieldOffset(0x00)] public  fixed int                   script_offset_stack[4];
    [FieldOffset(0x10)] public  fixed short                 script_idx_stack[4];
    [FieldOffset(0x18)] public        byte*                 instruction_ptr;
    [FieldOffset(0x1C)] private       byte                  __0x1C;
    [FieldOffset(0x1D)] public        byte                  stack_depth;
    [FieldOffset(0x1E)] private       byte                  __0x1E;
    [FieldOffset(0x1F)] public        byte                  wait_state;
    [FieldOffset(0x20)] public        int                   reg_x;
    [FieldOffset(0x24)] public        int                   reg_y;
    [FieldOffset(0x28)] public        float                 reg_a;
    [FieldOffset(0x2C)] public        AtelWorkThreadStorage thread_local_storage;
    [FieldOffset(0x40)] public        ushort                ctrl_idx;
    [FieldOffset(0x44)] public        void*                 motion;
    [FieldOffset(0x48)] public        void*                 rotation;
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x14)]
public unsafe struct AtelWorkThreadStorage
{
    private static void throw_if_var_oversized<T>(int offset, bool is_read) where T : unmanaged
    {
        int oversized_by = offset + sizeof(T) - sizeof(AtelWorkThreadStorage);

        if (oversized_by > 0)
        {
            throw new System.ArgumentOutOfRangeException(
                paramName: nameof(offset),
                message:   $"Cannot {(is_read ? "read" : "write")} value of type {typeof(T).FullName} from AtelWorkThread storage at offset {offset}; the value is {oversized_by} bytes too large."
            );
        }
    }

    public T get_value<T>(int offset) where T : unmanaged
    {
        throw_if_var_oversized<T>(offset, is_read: true);

        fixed (AtelWorkThreadStorage* storage = &this)
        {
            return *(T*)((nint)storage + offset);
        }
    }

    public void set_value<T>(int offset, T value) where T : unmanaged
    {
        throw_if_var_oversized<T>(offset, is_read: false);

        fixed (AtelWorkThreadStorage* storage = &this)
        {
            *(T*)((nint)storage + offset) = value;
        }
    }
}
