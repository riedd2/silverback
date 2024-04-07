// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Validation;
using Silverback.Tests.Performance.TestTypes;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Performance;

// |                                     Method |      Mean |     Error |    StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
// |------------------------------------------- |----------:|----------:|----------:|-------:|-------:|------:|----------:|
// |                               ValidMessage |  3.535 us | 0.0088 us | 0.0078 us | 0.4158 |      - |     - |      4 KB |
// |                      SinglePropertyInvalid |  4.032 us | 0.0320 us | 0.0299 us | 0.4654 |      - |     - |      5 KB |
// |                       AllPropertiesInvalid |  5.312 us | 0.0148 us | 0.0132 us | 0.5951 |      - |     - |      6 KB |
// |                          ValidLargeMessage |  9.084 us | 0.0500 us | 0.0468 us | 1.0071 |      - |     - |     10 KB |
// |    LargeMessageHavingSinglePropertyInvalid |  8.989 us | 0.0398 us | 0.0372 us | 1.0071 |      - |     - |     10 KB |
// | LargeMessageHavingSeveralPropertiesInvalid | 12.420 us | 0.0482 us | 0.0451 us | 1.4191 | 0.0153 |     - |     15 KB |
[SimpleJob]
[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Benchmarks must be instance methods")]
public class MessageValidatorBenchmark
{
    private readonly TestValidationMessage _messageHavingSinglePropertyInvalid;

    private readonly TestValidationMessage _messageHavingAllPropertiesInvalid;

    private readonly TestValidationMessage _validMessage;

    private readonly TestValidationLargeMessage _largeMessageHavingSeveralPropertiesInvalid;

    private readonly TestValidationLargeMessage _largeMessageHavingSinglePropertyInvalid;

    private readonly TestValidationLargeMessage _validLargeMessage;

    public MessageValidatorBenchmark()
    {
        _messageHavingSinglePropertyInvalid =
            TestValidationMessage.MessageHavingSinglePropertyInvalid;
        _messageHavingAllPropertiesInvalid =
            TestValidationMessage.MessageHavingAllPropertiesInvalid;
        _validMessage =
            TestValidationMessage.ValidMessage;
        _largeMessageHavingSeveralPropertiesInvalid =
            TestValidationLargeMessage.MessageHavingSeveralPropertiesInvalid;
        _largeMessageHavingSinglePropertyInvalid =
            TestValidationLargeMessage.MessageHavingSinglePropertyInvalid;
        _validLargeMessage =
            TestValidationLargeMessage.ValidMessage;
    }

    [Benchmark]
    public void ValidMessage() =>
        MessageValidator.IsValid(_validMessage, MessageValidationMode.LogWarning, out _);

    [Benchmark]
    public void SinglePropertyInvalid() =>
        MessageValidator.IsValid(_messageHavingSinglePropertyInvalid, MessageValidationMode.LogWarning, out _);

    [Benchmark]
    public void AllPropertiesInvalid() =>
        MessageValidator.IsValid(_messageHavingAllPropertiesInvalid, MessageValidationMode.LogWarning, out _);

    [Benchmark]
    public void ValidLargeMessage() =>
        MessageValidator.IsValid(_validLargeMessage, MessageValidationMode.LogWarning, out _);

    [Benchmark]
    public void LargeMessageHavingSinglePropertyInvalid() =>
        MessageValidator.IsValid(_largeMessageHavingSinglePropertyInvalid, MessageValidationMode.LogWarning, out _);

    [Benchmark]
    public void LargeMessageHavingSeveralPropertiesInvalid() =>
        MessageValidator.IsValid(_largeMessageHavingSeveralPropertiesInvalid, MessageValidationMode.LogWarning, out _);
}
