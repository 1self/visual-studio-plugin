// Guids.cs
// MUST match guids.h
using System;

namespace N1self.C1selfVisualStudioExtension
{
    static class GuidList
    {
        public const string guid1selfVisualStudioExtensionPkgString = "b8e9e30c-7b19-455b-a83b-33a81be4e0f5";
        public const string guid1selfVisualStudioExtensionCmdSetString = "41f7014c-a43c-4b8c-ae55-270bd9608ba9";
        public const string guidToolWindowPersistanceString = "8dfc24d0-676e-418c-815d-6d1b18d892f5";
        public const string guid1selfVisualStudioExtensionEditorFactoryString = "0d4dd946-ff9c-46e1-86e5-6e9052d3c9ea";

        public static readonly Guid guid1selfVisualStudioExtensionCmdSet = new Guid(guid1selfVisualStudioExtensionCmdSetString);
        public static readonly Guid guid1selfVisualStudioExtensionEditorFactory = new Guid(guid1selfVisualStudioExtensionEditorFactoryString);
    };
}