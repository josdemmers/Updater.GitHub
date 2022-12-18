using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.GitHub.Events
{
    public class HttpClientEvents
    {
        public class DownloadProgressUpdatedEvent : PubSubEvent<HttpProgress> { }
        public class UploadProgressUpdatedEvent : PubSubEvent<HttpProgress> { }
        public class DownloadCompletedEvent : PubSubEvent<string> { }

        public class HttpProgress
        {
            public long Bytes { get; set; }
            public int Progress { get; set; }
        }
    }
}
