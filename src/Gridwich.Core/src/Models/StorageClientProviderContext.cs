using System;
using System.Globalization;
using Gridwich.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gridwich.CoreTests")]

namespace Gridwich.Core.Models
{
    /// <summary>
    /// A context object to encapsulate clientRequestID and optionally, an Etag value.
    ///
    /// Note that there are methods/properties provided allowing get/set of
    /// clientRequestID/Operation Context as either string or JObject.
    /// </summary>
    /// <remarks>
    /// Instances of this class are required for all StorageService operations which make
    /// requests to Azure Storage.  This classes instance variables are manipulated by
    /// the HTTP pipeline attached to  Blob and Container sleeve client instances.
    ///
    /// This class builds a storage context, based on either a non-null JSON object
    /// or a string containing one of:<ul>
    /// <li>null/empty/whitespace => like StorageContext.NoneMuted - so silent.
    /// <li>GUID => wrapped into JSON, not silent
    /// <li>JSON => used as-is, not silent by default.
    /// </ul>
    /// Any other string input will result a failure.  Constructors throw an
    /// ArgumentOutOfRangeException exception in this case.
    ///
    /// The JSON payload will similar to one of the following.  Additional properties
    /// are nearly always present to reflect those sent in by the Requestor.<ul>
    /// <li><c>{}</c><br/>
    /// The minimal non-muted context.
    /// <li><c>{"~muted":1}</c><br/>
    /// The minimal muted context.
    /// <li><c>{"~GUID":"ebc5dcad-d92a-4b65-ac96-309a703bdccf"}</c><br/>
    /// A context derived from a clientRequestId whose value was a GUID string.  Not muted.
    /// <li><c>{"~GUID":"ebc5dcad-d92a-4b65-ac96-309a703bdccf","~muted":1}</c><br/>
    /// Same, except muted.
    /// <li><c>{"~special":"Some arbitrary\" text"}</c><br/>
    /// A context derived a non-empty, non-JSON, non-GUID clientRequestId value. Not muted.
    /// Note that JSON-escaping is handled by the class, so the double quote above was
    /// escaped automatically.
    /// <li><c>{"~special":"Some arbitrary\" text","~muted":1}</c><br/>
    /// Same, except muted.
    /// </ul>
    ///
    /// The GUID behavior is done this way for compatability with the BlobDeleted
    /// BlobCreated handlers.  For externally-driven events (e.g. Aspera creates
    /// a blob), the clientRequestId value will be a GUID, rather than one of
    /// our JSON-flavored ones.  This, plus the fact that normal behavior is to
    /// publish these to Requestor, leads to the need to make them non-muted by default.
    ///
    /// The static CreateSafe method provides functionality equivalent to the
    /// constructors, but do not throw an exception, given a bad input string.
    /// Instead, in this circumstance (non-JSON, non-GUID input), it will treat
    /// the input as equivalent to an empty string.
    /// </remarks>
    public class StorageClientProviderContext
    {
        #region "Internal Constants"
        // *** Note that these internal constants are visible to the corresponding test assembly.

        /// <summary>
        /// This is the JSON property whose presence/absence indicates whether the
        /// context is muted (property present) or not (property absent).  When
        /// present, the property will have an integer value of 1.  That said,
        /// the actual value doesn't really matter, only the presence or absence
        /// of the property does.
        /// </summary>
        /// <remarks>
        /// Note that the '~' prefix was chosen in order to "encourage" this name to
        /// sort to later in the property list.  NewtonSoft's JObject serializers produce
        /// ascending alphabetical order of properties when converting to strings.
        /// </remarks>
        internal const string MutedPropertyName = "~muted";

        /// <summary>
        /// The property name used when wrapping a bare GUID into a JObject.
        /// e.g. when the constructors see a GUID string instead of a proper
        /// JSON Object, this is how they wrap the GUID.
        /// </summary>
        internal const string GuidPropertyName = "~GUID";

