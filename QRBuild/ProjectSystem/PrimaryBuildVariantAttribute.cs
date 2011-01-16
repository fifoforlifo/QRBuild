using System;

namespace QRBuild.ProjectSystem
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PrimaryBuildVariantAttribute : Attribute
    {
    }
}
