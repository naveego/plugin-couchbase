using System;
using System.Collections.Generic;

namespace PluginCouchbase.Helper
{
    public class Settings
    {
        public List<string> Servers { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if (Servers.Count == 0)
            {
                throw new Exception("the Servers property must be set");
            }

            if (String.IsNullOrEmpty(Username))
            {
                throw new Exception("the Username property must be set");
            }
            
            if (String.IsNullOrEmpty(Password))
            {
                throw new Exception("the Password property must be set");
            }
        }
    }
}