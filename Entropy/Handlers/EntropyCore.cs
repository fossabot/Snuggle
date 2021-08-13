﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DragonLib;
using Equilibrium;
using Equilibrium.Interfaces;
using Equilibrium.Logging;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Entropy.Handlers {
    [PublicAPI]
    public class EntropyCore : Singleton<EntropyCore>, INotifyPropertyChanged, IDisposable {
        private object SaveLock = new();

        public EntropyCore() {
            Dispatcher = Dispatcher.CurrentDispatcher;
            var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SettingsFile = Path.Combine(workDir ?? "./", "Entropy.json");
            WorkerThread = new Thread(WorkLoop);
            WorkerThread.Start();
            LogTarget = new MultiLogger {
                Loggers = {
                    new ConsoleLogger(),
                    new DebugLogger(),
                    // Log,
                },
            };
            SetOptions(File.Exists(SettingsFile) ? EntropySettings.FromJson(File.ReadAllText(SettingsFile)) : EntropySettings.Default);
            UpdateColors();
        }

        public Dispatcher Dispatcher { get; set; }
        public AssetCollection Collection { get; } = new();
        public EntropyStatus Status { get; } = new();
        // public EntropyLog Log { get; set; } = new();
        public ILogger LogTarget { get; }
        public EntropySettings Settings { get; private set; } = EntropySettings.Default;
        public Thread WorkerThread { get; private set; }
        public CancellationTokenSource TokenSource { get; private set; } = new();
        private BlockingCollection<(string Name, Action<CancellationToken> Work)> Tasks { get; set; } = new();
        public List<EntropyObject> Objects => Collection.Files.SelectMany(x => x.Value.GetAllObjects()).Select(x => new EntropyObject(x)).ToList();
        public EntropyObject? SelectedObject { get; set; }
        public HashSet<object> Filters { get; set; } = new();
        public IReadOnlyList<EntropyObject> SelectedObjects { get; set; } = Array.Empty<EntropyObject>();
        public string? Search { get; set; }
        private string SettingsFile { get; }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateColors() {
            var resources = Application.Current.Resources.MergedDictionaries;
            foreach (var resource in resources) {
                if (resource.Source != null) {
                    if (resource.Source.AbsolutePath.EndsWith("MaterialDesignTheme.Dark.xaml") ||
                        resource.Source.AbsolutePath.EndsWith("MaterialDesignTheme.Light.xaml")) {
                        resource.Source = new Uri($"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.{(Settings.LightMode ? "Light" : "Dark")}.xaml");
                    } else if (resource.Source.AbsolutePath.Contains("Primary/MaterialDesignColor.")) {
                        resource.Source = new Uri($"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.{Settings.Color:G}.xaml");
                    }
                }
            }
        }

        ~EntropyCore() {
            Dispose(false);
        }

        protected void Dispose(bool disposing) {
            Reset(false);

            if (disposing) {
                Collection.Dispose();
            }

            Environment.Exit(0);
        }

        private void WorkLoop() {
            try {
                var tasks = Tasks;
                var sw = new Stopwatch();
                foreach (var (name, task) in tasks.GetConsumingEnumerable(TokenSource.Token)) {
                    try {
                        sw.Start();
                        task(TokenSource.Token);
                        sw.Stop();
                        var elapsed = sw.Elapsed;
                        LogTarget.Info("Worker", $"Spent {elapsed} working on {name} task");
                        sw.Reset();
                    } catch (Exception e) {
                        LogTarget.Error("Worker", $"Failed to perform {name} task", e);
                    }

                    LogTarget.Info("Worker", $"Memory Tension: {GC.GetTotalMemory(false).GetHumanReadableBytes()}");
                }
            } catch (TaskCanceledException) {
                // ignored
            } catch (OperationCanceledException) {
                // ignored
            } catch (Exception e) {
                LogTarget.Error("Worker", "Failed to get tasks", e);
            }
        }

        public void Reset(bool respawn = true) {
            Tasks.CompleteAdding();
            Tasks = new BlockingCollection<(string Name, Action<CancellationToken> Work)>();
            TokenSource.Cancel();
            TokenSource.Dispose();
            WorkerThread.Join();
            SelectedObject = null;
            Collection.Reset();
            Status.Reset();
            // Log.Clear();
            Search = string.Empty;
            Filters.Clear();
            EntropyTextureFile.ClearMemory();
            if (respawn) {
                TokenSource = new CancellationTokenSource();
                WorkerThread = new Thread(WorkLoop);
                WorkerThread.Start();
                OnPropertyChanged(nameof(Objects));
                OnPropertyChanged(nameof(Filters));
                OnPropertyChanged(nameof(SelectedObject));
                OnPropertyChanged(nameof(SelectedObjects));
            }
        }

        public void WorkerAction(string name, Action<CancellationToken> action) {
            Tasks.Add((name, action));
        }

        public Task<T> WorkerAction<T>(string name, Func<CancellationToken, T> task) {
            var tcs = new TaskCompletionSource<T>();
            Tasks.Add((name, token => {
                try {
                    tcs.SetResult(task(token));
                } catch (TaskCanceledException) {
                    tcs.SetCanceled(token);
                } catch (Exception e) {
                    tcs.SetException(e);
                }
            }));
            return tcs.Task;
        }

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            Dispatcher.Invoke(() => { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
        }

        public void SaveOptions() {
            lock (SaveLock) {
                File.WriteAllText(SettingsFile, Settings.ToJson());
            }

            OnPropertyChanged(nameof(Settings));
        }

        public void SetOptions(EntropySettings options) {
            Settings = options with { Options = options.Options with { Reporter = Status, Logger = LogTarget } };
            SaveOptions();
        }

        public void SetOptions(EquilibriumOptions options) {
            Settings = Settings with { Options = options with { Reporter = Status, Logger = LogTarget } };
            SaveOptions();
        }
    }
}
