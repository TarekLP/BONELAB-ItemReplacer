using System;
using System.IO;
using System.Collections.Generic;

using MelonLoader;

namespace ItemReplacer.Utilities
{
    public sealed class UnityFileSystemWatcher : FileSystemWatcher, IDisposable
    {

        public new event EventHandler<RenamedEventArgs> Renamed;

        public new event EventHandler<FileSystemEventArgs> Created, Deleted, Changed;

        public new event EventHandler Disposed;

        public new event EventHandler<ErrorEventArgs> Error;

        private readonly List<EventArgs> _Queue = [];
        public IReadOnlyList<EventArgs> Queue => _Queue.AsReadOnly();

        public UnityFileSystemWatcher()
            => Init();

        public UnityFileSystemWatcher(string path) : base(path)
            => Init();

        public UnityFileSystemWatcher(string path, string filter) : base(path, filter)
            => Init();


        private void Init()
        {
            base.Renamed += (sender, e) => _Queue.Add(e);
            base.Created += (sender, e) => _Queue.Add(e);
            base.Deleted += (sender, e) => _Queue.Add(e);
            base.Changed += (sender, e) => _Queue.Add(e);
            base.Disposed += (sender, e) => _Queue.Add(e);

            base.Error += (sender, e) => _Queue.Add(e);
            MelonEvents.OnUpdate.Subscribe(Update);
        }

        private void Update()
        {
            if (Queue.Count > 0)
            {
                for (int i = Queue.Count - 1; i >= 0; i--)
                {
                    var @event = _Queue[i];
                    try
                    {
                        if (@event is RenamedEventArgs renamed)
                            Renamed?.Invoke(this, renamed);
                        else if (@event is FileSystemEventArgs fse_args)
                            TriggerProperEvent(fse_args);
                        else if (@event is ErrorEventArgs error)
                            Error?.Invoke(this, error);
                        else if (@event is EventArgs args)
                            WatcherDisposed(args);
                    }
                    catch (Exception ex)
                    {
                        ErrorMsg("An unexpected error has occurred while triggering file system watcher events", ex);
                    }
                    finally
                    {
                        _Queue.RemoveAt(i);
                    }
                }
            }
        }

        private void TriggerProperEvent(FileSystemEventArgs args)
        {
            if (args.ChangeType == WatcherChangeTypes.Created)
                Created?.Invoke(this, args);
            else if (args.ChangeType == WatcherChangeTypes.Deleted)
                Deleted?.Invoke(this, args);
            else if (args.ChangeType == WatcherChangeTypes.Changed)
                Changed?.Invoke(this, args);
        }

        private void WatcherDisposed(EventArgs args)
        {
            try
            {
                Disposed?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                ErrorMsg("An unexpected error has occurred while running Disposed event", ex);
            }
            finally
            {
                Dispose();
            }
        }

        private static void ErrorMsg(string message, Exception ex)
            => MelonLogger.Error($"[FileSystemWatcher] {message}", ex);

        public new void Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch (Exception ex)
            {
                ErrorMsg("An exception occurred while disposing of FileSystemWatcher", ex);
            }
            MelonEvents.OnUpdate.Unsubscribe(Update);
            Disposed?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
        }
    }
}