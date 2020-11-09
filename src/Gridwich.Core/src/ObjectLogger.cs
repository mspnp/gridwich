using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core
{
#pragma warning disable SA1600 // Elements should be documented

    [ExcludeFromCodeCoverage]
    public class ObjectLogger<T> : IObjectLogger<T>
    {
        private readonly TelemetryClient _client;
        private readonly IAppInsightsUrlCreator _urlCreator;

        public ObjectLogger(TelemetryClient client, IAppInsightsUrlCreator urlCreator)
        {
            _client = client;
            _urlCreator = urlCreator;
        }

        private string TrackEvent(EventId eventId, object o, string message, params object[] args)
        {
            var evt = eventId.ToEventTelemetry(message, args);

            if (o != null)
            {
                var objectData = ConvertToDictionary(o);
                foreach (var i in objectData)
                {
                    evt.Properties.Add(i.Key, i.Value.ToString());
                }
            }

            _client.TrackEvent(evt);

            return evt.Context.Operation.Id;
        }

        private static string GetQueryForEventId(string eventId) => $@"customEvents | where operation_Id == ""{eventId}""";

        private string TrackException(EventId eventId, Exception ex, object o, string message, params object[] args)
        {
            var evt = eventId.ToExceptionTelemetry(ex, message, args);

            if (o != null)
            {
                var objectData = ConvertToDictionary(o);
                foreach (var i in objectData)
                {
                    evt.Properties.Add(i.Key, i.Value.ToString());
                }
            }

            _client.TrackException(evt);

            return evt.Context.Operation.Id;
        }

        private static string GetQueryForExceptionId(string exceptionId) => $@"exceptions | where operation_Id == ""{exceptionId}""";

        private string LogImpl(EventId eventId, Exception ex, object objectData, string message, params object[] args) => ex == null ? TrackEvent(eventId, objectData, message, args) : TrackException(eventId, ex, objectData, message, args);

        public void LogEvent(EventId eventId, string message, params object[] args) => LogImpl(eventId, null, null, message, args);

        public void LogEvent(out Uri locator, EventId eventId, string message, params object[] args) => locator = _urlCreator.CreateUrl(GetQueryForEventId(LogImpl(eventId, null, null, message, args)));

        public void LogEventObject(EventId eventId, object o) => LogImpl(eventId, null, o, null);

        public void LogEventObject(out Uri locator, EventId eventId, object o) => locator = _urlCreator.CreateUrl(GetQueryForEventId(LogImpl(eventId, null, o, null)));

        public void LogException(EventId eventId, Exception ex, string message, params object[] args) => LogImpl(eventId, ex, null, message, args);

        public void LogException(out Uri locator, EventId eventId, Exception ex, string message, params object[] args) => locator = _urlCreator.CreateUrl(GetQueryForExceptionId(LogImpl(eventId, ex, null, message, args)));

        public void LogExceptionObject(EventId eventId, Exception ex, object o) => LogImpl(eventId, ex, o, null);

        public void LogExceptionObject(out Uri locator, EventId eventId, Exception ex, object o) => locator = _urlCreator.CreateUrl(GetQueryForExceptionId(LogImpl(eventId, ex, o, null)));

        /// <summary>
        /// This routine convert objects into dictionary of type Dictionary<string, object>
        /// so that the log will put the key / value pairs into the log
        /// </summary>
        /// <param name="o">The object to convert.</param>
        /// <returns></returns>
        private Dictionary<string, object> ConvertToDictionary(object o)
        {
            try
            {
                JObject jsonObject;
                var oType = o.GetType();
                if (o is string)
                {
                    return new Dictionary<string, object> { { "Message", o } };
                }
                if (oType.IsArray)
                {
                    var array = o;
                    var no = new { array };
                    jsonObject = JObject.FromObject(no);
                }
                else if (oType.FullName == "System.Uri")
                {
                    return new Dictionary<string, object> { { "Uri", o.ToString() } };
                }
                else
                {
                    jsonObject = JObject.FromObject(o);
                }

                // SE: Extract the list of top level properties that have no child objects
                IEnumerable<JToken> jTokens = jsonObject.Descendants().Where(p => !p.Any());
                // OLD version: IEnumerable<JToken> jTokens2 = jsonObject.Descendants().Where(p => p.Count() == 0);
                // MSG for OLD: warning CA1827: Count() is used where Any() could be used instead to improve performance.
                Dictionary<string, object> results = jTokens.Aggregate(new Dictionary<string, object>(),
                    (properties, jToken) =>
                    {
                        properties.Add(jToken.Path, jToken.ToString());
                        return properties;
                    });
                return results ?? new Dictionary<string, object> { { "Logger Error", "Null Object" } };
            }
            catch (JsonReaderException hre)
            {
                LogEvent(LogEventIds.ObjectLoggerParsingError,
                    "JSonReaderException Error in logging parser: " + hre.Message);
                return null;
            }
            catch (Exception e)
            {
                LogEvent(LogEventIds.ObjectLoggerParsingError,
                    "Unknown Error in logging parser: " + e.Message);
                return null;
            }
        }
    }

    public static class ObjectLoggerExtensions
    {
        public static void AddObjectLogger(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
            {
                EnableAdaptiveSampling = false,
                EnableDebugLogger = true,
            });
            services.AddSingleton(typeof(IAppInsightsUrlCreator), typeof(LogRecordUrlCreator));
            services.AddSingleton(typeof(IObjectLogger<>), typeof(ObjectLogger<>));
        }

        private const string EventPrefix = @"Gridwich";

        public static EventTelemetry ToEventTelemetry(this EventId evt, string message, params object[] args)
        {
            var retVal = new EventTelemetry(evt.Name ?? throw new ArgumentNullException(nameof(evt), @"Event name cannot be null"));

            retVal.Properties.Add($@"{EventPrefix}.EventId", evt.Id.ToString(CultureInfo.InvariantCulture));
            retVal.Properties.Add($@"{EventPrefix}.EventName", evt.Name.ToString(CultureInfo.InvariantCulture));
            retVal.Properties.Add($@"{EventPrefix}.Severity", evt.GetLevelName());
            retVal.Properties.Add($@"{EventPrefix}.Subsystem", evt.GetSubsystemName());

            if (!string.IsNullOrWhiteSpace(message))
            {
                retVal.Properties.Add($@"{EventPrefix}.Message", string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return retVal;
        }

        public static ExceptionTelemetry ToExceptionTelemetry(this EventId evt, Exception ex, string message, params object[] args)
        {
            var retVal = new ExceptionTelemetry(ex ?? throw new ArgumentNullException(nameof(ex)))
            {
                ProblemId = evt.Id.ToString(CultureInfo.InvariantCulture)
            };

            retVal.Properties.Add($@"{EventPrefix}.EventId", evt.Id.ToString(CultureInfo.InvariantCulture));
            retVal.Properties.Add($@"{EventPrefix}.EventName", evt.Name.ToString(CultureInfo.InvariantCulture));
            retVal.Properties.Add($@"{EventPrefix}.Severity", evt.GetLevelName());
            retVal.Properties.Add($@"{EventPrefix}.Subsystem", evt.GetSubsystemName());

            if (!string.IsNullOrWhiteSpace(message))
            {
                retVal.Properties.Add($@"{EventPrefix}.Message", string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return retVal;
        }
    }
#pragma warning restore SA1600 // Elements should be documented
}