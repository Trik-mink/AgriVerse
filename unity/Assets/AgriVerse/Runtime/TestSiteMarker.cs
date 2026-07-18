using UnityEngine;

namespace AgriVerse.Client
{
    public sealed class TestSiteMarker : MonoBehaviour
    {
        public string SiteId { get; private set; }

        public void Configure(string siteId)
        {
            SiteId = siteId;
        }
    }
}
