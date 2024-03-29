﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Security.Messaging.SecurityMessages;
using SenseNet.Diagnostics;
// ReSharper disable ArrangeStaticMemberQualifier

namespace SenseNet.Security.Messaging
{
    internal static class SecurityActivityQueue
    {
        internal static int SecurityActivityLoadingBufferSize = 200;

        internal static void HealthCheck()
        {
            if (IsWorking())
            {
                SnTrace.Security.Write("SAQ: Health check triggered but ignored.");
                return;
            }
            SnTrace.Security.Write("SAQ: Health check triggered.");

            var state = TerminationHistory.GetCurrentState();
            var gapsLength = state.Gaps.Length;
            if (gapsLength > 0)
            {
                SnTrace.SecurityQueue.Write("SAQ: Health checker is processing {0} gap{1}.", gapsLength, gapsLength > 1 ? "s" : "");

                var notLoaded = state.Gaps.ToList();
                foreach (var activity in new SecurityActivityLoader(state.Gaps, false))
                {
                    SecurityActivityQueue.ExecuteActivity(activity);
                    // memorize executed
                    notLoaded.Remove(activity.Id);
                }
                // forget not loaded activities.
                TerminationHistory.RemoveFromGaps(notLoaded);
            }

            var lastId = TerminationHistory.GetLastTerminatedId();
            var lastDbId = DataHandler.GetLastSecurityActivityId(SecurityContext.StartedAt);
            if (lastId < lastDbId)
            {
                SnTrace.SecurityQueue.Write("SAQ: Health checker is processing activities from {0} to {1}", lastId + 1, lastDbId);
                foreach (var activity in new SecurityActivityLoader(lastId + 1, lastDbId, false))
                    SecurityActivityQueue.ExecuteActivity(activity);
            }
        }
        public static bool IsWorking()
        {
            return !(Serializer.IsEmpty && DependencyManager.IsEmpty);
        }

        internal static void Startup(CompletionState uncompleted, int lastActivityIdFromDb)
        {
            CommunicationMonitor.Stop();

            Serializer.Reset();
            DependencyManager.Reset();
            TerminationHistory.Reset(uncompleted.LastActivityId, uncompleted.Gaps);
            Serializer.Start(lastActivityIdFromDb, uncompleted.LastActivityId, uncompleted.Gaps);

            CommunicationMonitor.Start();
        }

        internal static void Shutdown()
        {
            Serializer.Reset();
            DependencyManager.Reset();
            TerminationHistory.Reset(0);
        }

        public static CompletionState GetCurrentCompletionState()
        {
            return TerminationHistory.GetCurrentState();
        }
        public static SecurityActivityQueueState GetCurrentState()
        {
            return new SecurityActivityQueueState
            {
                Serializer = Serializer.GetCurrentState(),
                DependencyManager = DependencyManager.GetCurrentState(),
                Termination = TerminationHistory.GetCurrentState()
            };
        }

        public static void ExecuteActivity(SecurityActivity activity)
        {
            if (!activity.FromDatabase && !activity.FromReceiver)
                DataHandler.SaveActivity(activity);

            Serializer.EnqueueActivity(activity);
        }

        /// <summary>Only for tests</summary>
        internal static void _setCurrentExecutionState(CompletionState state)
        {
            Serializer.Reset(state.LastActivityId);
            DependencyManager.Reset();
            TerminationHistory.Reset(state.LastActivityId, state.Gaps);
        }
        internal static void __enableExecution()
        {
            Executor.__enable();
        }
        internal static void __disableExecution()
        {
            Executor.__disable();
        }
        internal static SecurityActivity[] __getWaitingSet()
        {
            return DependencyManager.__getWaitingSet();
        }

        //============================================================== subclasses

