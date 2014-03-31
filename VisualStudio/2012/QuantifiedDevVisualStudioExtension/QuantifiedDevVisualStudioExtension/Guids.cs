// Guids.cs
// MUST match guids.h
using System;

namespace QuantifiedDev.QuantifiedDevVisualStudioExtension
{
    static class GuidList
    {
        public const string guidQuantifiedDevVisualStudioExtensionPkgString = "b8e9e30c-7b19-455b-a83b-33a81be4e0f5";
        public const string guidQuantifiedDevVisualStudioExtensionCmdSetString = "41f7014c-a43c-4b8c-ae55-270bd9608ba9";
        public const string guidToolWindowPersistanceString = "8dfc24d0-676e-418c-815d-6d1b18d892f5";
        public const string guidQuantifiedDevVisualStudioExtensionEditorFactoryString = "0d4dd946-ff9c-46e1-86e5-6e9052d3c9ea";

        public static readonly Guid guidQuantifiedDevVisualStudioExtensionCmdSet = new Guid(guidQuantifiedDevVisualStudioExtensionCmdSetString);
        public static readonly Guid guidQuantifiedDevVisualStudioExtensionEditorFactory = new Guid(guidQuantifiedDevVisualStudioExtensionEditorFactoryString);
    };
}