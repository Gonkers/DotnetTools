namespace NuGetVersionClaimer
{
    public static class Constants
    {
        public const string NugetFeedUrl = "https://api.nuget.org/v3/index.json";
        public static class ExitCodes
        {
            public const int
                ProjectFileMissing = 1,
                ProjectFileCorrupt = 2,
                MissingPackageId = 3,
                UnknownError = 99;
        }
    }
}
