#if !NETSTANDARD1_4
// The MIT License (MIT)
// 
// Copyright (c) 2015 Microsoft
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Microsoft.Diagnostics.Tracing.Logging.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Diagnostics.Tracing.Parsers;

    /// <summary>
    /// The possible types of events that can be read.
    /// </summary>
    [Flags]
    public enum EventTypes
    {
        None = 0x0,

        /// <summary>
        /// Events generated by EventSource-based classes.
        /// </summary>
        EventSource = 0x1,

        /// <summary>
        /// Events generated by the CLR.
        /// </summary>
        Clr = 0x2,

        /// <summary>
        /// Events generated by the NT kernel.
        /// </summary>
        Kernel = 0x4,

        /// <summary>
        /// Events generated by any registered provider on the host system.
        /// </summary>
        Registered = 0x8,

        /// <summary>
        /// Events of unknown origin (raw events).
        /// WARNING: Using this filter will disable queueing of unknown events with pending EventSource
        /// manifests. Use with caution.
        /// </summary>
        Unknown = 0x16,

        /// <summary>
        /// All canonical supported event types.
        /// </summary>
        All = (EventSource | Clr | Kernel | Registered)
    }

    /// <summary>
    /// Base class for processing ETW events (from an ETWTraceEventSource)
    /// </summary>
    /// <remarks>
    /// By default the reader handles only EventSource types, however CLR and Kernel events may also be read by
    /// modifying the <see cref="ProcessEventTypes">property</see>.
    /// </remarks>
    public abstract class ETWProcessor : IDisposable
    {
        /// <summary>
        /// Delegate called when a (known) event is processed.
        /// </summary>
        /// <param name="ev"></param>
        public delegate void EventProcessedHandler(ETWEvent ev);

        /// <summary>
        /// Delegate called when a processing session is ended. May be called multiple times when multiple sessions
        /// are processed (e.g. when processing many files).
        /// </summary>
        /// <param name="name">Name of the session. May be a filename or realtime session name.</param>
        /// <param name="end">End time of the session.</param>
        /// <param name="eventCount">Number of total events processed in the session.</param>
        /// <param name="lostEventCount">Number of events lost in the session.</param>
        /// <param name="unreadableEventCount">Number of unreadable events in the session.</param>
        public delegate void SessionEndHandler(string name, DateTime end, long eventCount, long lostEventCount,
                                               long unreadableEventCount);

        /// <summary>
        /// Delegate called when a processing session is started. May be called multiple times when multiple sessions
        /// are processed (e.g. when processing many files).
        /// </summary>
        /// <param name="name">Name of the session. May be a filename or realtime session name.</param>
        /// <param name="start">Start time of the session.</param>
        public delegate void SessionStartHandler(string name, DateTime start);

        /// <summary>
        /// Default time to keep buffer unknown events for while waiting for an EventSource manifest.
        /// </summary>
        public static readonly TimeSpan DefaultBufferTimeForUnknownEvents = new TimeSpan(0, 0, 5);

        /// <summary>
        /// Should be set to the name of the current session (filename or realtime session name).
        /// </summary>
        protected string CurrentSessionName;

        /// <summary>
        /// Control for the maximum amount of time to buffer unknown events while waiting for an EventSource manifest.
        /// A span of <see cref="TimeSpan.Zero"/> (no time) means no buffering will be performed. Negative timespans
        /// will also result in no buffering.
        /// </summary>
        /// <remarks>
        /// By default the ETWProcessor will buffer unknown events if it is asked to listen to Dynamic (EventSource)
        /// types of providers. The idea is that the manifest for the EventSource may show up somewhat later in a
        /// stream, perhaps in a separate buffer from the earlier events. This can be seen in high-volume logs created
        /// by the library, where the manifest emission may be preceded by one or more buffers' worth of data.
        ///
        /// The default value will allow for this condition to occur while tolerating a fairly large timespan, relative
        /// to the standard flush frequency of file-backed ETW data.
        /// </remarks>
        public TimeSpan MaximumBufferTimeForUnknownEvents = DefaultBufferTimeForUnknownEvents;

        protected bool Processing;

        /// <summary>
        /// Should be set to the currently active TraceEventSource by the actual processor.
        /// </summary>
        protected ETWTraceEventSource TraceEventSource;

        protected Dictionary<Guid, Queue<TraceEvent>> UnhandledEvents;

        /// <summary>
        /// Total number of events lost.
        /// </summary>
        public long EventsLost { get; protected set; }

        /// <summary>
        /// Total number of events which were not readable
        /// </summary>
        /// <remarks>
        /// An event may not be readable for a variety of reasons, including having a malformed record or
        /// containing data not supported by the TraceEvent library.
        /// </remarks>
        public long UnreadableEvents { get; protected set; }

        /// <summary>
        /// Get the number of valid events read.
        /// </summary>
        public long Count { get; protected set; }

        /// <summary>
        /// The types of events that should be extracted from the provided files.
        /// </summary>
        public EventTypes ProcessEventTypes { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Begin processing events.
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Stop processing events.
        /// </summary>
        public virtual void StopProcessing()
        {
            if (this.TraceEventSource != null)
            {
                this.TraceEventSource.StopProcessing();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.TraceEventSource != null && disposing)
            {
                this.StopProcessing();
                this.TraceEventSource.Dispose();
                this.TraceEventSource = null;
            }
        }

        /// <summary>
        /// Called when the processing session is started. May be called multiple times.
        /// </summary>
        public event SessionStartHandler SessionStart;

        /// <summary>
        /// Called when the processing session is ended. May be called multiple times.
        /// </summary>
        public event SessionEndHandler SessionEnd;

        /// <summary>
        /// Triggered when an event is processed.
        /// </summary>
        public event EventProcessedHandler EventProcessed;

        /// <summary>
        /// Adds the appropriate handlers to <see cref="TraceEventSource"/> based on the value of
        /// <see cref="ProcessEventTypes"/> and processes the events in the source.
        /// </summary>
        protected void ProcessEvents()
        {
            if (this.ProcessEventTypes.HasFlag(EventTypes.Registered))
            {
                var registeredEventParser = new RegisteredTraceEventParser(this.TraceEventSource);
                registeredEventParser.All += this.ProcessEvent;
            }

            if (this.ProcessEventTypes.HasFlag(EventTypes.EventSource))
            {
                // We don't pre-process to get manifests, instead we hold onto unhandled events until we see their
                // provider get activated, then we attempt to re-process them. This sort of goofs up ordering, but
                // ETW wasn't reliably ordered to begin with. In general the expectation is that the manifests will
                // show up near the initial events so these queues are not expected to grow substantially.
                // We can only do this if the user isn't asking for unknown events, however.
                if (!this.ProcessEventTypes.HasFlag(EventTypes.Unknown))
                {
                    this.TraceEventSource.UnhandledEvents += this.ProcessUnhandledEvent;
                    this.UnhandledEvents = new Dictionary<Guid, Queue<TraceEvent>>();
                }

                this.TraceEventSource.Dynamic.All += this.ProcessEvent;
            }

            if (this.ProcessEventTypes.HasFlag(EventTypes.Clr))
            {
                this.TraceEventSource.Clr.All += this.ProcessEvent;
            }

            if (this.ProcessEventTypes.HasFlag(EventTypes.Kernel))
            {
                this.TraceEventSource.Kernel.All += this.ProcessEvent;
            }

            if (this.ProcessEventTypes.HasFlag(EventTypes.Unknown))
            {
                this.MaximumBufferTimeForUnknownEvents = TimeSpan.Zero;
                this.TraceEventSource.UnhandledEvents += this.ProcessEvent;
            }

            this.OnSessionStart(this.CurrentSessionName, this.TraceEventSource.SessionStartTime);
            long beginCount = this.Count;
            long beginUnreadable = this.UnreadableEvents;

            this.TraceEventSource.Process();

            this.EventsLost += this.TraceEventSource.EventsLost;

            // Calculate the number of events we never managed to read.
            if (this.UnhandledEvents != null)
            {
                foreach (var unhandledList in this.UnhandledEvents.Values)
                {
                    this.UnreadableEvents += unhandledList.Count;
                }
            }

            // If the session is realtime then the end time will basically be nonsense, but we can pretty
            // reasonably infer it just ended right now.
            this.OnSessionEnd(this.CurrentSessionName,
                              (this.TraceEventSource.SessionEndTime == DateTime.MaxValue
                                   ? DateTime.Now
                                   : this.TraceEventSource.SessionEndTime),
                              this.Count - beginCount, this.TraceEventSource.EventsLost,
                              this.UnreadableEvents - beginUnreadable);
        }

        /// <summary>
        /// Trigger EventProcessed. The base processor will do this with any actual session, this is primarily intended
        /// for mocking a processor.
        /// </summary>
        /// <param name="ev">The event to inject.</param>
        protected void OnEvent(ETWEvent ev)
        {
            this.EventProcessed(ev);
        }

        /// <summary>
        /// Trigger SessionStart. The base processor will do this with any actual session, this is primarily intended
        /// for mocking a processor.
        /// </summary>
        /// <param name="sessionName">Session name.</param>
        /// <param name="sessionStart">Start time of session.</param>
        protected void OnSessionStart(string sessionName, DateTime sessionStart)
        {
            if (this.SessionStart != null)
            {
                this.SessionStart(sessionName, sessionStart);
            }
        }

        /// <summary>
        /// Trigger SessionEnd. The base processor will do this with any actual session, this is primarily intended
        /// for mocking a processor.
        /// </summary>
        /// <param name="sessionName">Session name.</param>
        /// <param name="sessionEnd">End time of session.</param>
        /// <param name="eventCount">Events read in session.</param>
        /// <param name="lostEventCount">Events lost while processing session.</param>
        /// <param name="unreadableEventCount">Events which were unreadable while processing session.</param>
        protected void OnSessionEnd(string sessionName, DateTime sessionEnd, long eventCount, long lostEventCount,
                                    long unreadableEventCount)
        {
            if (this.SessionEnd != null)
            {
                this.SessionEnd(sessionName, sessionEnd, eventCount, lostEventCount, unreadableEventCount);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "TraceEvent throws Exception, can't avoid catching it")]
        private void ProcessEvent(TraceEvent ev)
        {
            ETWEvent processedEntry;
            try
            {
                processedEntry = new ETWEvent(this.TraceEventSource.LogFileName, ev);
                ++this.Count;
            }
            catch (Exception)
            {
                ++this.UnreadableEvents;
                return;
            }

            this.OnEvent(processedEntry);
        }

        private void ProcessUnhandledEvent(TraceEvent ev)
        {
            if (ev.ProviderGuid == Guid.Empty)
            {
                ++this.UnreadableEvents;
                return;
            }

            Queue<TraceEvent> eventList = null;
            if (this.MaximumBufferTimeForUnknownEvents != TimeSpan.Zero)
            {
                this.UnhandledEvents.TryGetValue(ev.ProviderGuid, out eventList);
            }

            if (ev.ID == DynamicTraceEventParser.ManifestEventID)
            {
                // For manifest IDs if we found a provider (meaning this was the final manifest event needed to
                // create the provider) then we can reprocess any unknown events. In any case we do not pass manifest
                // events up to our own users because they are unlikely to be particularly useful on their own -- if
                // in future users want to capture manifests when they are emitted we could trigger a specific event
                // with the full manifest payload here. Nobody needs that today, though.
                if (eventList != null)
                {
                    this.UnhandledEvents.Remove(ev.ProviderGuid);
                    this.ReprocessUnhandledEvents(eventList);
                }
                return;
            }

            if (this.MaximumBufferTimeForUnknownEvents != TimeSpan.Zero)
            {
                if (eventList == null)
                {
                    eventList = new Queue<TraceEvent>();
                    this.UnhandledEvents[ev.ProviderGuid] = eventList;
                }

                while (eventList.Count > 0
                       && ev.TimeStamp - eventList.Peek().TimeStamp > this.MaximumBufferTimeForUnknownEvents)
                {
                    eventList.Dequeue();
                }

                eventList.Enqueue(ev.Clone());
            }
        }
#pragma warning disable 0618
        private void ReprocessUnhandledEvents(IEnumerable<TraceEvent> eventList)
        {
            foreach (var ev in eventList)
            {
                this.TraceEventSource.ReprocessEvent(ev);
            }
        }
#pragma warning restore 0618
    }
}
#endif