        /// <summary>
        /// The property name used when wrapping a bare non-GUID/non-JSON
        /// "context" identifier into a JSON object.
        /// </summary>
        internal const string GeneralPropertyName = "~special";
        #endregion
        #region "Private Instance Variables"
        // Note: the contextObject will be the main store for the operationContext, with
        // the string representation generated when/if needed, then retained.

        /// <summary>The operation context</summary>
        private JObject contextObject;

        /// <summary>
        /// The string representation of the contextObject -
        /// a compressed JSON string.  If null, then getter takes care
        /// of filling it in "on-demand" from the contextObject
        /// </summary>
        private string contextObjectAsString;

        #endregion
        #region "Private Helpers"

        /// <summary>
        /// True if the instance is one of the "read-only" special cases - None or NoneMuted.
        /// </summary>
        /// <remarks>
        /// This is used to fail property sets so as not to "corrupt" what are supposed to
        /// be immutable instances.  If other such instances are added in future, be sure to
        /// add them here.
        /// </remarks>
        private static bool IsReadOnlyInstance(StorageClientProviderContext scpc)
        {
            // Special case, don't let muting be reset for either of the static instances.
            //
            // Note: the null checks are included because the constructor uses the
            // IsMuted property which uses this method.  During static construction of
            // these two special instances, the values under construction will not yet
            // have been assigned, so those constructors using IsMuted will work as
            // hoped.
            var result = ((None != null) && object.ReferenceEquals(scpc, None)) ||
                         ((NoneMuted != null) && object.ReferenceEquals(scpc, NoneMuted));
            return result;
        }

        /// <summary>
        /// Ensure that the JObject representation of the string is there.
        /// </summary>
        /// <remarks>
        /// As the most common init/reset will likely be via resetting the ClientRequest
        /// string, be lazy about ensuring that the JObject equivalent is present/correct.
        /// </remarks>
        private void EnsureContextObject()
        {
            if (contextObject != null)
            {
                return;
            }

            // else, we'll have to create a new one from the string representation.
            if (contextObjectAsString == null)
            {
                // whoops, somehow there isn't a string to convert from... shouldn't happen.
                throw new ApplicationException($"Invalid {this.GetType().Name} state invalid - no operationContext");
            }

            contextObject = StringContextToJObject(contextObjectAsString, out var isArtificial);
        }

        /// <summary>
        /// Convert an OperationContext string into a JObject.  The conversion allows for handling
        /// of blank/null or GUID context strings input, in addition to the normal JSON string format.
        /// </summary>
        /// <remarks>
        /// As the most common init/reset will likely be via resetting the ClientRequest
        /// string, be lazy about ensuring that the JObject equivalent is present/correct.
        /// </remarks>
        /// <param name="operationContextString">The JSON string count on a ClientRequestId header.
        /// e.g. <c>{"abc":"def"}</c></param>
        /// <param name="isArtificial">True if the return value was not the result of a simple JSON
        /// conversion of the input string.  i.e., if the return value is some JSON wrapping of the
        /// input string which was then converted to a JObject.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the method couldn't
        /// understand the input and therefore couldn't perform the converstion to JObject</exception>
        private static JObject StringContextToJObject(string operationContextString, out bool isArtificial)
        {
            isArtificial = true; // i.e., we couldn't successfully parse it directly into a JObject.

            // If nothing, return a non-property JObject
            if (string.IsNullOrWhiteSpace(operationContextString))
            {
                return new JObject();
            }

            JObject result = null;

            try
            {
                result = JsonHelpers.DeserializeOperationContext(operationContextString);
                isArtificial = false;
            }
            catch (JsonReaderException)
            {
                // Well, it's not the well-formed JSON we'd hoped for. We'll create an empty context.
                // Most likely, it's a GUID.  If so, process it into a fully-blown, but not muted
                // See commentary at top of file for why this is not muted.  Why encapsulate the
                // GUID into JSON?  Simply to not lose the value.
                if (Guid.TryParse(operationContextString, out Guid g))
                {
                    result = new JObject();
                    result.Add(GuidPropertyName, g.ToString());
                }
                else
                {
                    // We know it's not empty/blanks, JSON, or a GUID. Time to give up.
                    throw new ArgumentOutOfRangeException(
                                nameof(operationContextString),
                                operationContextString,
                                "Could not parse operationContext");
                }
            }
            return result;
        }

