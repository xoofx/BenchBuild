﻿// Extract from https://github.com/JonHanna/SpookilySharp/
// SpookyHash.cs
//
// Author:
//     Jon Hanna <jon@hackcraft.net>
//
// © 2014–2017 Jon Hanna
//
// Licensed under the MIT license. See the LICENSE file in the repository root for more details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;

namespace BuildServer.Tasks;

/// <summary>Provides an implementation of SpookyHash, either incrementally or (by static methods) in a single
/// operation.</summary>
internal struct SpookyHash
{
    private const ulong SpookyConst = 0xDEADBEEFDEADBEEF;
    private const int NumVars = 12;
    private const int BlockSize = NumVars * 8;
    private const int BufSize = 2 * BlockSize;

    public static unsafe void Hash128(string message, out ulong hash1, out ulong hash2)
    {
        hash1 = SpookyConst;
        hash2 = SpookyConst;
        fixed (void* pMessage = message)
        {
            Hash128(pMessage, message.Length, ref hash1, ref hash2);
        }
    }

    /// <summary>Calculates the 128-bit SpookyHash for a message.</summary>
    /// <param name="message">Pointer to the first element to hash.</param>
    /// <param name="length">The size, in bytes, of the elements to hash.</param>
    /// <param name="hash1">Takes as input a seed value, returns as first output half of the hash.</param>
    /// <param name="hash2">Takes as input a seed value, returns as second output half of the hash.</param>
    /// <remarks>This is not a CLS-compliant method, and is not accessible by some .NET languages.</remarks>
    /// <exception cref="AccessViolationException">This is an unsafe method. If you attempt to read past the buffer
    /// that <paramref name="message"/> points too, you may raise an <see cref="AccessViolationException"/>, or you
    /// may have incorrect results.</exception>
    public static unsafe void Hash128(void* message, int length, ref ulong hash1, ref ulong hash2)
    {
        if ((int)message == 0)
        {
            hash1 = 0;
            hash2 = 0;
            return;
        }

        if (length < BufSize)
        {
            Short(message, length, ref hash1, ref hash2);
            return;
        }

        ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;

        h0 = h3 = h6 = h9 = hash1;
        h1 = h4 = h7 = h10 = hash2;
        h2 = h5 = h8 = h11 = SpookyConst;

        ulong* p64 = (ulong*)message;

        ulong* end = p64 + length / BlockSize * NumVars;
        ulong* buf = stackalloc ulong[NumVars];
        if (((long)message & 7) == 0)
        {
            while (p64 < end)
            {
                h0 += p64[0];
                h2 ^= h10;
                h11 ^= h0;
                h0 = h0 << 11 | h0 >> -11;
                h11 += h1;
                h1 += p64[1];
                h3 ^= h11;
                h0 ^= h1;
                h1 = h1 << 32 | h1 >> 32;
                h0 += h2;
                h2 += p64[2];
                h4 ^= h0;
                h1 ^= h2;
                h2 = h2 << 43 | h2 >> -43;
                h1 += h3;
                h3 += p64[3];
                h5 ^= h1;
                h2 ^= h3;
                h3 = h3 << 31 | h3 >> -31;
                h2 += h4;
                h4 += p64[4];
                h6 ^= h2;
                h3 ^= h4;
                h4 = h4 << 17 | h4 >> -17;
                h3 += h5;
                h5 += p64[5];
                h7 ^= h3;
                h4 ^= h5;
                h5 = h5 << 28 | h5 >> -28;
                h4 += h6;
                h6 += p64[6];
                h8 ^= h4;
                h5 ^= h6;
                h6 = h6 << 39 | h6 >> -39;
                h5 += h7;
                h7 += p64[7];
                h9 ^= h5;
                h6 ^= h7;
                h7 = h7 << 57 | h7 >> -57;
                h6 += h8;
                h8 += p64[8];
                h10 ^= h6;
                h7 ^= h8;
                h8 = h8 << 55 | h8 >> -55;
                h7 += h9;
                h9 += p64[9];
                h11 ^= h7;
                h8 ^= h9;
                h9 = h9 << 54 | h9 >> -54;
                h8 += h10;
                h10 += p64[10];
                h0 ^= h8;
                h9 ^= h10;
                h10 = h10 << 22 | h10 >> -22;
                h9 += h11;
                h11 += p64[11];
                h1 ^= h9;
                h10 ^= h11;
                h11 = h11 << 46 | h11 >> -46;
                h10 += h0;
                p64 += NumVars;
            }
        }
        else
        {
            while (p64 < end)
            {
                MemoryCopy(buf, p64, BlockSize);

                h0 += buf[0];
                h2 ^= h10;
                h11 ^= h0;
                h0 = h0 << 11 | h0 >> -11;
                h11 += h1;
                h1 += buf[1];
                h3 ^= h11;
                h0 ^= h1;
                h1 = h1 << 32 | h1 >> 32;
                h0 += h2;
                h2 += buf[2];
                h4 ^= h0;
                h1 ^= h2;
                h2 = h2 << 43 | h2 >> -43;
                h1 += h3;
                h3 += buf[3];
                h5 ^= h1;
                h2 ^= h3;
                h3 = h3 << 31 | h3 >> -31;
                h2 += h4;
                h4 += buf[4];
                h6 ^= h2;
                h3 ^= h4;
                h4 = h4 << 17 | h4 >> -17;
                h3 += h5;
                h5 += buf[5];
                h7 ^= h3;
                h4 ^= h5;
                h5 = h5 << 28 | h5 >> -28;
                h4 += h6;
                h6 += buf[6];
                h8 ^= h4;
                h5 ^= h6;
                h6 = h6 << 39 | h6 >> -39;
                h5 += h7;
                h7 += buf[7];
                h9 ^= h5;
                h6 ^= h7;
                h7 = h7 << 57 | h7 >> -57;
                h6 += h8;
                h8 += buf[8];
                h10 ^= h6;
                h7 ^= h8;
                h8 = h8 << 55 | h8 >> -55;
                h7 += h9;
                h9 += buf[9];
                h11 ^= h7;
                h8 ^= h9;
                h9 = h9 << 54 | h9 >> -54;
                h8 += h10;
                h10 += buf[10];
                h0 ^= h8;
                h9 ^= h10;
                h10 = h10 << 22 | h10 >> -22;
                h9 += h11;
                h11 += buf[11];
                h1 ^= h9;
                h10 ^= h11;
                h11 = h11 << 46 | h11 >> -46;
                h10 += h0;
                p64 += NumVars;
            }
        }

        int remainder = length - (int)((byte*)end - (byte*)message);
        if (remainder != 0)
        {
            MemoryCopy(buf, end, remainder);
        }

        MemoryZero((byte*)buf + remainder, BlockSize - remainder);
        ((byte*)buf)[BlockSize - 1] = (byte)remainder;

        h0 += buf[0];
        h1 += buf[1];
        h2 += buf[2];
        h3 += buf[3];
        h4 += buf[4];
        h5 += buf[5];
        h6 += buf[6];
        h7 += buf[7];
        h8 += buf[8];
        h9 += buf[9];
        h10 += buf[10];
        h11 += buf[11];
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h11 = h11 << 42 | h11 >> -42;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h11 = h11 << 42 | h11 >> -42;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        hash2 = h1;
        hash1 = h0;
    }

