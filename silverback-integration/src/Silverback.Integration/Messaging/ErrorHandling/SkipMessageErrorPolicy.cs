﻿// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        public SkipMessageErrorPolicy(ILogger<SkipMessageErrorPolicy> logger)
            : base(logger)
        {
        }

        public override ErrorAction HandleError(FailedMessage failedMessage, Exception exception) =>
            ErrorAction.SkipMessage;
    }
}