        /// <summary>
        /// A common helper used by multiple constructors to set up a new Context instance.
        /// See constructor docs below for parameter descriptions.
        /// </summary>
        private void ConstructorHelper(
            JObject opContext, bool? doMute, bool? trackETag, string initialETag, bool doClone)
        {
            _ = opContext ?? throw new ArgumentNullException(nameof(opContext));

            contextObject = doClone ? opContext.DeepClone() as JObject : opContext;

            if (doMute.HasValue)
            {
                this.IsMuted = doMute.Value;
            }

            contextObjectAsString = JsonHelpers.SerializeOperationContext(contextObject);

            // ETag setup, defaulting to none.
            TrackingETag = trackETag.HasValue ? trackETag.Value : false;
            ETag = string.IsNullOrWhiteSpace(initialETag) ? null : initialETag.Trim();
        }
        #endregion
        #region "Public Constants"

        /// <summary>A special instance representing a lack of context information, and not muted.</summary>
        public static readonly StorageClientProviderContext None
                = StorageClientProviderContext.CreateSafe("{}", muteContext: false, trackETag: false);

        /// <summary>A special instance representing a lack of context information, and also muted.</summary>
        public static readonly StorageClientProviderContext NoneMuted
                = StorageClientProviderContext.CreateSafe("{}", muteContext: true, trackETag: false);
        #endregion
        #region "Public Properties"