    [SecurityCritical]
    private static unsafe void Short(void* message, int length, ref ulong hash1, ref ulong hash2)
    {
        if (length != 0 && ((long)message & 7) != 0)
        {
            ulong* buf = stackalloc ulong[2 * NumVars];
            MemoryCopy(buf, message, length);
            message = buf;
        }

        ulong* p64 = (ulong*)message;

        int remainder = length & 31;
        ulong a = hash1;
        ulong b = hash2;
        ulong c = SpookyConst;
        ulong d = SpookyConst;

        if (length > 15)
        {
            ulong* end = p64 + length / 32 * 4;
            for (; p64 < end; p64 += 4)
            {
                c += p64[0];
                d += p64[1];
                c = c << 50 | c >> -50;
                c += d;
                a ^= c;
                d = d << 52 | d >> -52;
                d += a;
                b ^= d;
                a = a << 30 | a >> -30;
                a += b;
                c ^= a;
                b = b << 41 | b >> -41;
                b += c;
                d ^= b;
                c = c << 54 | c >> -54;
                c += d;
                a ^= c;
                d = d << 48 | d >> -48;
                d += a;
                b ^= d;
                a = a << 38 | a >> -38;
                a += b;
                c ^= a;
                b = b << 37 | b >> -37;
                b += c;
                d ^= b;
                c = c << 62 | c >> -62;
                c += d;
                a ^= c;
                d = d << 34 | d >> -34;
                d += a;
                b ^= d;
                a = a << 5 | a >> -5;
                a += b;
                c ^= a;
                b = b << 36 | b >> -36;
                b += c;
                d ^= b;
                a += p64[2];
                b += p64[3];
            }

            if (remainder >= 16)
            {
                c += p64[0];
                d += p64[1];
                c = c << 50 | c >> -50;
                c += d;
                a ^= c;
                d = d << 52 | d >> -52;
                d += a;
                b ^= d;
                a = a << 30 | a >> -30;
                a += b;
                c ^= a;
                b = b << 41 | b >> -41;
                b += c;
                d ^= b;
                c = c << 54 | c >> -54;
                c += d;
                a ^= c;
                d = d << 48 | d >> -48;
                d += a;
                b ^= d;
                a = a << 38 | a >> -38;
                a += b;
                c ^= a;
                b = b << 37 | b >> -37;
                b += c;
                d ^= b;
                c = c << 62 | c >> -62;
                c += d;
                a ^= c;
                d = d << 34 | d >> -34;
                d += a;
                b ^= d;
                a = a << 5 | a >> -5;
                a += b;
                c ^= a;
                b = b << 36 | b >> -36;
                b += c;
                d ^= b;
                p64 += 2;
                remainder -= 16;
            }
        }

        d += (ulong)length << 56;
        switch (remainder)
        {
            case 15:
                d += (ulong)((byte*)p64)[14] << 48;
                goto case 14;
            case 14:
                d += (ulong)((byte*)p64)[13] << 40;
                goto case 13;
            case 13:
                d += (ulong)((byte*)p64)[12] << 32;
                goto case 12;
            case 12:
                d += ((uint*)p64)[2];
                c += p64[0];
                break;
            case 11:
                d += (ulong)((byte*)p64)[10] << 16;
                goto case 10;
            case 10:
                d += (ulong)((byte*)p64)[9] << 8;
                goto case 9;
            case 9:
                d += ((byte*)p64)[8];
                goto case 8;
            case 8:
                c += p64[0];
                break;
            case 7:
                c += (ulong)((byte*)p64)[6] << 48;
                goto case 6;
            case 6:
                c += (ulong)((byte*)p64)[5] << 40;
                goto case 5;
            case 5:
                c += (ulong)((byte*)p64)[4] << 32;
                goto case 4;
            case 4:
                c += ((uint*)p64)[0];
                break;
            case 3:
                c += (ulong)((byte*)p64)[2] << 16;
                goto case 2;
            case 2:
                c += (ulong)((byte*)p64)[1] << 8;
                goto case 1;
            case 1:
                c += ((byte*)p64)[0];
                break;
            case 0:
                c += SpookyConst;
                d += SpookyConst;
                break;
        }

        d ^= c;
        c = c << 15 | c >> -15;
        d += c;
        a ^= d;
        d = d << 52 | d >> -52;
        a += d;
        b ^= a;
        a = a << 26 | a >> -26;
        b += a;
        c ^= b;
        b = b << 51 | b >> -51;
        c += b;
        d ^= c;
        c = c << 28 | c >> -28;
        d += c;
        a ^= d;
        d = d << 9 | d >> -9;
        a += d;
        b ^= a;
        a = a << 47 | a >> -47;
        b += a;
        c ^= b;
        b = b << 54 | b >> -54;
        c += b;
        d ^= c;
        c = c << 32 | c >> -32;
        d += c;
        a ^= d;
        d = d << 25 | d >> -25;
        a += d;
        b ^= a;
        a = a << 63 | a >> -63;
        b += a;
        hash2 = b;
        hash1 = a;
    }

