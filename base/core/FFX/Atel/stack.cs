﻿namespace Fahrenheit.Core.FFX.Atel;

public class AtelStackOverflowException : Exception {
    public AtelStackOverflowException()                                         : base()                        { }
    public AtelStackOverflowException(string message)                           : base(message)                 { }
    public AtelStackOverflowException(string message, Exception innerException) : base(message, innerException) { }
}

public enum AtelStackType : byte {
    I32  = 0x01,
    F32  = 0x02,
    None = 0xCD,
}

[InlineArray(20)]
public struct AtelStackTypeArray {
    private AtelStackType _b;
}

[InlineArray(80)]
public struct AtelStackValueArray {
    private byte _b;

    [UnscopedRef] public Span<int>   as_int  () { return MemoryMarshal.Cast<byte, int>  (this); }
    [UnscopedRef] public Span<float> as_float() { return MemoryMarshal.Cast<byte, float>(this); }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x4)]
public unsafe struct AtelStackVar {
    [FieldOffset(0x0)] public int   as_int;
    [FieldOffset(0x0)] public float as_float;
}

public struct AtelStack {
    public const int MAX_SIZE = 20;

    public int                 size;
    public AtelStackValueArray values;
    public AtelStackTypeArray  types;

    public int pop_int() {
        if (size <= 0) throw new AtelStackOverflowException("Attempted to pop from an empty stack");
        int i = size - 1;

        AtelStackVar value = this[i].var;

        int x = types[i] switch {
            AtelStackType.F32 => (int)value.as_float,
            AtelStackType.I32 => value.as_int,
            _                 => 0
        };

        types[i] = AtelStackType.None;
        size -= 1;
        return x;
    }

    public float pop_float() {
        if (size <= 0) throw new AtelStackOverflowException("Attempted to pop from an empty stack");
        int i = size - 1;

        AtelStackVar value = this[i].var;

        float x = types[i] switch {
            AtelStackType.F32 => (int)value.as_float,
            AtelStackType.I32 => value.as_int,
            _                 => 0
        };

        types[i] = AtelStackType.None;
        size -= 1;
        return x;
    }

    public void push_int(int value) {
        if (size >= MAX_SIZE) throw new AtelStackOverflowException("Attempted to push onto a full stack");

        types[size] = AtelStackType.I32;
        values.as_int()[size] = value;
        size += 1;
    }

    public void push_float(float value) {
        if (size >= MAX_SIZE) throw new AtelStackOverflowException("Attempted to push onto a full stack");

        types[size] = AtelStackType.F32;
        values.as_float()[size] = value;
        size += 1;
    }

    private (AtelStackVar var, AtelStackType type) this[int i] {
        get {
            if (i < 0 || i >= size) throw new IndexOutOfRangeException("Stack index should be between 0 and the stack's size");
            AtelStackVar return_value = new();

            switch (types[i]) {
                case AtelStackType.I32:
                    return_value.as_int = values.as_int()[i];
                    return (return_value, AtelStackType.I32);
                case AtelStackType.F32:
                    return_value.as_float = values.as_float()[i];
                    return (return_value, AtelStackType.F32);
                default:
                    return_value.as_int = 0;
                    return (return_value, AtelStackType.None);
            }
        }
    }
}
