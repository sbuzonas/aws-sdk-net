/*
 * Copyright 2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Amazon.Runtime.Internal
{
    /// <summary>
    /// Class to easily convert from Dictionary&lt;string, string&gt; to ProfileOptions and back.
    /// </summary>
    public class CredentialProfilePropertyMapping
    {
        private static readonly HashSet<string> TypePropertySet =
            new HashSet<string>(typeof(CredentialProfileOptions).GetProperties().Select((p) => p.Name), StringComparer.OrdinalIgnoreCase);

        private static readonly PropertyInfo[] credentialProfileProperties = typeof(CredentialProfileOptions).GetProperties();

        private Dictionary<string, string> nameMapping;
        private HashSet<string> mappedNames;

        public CredentialProfilePropertyMapping(Dictionary<string, string> nameMapping)
        {
            if (!TypePropertySet.SetEquals(new HashSet<string>(nameMapping.Keys, StringComparer.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The nameMapping Dictionary must contain a name mapping for each ProfileOptions property, and no additional keys.");
            }

            this.nameMapping = nameMapping;
            mappedNames = new HashSet<string>(nameMapping.Values.Where(v => !string.IsNullOrEmpty(v)), StringComparer.OrdinalIgnoreCase);
        }

        public List<KeyValuePair<string, string>> Convert(CredentialProfileOptions profileOptions)
        {
            var list = new List<KeyValuePair<string, string>>();
            var properties = typeof(CredentialProfileOptions).GetProperties();

            // ensure repeatable order
            Array.Sort(properties.Select((p)=>p.Name).ToArray(), properties);

            foreach (var property in properties)
            {
                var value = (string)property.GetValue(profileOptions, null);
                if (!string.IsNullOrEmpty(value))
                {
                    if (nameMapping[property.Name] == null)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                              "The CredentialProfileOptions.{0} property is not populated in this CredentialProfilePropertyMapping.",
                              property.Name));
                    }
                    else
                    {
                        list.Add(new KeyValuePair<string, string>(nameMapping[property.Name], value));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Remove keys related to the CredentialsProfileOptions from the dictionary
        /// and create a CredentialsProfileOptions object out of them.
        ///
        /// Warning: This method modifies the contents of the properties dictionary.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public CredentialProfileOptions ExtractCredentialProfileOptions(Dictionary<string, string> properties)
        {
            var profileOptions = new CredentialProfileOptions();

            foreach (var property in credentialProfileProperties)
            {
                string value = null;
                var mappedName = nameMapping[property.Name];
                if (mappedName != null)
                {
                    if (properties.TryGetValue(mappedName, out value))
                    {
                        property.SetValue(profileOptions, value, null);
                    }
                    properties.Remove(mappedName);
                }
            }
            return profileOptions;
        }

        /// <summary>
        /// Make sure the profileProperties dictionary doesn't contain any keys that
        /// overlap with the names of mapped names for CredentialProfileOptions property names.
        /// Check is case-insensitive for added safety.
        /// </summary>
        /// <param name="profileProperties"></param>
        public void ValidateNoProfileOptionsProperties(Dictionary<string, string> profileProperties)
        {
            if (profileProperties != null)
            {
                foreach (var key in profileProperties.Keys)
                {
                    if (mappedNames.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("The profile properties dictionary cannot contain a key named " + key +
                            " because it is in the name mapping dictionary.");
                    }
                }
            }
        }
    }
}