    private unsafe fixed ulong _data[2 * NumVars];

    private ulong _state0,
        _state1,
        _state2,
        _state3,
        _state4,
        _state5,
        _state6,
        _state7,
        _state8,
        _state9,
        _state10,
        _state11;

    private int _length;
    private int _remainder;

    public void Init()
    {
        Init(SpookyConst, SpookyConst);
    }

    /// <summary>Re-initialise the <see cref="SpookyHash"/> object with the specified seed.</summary>
    /// <param name="seed1">First half of a 128-bit seed for the hash.</param>
    /// <param name="seed2">Second half of a 128-bit seed for the hash.</param>
    /// <remarks>This is not a CLS-compliant method, and is not accessible by some .NET languages.</remarks>
    public void Init(ulong seed1, ulong seed2)
    {
        _length = _remainder = 0;
        _state0 = seed1;
        _state1 = seed2;
    }

    /// <summary>Re-initialise the <see cref="SpookyHash"/> object with the specified seed.</summary>
    /// <param name="seed1">First half of a 128-bit seed for the hash.</param>
    /// <param name="seed2">Second half of a 128-bit seed for the hash.</param>
    public void Init(long seed1, long seed2) => Init((ulong)seed1, (ulong)seed2);

    /// <summary>Updates the in-progress hash generation with more of the message.</summary>
    /// <param name="message">String to hash.</param>
    /// <param name="startIndex">Start index in the string, from which to hash.</param>
    /// <param name="length">How many characters to hash.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> was null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero, or greater
    /// than the length of the array.</exception>
    /// <exception cref="ArgumentException"><paramref name="startIndex"/> plus <paramref name="length"/> is greater
    /// than the length of the array.</exception>
    [SecuritySafeCritical]
    public unsafe void Update(string message, int startIndex, int length)
    {
        ExceptionHelper.CheckString(message, startIndex, length);
        fixed (char* ptr = message)
        {
            Update(ptr + startIndex, length << 1);
        }
    }

