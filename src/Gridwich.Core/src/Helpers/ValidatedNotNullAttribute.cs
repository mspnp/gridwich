using System;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// This attribute can be added on parameters to methods that handle
    /// null validations to avoid getting CA1062 ("should check parameter for null")
    /// warnings, when the method being called is the one doing that validation.
    ///
    /// It is intended for helper methods (e.g. StringHelpers.NullIfNullOrWhteSpace)
    /// to declare that they are prepared for the argument in question to be null and
    /// thereby avoid triggering a CA1062 in the caller.
    ///
    /// This attribute is not intended for outside of such restricted uses.  i.e.,
    /// it should not be used in mainline code unless there is a circumstance where
    /// a parameter being null is acceptable, but there is no easy way to covince
    /// Code Analsysis of it, short of a Suppress attribute on the method.
    ///
    /// IMPORTANT: This attribute is only intended for use with methods that actually
    /// validate method parameters.  It should not be used with methods that simply are
    /// willing to accomodate null paramters as a matter of course.  It should only be
    /// used for methods that are actually validating parameters at method entry.
    /// <summary>
    /// <remarks>
    /// Code Analysis tools are looking for this attribute by unqualified name.  They
    /// do not care what package it comes from, just that it is called "ValidatedNotNull".
    ///
    /// It is currently configured to apply only at the Parameter level.
    ///
    /// A good background summary at: https://esmithy.net/2011/03/15/suppressing-ca1062
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class T {
    ///     public static bool CheckIfAccountIsValid([ValidatedNotNull] string s) { ... }
    /// }
    /// </code>
    /// avoids a CA1062, when used like:
    /// <code>
    ///     public void SomeMethod(string accountName) {
    ///         if (T.CheckIfAccountIsValid(accountName)) ...
    ///     }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ValidatedNotNullAttribute : Attribute
    {
        // Note: only the class name matters, no need for a body.
    }
}