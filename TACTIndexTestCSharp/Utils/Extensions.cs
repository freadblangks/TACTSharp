﻿using System.Buffers.Binary;

static class Extensions
{
    public static int ReadInt16BE(this ReadOnlySpan<byte> source)
    {
        return BinaryPrimitives.ReadInt16BigEndian(source);
    }

    public static int ReadInt32BE(this ReadOnlySpan<byte> source)
    {
        return BinaryPrimitives.ReadInt32BigEndian(source);
    }

    public static long ReadInt40BE(this ReadOnlySpan<byte> source)
    {
        return source[4] | source[3] << 8 | source[2] << 16 | source[1] << 24 | source[0] << 32;
    }
}