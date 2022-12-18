using Prism.Events;

namespace Updater.GitHub.Events
{
    public class ReleaseEvents
    {
        public class ReleaseInfoUpdatedEvent : PubSubEvent { }
        public class ReleaseExtractedEvent : PubSubEvent { }
    }
}