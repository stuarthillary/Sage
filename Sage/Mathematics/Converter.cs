using System;
using System.Runtime.CompilerServices;

namespace Highpoint.Sage.Mathematics;

// Inspired by https://www.singulink.com/codeindex/post/generic-math-at-raw-operator-speed
public static class Converter<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(T value)
    {
        if (typeof(T) == typeof(double))
        {
            return (int)((double)((object)value));
        }
        if (typeof(T) == typeof(float))
        {
            return (int)((float)((object)value));
        }
        if (typeof(T) == typeof(int))
        {
            return (int)((object)value);
        }
        if (typeof(T) == typeof(long))
        {
            return (int)((long)((object)value));
        }
        if (typeof(T) == typeof(uint))
        {
            return (int)((uint)((object)value));
        }
        if (typeof(T) == typeof(ulong))
        {
            return (int)((ulong)((object)value));
        }
        if (typeof(T) == typeof(short))
        {
            return (int)((short)((object)value));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (int)((sbyte)((object)value));
        }
        if (typeof(T) == typeof(ushort))
        {
            return (int)((ushort)((object)value));
        }
        if (typeof(T) == typeof(byte))
        {
            return (int)((byte)((object)value));
        }
        if (typeof(T) == typeof(Half))
        {
            return (int)((Half)((object)value));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (int)((Int128)((object)value));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (int)((UInt128)((object)value));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (int)((decimal)((object)value));
        }
        if (typeof(T) == typeof(TimeSpan))
        {
            return (int)((TimeSpan)((object)value)).Ticks;
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
}