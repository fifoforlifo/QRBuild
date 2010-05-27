namespace QRBuild
{
    /// Indicates the status of a BuildNode
    internal enum BuildStatus
    {
        NotStarted,
        InProgress,
        //  Success Codes
        SuccessMin,
        TranslationUpToDate,
        ExecuteSucceeded,
        SuccessMax,
        //  Failure Codes
        FailMin,
        FailureUnknown,
        InputsDoNotExist,
        ExecuteFailed,
        FailMax,
    }


    internal static class BuildStatusExtensions
    {
        public static bool Succeeded(this BuildStatus status)
        {
            return (BuildStatus.SuccessMin < status && status < BuildStatus.SuccessMax);
        }

        public static bool Failed(this BuildStatus status)
        {
            return (BuildStatus.FailMin < status && status < BuildStatus.FailMax);
        }

        public static bool Executed(this BuildStatus status)
        {
            return Succeeded(status) || Failed(status);
        }
    }
}