    /// <summary>Updates the in-progress hash generation with more of the message.</summary>
    /// <param name="message">String to hash.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> was null.</exception>
    public void Update(string message)
    {
        ExceptionHelper.CheckMessageNotNull(message);
        Update(message, 0, message.Length);
    }

    /// <summary>Updates the in-progress hash generation with each <see cref="string"/> in an sequence of strings.
    /// </summary>
    /// <param name="message">The sequence of <see cref="string"/>s to hash.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> was null.</exception>
    /// <remarks>It is acceptable for strings within the sequence to be <see langword="null"/>. They will affect the
    /// hash produced. i.e. the sequence <c>{"a", "b", "c"}</c> will produce a different hash than
    /// <c>{"a", null, "b", "c"}</c>. This is often useful, but if this is undesirable in a given case (you want the
    /// same hash as the concatenation), then filter out <see langword="null"/> <see cref="string"/>s first.
    /// </remarks>
    [SecuritySafeCritical]
    public unsafe void Update(IEnumerable<string> message)
    {
        ExceptionHelper.CheckMessageNotNull(message);
        foreach (string item in message)
        {
            if (item == null)
            {
                Update(SpookyConst); // Just to make sure we produce a different hash for this case.
            }
            else
            {
                fixed (char* ptr = item)
                {
                    Update(ptr, item.Length << 1);
                }
            }
        }
    }

    public unsafe void Update(ulong message)
    {
        if ((_remainder & (sizeof(ulong) - 1)) == 0 && _remainder + sizeof(ulong) < BufSize)
        {
            fixed (ulong* uptr = _data)
                *(ulong*)((byte*)uptr + _remainder) = message;
            _length += sizeof(ulong);
            _remainder += sizeof(ulong);
        }
        else
        {
            Update(&message, sizeof(ulong));
        }
    }

    public unsafe void Update(Span<byte> buffer)
    {
        fixed (void* pBuffer = buffer)
        {
            Update(pBuffer, buffer.Length);
        }
    }

    /// <summary>Updates the in-progress hash generation with more of the message.</summary>
    /// <param name="message">Pointer to the data to hash.</param>
    /// <param name="length">How many bytes to hash.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> was a null pointer.</exception>
    /// <exception cref="AccessViolationException">This is an unsafe method. If you attempt to read past the buffer
    /// that <paramref name="message"/> points too, you may raise an <see cref="AccessViolationException"/>, or you
    /// may have incorrect results.</exception>
    public unsafe void Update(void* message, int length)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (length == 0)
        {
            return;
        }

        ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;
        int newLength = length + _remainder;
        if (newLength < BufSize)
        {
            fixed (ulong* uptr = _data)
            {
                MemoryCopy((byte*)uptr + _remainder, message, length);
            }

            _length = length + _length;
            _remainder = newLength;
            return;
        }

        if (_length < BufSize)
        {
            h0 = h3 = h6 = h9 = _state0;
            h1 = h4 = h7 = h10 = _state1;
            h2 = h5 = h8 = h11 = SpookyConst;
        }
        else
        {
            h0 = _state0;
            h1 = _state1;
            h2 = _state2;
            h3 = _state3;
            h4 = _state4;
            h5 = _state5;
            h6 = _state6;
            h7 = _state7;
            h8 = _state8;
            h9 = _state9;
            h10 = _state10;
            h11 = _state11;
        }

        _length += length;

