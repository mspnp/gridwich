namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Constants related to Environment Type.  Used to enable or disable code.
    /// </summary>
    public static class EnvironmentTypeConstants
    {
        /// <summary>
        /// Gets Environment Type app setting name.
        /// </summary>
        public static string EnvironmentTypeSettingName => "DEPLOYMENT_ENVIRONMENT_TYPE";

        /// <summary>
        /// Gets Environment Type for DEV.
        /// </summary>
        public static string EnvironmentTypeDevelopment => "DEV";

        /// <summary>
        /// Gets Environment Type for PRODUCTION.
        /// </summary>
        public static string EnvironmentTypeProduction => "PRODUCTION";

        /// <summary>
        /// Gets Environment Type for UAT.
        /// </summary>
        public static string EnvironmentTypeUAT => "UAT";

        /// <summary>
        /// Gets Environment Type for QA.
        /// </summary>
        public static string EnvironmentTypeQA => "QA";

        /// <summary>
        /// Gets value for an unspecified environment type.
        /// </summary>
        public static string EnvironmentTypeUnspecified => "UNSPECIFIED";
    }
}