        /// <summary>
        /// Get/Set whether the context is muted.
        ///
        /// Muted corresponds to whether related output events are expected
        /// to be published -- i.e., muted events are not published.
        ///
        /// Effectively, this is whether they are published to Requestor.  Muting
        /// is used for Storage operations which are internal to Gridwich, such a
        /// blob copying done in setup for an encoding job -- where Azure Storage
        /// generates "Blob created" notifications that Gridwich should not be
        /// "bothering" Requestor with.
        /// </summary>
        public bool IsMuted
        {
            get
            {
                EnsureContextObject();
                var result = contextObject.ContainsKey(MutedPropertyName);
                return result;
            }
            set
            {
                // Special case, don't let muting be reset for either of the static instances.
                // While it's easy to make the static reference to those readonly, this is
                // needed to avoid somewhere in Gridwich changing the state of what is really a
                // const, save C# restrictions which precluded that being declaratively-expressed.
                if (IsReadOnlyInstance(this))
                {
                    // could throw an exception, but cleaner to just ignore.
                    return;
                }

                // We may be about to invalidate the string representation
                var needToResetString = value != IsMuted;

                if (value)
                {
                    // Add the muted property
                    contextObject[MutedPropertyName] = 1;
                }
                else
                {
                    // Remove the muted property, if present.
                    // Note: this won't throw if the property wasn't present.
                    contextObject.Remove(MutedPropertyName);
                }

                if (needToResetString)
                {
                    contextObjectAsString = JsonHelpers.SerializeOperationContext(contextObject);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to send an ETag HTTP header
        /// on the next request.  If true, set the ETag value for requests.
        /// If false the ETag HTTP header is not set for requests.
        ///
        /// This flag may be changed before a request and retains the same
        /// value until changed.
        /// </summary>
        public bool TrackingETag { get; set; }

        /// <summary>
        /// Holds the last value extracted from an HTTP response, which is also
        /// (if TrackingETag is true) the value to include with the next HTTP request.
        ///
        /// On processing an HTTP response, if the ETag HTTP header is present,
        /// it will always be extracted and updated here.  That is, only whether the
        /// ETag is sent on the next request is controlled by the TrackingETag flag.
        /// Extraction from responses is always performed if the HTTP header is present.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets (a copy of) the ClientRequestID as a JObject.
        /// </summary>
        /// <remarks>
        /// To help with consistency around the pack/unpack of Operation Context,
        /// provide for retrieving the ClientRequestID as a JObject.
        /// </remarks>
        /// <returns>
        /// If ClientRequestID is valid JSON, return the corresponding JObject.
        /// Otherwise, null.
        /// </returns>
        public JObject ClientRequestIdAsJObject
        {
            get
            {
                return this.contextObject?.DeepClone() as JObject;
            }
        }

        /// <summary>
        /// Gets the string value suitable to place on an x-ms-client-request-id HTTP header.
        ///
        /// If null or empty, the HTTP pipeline of the Blob/Container clients will not
        /// set that HTTP header on the request.
        ///
        /// The string value is a compacted JSON representation of the JObject value.
        /// </summary>
        /// <remarks>
        /// This class handles the "duality" of OperationContext representations -- string
        /// vs. JObject. While Storage operations use strings to represent the context (on
        /// HTTP headers), the DTOs use JObject representation exclusively.  This creates
        /// a potential disconnect with keeping both in sync efficiently, were user code
        /// permitted to assign new values from both representations.
        ///
        /// To keep this simpler, this class provides only public gets for either
        /// representation.  The two values are kept in sync internally, with:<ul>
        /// <li>the string representation created only if needed (and cached).
        /// <li>the context object is only settable at constructor time.
        /// <li>the context object presented to the constructor is deep-cloned to
        /// insulate from changes to the original context object.
        /// </ul>
        /// </remarks>
        public string ClientRequestID
        {
            get
            {
                if (contextObjectAsString == null)
                {
                    contextObjectAsString = JsonHelpers.SerializeOperationContext(contextObject);
                }
                return contextObjectAsString;
            }
        }
        #endregion
        #region "Public Instance Methods"

        /// <summary>
        /// Reset the fields of the current instance to those in the argument context.
        /// </summary>
        /// <remarks>
        /// At first glance, this method may appear unneeded.  It is here to avoid the
        /// temptation for a user to try directly assigning context instances.
        ///
        /// That would not go well due to the way that the HTTP pipeline policy tied to the
        /// SDK client instance must have a predictable place from which to obtain desired
        /// header values.  Thus, the values must be updated within this instance. Simply
        /// "pointing" to a different StorageClientProviderContext instance wouldn't work.
        ///
        /// See Gridwich.SagaParticipants.Storage.AzureStorage/BlobClientPipelinePolicy.cs for
        /// more information on the HTTP pipeline policy used by Blob and Container SDK clients.
        ///
        /// Note: using this method to try to create equivalents of None or NoneMuted will
        /// not work exactly as one might expect.  While the result
        /// </remarks>
        /// <param name="ctx">The source context.</param>
        /// <exception cref="System.ArgumentNullException">if source instance is null.</exception>
        /// <exception cref="System.ArgumentException">if target instance is read-only.</exception>
        public void ResetTo(StorageClientProviderContext ctx)
        {
            _ = ctx ?? throw new ArgumentNullException(nameof(ctx));

            // If happens to be resetting to self
            if (object.ReferenceEquals(this, ctx))
            {
                return;
            }

            // If trying something like None.ResetTo(anotherInstance)
            if (IsReadOnlyInstance(this))
            {
                throw new ArgumentException("Cannot use ResetTo to alter read-only StorageClientProviderContext instance");
            }

            this.ETag = ctx.ETag;
            this.TrackingETag = ctx.TrackingETag;
            this.contextObjectAsString = ctx.contextObjectAsString;
            this.contextObject = ctx.contextObject?.DeepClone() as JObject;
        }
        #endregion
        #region "Public Static Methods"

        /// <summary>
        /// Create a context instance, being more lenient than the constructors regarding
        /// the input value.  In addition to the normal JSON input, this method will also
        /// tolerate null/empty strings, GUID strings and other non-JSON strings.  It
        /// produces a context for each.  For any of the tolerated cases, it will produce
        /// either an empty context or a wrap-up of the passed value.  Muting, etc. are
        /// processed/included in those results as dictated by the parameters.
        ///
        /// If the caller provides either logging Action argument, they will be used to
        /// records errors encountered.
        /// </summary>
        /// <remarks>
        /// This method does not throw an exception for non-JSON cases, as the constructors
        /// tend to (thus "Safe" in the name).  But, it retains a code path that will
        /// log/throw an exception on bad input.  This should never happen in real use
        /// since if the code reaches that point, it will be that the method itself constructed
        /// JSON that was unparseable.  The code remains to ensure that will be logged if
        /// logger Actions were given and the problem somehow occurs.  Generally, application
        /// code wouldn't bother using a try/catch around calls to CreateSafe - otherwise, a
        /// constructor may suffice.
        /// </remarks>
        /// If there is an error (e.g. JSON parsing or otherwise), and the corresponging log*Error
        /// argument is non-null, invoke that to let the caller log the error.
        /// </summary>
        /// <param name="operationContextString">
        /// A string representing the Operation Context as a single JSON object.
        /// e.g. { "a" : "b" }</param>
        /// <param name="muteContext">If true, context should be set as muted -- i.e., intended for
        /// internal operations whose resulting notifications (if any) are not expected to be routed
        /// to callers (e.g., Requestor).</param>
        /// <param name="trackETag">If true, send the eTag value on each request (if it's not currently
        /// empty).</param>
        /// <param name="eTag">The current eTag string.  With each response, this value is updated to
        /// reflect the value returned last from Azure Storage (regardless of trackETag's value).
        /// For each HTTP request, the eTag value is only set if trackETag is true and the value of eTag
        /// is not empty.</param>
        /// <param name="logParseError">A callback invoked (if non-null) should a JSON parse error
        /// occur in processing operationContextString. Second argument to this callback is string that
        /// was being parsed.</param>
        /// <param name="logOtherError">A callback invoked (if non-null) should an exception, other
        ///  than a JSON parse error occur in processing operationContextString.</param>
        /// <returns>An instance of StorageProviderContext.</returns>
        public static StorageClientProviderContext CreateSafe(
                        string operationContextString,
                        bool? muteContext = null,
                        bool? trackETag = null,
                        string eTag = null,
                        Action<Exception, string> logParseError = null,
                        Action<Exception> logOtherError = null)
        {
            var muteContextSpecified = muteContext.HasValue;

            if (!trackETag.HasValue)
            {
                trackETag = false; // default, if none given
            }

            JObject contextObject = null;
            bool haveLoggedParseIssues = false;

            var emptyInput = string.IsNullOrWhiteSpace(operationContextString);

            // #1 -- handle case where there's some string to try as JSON
            if (!emptyInput)
            {
                try
                {
                    contextObject = JsonHelpers.DeserializeOperationContext(operationContextString);
                }
                catch (Exception ep)
                {
                    logParseError?.Invoke(ep, $"Error parsing Storage operation context from: '{operationContextString}'");
                    haveLoggedParseIssues = true;
                    // and continue...
                }
            }

            // #2 -- didn't work as JSON, check it for GUID, etc.
            if (contextObject == null)
            {
                try
                {
                    contextObject = StringContextToJObject(operationContextString, out bool _);
                }
                catch (Exception es)
                {
                    if (!haveLoggedParseIssues)
                    {
                        logParseError?.Invoke(es, $"Error converting Storage operation context from: '{operationContextString}'");
                        haveLoggedParseIssues = true;
                        // and continue...
                    }
                    // Something (really) went wrong trying to create the StorageContext instance.
                    // It could be being passed incomplete JSON  or someone sending in a random sentence of text.
                    // Rather than stop the Gridwich with an exception, wrap whatever was given into
                    // a blank OperationContext as the value of a known JSON property.
                    contextObject = new JObject();
                    contextObject.Add(GeneralPropertyName, operationContextString);
                }
            }

            // #3 -- we finally have something to use as an OperationContext, now just wrap it up in a StorageContext.
            StorageClientProviderContext result = null;
            try
            {
                result = new StorageClientProviderContext(contextObject, muteContext, trackETag, eTag);
            }
            catch (Exception eo)
            {
                // If we get here, it's not the fault of the caller.  It means that this
                // method has somehow manipulated the input string into invalid JSON.
                // This should not occur in the real world.
                logOtherError?.Invoke(eo);
                throw;
            }

            return result;
        }
        #endregion
        #region "Constructors"

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageClientProviderContext"/> class,
        /// based on the input JObject (usually from the operationContext in a DTO).
        ///
        /// Note that it's possible for a DTO to have a null operationContext value.  To
        /// cleanly accomodate this, silently accept null and substitute an empty JObject.
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="muted">If true, mute the context (i.e., do not publish result events).
        /// If not specified, it's value will be inferred from the operationContext contents.</param>
        /// <param name="trackETag">if true, send/receive HTTP ETags across storage requests.</param>
        /// <param name="initialETag">The ETag value to send on the next storage request.</param>
        public StorageClientProviderContext(JObject operationContext, bool? muted = null, bool? trackETag = null, string initialETag = null)
        {
            // There are cases where the operationContext is possibly empty in a DTO.  To
            // cleanly accomodate this, we'll just swap in an empty JObject.
            operationContext = operationContext ?? new JObject();

            ConstructorHelper(operationContext, muted, trackETag, initialETag, doClone: true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageClientProviderContext"/> class,
        /// based on the input JSON string (commonly from an HTTP response's clientRequestId property/header).
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="muted">If true, mute the context (i.e., do not publish result events).
        /// If not specified, it's value will be inferred from the operationContext contents.</param>
        /// <param name="trackETag">if true, send/receive HTTP ETags across storage requests.</param>
        /// <param name="initialETag">The ETag value to send on the next storage request.</param>
        /// <exception cref="ArgumentNullException">if operationContext is null.</exception>
        public StorageClientProviderContext(string operationContextString, bool? muted = null, bool? trackETag = null, string initialETag = null)
        {
            // Special case, if we get an operation with no context string, it could end up as null
            // due to the way the DTOs are created/converted.  To paper over this quirk, treat null
            // like empty string.
            //
            // Note: checking for null is sufficient as the whitespace case is handled by StringContextToJObject.
            operationContextString = operationContextString ?? string.Empty;

            contextObject = StringContextToJObject(operationContextString, out bool _);
            ConstructorHelper(contextObject, muted, trackETag, initialETag, doClone: false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageClientProviderContext"/> class,
        /// based on the input JSON string (commonly from an HTTP response's clientRequestId property/header).
        /// Muting is inferred from the operationContextString contents.
        /// </summary>
        /// <remarks>
        /// This constructor is here only for backwards compatability, due to the later addition of the
        /// muted parameter.  As a later activity, the 13 call points could have been changed to use the
        /// newer constructor signature (and eliminate this one).
        /// </remarks>
        /// <param name="operationContextString">The client request identifier.</param>
        /// <param name="trackETag">if set to <c>true</c> [track e tag].</param>
        /// <param name="initialETag">The initial e tag.</param>
        /// <exception cref="ArgumentNullException">If operationContextString is null.</exception>
        public StorageClientProviderContext(string operationContextString, bool trackETag, string initialETag)
            : this(operationContextString, null, trackETag, initialETag)
        {
            // nothing else
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageClientProviderContext"/> class,
        /// based on another context.  i.e., like a copy constructor.
        /// </summary>
        /// <param name="ctx">The source context.</param>
        public StorageClientProviderContext(StorageClientProviderContext ctx)
        {
            ResetTo(ctx);
        }
        #endregion
    }
}
