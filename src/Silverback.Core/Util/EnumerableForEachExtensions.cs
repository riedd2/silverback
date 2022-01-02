﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableForEachExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T element in source)
        {
            action(element);
        }
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int index = 0;
        foreach (T element in source)
        {
            action(element, index++);
        }
    }

    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        foreach (T element in source)
        {
            await action(element).ConfigureAwait(false);
        }
    }

    public static void ParallelForEach<T>(
        this IEnumerable<T> source,
        Action<T> action,
        int? maxDegreeOfParallelism = null) =>
        Parallel.ForEach(
            source,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1 },
            action);

    // http://blog.briandrupieski.com/throttling-asynchronous-methods-in-csharp
    public static Task ParallelForEachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> action,
        int? maxDegreeOfParallelism = null) =>
        source.ParallelSelectAsync(
            async item =>
            {
                await action(item).ConfigureAwait(false);
                return 0;
            },
            maxDegreeOfParallelism);
}
