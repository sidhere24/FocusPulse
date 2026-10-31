using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace FocusPulse
{
    public class SyncClient
    {
        private readonly string _endpoint;
        private readonly int _timeoutMs;
        private readonly JavaScriptSerializer _serializer;

        public bool IsConnected { get; private set; }
        public string NodeVersion { get; private set; }
        public string HostIdentifier { get; private set; }

        public string ActivityChannelId { get; private set; }
        public string IdleChannelId { get; private set; }

        public SyncClient(int port = 5600, int timeoutMs = 2000)
        {
            _endpoint = string.Format("http://127.0.0.1:{0}/api/0", port);
            _timeoutMs = timeoutMs;
            _serializer = new JavaScriptSerializer();
            _serializer.MaxJsonLength = 50 * 1024 * 1024;
            IsConnected = false;
        }

        public bool ProbeLocalNode()
        {
            try
            {
                string url = _endpoint + "/info";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = _timeoutMs;
                req.ReadWriteTimeout = _timeoutMs;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            string json = reader.ReadToEnd();
                            Dictionary<string, object> dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                            if (dict != null)
                            {
                                HostIdentifier = dict.ContainsKey("hostname") ? Convert.ToString(dict["hostname"]) : "localhost";
                                NodeVersion = dict.ContainsKey("version") ? Convert.ToString(dict["version"]) : "v1.0";
                                IsConnected = true;
                                DiscoverChannels();
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                IsConnected = false;
            }
            return false;
        }

        private void DiscoverChannels()
        {
            try
            {
                string url = _endpoint + "/buckets/";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = _timeoutMs;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            string json = reader.ReadToEnd();
                            Dictionary<string, object> channels = _serializer.Deserialize<Dictionary<string, object>>(json);
                            if (channels != null)
                            {
                                foreach (KeyValuePair<string, object> kvp in channels)
                                {
                                    string channelKey = kvp.Key;
                                    if (channelKey.IndexOf("window", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        ActivityChannelId = channelKey;
                                    }
                                    else if (channelKey.IndexOf("afk", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        IdleChannelId = channelKey;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public DaySummary QuerySynchronizedMetrics()
        {
            if (!IsConnected || string.IsNullOrEmpty(ActivityChannelId))
            {
                return null;
            }

            try
            {
                DateTime todayStart = DateTime.Today;
                DateTime todayEnd = todayStart.AddDays(1);
                string startIso = todayStart.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                string endIso = todayEnd.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

                string channelUrl = string.Format("{0}/buckets/{1}/events?start={2}&end={3}&limit=10000",
                    _endpoint, ActivityChannelId, Uri.EscapeDataString(startIso), Uri.EscapeDataString(endIso));

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(channelUrl);
                req.Method = "GET";
                req.Timeout = 4000;

                DaySummary summary = new DaySummary();
                summary.Date = todayStart.ToString("yyyy-MM-dd");

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        object[] events = _serializer.Deserialize<object[]>(json);
                        if (events != null)
                        {
                            foreach (object evObj in events)
                            {
                                Dictionary<string, object> ev = evObj as Dictionary<string, object>;
                                if (ev == null) continue;

                                double dur = 0;
                                if (ev.ContainsKey("duration"))
                                {
                                    dur = Convert.ToDouble(ev["duration"]);
                                }

                                if (ev.ContainsKey("data"))
                                {
                                    Dictionary<string, object> data = ev["data"] as Dictionary<string, object>;
                                    if (data != null && data.ContainsKey("app"))
                                    {
                                        string app = Convert.ToString(data["app"]);
                                        if (!string.IsNullOrEmpty(app))
                                        {
                                            summary.TotalActiveSeconds += dur;
                                            double currentAppSec = 0;
                                            if (summary.AppDurations.TryGetValue(app, out currentAppSec))
                                            {
                                                summary.AppDurations[app] = currentAppSec + dur;
                                            }
                                            else
                                            {
                                                summary.AppDurations[app] = dur;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return summary;
            }
            catch
            {
                return null;
            }
        }
    }
}
