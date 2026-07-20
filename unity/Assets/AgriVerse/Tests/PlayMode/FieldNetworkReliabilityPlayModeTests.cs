using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AgriVerse.Client.Tests
{
    public sealed class FieldNetworkReliabilityPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            DestroyPersistentObjects();
            yield return null;
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator OfflineLaunchRecoversAllScenarioConsumersThroughRetry()
        {
            LogAssert.ignoreFailingMessages = true;
            int port = ReserveFreePort();
            string baseUrl = "http://127.0.0.1:" + port;
            GameObject root =
                new GameObject("FieldNetworkRecoveryTest");
            InvestigationController investigation =
                root.AddComponent<InvestigationController>();
            investigation.ConfigureEndpointsForTesting(
                baseUrl,
                baseUrl);
            investigation.ConfigurePresentation(
                createUi: false,
                createMarkers: false);
            InterviewController interviews =
                root.AddComponent<InterviewController>();
            interviews.ConfigureEndpointsForTesting(
                baseUrl,
                baseUrl);
            interviews.ConfigurePresentation(
                createMarkers: false,
                activateAutomatically: false);
            PlanController plan =
                root.AddComponent<PlanController>();
            plan.ConfigureEndpointsForTesting(
                baseUrl,
                baseUrl);
            EpisodePresentationController presentation =
                root.AddComponent<EpisodePresentationController>();

            yield return WaitFor(
                () =>
                    investigation.LoadState ==
                        InvestigationLoadState.Failed &&
                    interviews.LoadState ==
                        InvestigationLoadState.Failed &&
                    plan.LoadState ==
                        InvestigationLoadState.Failed &&
                    presentation
                        .ConnectionStatusTextForTesting
                        .Contains(
                            "FIELD NETWORK OFFLINE"),
                10f,
                "The closed port did not produce the expected offline state.");

            Assert.That(
                presentation.ConnectionStatusTextForTesting,
                Does.Contain("FIELD NETWORK OFFLINE"));
            Assert.That(
                presentation.FieldNetworkPinCountForTesting,
                Is.EqualTo(5));
            Assert.That(
                presentation.RetryVisibleForTesting,
                Is.True);

            ScenarioDto scenario =
                RecoveryScenario();
            Assert.That(
                presentation.SelectFieldLocationForTesting(
                    scenario.id),
                Is.True);
            presentation.SetPlayerNameForTesting("Lan");
            Assert.That(
                presentation.MissionStartInteractableForTesting,
                Is.False);

            using (var server =
                   new LocalScenarioServer(
                       port,
                       JsonUtility.ToJson(scenario)))
            {
                server.Start();
                presentation.RetryConnectionForTesting();
                yield return WaitFor(
                    () =>
                        investigation.LoadState ==
                            InvestigationLoadState.Ready &&
                        interviews.LoadState ==
                            InvestigationLoadState.Ready &&
                        plan.LoadState ==
                            InvestigationLoadState.Ready &&
                        !presentation
                            .ConnectionStatusVisibleForTesting,
                    15f,
                    "Retry did not restore every scenario consumer.");
            }

            Assert.That(
                presentation.SelectedFieldLocationIdForTesting,
                Is.EqualTo(scenario.id));
            Assert.That(
                presentation.PlayerNameForTesting,
                Is.EqualTo("Lan"));
            Assert.That(
                presentation.MissionStartInteractableForTesting,
                Is.True);

            Object.Destroy(root);
            yield return null;
        }

        private static IEnumerator WaitFor(
            Func<bool> condition,
            float timeout,
            string failure)
        {
            float deadline =
                Time.realtimeSinceStartup + timeout;
            while (!condition() &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, failure);
        }

        private static int ReserveFreePort()
        {
            var listener =
                new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port =
                ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static ScenarioDto RecoveryScenario() =>
            new ScenarioDto
            {
                id = "vietnam-mekong-salinity",
                title =
                    "Saving a Mekong Delta farming community",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Mekong Delta",
                    env_asset = "mekong_delta_3d"
                },
                test_sites = new[]
                {
                    new TestSiteDto
                    {
                        id = "site",
                        label = "Test site"
                    }
                },
                stakeholders = new[]
                {
                    new StakeholderDto
                    {
                        id = "person",
                        name = "Stakeholder",
                        role = "Farmer",
                        persona = "Public persona"
                    }
                },
                interventions = new[]
                {
                    new InterventionDto
                    {
                        id = "intervention",
                        label = "Intervention"
                    }
                }
            };

        private static void DestroyPersistentObjects()
        {
            foreach (EpisodeSession session in
                     Object.FindObjectsByType<EpisodeSession>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }
            foreach (EvidenceNotebookSession session in
                     Object.FindObjectsByType<EvidenceNotebookSession>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }
            foreach (InterviewNotebookSession session in
                     Object.FindObjectsByType<InterviewNotebookSession>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }
            foreach (PlanSession session in
                     Object.FindObjectsByType<PlanSession>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }
            foreach (RuntimePanelManager manager in
                     Object.FindObjectsByType<RuntimePanelManager>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(manager.gameObject);
            }
            foreach (EventSystem eventSystem in
                     Object.FindObjectsByType<EventSystem>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(eventSystem.gameObject);
            }
        }

        private sealed class LocalScenarioServer :
            IDisposable
        {
            private readonly TcpListener listener;
            private readonly byte[] body;
            private Thread thread;
            private volatile bool running;

            internal LocalScenarioServer(
                int port,
                string json)
            {
                listener =
                    new TcpListener(
                        IPAddress.Loopback,
                        port);
                body = Encoding.UTF8.GetBytes(json);
            }

            internal void Start()
            {
                listener.Start();
                running = true;
                thread = new Thread(Serve)
                {
                    IsBackground = true,
                    Name = "AgriVerse scenario test server"
                };
                thread.Start();
            }

            private void Serve()
            {
                while (running)
                {
                    try
                    {
                        using (TcpClient client =
                               listener.AcceptTcpClient())
                        using (NetworkStream stream =
                               client.GetStream())
                        using (var reader =
                               new StreamReader(
                                   stream,
                                   Encoding.ASCII,
                                   false,
                                   1024,
                                   true))
                        {
                            string line;
                            do
                            {
                                line = reader.ReadLine();
                            }
                            while (!string.IsNullOrEmpty(line));

                            byte[] header =
                                Encoding.ASCII.GetBytes(
                                    "HTTP/1.1 200 OK\r\n" +
                                    "Content-Type: application/json\r\n" +
                                    "Content-Length: " +
                                    body.Length +
                                    "\r\n" +
                                    "Connection: close\r\n\r\n");
                            stream.Write(
                                header,
                                0,
                                header.Length);
                            stream.Write(
                                body,
                                0,
                                body.Length);
                            stream.Flush();
                        }
                    }
                    catch (SocketException)
                    {
                        if (running) throw;
                    }
                    catch (ObjectDisposedException)
                    {
                        if (running) throw;
                    }
                }
            }

            public void Dispose()
            {
                running = false;
                listener.Stop();
                thread?.Join(1000);
            }
        }
    }
}