        fixed (ulong* p64Fixed = _data)
        {
            ulong* p64;
            if (_remainder != 0)
            {
                int prefix = BufSize - _remainder;
                MemoryCopy((byte*)p64Fixed + _remainder, message, prefix);

                h0 += p64Fixed[0];
                h2 ^= h10;
                h11 ^= h0;
                h0 = h0 << 11 | h0 >> -11;
                h11 += h1;
                h1 += p64Fixed[1];
                h3 ^= h11;
                h0 ^= h1;
                h1 = h1 << 32 | h1 >> -32;
                h0 += h2;
                h2 += p64Fixed[2];
                h4 ^= h0;
                h1 ^= h2;
                h2 = h2 << 43 | h2 >> -43;
                h1 += h3;
                h3 += p64Fixed[3];
                h5 ^= h1;
                h2 ^= h3;
                h3 = h3 << 31 | h3 >> -31;
                h2 += h4;
                h4 += p64Fixed[4];
                h6 ^= h2;
                h3 ^= h4;
                h4 = h4 << 17 | h4 >> -17;
                h3 += h5;
                h5 += p64Fixed[5];
                h7 ^= h3;
                h4 ^= h5;
                h5 = h5 << 28 | h5 >> -28;
                h4 += h6;
                h6 += p64Fixed[6];
                h8 ^= h4;
                h5 ^= h6;
                h6 = h6 << 39 | h6 >> -39;
                h5 += h7;
                h7 += p64Fixed[7];
                h9 ^= h5;
                h6 ^= h7;
                h7 = h7 << 57 | h7 >> -57;
                h6 += h8;
                h8 += p64Fixed[8];
                h10 ^= h6;
                h7 ^= h8;
                h8 = h8 << 55 | h8 >> -55;
                h7 += h9;
                h9 += p64Fixed[9];
                h11 ^= h7;
                h8 ^= h9;
                h9 = h9 << 54 | h9 >> -54;
                h8 += h10;
                h10 += p64Fixed[10];
                h0 ^= h8;
                h9 ^= h10;
                h10 = h10 << 22 | h10 >> -22;
                h9 += h11;
                h11 += p64Fixed[11];
                h1 ^= h9;
                h10 ^= h11;
                h11 = h11 << 46 | h11 >> -46;
                h10 += h0;
                p64 = p64Fixed + NumVars;
                h0 += p64[0];
                h2 ^= h10;
                h11 ^= h0;
                h0 = h0 << 11 | h0 >> -11;
                h11 += h1;
                h1 += p64[1];
                h3 ^= h11;
                h0 ^= h1;
                h1 = h1 << 32 | h1 >> -32;
                h0 += h2;
                h2 += p64[2];
                h4 ^= h0;
                h1 ^= h2;
                h2 = h2 << 43 | h2 >> -43;
                h1 += h3;
                h3 += p64[3];
                h5 ^= h1;
                h2 ^= h3;
                h3 = h3 << 31 | h3 >> -31;
                h2 += h4;
                h4 += p64[4];
                h6 ^= h2;
                h3 ^= h4;
                h4 = h4 << 17 | h4 >> -17;
                h3 += h5;
                h5 += p64[5];
                h7 ^= h3;
                h4 ^= h5;
                h5 = h5 << 28 | h5 >> -28;
                h4 += h6;
                h6 += p64[6];
                h8 ^= h4;
                h5 ^= h6;
                h6 = h6 << 39 | h6 >> -39;
                h5 += h7;
                h7 += p64[7];
                h9 ^= h5;
                h6 ^= h7;
                h7 = h7 << 57 | h7 >> -57;
                h6 += h8;
                h8 += p64[8];
                h10 ^= h6;
                h7 ^= h8;
                h8 = h8 << 55 | h8 >> -55;
                h7 += h9;
                h9 += p64[9];
                h11 ^= h7;
                h8 ^= h9;
                h9 = h9 << 54 | h9 >> -54;
                h8 += h10;
                h10 += p64[10];
                h0 ^= h8;
                h9 ^= h10;
                h10 = h10 << 22 | h10 >> -22;
                h9 += h11;
                h11 += p64[11];
                h1 ^= h9;
                h10 ^= h11;
                h11 = h11 << 46 | h11 >> -46;
                h10 += h0;
                p64 = (ulong*)((byte*)message + prefix);
                length -= prefix;
            }
            else
            {
                p64 = (ulong*)message;
            }

            ulong* end = p64 + length / BlockSize * NumVars;
            byte remainder = (byte)(length - ((byte*)end - (byte*)p64));
            if (((long)message & 7) == 0)
            {
                while (p64 < end)
                {
                    h0 += p64[0];
                    h2 ^= h10;
                    h11 ^= h0;
                    h0 = h0 << 11 | h0 >> -11;
                    h11 += h1;
                    h1 += p64[1];
                    h3 ^= h11;
                    h0 ^= h1;
                    h1 = h1 << 32 | h1 >> -32;
                    h0 += h2;
                    h2 += p64[2];
                    h4 ^= h0;
                    h1 ^= h2;
                    h2 = h2 << 43 | h2 >> -43;
                    h1 += h3;
                    h3 += p64[3];
                    h5 ^= h1;
                    h2 ^= h3;
                    h3 = h3 << 31 | h3 >> -31;
                    h2 += h4;
                    h4 += p64[4];
                    h6 ^= h2;
                    h3 ^= h4;
                    h4 = h4 << 17 | h4 >> -17;
                    h3 += h5;
                    h5 += p64[5];
                    h7 ^= h3;
                    h4 ^= h5;
                    h5 = h5 << 28 | h5 >> -28;
                    h4 += h6;
                    h6 += p64[6];
                    h8 ^= h4;
                    h5 ^= h6;
                    h6 = h6 << 39 | h6 >> -39;
                    h5 += h7;
                    h7 += p64[7];
                    h9 ^= h5;
                    h6 ^= h7;
                    h7 = h7 << 57 | h7 >> -57;
                    h6 += h8;
                    h8 += p64[8];
                    h10 ^= h6;
                    h7 ^= h8;
                    h8 = h8 << 55 | h8 >> -55;
                    h7 += h9;
                    h9 += p64[9];
                    h11 ^= h7;
                    h8 ^= h9;
                    h9 = h9 << 54 | h9 >> -54;
                    h8 += h10;
                    h10 += p64[10];
                    h0 ^= h8;
                    h9 ^= h10;
                    h10 = h10 << 22 | h10 >> -22;
                    h9 += h11;
                    h11 += p64[11];
                    h1 ^= h9;
                    h10 ^= h11;
                    h11 = h11 << 46 | h11 >> -46;
                    h10 += h0;
                    p64 += NumVars;
                }
            }
            else
            {
                fixed (ulong* dataPtr = _data)
                {
                    while (p64 < end)
                    {
                        MemoryCopy(dataPtr, p64, BlockSize);
                        h0 += _data[0];
                        h2 ^= h10;
                        h11 ^= h0;
                        h0 = h0 << 11 | h0 >> -11;
                        h11 += h1;
                        h1 += _data[1];
                        h3 ^= h11;
                        h0 ^= h1;
                        h1 = h1 << 32 | h1 >> -32;
                        h0 += h2;
                        h2 += _data[2];
                        h4 ^= h0;
                        h1 ^= h2;
                        h2 = h2 << 43 | h2 >> -43;
                        h1 += h3;
                        h3 += _data[3];
                        h5 ^= h1;
                        h2 ^= h3;
                        h3 = h3 << 31 | h3 >> -31;
                        h2 += h4;
                        h4 += _data[4];
                        h6 ^= h2;
                        h3 ^= h4;
                        h4 = h4 << 17 | h4 >> -17;
                        h3 += h5;
                        h5 += _data[5];
                        h7 ^= h3;
                        h4 ^= h5;
                        h5 = h5 << 28 | h5 >> -28;
                        h4 += h6;
                        h6 += _data[6];
                        h8 ^= h4;
                        h5 ^= h6;
                        h6 = h6 << 39 | h6 >> -39;
                        h5 += h7;
                        h7 += _data[7];
                        h9 ^= h5;
                        h6 ^= h7;
                        h7 = h7 << 57 | h7 >> -57;
                        h6 += h8;
                        h8 += _data[8];
                        h10 ^= h6;
                        h7 ^= h8;
                        h8 = h8 << 55 | h8 >> -55;
                        h7 += h9;
                        h9 += _data[9];
                        h11 ^= h7;
                        h8 ^= h9;
                        h9 = h9 << 54 | h9 >> -54;
                        h8 += h10;
                        h10 += _data[10];
                        h0 ^= h8;
                        h9 ^= h10;
                        h10 = h10 << 22 | h10 >> -22;
                        h9 += h11;
                        h11 += _data[11];
                        h1 ^= h9;
                        h10 ^= h11;
                        h11 = h11 << 46 | h11 >> -46;
                        h10 += h0;
                        p64 += NumVars;
                    }
                }
            }

            _remainder = remainder;
            if (remainder != 0)
            {
                MemoryCopy(p64Fixed, end, remainder);
            }
        }

