using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MelonLoader;

namespace KeepInventory.Utilities
{
    public sealed class SynchronousFileSystemWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher = new();

        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public NotifyFilters NotifyFilter
        {
            get => _watcher.NotifyFilter;
            set => _watcher.NotifyFilter = value;
        }

        public string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        public Collection<string> Filters
        {
            get => _watcher.Filters;
        }

        public string Path
        {
            get => _watcher.Path;
            set => _watcher.Path = value;
        }

        public event EventHandler<RenamedEventArgs> Renamed;

        public event EventHandler<FileSystemEventArgs> Created;

        public event EventHandler<FileSystemEventArgs> Deleted;

        public event EventHandler<FileSystemEventArgs> Changed;

        public event EventHandler Disposed;

        public event EventHandler<ErrorEventArgs> Error;

        private readonly List<EventArgs> _Queue = [];
        public IReadOnlyList<EventArgs> Queue => _Queue.AsReadOnly();

        public SynchronousFileSystemWatcher()
            => Init();

        public SynchronousFileSystemWatcher(string path)
        {
            _watcher.Path = path;
            Init();
        }

        public SynchronousFileSystemWatcher(string path, string filter)
        {
            _watcher.Path = path;
            _watcher.Filter = filter;
            Init();
        }

        private void Init()
        {
            _watcher.Renamed += (sender, e) => _Queue.Add(e);
            _watcher.Created += (sender, e) => _Queue.Add(e);
            _watcher.Deleted += (sender, e) => _Queue.Add(e);
            _watcher.Changed += (sender, e) => _Queue.Add(e);
            _watcher.Disposed += (sender, e) => _Queue.Add(e);

            _watcher.Error += (sender, e) => _Queue.Add(e);
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
                        {
                            Renamed?.Invoke(this, renamed);
                        }
                        else if (@event is FileSystemEventArgs fse_args)
                        {
                            if (fse_args.ChangeType == WatcherChangeTypes.Created)
                                Created?.Invoke(this, fse_args);
                            else if (fse_args.ChangeType == WatcherChangeTypes.Deleted)
                                Deleted?.Invoke(this, fse_args);
                            else if (fse_args.ChangeType == WatcherChangeTypes.Changed)
                                Changed?.Invoke(this, fse_args);
                        }
                        else if (@event is ErrorEventArgs error)
                        {
                            Error?.Invoke(this, error);
                        }
                        else if (@event is EventArgs args)
                        {
                            try
                            {
                                Disposed?.Invoke(this, args);
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Error("SynchronousFileSystemWatcher | An unexpected error has occurred while running Disposed event", ex);
                            }
                            finally
                            {
                                Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error("SynchronousFileSystemWatcher | An unexpected error has occurred while triggering file system watcher events", ex);
                    }
                    finally
                    {
                        _Queue.RemoveAt(i);
                    }
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _watcher.Dispose();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("SynchronousFileSystemWatcher | An exception occurred while disposing of FileSystemWatcher", ex);
            }
            MelonEvents.OnUpdate.Unsubscribe(Update);
            GC.SuppressFinalize(this);
        }
    }
}