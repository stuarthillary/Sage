using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Highpoint.Sage.Mathematics;

/// <summary>
/// Operations for INumber Of T types.
///     System.Byte
///     System.Char
///     System.Decimal
///     System.Double
///     System.Half
///     System.Int128
///     System.Int16
///     System.Int32
///     System.Int64
///     System.IntPtr
///     System.Numerics.BigInteger
///     System.Runtime.InteropServices.NFloat
///     System.SByte
///     System.Single
///     System.UInt128
///     System.UInt16
///     System.UInt32
///     System.UInt64
///     System.UIntPtr
/// </summary>
/// <typeparam name="T"></typeparam>
public static class Operations<T> where T: INumber<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Add(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) + (byte)((object)right)));
        }
        if (typeof(T) == typeof(char))
        {
            return (T)((object)((char)((object)left) + (char)((object)right)));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) + (decimal)((object)right)));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) + (double)((object)right)));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) + (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) + (Int128)((object)right)));
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (T)((object)((Int16)((object)left) + (Int16)((object)right)));
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (T)((object)((Int32)((object)left) + (Int32)((object)right)));
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (T)((object)((Int64)((object)left) + (Int64)((object)right)));
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (T)((object)((IntPtr)((object)left) + (IntPtr)((object)right)));
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (T)((object)((BigInteger)((object)left) + (BigInteger)((object)right)));
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (T)((object)((NFloat)((object)left) + (NFloat)((object)right)));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) + (sbyte)((object)right)));
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (T)((object)((Single)((object)left) + (Single)((object)right)));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) + (UInt128)((object)right)));
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (T)((object)((UInt16)((object)left) + (UInt16)((object)right)));
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (T)((object)((UInt32)((object)left) + (UInt32)((object)right)));
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (T)((object)((UInt64)((object)left) + (UInt64)((object)right)));
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (T)((object)((UIntPtr)((object)left) + (UIntPtr)((object)right)));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Subtract(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) - (byte)((object)right)));
        }
        if (typeof(T) == typeof(char))
        {
            return (T)((object)((char)((object)left) - (char)((object)right)));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) - (decimal)((object)right)));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) - (double)((object)right)));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) - (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) - (Int128)((object)right)));
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (T)((object)((Int16)((object)left) - (Int16)((object)right)));
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (T)((object)((Int32)((object)left) - (Int32)((object)right)));
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (T)((object)((Int64)((object)left) - (Int64)((object)right)));
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (T)((object)((IntPtr)((object)left) - (IntPtr)((object)right)));
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (T)((object)((BigInteger)((object)left) - (BigInteger)((object)right)));
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (T)((object)((NFloat)((object)left) - (NFloat)((object)right)));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) - (sbyte)((object)right)));
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (T)((object)((Single)((object)left) - (Single)((object)right)));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) - (UInt128)((object)right)));
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (T)((object)((UInt16)((object)left) - (UInt16)((object)right)));
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (T)((object)((UInt32)((object)left) - (UInt32)((object)right)));
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (T)((object)((UInt64)((object)left) - (UInt64)((object)right)));
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (T)((object)((UIntPtr)((object)left) - (UIntPtr)((object)right)));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Multiply(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) * (byte)((object)right)));
        }
        if (typeof(T) == typeof(char))
        {
            return (T)((object)((char)((object)left) * (char)((object)right)));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) * (decimal)((object)right)));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) * (double)((object)right)));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) * (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) * (Int128)((object)right)));
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (T)((object)((Int16)((object)left) * (Int16)((object)right)));
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (T)((object)((Int32)((object)left) * (Int32)((object)right)));
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (T)((object)((Int64)((object)left) * (Int64)((object)right)));
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (T)((object)((IntPtr)((object)left) * (IntPtr)((object)right)));
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (T)((object)((BigInteger)((object)left) * (BigInteger)((object)right)));
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (T)((object)((NFloat)((object)left) * (NFloat)((object)right)));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) * (sbyte)((object)right)));
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (T)((object)((Single)((object)left) * (Single)((object)right)));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) * (UInt128)((object)right)));
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (T)((object)((UInt16)((object)left) * (UInt16)((object)right)));
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (T)((object)((UInt32)((object)left) * (UInt32)((object)right)));
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (T)((object)((UInt64)((object)left) * (UInt64)((object)right)));
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (T)((object)((UIntPtr)((object)left) * (UIntPtr)((object)right)));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Divide(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) / (byte)((object)right)));
        }
        if (typeof(T) == typeof(char))
        {
            return (T)((object)((char)((object)left) / (char)((object)right)));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) / (decimal)((object)right)));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) / (double)((object)right)));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) / (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) / (Int128)((object)right)));
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (T)((object)((Int16)((object)left) / (Int16)((object)right)));
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (T)((object)((Int32)((object)left) / (Int32)((object)right)));
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (T)((object)((Int64)((object)left) / (Int64)((object)right)));
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (T)((object)((IntPtr)((object)left) / (IntPtr)((object)right)));
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (T)((object)((BigInteger)((object)left) / (BigInteger)((object)right)));
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (T)((object)((NFloat)((object)left) / (NFloat)((object)right)));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) / (sbyte)((object)right)));
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (T)((object)((Single)((object)left) / (Single)((object)right)));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) / (UInt128)((object)right)));
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (T)((object)((UInt16)((object)left) / (UInt16)((object)right)));
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (T)((object)((UInt32)((object)left) / (UInt32)((object)right)));
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (T)((object)((UInt64)((object)left) / (UInt64)((object)right)));
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (T)((object)((UIntPtr)((object)left) / (UIntPtr)((object)right)));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqual(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (byte)((object)left) >= (byte)((object)right);
        }
        if (typeof(T) == typeof(char))
        {
            return (char)((object)left) >= (char)((object)right);
        }
        if (typeof(T) == typeof(decimal))
        {
            return (decimal)((object)left) >= (decimal)((object)right);
        }
        if (typeof(T) == typeof(double))
        {
            return (double)((object)left) >= (double)((object)right);
        }
        if (typeof(T) == typeof(Half))
        {
            return (Half)((object)left) >= (Half)((object)right);
        }
        if (typeof(T) == typeof(Int128))
        {
            return (Int128)((object)left) >= (Int128)((object)right);
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (Int16)((object)left) >= (Int16)((object)right);
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (Int32)((object)left) >= (Int32)((object)right);
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (Int64)((object)left) >= (Int64)((object)right);
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (IntPtr)((object)left) >= (IntPtr)((object)right);
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (BigInteger)((object)left) >= (BigInteger)((object)right);
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (NFloat)((object)left) >= (NFloat)((object)right);
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (sbyte)((object)left) >= (sbyte)((object)right);
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (Single)((object)left) >= (Single)((object)right);
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (UInt128)((object)left) >= (UInt128)((object)right);
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (UInt16)((object)left) >= (UInt16)((object)right);
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (UInt32)((object)left) >= (UInt32)((object)right);
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (UInt64)((object)left) >= (UInt64)((object)right);
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (UIntPtr)((object)left) >= (UIntPtr)((object)right);
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThan(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (byte)((object)left) > (byte)((object)right);
        }
        if (typeof(T) == typeof(char))
        {
            return (char)((object)left) > (char)((object)right);
        }
        if (typeof(T) == typeof(decimal))
        {
            return (decimal)((object)left) > (decimal)((object)right);
        }
        if (typeof(T) == typeof(double))
        {
            return (double)((object)left) > (double)((object)right);
        }
        if (typeof(T) == typeof(Half))
        {
            return (Half)((object)left) > (Half)((object)right);
        }
        if (typeof(T) == typeof(Int128))
        {
            return (Int128)((object)left) > (Int128)((object)right);
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (Int16)((object)left) > (Int16)((object)right);
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (Int32)((object)left) > (Int32)((object)right);
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (Int64)((object)left) > (Int64)((object)right);
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (IntPtr)((object)left) > (IntPtr)((object)right);
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (BigInteger)((object)left) > (BigInteger)((object)right);
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (NFloat)((object)left) > (NFloat)((object)right);
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (sbyte)((object)left) > (sbyte)((object)right);
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (Single)((object)left) > (Single)((object)right);
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (UInt128)((object)left) > (UInt128)((object)right);
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (UInt16)((object)left) > (UInt16)((object)right);
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (UInt32)((object)left) > (UInt32)((object)right);
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (UInt64)((object)left) > (UInt64)((object)right);
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (UIntPtr)((object)left) > (UIntPtr)((object)right);
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThan(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (byte)((object)left) < (byte)((object)right);
        }
        if (typeof(T) == typeof(char))
        {
            return (char)((object)left) < (char)((object)right);
        }
        if (typeof(T) == typeof(decimal))
        {
            return (decimal)((object)left) < (decimal)((object)right);
        }
        if (typeof(T) == typeof(double))
        {
            return (double)((object)left) < (double)((object)right);
        }
        if (typeof(T) == typeof(Half))
        {
            return (Half)((object)left) < (Half)((object)right);
        }
        if (typeof(T) == typeof(Int128))
        {
            return (Int128)((object)left) < (Int128)((object)right);
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (Int16)((object)left) < (Int16)((object)right);
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (Int32)((object)left) < (Int32)((object)right);
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (Int64)((object)left) < (Int64)((object)right);
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (IntPtr)((object)left) < (IntPtr)((object)right);
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (BigInteger)((object)left) < (BigInteger)((object)right);
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (NFloat)((object)left) < (NFloat)((object)right);
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (sbyte)((object)left) < (sbyte)((object)right);
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (Single)((object)left) < (Single)((object)right);
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (UInt128)((object)left) < (UInt128)((object)right);
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (UInt16)((object)left) < (UInt16)((object)right);
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (UInt32)((object)left) < (UInt32)((object)right);
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (UInt64)((object)left) < (UInt64)((object)right);
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (UIntPtr)((object)left) < (UIntPtr)((object)right);
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqual(T left, T right)
    {
        if (typeof(T) == typeof(byte))
        {
            return (byte)((object)left) <= (byte)((object)right);
        }
        if (typeof(T) == typeof(char))
        {
            return (char)((object)left) <= (char)((object)right);
        }
        if (typeof(T) == typeof(decimal))
        {
            return (decimal)((object)left) <= (decimal)((object)right);
        }
        if (typeof(T) == typeof(double))
        {
            return (double)((object)left) <= (double)((object)right);
        }
        if (typeof(T) == typeof(Half))
        {
            return (Half)((object)left) <= (Half)((object)right);
        }
        if (typeof(T) == typeof(Int128))
        {
            return (Int128)((object)left) <= (Int128)((object)right);
        }
        // short
        if (typeof(T) == typeof(Int16)) 
        {
            return (Int16)((object)left) <= (Int16)((object)right);
        }
        // int
        if (typeof(T) == typeof(Int32))
        {
            return (Int32)((object)left) <= (Int32)((object)right);
        }
        // long
        if (typeof(T) == typeof(Int64))
        {
            return (Int64)((object)left) <= (Int64)((object)right);
        }
        if (typeof(T) == typeof(IntPtr))
        {
            return (IntPtr)((object)left) <= (IntPtr)((object)right);
        }
        if (typeof(T) == typeof(BigInteger))
        {
            return (BigInteger)((object)left) <= (BigInteger)((object)right);
        }
        if (typeof(T) == typeof(NFloat))
        {
            return (NFloat)((object)left) <= (NFloat)((object)right);
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (sbyte)((object)left) <= (sbyte)((object)right);
        }
        //float
        if (typeof(T) == typeof(Single))
        {
            return (Single)((object)left) <= (Single)((object)right);
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (UInt128)((object)left) <= (UInt128)((object)right);
        }
        // ushort
        if (typeof(T) == typeof(UInt16))
        {
            return (UInt16)((object)left) <= (UInt16)((object)right);
        }
        // uint
        if (typeof(T) == typeof(UInt32))
        {
            return (UInt32)((object)left) <= (UInt32)((object)right);
        }
        // ulong
        if (typeof(T) == typeof(UInt64))
        {
            return (UInt64)((object)left) <= (UInt64)((object)right);
        }
        if (typeof(T) == typeof(UIntPtr))
        {
            return (UIntPtr)((object)left) <= (UIntPtr)((object)right);
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DivideByUInt32(T left, uint right)
    {
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) / right));
        }
        if (typeof(T) == typeof(float))
        {
            return (T)((object)((float)((object)left) / right));
        }
        if (typeof(T) == typeof(int))
        {
            return (T)((object)((int)((object)left) / right));
        }
        if (typeof(T) == typeof(long))
        {
            return (T)((object)((long)((object)left) / right));
        }
        if (typeof(T) == typeof(uint))
        {
            return (T)((object)((uint)((object)left) / right));
        }
        if (typeof(T) == typeof(ulong))
        {
            return (T)((object)((ulong)((object)left) / right));
        }
        if (typeof(T) == typeof(short))
        {
            return (T)((object)((short)((object)left) / right));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) / right));
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)((object)((ushort)((object)left) / right));
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) / right));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) / (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) / right));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) / right));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) / right));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MultiplyByUInt32(T left, uint right)
    {
        if (typeof(T) == typeof(double))
        {
            return (T)((object)((double)((object)left) * right));
        }
        if (typeof(T) == typeof(float))
        {
            return (T)((object)((float)((object)left) * right));
        }
        if (typeof(T) == typeof(int))
        {
            return (T)((object)((int)((object)left) * right));
        }
        if (typeof(T) == typeof(long))
        {
            return (T)((object)((long)((object)left) * right));
        }
        if (typeof(T) == typeof(uint))
        {
            return (T)((object)((uint)((object)left) * right));
        }
        if (typeof(T) == typeof(ulong))
        {
            return (T)((object)((ulong)((object)left) * right));
        }
        if (typeof(T) == typeof(short))
        {
            return (T)((object)((short)((object)left) * right));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)((object)((sbyte)((object)left) * right));
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)((object)((ushort)((object)left) * right));
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)((object)((byte)((object)left) * right));
        }
        if (typeof(T) == typeof(Half))
        {
            return (T)((object)((Half)((object)left) * (Half)((object)right)));
        }
        if (typeof(T) == typeof(Int128))
        {
            return (T)((object)((Int128)((object)left) * right));
        }
        if (typeof(T) == typeof(UInt128))
        {
            return (T)((object)((UInt128)((object)left) * right));
        }
        if (typeof(T) == typeof(decimal))
        {
            return (T)((object)((decimal)((object)left) * right));
        }
        throw new NotSupportedException($"'{typeof(T)}' is not a supported value type for operations.");
    }
}
