// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class DiagnosticsListMessage
    {
        public FrameworkData Framework { get; set; }

        public IList<DiagnosticMessageView> Errors { get; set; }

        public IList<DiagnosticMessageView> Warnings { get; set; }
    }
}