        private static class Serializer
        {
            internal static void Reset(int lastQueued = 0)
            {
                lock (_arrivalQueueLock)
                {
                    SnTrace.SecurityQueue.Write("SAQ: RESET: ArrivalQueue.Count: {0}", _arrivalQueue.Count);
                    foreach (var activity in _arrivalQueue)
                        activity.Finish();
                    _arrivalQueue.Clear();
                    _lastQueued = lastQueued;
                }
            }
            /// <summary>
            /// MUST BE SYNCHRON
            /// GAPS MUST BE ORDERED
            /// </summary>
            internal static void Start(int lastDatabaseId, int lastExecutedId, int[] gaps)
            {
                var hasUnprocessed = gaps.Length > 0 || lastDatabaseId != lastExecutedId;

                SnLog.WriteInformation(EventMessage.Information.StartTheSystem, EventId.RepositoryLifecycle,
                    // ReSharper disable once ArgumentsStyleOther
                    properties: new Dictionary<string, object>{
                        {"LastDatabaseId", lastDatabaseId},
                        {"LastExecutedId", lastExecutedId},
                        {"CountOfGaps", gaps.Length},
                        {"Gaps", string.Join(", ", gaps)}
                    });

                DependencyManager.Start();

                var count = 0;
                if (gaps.Any())
                {
                    var loadedActivities = new SecurityActivityLoader(gaps, true);
                    foreach (var loadedActivity in loadedActivities)
                    {
                        SnTrace.SecurityQueue.Write("SAQ: Startup: SA{0} enqueued from db.", loadedActivity.Id);

                        SecurityActivityHistory.Arrive(loadedActivity);
                        _arrivalQueue.Enqueue(loadedActivity);
                        _lastQueued = loadedActivity.Id;
                        count++;
                    }
                }
                if (lastExecutedId < lastDatabaseId)
                {
                    var loadedActivities = new SecurityActivityLoader(lastExecutedId + 1, lastDatabaseId, true);
                    foreach (var loadedActivity in loadedActivities)
                    {
                        SnTrace.SecurityQueue.Write("SAQ: Startup: SA{0} enqueued from db.", loadedActivity.Id);
                        SecurityActivityHistory.Arrive(loadedActivity);
                        SnTrace.SecurityQueue.Write("SecurityActivityArrived SA{0}", loadedActivity.Id);
                        _arrivalQueue.Enqueue(loadedActivity);
                        _lastQueued = loadedActivity.Id;
                        count++;
                    }
                }

                if (_lastQueued < lastExecutedId)
                    _lastQueued = lastExecutedId;

                // ensure that the arrival activity queue is not empty at this pont.
                DependencyManager.ActivityEnqueued();

                if (lastDatabaseId != 0 || lastExecutedId != 0 || gaps.Any())
                    while (IsWorking())
                        Thread.Sleep(200);

                if (hasUnprocessed)
                    SnLog.WriteInformation(string.Format(EventMessage.Information.ExecutingUnprocessedActivitiesFinished, count),
                        EventId.RepositoryLifecycle);
            }

            internal static bool IsEmpty => _arrivalQueue.Count == 0;

            private static readonly object _arrivalQueueLock = new object();
            private static int _lastQueued;
            private static readonly Queue<SecurityActivity> _arrivalQueue = new Queue<SecurityActivity>();

            public static void EnqueueActivity(SecurityActivity activity)
            {
                SnTrace.SecurityQueue.Write("SAQ: SA{0} arrived{1}. {2}", activity.Id, activity.FromReceiver ? " from another computer" : "", activity.TypeName);

                SecurityActivityHistory.Arrive(activity);

                lock (_arrivalQueueLock)
                {
                    if (activity.Id <= _lastQueued)
                    {
                        var sameActivity = _arrivalQueue.FirstOrDefault(a => a.Id == activity.Id);
                        if (sameActivity != null)
                        {
                            sameActivity.Attach(activity);
                            SnTrace.SecurityQueue.Write("SAQ: SA{0} attached to another one in the queue", activity.Id);
                            return;
                        }
                        DependencyManager.AttachOrFinish(activity);
                        return;
                    }

                    if (activity.Id > _lastQueued + 1)
                    {
                        //var loadedActivities = LoadActivities(_lastQueued + 1, activity.Id - 1);
                        var from = _lastQueued + 1;
                        var to = activity.Id - 1;
                        var expectedCount = to - from + 1;
                        var loadedActivities = Retrier.Retry(
                            3,
                            100,
                            () => LoadActivities(from, to),
                            (r, i, e) =>
                            {
                                if (i < 3)
                                    SnTrace.SecurityQueue.Write("SAQ: Loading attempt {0}", 4 - i);
                                if (e != null)
                                    return false;
                                return r.Count() == expectedCount;
                            });

                        foreach (var loadedActivity in loadedActivities)
                        {
                            SecurityActivityHistory.Arrive(loadedActivity);
                            _arrivalQueue.Enqueue(loadedActivity);
                            _lastQueued = loadedActivity.Id;
                            SnTrace.SecurityQueue.Write("SAQ: SA{0} enqueued from db.", loadedActivity.Id);
                            DependencyManager.ActivityEnqueued();
                        }
                    }
                    _arrivalQueue.Enqueue(activity);
                    _lastQueued = activity.Id;
                    SnTrace.SecurityQueue.Write("SAQ: SA{0} enqueued.", activity.Id);
                    DependencyManager.ActivityEnqueued();
                }
            }
            public static SecurityActivity DequeueActivity()
            {
                lock (_arrivalQueueLock)
                {
                    if (_arrivalQueue.Count == 0)
                        return null;
                    var activity = _arrivalQueue.Dequeue();
                    SnTrace.SecurityQueue.Write("SAQ: SA{0} dequeued.", activity.Id);
                    return activity;
                }
            }

