namespace Gonkers.NuGetTools.NextPatchVersion
{
    public static class Constants
    {
        public const string NugetFeedUrl = "https://api.nuget.org/v3/index.json";
        public static class ExitCodes
        {
            public const int
                SettingsFileMissing = 1,
                SettingsFileCorrupt = 2,
                MissingPackageId = 3,
                UnknownError = 99;
        }
    }
}
