using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// JsonHelpers
    /// </summary>
    public static class JsonHelpers
    {
        /// <summary>
        /// Setups the json serialization.
        /// </summary>
        /// <seealso cref="https://www.newtonsoft.com/json/help/html/DefaultSettings.htm">
        public static void SetupJsonSerialization()
        {
            var tmp = GridwichSerializerSettings;
            JsonConvert.DefaultSettings = () => tmp;
        }

        /// <summary>
        /// Gets the Gridwich serializer settings.
        /// </summary>
        public static JsonSerializerSettings GridwichSerializerSettings
        {
            // For now, we'll create a new settings instance each time asked.  This is for
            // safety to avoid some code somewhere "adjusting" the settings and disrupting
            // the entire application.  If this (repeated creation) becomes an issue, it
            // can be revisited later.
            get
            {
                var serializerSettings = new JsonSerializerSettings();
                ResetSerializationSettingsForGridwich(serializerSettings);
                return serializerSettings;
            }
        }

        /// <summary>
        /// Resets the serialization settings for Gridwich.
        /// </summary>
        /// <param name="inSettings">The in settings.</param>
        /// <exception cref="ArgumentNullException">inSettings</exception>
        public static void ResetSerializationSettingsForGridwich(JsonSerializerSettings inSettings)
        {
            _ = inSettings ?? throw new ArgumentNullException(nameof(inSettings));
            // TODO: for debugging, indented is more readable, for production, best to use Formatting.None
            // inSettings.Formatting = Formatting.None,
            inSettings.Formatting = Formatting.Indented;

            // Use Camel-case for identifiers instead of default Pascal Case.  This avoids marking up every
            // struct member with attributes to force the name casing.
            inSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            // Reference Loops
            //
            // This how the serializer handles nested objects.  For example A refers to B which refers to A.
            // To get around this, there is a "$ref"/"$id" syntax available, but it doesn't get used for Gridwich.
            // We could use error, but "Serialize" will suffice as it doesn't arise.  This has the added
            // advantage of aligning with the EventGridClient settings (it's unclear whether it has the
            // right exception-handling code in place to handle "Error").
            //
            // For examples of where this setting may apply:
            //   https://stackoverflow.com/questions/23453977/what-is-the-difference-between-preservereferenceshandling-and-referenceloophandl
            // Docs: https://www.newtonsoft.com/json/help/html/P_Newtonsoft_Json_JsonSerializerSettings_ReferenceLoopHandling.htm
            inSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;

            // Null Value Handling
            //
            // The short version is: given a null property, should the serializer serialize it with the value null,
            // or simply not include the property in the JSON output? The EventGridClient elects to do the former,
            // but it is unclear why.  For our use, it seems better to elide null properties, affording the
            // developer a measure of control over which properties will be emitted.
            // TODO: investigate why this was set this way on EventGridClient.
            inSettings.NullValueHandling = NullValueHandling.Ignore;

            // Throw an exception if we deserialize a JSON object which does not contain all
            // the public members of the target type.
            // PR83 - Removing as per Azure Function use requirement (not to complain about JSON < struct members)
            // MissingMemberHandling = MissingMemberHandling.Error

            // ShowConverterList(inSettings.Converters, "<<<< Befpre <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

            // Duplicate the converter list used by EventGridClient, plus ensure StringEnumconverter.

            // Using ISO format TimeSpan Converters (as does EventGridClient).
            PlaceConverterAtIndex(inSettings.Converters, new Iso8601TimeSpanConverter(), 0);

            // For now, leave out the speciality [de]serializer used by EventGridClient.  For our use, it shouldn't be needed.
            /*
            PlaceConverterAtIndex(
                inSettings.Converters,
                new PolymorphicSerializeJsonConverter<MediaJobOutput>("@odata.type"),
                1,
                new string[] { "Microsoft.Rest.Serialization.PolymorphicDeserializeJsonConverter`1[[Microsoft.Azure.EventGrid.Models.MediaJobOutput, Microsoft.Azure.EventGrid, Version=3.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]" }
                );
            */

            PlaceConverterAtIndex(inSettings.Converters, new CloudErrorJsonConverter(), 2);

            // Ensure that all enums get serialized/read as their string enumerator names.
            // e.g.   "myEnum":"A"  not "myEnum":0
            PlaceConverterAtIndex(inSettings.Converters, new StringEnumConverter(), 3);

            // Special converter needed for AccessTiers as they are a set of static
            // Azure.Storage.Blobs.Models.AccessTier instances from the SDK that are
            // "nearly" an enum.  This converter handles the values as strings.
            PlaceConverterAtIndex(inSettings.Converters, new AccessTierConverter(), 4);

            // ShowConverterList(inSettings.Converters, "<<<< After  <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
        }

        /// <summary>
        /// Parse from a string to JObject (obeying Gridwich JSON rules by default).
        /// If retainExact is true, use JObject.Parse to keep the exact member name
        /// spellings.  If false (the default), use JsonConvert to deserialize the JSON
        /// string according to Gridwich conventions (e.g., including camel-casing member
        /// names).
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="retainExactMemberSpellings">if set to <c>true</c> [retain exact member spellings].</param>
        /// <returns>
        ///   <see cref="JObject"/>
        /// </returns>
        public static JObject JsonToJObject(string json, bool retainExactMemberSpellings = false)
        {
            JObject ret;
            if (retainExactMemberSpellings)
            {
                ret = JObject.Parse(json);
            }
            else
            {
                ret = JsonConvert.DeserializeObject<JObject>(json);
            }

            return ret;
        }

        /// <summary>
        /// Clean up the result of a JsonConvert default serialization of an object.static
        /// Unfortunately, they elect to escape double quotes plus newline/cr.  Plus the
        /// entire string also gets enclosed in an extra set of (escaped) double quotes.static
        /// Specifically, instead of getting
        /// {
        /// \"a\": 22
        /// }
        /// you might get
        /// \"{ \\\r\\\n \\\"a\\\": 22 \\\r\\\n }\"
        /// This function returns a string with all of that cleaned up into a string containing
        /// {
        /// "a" : 22
        /// }
        /// </summary>
        /// <param name="jsonStr">The json string.</param>
        /// <returns>
        /// The JSON packed into a string, with the escapes removed.  i.e. suitable for
        /// consumption by a JsonConvert.DeserializeObject(...)
        /// </returns>
        /// <exception cref="ArgumentNullException">jsonStr</exception>
        public static string UnescapeJson(string jsonStr)
        {
            _ = jsonStr ?? throw new ArgumentNullException(nameof(jsonStr));

            int leadStartIndex = jsonStr.StartsWith('"') ? 1 : 0; // # of lead chars to skip
            var sb = new System.Text.StringBuilder(jsonStr.Substring(leadStartIndex));
            if (jsonStr.EndsWith('"'))
            { // drop trailing double quote if present
                sb.Length--;
            }

            // Are there any double-escaped sequences to fix?  Look for 3 slashes in a row
            if (jsonStr.Contains("\\"))
            {
                sb.Replace("\\r", "\r");
                sb.Replace("\\n", "\n");
                sb.Replace("\\\"", "\"");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compress all possible whitespace out of a JSON string
        /// and return that compressed version.
        /// </summary>
        /// <param name="jsonIn">The json in.</param>
        /// <returns>
        ///   <see cref="string"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">jsonIn</exception>
        public static string CompressJson(string jsonIn)
        {
            _ = jsonIn ?? throw new ArgumentNullException(nameof(jsonIn));

            bool inString = false;  // are we within a JSON string?

            var outSb = new StringBuilder(jsonIn.Length + 15);  // a little extra room for expansions
            _ = jsonIn + " "; // makes for easier condition checking near end of input.
            var tmpArray = jsonIn.ToCharArray();

            int i = 0;
            while (i < jsonIn.Length)
            {
                char c = tmpArray[i++];
                // DebugHelpers.WriteLine($"Examining character '{c}'");
                if (c == '"')
                {
                    inString = !inString;
                    outSb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '\\': // Start of escape sequence
                        // unless we're in a string, toss them.
                        if (!inString)
                        {
                            continue;
                        }
                        // else, we're in a string, so keep sequence.
                        outSb.Append(c);
                        outSb.Append(tmpArray[i++]);
                        continue;
                    case ' ':
                        if (inString)
                        {
                            outSb.Append(c);
                        }
                        continue;
                    default:
                        if (inString)
                        {
                            switch (c)
                            {
                                case '\r': outSb.Append("\\r"); break;
                                case '\n': outSb.Append("\\n"); break;
                                case '\t': outSb.Append("\\t"); break;
                                case '\f': outSb.Append("\\f"); break;
                                default: outSb.Append(c); break;
                            }
                        }
                        else
                        {
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                case '\t':
                                case '\f': break;
                                default: outSb.Append(c); break;
                            }
                        }
                        // i++;
                        continue;
                }
            }
            return outSb.ToString();
        }

        /// <summary>
        /// Return true if the two JSON strings are the equivalent
        /// i.e., the same aside from spacing, formatting, etc.
        /// Note that this function starts with simple string comparisons, but
        /// if that fails, will try compressed versions of the two operands, failing
        /// that, will compare the results of parsed versions of both (which accomodates
        /// operands that differ only by the ordering of members).  Aim is to bail get
        /// out as cheaply as possible, while still being thorough.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        /// true if the two operands are equivalent (same members and values)
        /// </returns>
        /// <exception cref="ArgumentException">lhs is invalid - lhs
        /// or
        /// rhs is invalid - rhs</exception>
        public static bool JsonEqual(string lhs, string rhs)
        {
            if (string.IsNullOrWhiteSpace(lhs))
            {
                throw new ArgumentException("lhs is invalid", nameof(lhs));
            }

            if (string.IsNullOrWhiteSpace(rhs))
            {
                throw new ArgumentException("rhs is invalid", nameof(rhs));
            }

            // Simplest case, exactly equal.
            // Notes:
            //   1. letting it pass if both are null or empty.
            //   2. Also assuming that non-empty strings contain valid JSON. Validation at
            //      that level is only done if simpler string/compacted-string comparison
            //      fails.
            if (lhs == rhs)
            {
                return true; // Easiest case, exactly equal.
            }

            string lhsc = string.Empty; // compressed versions
            string rhsc = string.Empty;

            // If one string is longer than the other, compress it and
            // compare it against the other (uncompressed) string
            if (lhs.Length != rhs.Length)
            {
                if (lhs.Length > rhs.Length)
                {
                    lhsc = CompressJson(lhs);
                    if (lhsc == rhs)
                    {
                        return true;
                    }
                    rhsc = CompressJson(rhs);
                }
                else
                { // lhs shorter than rhs
                    rhsc = CompressJson(rhs);
                    if (rhsc == lhs)
                    {
                        return true;
                    }
                    lhsc = CompressJson(lhs);
                }
                // both sides are now compressed, so direct compare.
                if (lhsc == rhsc)
                {
                    return true;
                }
            }

            // If we got here, either the two strings started at the same
            // length (but !=) or if they didn't, they didn't compress to the same
            // string.  So ensure we did a compare of compressed.  If that
            // fails, resort to a full object parse to handle the case of
            // differing member ordering.
            lhsc = string.IsNullOrEmpty(lhsc) ? CompressJson(lhs) : lhsc;
            rhsc = string.IsNullOrEmpty(rhsc) ? CompressJson(rhs) : rhsc;

            if (lhsc == rhsc)
            {
                return true; // Easy case - same JSON, possibly with just different spacing
            }

            // Might have different member orderings, etc. So parse into
            // JObjects and let JToken do the member-by-member comparison.
            try
            {
                var lhsjo = JsonConvert.DeserializeObject<JObject>(lhs);
                var rhsjo = JsonConvert.DeserializeObject<JObject>(rhs);

                return JToken.DeepEquals(lhsjo, rhsjo);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSerializableType(object o)
        {
            // We'll only serialize top-level classes or structs or arrays
            var theType = o.GetType();
            bool acceptable = false;

            if (theType.IsClass)
            {
                acceptable = true;
                if (theType.FullName == "System.String")
                {
                    acceptable = false;
                }
            }
            else if (theType.IsValueType)
            {
                acceptable = true;
                if (theType.IsEnum || theType.IsPrimitive)
                {
                    acceptable = false;
                }
            }

            return acceptable;
        }

        #region OperationContext

        /// <summary>
        /// Serialize a JObject representing an Operation Context.
        /// This is one of the cases where we need exact property name fidelity.
        /// Because the string representation is sometimes used as a ClientRequestId
        /// value, the serialized version is compressed to get rid of formatting.
        /// Note: verified that Requestor (which provides and receives back Operation
        /// Context values) accomodates the possibility the differing formatting or
        /// different member ordering, etc.  As long as the members have exactly
        /// the same naming on both ends of the round-trip, it all works.
        /// </summary>
        /// <param name="jo">The jo.</param>
        /// <returns>
        ///   <see cref="string"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">jo</exception>
        public static string SerializeOperationContext(JObject jo)
        {
            _ = jo ?? throw new ArgumentNullException(nameof(jo));

            string s = jo.ToString();
            s = CompressJson(s);
            return s;
        }

        /// <summary>
        /// Adjust a JSON string to be suitable as an Operation Context.
        /// Since we want name fidelity, this just means compressing the JSON.
        /// </summary>
        /// <param name="jsonString">The json string.</param>
        /// <returns>
        ///   <see cref="string" />
        /// </returns>
        /// <exception cref="ArgumentNullException">jsonString</exception>
        public static string SerializeOperationContext(string jsonString)
        {
            _ = jsonString ?? throw new ArgumentNullException(nameof(jsonString));

            string s = CompressJson(jsonString);
            return s;
        }

        /// <summary>
        /// Adjust a JSON string representing an Operation Context to a JObject.static
        /// Currently, this is just a direct Parse, but centralized in case adjustments
        /// are needed in future.
        /// </summary>
        /// <param name="jsonString">The json string.</param>
        /// <returns>
        ///   <see cref="JObject"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">jsonString</exception>
        public static JObject DeserializeOperationContext(string jsonString)
        {
            _ = jsonString ?? throw new ArgumentNullException(nameof(jsonString));

            JObject jo = JObject.Parse(jsonString);
            return jo;
        }
        #endregion

        /// <summary>
        /// Serialize the class instance to Json string.  The string will be
        /// unescaped -- it will be suitable to use with JsonConvert.Deserialize().
        /// </summary>
        /// <param name="theObj">The object.</param>
        /// <returns>
        ///   <see cref="string"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">theObj - Only Class or Struct instances accepted to serialize. Received null</exception>
        /// <exception cref="ArgumentException">Only Class or Struct instances accepted to serialize. Passed type {theObj.GetType().FullName}. - theObj - null</exception>
        public static string SerializeObjectToString(object theObj)
        {
            if (theObj == null)
            {
                throw new ArgumentNullException(nameof(theObj), $"Only Class or Struct instances accepted to serialize. Received null");
            }

            if (IsSerializableType(theObj))
            {
                return SerializeObjectToStringWorker(theObj);
            }

            // Otherwise, it's not a type we should have been getting here -- e.g., int, etc.
            throw new ArgumentException($"Only Class or Struct instances accepted to serialize. Passed type {theObj.GetType().FullName}.", nameof(theObj), null);
        }

        /// <summary>
        /// Serialize the object to Json string.  The string will be
        /// unescaped -- it will be suitable to use with JsonConvert.Deserialize().
        /// </summary>
        /// <param name="theObj">The object.</param>
        /// <returns>
        ///   <see cref="string"/>
        /// </returns>
        private static string SerializeObjectToStringWorker(object theObj)
        {
            string jsonStr = JsonConvert.SerializeObject(theObj);
            string res = UnescapeJson(jsonStr);
            return res;
        }

        /// <summary>
        /// Convert a string of JSON text to a corresponding instance
        /// of type TD.
        /// </summary>
        /// <typeparam name="TD">The type of the d.</typeparam>
        /// <param name="jsonStr">The json string.</param>
        /// <returns>
        ///   <see cref="TD"/>
        /// </returns>
        public static TD DeserializeFromString<TD>(string jsonStr)
        {
            var ret = JsonConvert.DeserializeObject<TD>(jsonStr);
            return ret;
        }

        /*
        /// <summary>
        /// Run the object in question through a serialization/deserialization
        /// sequence.  As below this is basically DeSerialize(Serialize(o)),
        /// but could conceivably contain other intermediate steps in future.
        ///
        /// This will be most useful for unit tests to verify that the serialization
        /// is correct -- alllowing full round-trip processing.
        /// </summary>
        public static TD NormalizeJsonObject<TD>(TD obj) {
            string str = SerializeObjectToString(obj);
            var ret = DeserializeFromString<TD>(str);
            return ret;
        }
        */

        /// <summary>
        /// Return the JToken for the value of the named property in the JSON string.
        /// This only searches for the specified top-level property.
        /// i.e.true, no recursive search,
        /// </summary>
        /// <param name="jsonString">A JSON string to be parsed</param>
        /// <param name="propertyName">The name of the property to be searched for.null</param>
        /// <param name="respectCase">If false, do case insensitive matching.</param>
        /// <returns>The JToken value, if found. Otherwise null.</returns>
        public static JToken GetProperty(string jsonString, string propertyName, bool respectCase = true)
        {
            JObject jo = JObject.Parse(jsonString);
            return GetProperty(jo, propertyName, respectCase);
        }

        /// <summary>
        /// Return the JToken for the value of the named property in the JObject.
        /// This only searches for the specified top-level property.
        /// i.e.true, no recursive search,
        /// </summary>
        /// <param name="jo">A JObject in which to search</param>
        /// <param name="propertyName">The name of the property to be searched for.null</param>
        /// <param name="respectCase">If false, do case insensitive matching.</param>
        /// <returns>The JToken value, if found. Otherwise null.</returns>
        public static JToken GetProperty(JObject jo, string propertyName, bool respectCase = true)
        {
            _ = jo ?? throw new ArgumentNullException(nameof(jo));

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("propertyName is invalid", nameof(propertyName));
            }

            var compareFlavor = respectCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

            _ = jo.TryGetValue(propertyName, compareFlavor, out var retVal);

            return retVal;
        }

        /// <summary>
        /// Determines whether the specified the json contains property.
        /// </summary>
        /// <param name="theJson">The json.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="respectCase">if set to <c>true</c> [respect case].</param>
        /// <returns>
        ///   <c>true</c> if the specified the json contains property; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsProperty(string theJson, string propertyName, bool respectCase = true)
        {
            JObject jo = JObject.Parse(theJson);
            return ContainsProperty(jo, propertyName, respectCase);
        }

        /// <summary>
        /// Determines whether the specified jo contains property.
        /// </summary>
        /// <param name="jo">The jo.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="respectCase">if set to <c>true</c> [respect case].</param>
        /// <returns>
        ///   <c>true</c> if the specified jo contains property; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsProperty(JObject jo, string propertyName, bool respectCase = true)
        {
            return GetProperty(jo, propertyName, respectCase) != null;
        }

        /// <summary>
        /// Tries the get property from json file.
        /// </summary>
        /// <typeparam name="T">The type to which to cast the value, if found</typeparam>
        /// <param name="filepath">The filepath.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="val">The value.</param>
        /// <returns>
        ///   <see cref="bool"/>
        /// </returns>
        public static bool TryGetPropertyFromJSONFile<T>(string filepath, string propertyPath, ref T val)
        {
            try
            {
                string fileText = File.ReadAllText(filepath);
                JObject jo = JObject.Parse(fileText);
                var token = jo.SelectToken(propertyPath);
                var tokenValue = token.ToObject<T>();
                if (tokenValue == null)
                {
                    return false;
                }
                val = tokenValue;
            }
            catch
            {
                throw;   // simple for now.
            }

            return true;
        }

        /// <summary>
        /// If the converter of the same type as jc is not already in the list, insert it at the preferred index (or at
        /// the end of the list if the index is too large).
        /// </summary>
        /// <param name="currentList">The current list.</param>
        /// <param name="jc">The jc.</param>
        /// <param name="preferredIndex">Index of the preferred.</param>
        /// <returns>
        /// true if the converter was added to the list
        /// </returns>
        private static bool PlaceConverterAtIndex(IList<JsonConverter> currentList, JsonConverter jc, int preferredIndex)
        {
            return PlaceConverterAtIndex(currentList, jc, preferredIndex, null);
        }

        /// <summary>
        /// If the converter of the same type as jc is not already in the list, insert it at the preferred index (or at
        /// the end of the list if the index is too large).  Additionally, if instances of any of the types named in
        /// unlessClassPresent are already present in currentList, never add jc and return false.
        /// </summary>
        /// <param name="currentList">The current list.</param>
        /// <param name="jc">The jc.</param>
        /// <param name="preferredIndex">Index of the preferred.</param>
        /// <param name="unlessClassPresent">The unless class present.</param>
        /// <returns>
        /// true if the converter was added to the list
        /// </returns>
        private static bool PlaceConverterAtIndex(IList<JsonConverter> currentList, JsonConverter jc, int preferredIndex, IList<string> unlessClassPresent)
        {
            // TODO: test for converter in unless list.

            // Are there any classes whose presence in the converter list which should preclude this addition?
            bool checkingUnlessList = (unlessClassPresent != null) && (unlessClassPresent.Count > 0);

            string jcFullName = jc.GetType().FullName;
            for (int i = 0; i < currentList.Count; i++)
            {
                string nextType = currentList[i].GetType().FullName;
                if (nextType == jcFullName)
                {
                    return false; // already in the list
                }
                if (checkingUnlessList)
                {
                    // DebugHelpers.WriteLine("Checking list against type: '{0}'", nextType);
                    if (unlessClassPresent.Contains(nextType))
                    {
                        return false; // Precluding class found.
                    }
                }
            }

            // else, we didn't find one of the same type.  So insert this one at it's
            // preferred index (or at the end if it's a short list)
            if (preferredIndex >= currentList.Count)
            {
                currentList.Add(jc);
            }
            else
            {
                currentList.Insert(preferredIndex, jc);
            }
            return true;
        }
    }
}