            private static IEnumerable<SecurityActivity> LoadActivities(int from, int to)
            {
                SnTrace.SecurityQueue.Write("SAQ: Loading activities {0} - {1}", from, to);
                return new SecurityActivityLoader(from, to, false);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static SecurityActivitySerializerState GetCurrentState()
            {
                lock (_arrivalQueueLock)
                    return new SecurityActivitySerializerState
                    {
                        LastQueued = _lastQueued,
                        Queue = _arrivalQueue.Select(x => x.Id).ToArray()
                    };
            }
        }

        private static class DependencyManager
        {
            internal static void Reset()
            {
                // Before call ensure that the arrival queue is empty.
                lock (_waitingSetLock)
                {
                    if (_waitingSet.Count > 0)
                        SnTrace.SecurityQueue.Write("SAQ: RESET: WaitingSet.Count: {0}", _waitingSet.Count);

                    foreach (var activity in _waitingSet)
                        activity.Finish();
                    _waitingSet.Clear();
                }
            }
            internal static void Start()
            {
                lock (_waitingSetLock)
                    _waitingSet.Clear();
            }
            internal static bool IsEmpty => _waitingSet.Count == 0;

            private static readonly object _waitingSetLock = new object();
            private static readonly List<SecurityActivity> _waitingSet = new List<SecurityActivity>();

            private static bool _run;
            public static void ActivityEnqueued()
            {
                if (_run)
                    return;
                _run = true;
                Task.Run(() => ProcessActivities());
            }

            private static void ProcessActivities()
            {
                while (true)
                {
                    var newerActivity = Serializer.DequeueActivity();
                    if (newerActivity == null)
                    {
                        _run = false;
                        return;
                    }
                    MakeDependencies(newerActivity);
                }
            }
            private static void MakeDependencies(SecurityActivity newerActivity)
            {
                lock (_waitingSetLock)
                {
                    foreach (var olderActivity in _waitingSet)
                    {
                        Debug.Assert(olderActivity.Id != newerActivity.Id);
                        if (newerActivity.MustWaitFor(olderActivity))
                        {
                            newerActivity.WaitFor(olderActivity);
                            SnTrace.SecurityQueue.Write("SAQ: SA{0} depends from SA{1}", newerActivity.Id, olderActivity.Id);
                            SecurityActivityHistory.Wait(newerActivity);
                        }
                    }

                    _waitingSet.Add(newerActivity);

                    if (newerActivity.WaitingFor.Count == 0)
                        Task.Run(() => Executor.Execute(newerActivity));
                }
            }