        _state0 = h0;
        _state1 = h1;
        _state2 = h2;
        _state3 = h3;
        _state4 = h4;
        _state5 = h5;
        _state6 = h6;
        _state7 = h7;
        _state8 = h8;
        _state9 = h9;
        _state10 = h10;
        _state11 = h11;
    }

    /// <summary>Produces the final hash of the message. It does not prevent further updates, and can be called
    /// multiple times while the hash is added to.</summary>
    /// <param name="hash1">The first half of the 128-bit hash.</param>
    /// <param name="hash2">The second half of the 128-bit hash.</param>
    public void Final(out long hash1, out long hash2)
    {
        Final(out ulong uhash1, out ulong uhash2);
        hash1 = (long)uhash1;
        hash2 = (long)uhash2;
    }

    private static ReadOnlySpan<byte> HexChars => new(new byte[16]
    {
        (byte)'0',
        (byte)'1',
        (byte)'2',
        (byte)'3',
        (byte)'4',
        (byte)'5',
        (byte)'6',
        (byte)'7',
        (byte)'8',
        (byte)'9',
        (byte)'a',
        (byte)'b',
        (byte)'c',
        (byte)'d',
        (byte)'e',
        (byte)'f',
    });

    public unsafe string FinalToHex()
    {
        // Fast to hex avoiding allocations until the final string
        Final(out ulong uhash1, out ulong uhash2);
        var hash = stackalloc char[32];
        int index = 0;
        for (int i = 0; i < 8; i++)
        {
            hash[index++] = (char)HexChars[(int)(uhash1 & 0xF)];
            uhash1 >>= 4;
            hash[index++] = (char)HexChars[(int)(uhash1 & 0xF)];
            uhash1 >>= 4;
        }
        for (int i = 0; i < 8; i++)
        {
            hash[index++] = (char)HexChars[(int)(uhash2 & 0xF)];
            uhash2 >>= 4;
            hash[index++] = (char)HexChars[(int)(uhash2 & 0xF)];
            uhash2 >>= 4;
        }
        return new string(hash, 0, 32);
    }

    /// <summary>Produces the final hash of the message. It does not prevent further updates, and can be called
    /// multiple times while the hash is added to.</summary>
    /// <param name="hash1">The first half of the 128-bit hash.</param>
    /// <param name="hash2">The second half of the 128-bit hash.</param>
    /// <remarks>This is not a CLS-compliant method, and is not accessible by some .NET languages.</remarks>
    [CLSCompliant(false)]
    [SecuritySafeCritical]
    public unsafe void Final(out ulong hash1, out ulong hash2)
    {
        if (_length < BufSize)
        {
            hash1 = _state0;
            hash2 = _state1;
            fixed (void* ptr = _data)
            {
                Short(ptr, _length, ref hash1, ref hash2);
            }

            return;
        }

        ulong h0 = _state0;
        ulong h1 = _state1;
        ulong h2 = _state2;
        ulong h3 = _state3;
        ulong h4 = _state4;
        ulong h5 = _state5;
        ulong h6 = _state6;
        ulong h7 = _state7;
        ulong h8 = _state8;
        ulong h9 = _state9;
        ulong h10 = _state10;
        ulong h11 = _state11;
        fixed (ulong* dataFixed = _data)
        {
            ulong* data = dataFixed;
            int remainder = _remainder;
            if (remainder >= BlockSize)
            {
                h0 += data[0];
                h2 ^= h10;
                h11 ^= h0;
                h0 = h0 << 11 | h0 >> -11;
                h11 += h1;
                h1 += data[1];
                h3 ^= h11;
                h0 ^= h1;
                h1 = h1 << 32 | h1 >> -32;
                h0 += h2;
                h2 += data[2];
                h4 ^= h0;
                h1 ^= h2;
                h2 = h2 << 43 | h2 >> -43;
                h1 += h3;
                h3 += data[3];
                h5 ^= h1;
                h2 ^= h3;
                h3 = h3 << 31 | h3 >> -31;
                h2 += h4;
                h4 += data[4];
                h6 ^= h2;
                h3 ^= h4;
                h4 = h4 << 17 | h4 >> -17;
                h3 += h5;
                h5 += data[5];
                h7 ^= h3;
                h4 ^= h5;
                h5 = h5 << 28 | h5 >> -28;
                h4 += h6;
                h6 += data[6];
                h8 ^= h4;
                h5 ^= h6;
                h6 = h6 << 39 | h6 >> -39;
                h5 += h7;
                h7 += data[7];
                h9 ^= h5;
                h6 ^= h7;
                h7 = h7 << 57 | h7 >> -57;
                h6 += h8;
                h8 += data[8];
                h10 ^= h6;
                h7 ^= h8;
                h8 = h8 << 55 | h8 >> -55;
                h7 += h9;
                h9 += data[9];
                h11 ^= h7;
                h8 ^= h9;
                h9 = h9 << 54 | h9 >> -54;
                h8 += h10;
                h10 += data[10];
                h0 ^= h8;
                h9 ^= h10;
                h10 = h10 << 22 | h10 >> -22;
                h9 += h11;
                h11 += data[11];
                h1 ^= h9;
                h10 ^= h11;
                h11 = h11 << 46 | h11 >> -46;
                h10 += h0;
                data += NumVars;
                remainder = remainder - BlockSize;
            }
            MemoryZero((byte*)data + remainder, BlockSize - remainder);
            *((byte*)data + BlockSize - 1) = (byte)remainder;
            h0 += data[0];
            h1 += data[1];
            h2 += data[2];
            h3 += data[3];
            h4 += data[4];
            h5 += data[5];
            h6 += data[6];
            h7 += data[7];
            h8 += data[8];
            h9 += data[9];
            h10 += data[10];
            h11 += data[11];
        }
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h11 = h11 << 42 | h11 >> -42;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h11 = h11 << 42 | h11 >> -42;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        h11 += h1;
        h2 ^= h11;
        h1 = h1 << 44 | h1 >> -44;
        h0 += h2;
        h3 ^= h0;
        h2 = h2 << 15 | h2 >> -15;
        h1 += h3;
        h4 ^= h1;
        h3 = h3 << 34 | h3 >> -34;
        h2 += h4;
        h5 ^= h2;
        h4 = h4 << 21 | h4 >> -21;
        h3 += h5;
        h6 ^= h3;
        h5 = h5 << 38 | h5 >> -38;
        h4 += h6;
        h7 ^= h4;
        h6 = h6 << 33 | h6 >> -33;
        h5 += h7;
        h8 ^= h5;
        h7 = h7 << 10 | h7 >> -10;
        h6 += h8;
        h9 ^= h6;
        h8 = h8 << 13 | h8 >> -13;
        h7 += h9;
        h10 ^= h7;
        h9 = h9 << 38 | h9 >> -38;
        h8 += h10;
        h11 ^= h8;
        h10 = h10 << 53 | h10 >> -53;
        h9 += h11;
        h0 ^= h9;
        h10 += h0;
        h1 ^= h10;
        h0 = h0 << 54 | h0 >> -54;
        hash2 = h1;
        hash1 = h0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void MemoryCopy(void* dest, void* src, int length)
    {
        Unsafe.CopyBlock(dest, src, (uint)length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void MemoryZero(void* dest, int length)
    {
        Unsafe.InitBlockUnaligned(dest, 0, (uint)length);
    }

    private static class ExceptionHelper
    {
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void CheckNotNull(object arg, string name)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void CheckNotNullString(string arg) => CheckNotNull(arg, "s");

        public static Exception BadHashCode128Format() =>
            new FormatException("The string did not contain a 32-digit hexadecimal number.");

        private static Exception StartIndexOutOfRange() => new ArgumentOutOfRangeException("startIndex");

        private static Exception NegativeLength() => new ArgumentOutOfRangeException("length");

        private static Exception PastArrayBounds() => new ArgumentException("Attempt to read beyond the end of the array.");

        private static Exception PastStringBounds() => new ArgumentException("Attempt to read beyond the end of the string.");

        private static void CheckNotNegativeLength(int length)
        {
            if (length < 0)
            {
                throw NegativeLength();
            }
        }

        private static void CheckIndexInRange(int startIndex, int length)
        {
            if ((uint)startIndex >= (uint)length)
            {
                throw StartIndexOutOfRange();
            }
        }

        public static void CheckArray<T>(T[] message, int startIndex, int length)
        {
            CheckNotNegativeLength(length);
            int len = message.Length;
            CheckIndexInRange(startIndex, len);
            if (startIndex + length > len)
            {
                throw PastArrayBounds();
            }
        }

        public static void CheckArrayIncNull<T>(T[] message, int startIndex, int length)
        {
            CheckMessageNotNull(message);
            CheckArray(message, startIndex, length);
        }

        public static void CheckBounds(string message, int startIndex, int length)
        {
            CheckNotNegativeLength(length);
            int len = message.Length;
            CheckIndexInRange(startIndex, len);
            if (startIndex + length > len)
            {
                throw PastStringBounds();
            }
        }

        public static void CheckString(string message, int startIndex, int length)
        {
            CheckMessageNotNull(message);
            CheckBounds(message, startIndex, length);
        }

        public static void CheckMessageNotNull(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
        }

        public static void CheckNotNull(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
        }
    }
}
