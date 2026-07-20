using System;
using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Loads the public scenario identity packaged with the player. This snapshot is
    /// sufficient to keep the field-network atlas explorable while the mission service
    /// is offline; scored play still requires a fresh sanitized backend response.
    /// </summary>
    public static class PackagedScenarioLoader
    {
        private const string ResourceName = "ScenarioLandingSnapshot";

        public static bool TryLoad(
            out ScenarioDto scenario,
            out string error)
        {
            scenario = null;
            error = string.Empty;
            TextAsset snapshot =
                Resources.Load<TextAsset>(ResourceName);
            if (snapshot == null)
            {
                error =
                    "The packaged public scenario snapshot is missing.";
                return false;
            }

            try
            {
                scenario = ScenarioDto.FromJson(snapshot.text);
            }
            catch (FormatException exception)
            {
                error =
                    "The packaged public scenario snapshot is invalid: " +
                    exception.Message;
                return false;
            }

            if (scenario.location == null ||
                string.IsNullOrWhiteSpace(
                    scenario.location.country) ||
                string.IsNullOrWhiteSpace(
                    scenario.location.region))
            {
                scenario = null;
                error =
                    "The packaged public scenario has no field location.";
                return false;
            }

            return true;
        }

        internal static ScenarioDto LoadRequired()
        {
            if (TryLoad(
                    out ScenarioDto scenario,
                    out string error))
            {
                return scenario;
            }

            throw new InvalidOperationException(error);
        }
    }
}
