﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// ReSharper disable CheckNamespace
#if NETSTANDARD

#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

internal static class MethodImplOptionsExtended
{
    public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)0x0200;
}

#endif