            internal static void Finish(SecurityActivity activity)
            {
                lock (_waitingSetLock)
                {
                    // activity is done in the ActivityQueue
                    _waitingSet.Remove(activity);

                    // terminate and release waiting threads if there is any.
                    activity.Finish();

                    // register activity termination in the log.
                    SecurityActivityHistory.Finish(activity.Id);

                    // register activity termination.
                    TerminationHistory.FinishActivity(activity);

                    // execute all activities that are completely freed.
                    foreach (var dependentItem in activity.WaitingForMe.ToArray())
                    {
                        dependentItem.FinishWaiting(activity);
                        if (dependentItem.WaitingFor.Count == 0)
                            Task.Run(() => Executor.Execute(dependentItem));
                    }
                }
            }
            internal static void AttachOrFinish(SecurityActivity activity)
            {
                lock (_waitingSetLock)
                {
                    var sameActivity = _waitingSet.FirstOrDefault(a => a.Id == activity.Id);
                    if (sameActivity != null)
                    {
                        sameActivity.Attach(activity);
                        SnTrace.SecurityQueue.Write("SAQ: SA{0} attached to another in the waiting set.", activity.Id);
                        return;
                    }
                }
                activity.Finish(); // release blocked thread
                SecurityActivityHistory.Finish(activity.Id);
                SnTrace.SecurityQueue.Write("SAQ: SA{0} ignored: finished but not executed.", activity.Id);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static SecurityActivityDependencyState GetCurrentState()
            {
                lock (_waitingSetLock)
                    return new SecurityActivityDependencyState { WaitingSet = _waitingSet.Select(x => x.Id).ToArray() };
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static SecurityActivity[] __getWaitingSet()
            {
                lock (_waitingSetLock)
                    return _waitingSet.ToArray();
            }
        }

        private static class TerminationHistory
        {
            private static readonly object _gapsLock = new object();
            private static int _lastId;
            private static readonly List<int> _gaps = new List<int>();

            internal static void Reset(int lastId, IEnumerable<int> gaps = null)
            {
                lock (_gapsLock)
                {
                    _lastId = lastId;
                    _gaps.Clear();
                    if (gaps != null)
                        _gaps.AddRange(gaps);
                }
            }

            internal static void FinishActivity(SecurityActivity activity)
            {
                var id = activity.Id;
                lock (_gapsLock)
                {
                    if (id > _lastId)
                    {
                        if (id > _lastId + 1)
                            _gaps.AddRange(Enumerable.Range(_lastId + 1, id - _lastId - 1));
                        _lastId = id;
                    }
                    else
                    {
                        _gaps.Remove(id);
                    }
                    SnTrace.SecurityQueue.Write("SAQ: State after finishing SA{0}: {1}", id, GetCurrentState());
                }
            }
            public static int GetLastTerminatedId()
            {
                return _lastId;
            }
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static CompletionState GetCurrentState()
            {
                lock (_gapsLock)
                    return new CompletionState { LastActivityId = _lastId, Gaps = _gaps.ToArray() };
            }

            internal static void RemoveFromGaps(IEnumerable<int> notLoaded)
            {
                lock (_gapsLock)
                    foreach (var item in notLoaded)
                        _gaps.Remove(item);
            }
        }

        private static class Executor
        {
            private static bool _enabled = true;
            internal static void __enable()
            {
                _enabled = true;
            }
            internal static void __disable()
            {
                _enabled = false;
            }
            public static void Execute(SecurityActivity activity)
            {
                if (!_enabled)
                    return;

                SecurityActivityHistory.Start(activity.Id);
                try
                {
                    using (var op = SnTrace.SecurityQueue.StartOperation("SAQ: EXECUTION START SA{0} .", activity.Id))
                    {
                        activity.ExecuteInternal();
                        op.Successful = true;
                    }
                }
                catch (Exception e)
                {
                    SnTrace.Security.Write("SAQ: EXECUTION ERROR SA{0}: {1}", activity.Id, e.Message);
                    SecurityActivityHistory.Error(activity.Id, e);
                }
                finally
                {
                    DependencyManager.Finish(activity);
                }
            }
        }
    }

    internal class Retrier
    {
        public static T Retry<T>(int count, int waitMilliseconds, Func<T> callback, Func<T, int, Exception, bool> expectation)
        {
            var retryCount = count;
            var result = default(T);
            while (retryCount > 0)
            {
                Exception error = null;
                try
                {
                    result = callback();
                }
                catch (Exception e)
                {
                    error = e;
                }

                if (expectation(result, retryCount, error))
                    break;
                retryCount--;
                Thread.Sleep(waitMilliseconds);
            }
            return result;
        }
    }

}
