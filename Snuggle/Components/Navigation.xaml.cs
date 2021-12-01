﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AdonisUI;
using DragonLib.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;
using Snuggle.Core.Options.Game;
using Snuggle.Handlers;

namespace Snuggle.Components;

public partial class Navigation {
    private static readonly Regex SplitPattern = new(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[a-z])(?=[0-9])", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private readonly Dictionary<UniteVersion, MenuItem> PokemonUniteVersionItems = new();
    private readonly Dictionary<RendererType, MenuItem> RendererTypeItems = new();
    private readonly Dictionary<UnityGame, MenuItem> UnityGameItems = new();

    public Navigation() {
        InitializeComponent();
        var instance = SnuggleCore.Instance;

        CacheData.IsChecked = instance.Settings.Options.CacheData;
        CacheDataIfLZMA.IsChecked = instance.Settings.Options.CacheDataIfLZMA;
        LightMode.IsChecked = instance.Settings.LightMode;

        BuildEnumMenu(UnityGameList, UnityGameItems, new[] { instance.Settings.Options.Game }, UpdateGame, CancelGameEvent);
        BuildEnumMenu(RendererTypes, RendererTypeItems, instance.Settings.MeshExportOptions.EnabledRenders, AddRenderer, RemoveRenderer);
        BuildToggleMenu(SerializationOptions, typeof(SnuggleExportOptions), nameof(SnuggleOptions.ExportOptions));
        BuildToggleMenu(RendererOptions, typeof(SnuggleMeshExportOptions), nameof(SnuggleOptions.MeshExportOptions));
        PopulateGameOptions();
        PopulateRecentItems();

        instance.PropertyChanged += (_, args) => {
            switch (args.PropertyName) {
                case nameof(SnuggleCore.Settings):
                    PopulateRecentItems();
                    break;
                case nameof(SnuggleCore.Objects):
                    PopulateItemTypes();
                    break;
            }
        };
    }

    private void BuildToggleMenu(MenuItem menu, Type type, string objectName) {
        // typeof(SnuggleOptions).GetProperty(objectName)?.SetValue(SnuggleCore.Instance.Settings, newValue)
        var current = typeof(SnuggleOptions).GetProperty(objectName)!.GetValue(SnuggleCore.Instance.Settings)!;
        var i = 0;
        var descriptions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var constructors = type.GetConstructors();
        foreach (var constructor in constructors) {
            var parameters = constructor.GetParameters();
            foreach (var parameter in parameters) {
                var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrWhiteSpace(description)) {
                    continue;
                }

                descriptions[parameter.Name!] = description;
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.PropertyType != typeof(bool)) {
                continue;
            }

            if (!descriptions.TryGetValue(property.Name, out var description)) {
                description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            }

            var item = new MenuItem {
                Tag = (type, objectName, property),
                Header = SplitName(property.Name),
                ToolTip = description,
                IsCheckable = true,
                IsChecked = property.GetValue(current) is true,
            };
            item.Checked += UpdateToggle;
            item.Unchecked += UpdateToggle;
            menu.Items.Insert(i++, item);
        }
    }

    internal static string SplitName(string name) => string.Join(' ', SplitPattern.Split(name));

    private void UpdateToggle(object sender, RoutedEventArgs args) {
        if (sender is not MenuItem item) {
            return;
        }

        var (type, objectName, targetProperty) = item.Tag is (Type, string, PropertyInfo) ? ((Type, string, PropertyInfo)) item.Tag : (null, null, null);
        if (type == null || objectName == null || targetProperty == null) {
            return;
        }

        var propertyMap = new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);
        var propertyInfoMap = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        var currentProperty = typeof(SnuggleOptions).GetProperty(objectName)!;
        var current = currentProperty.GetValue(SnuggleCore.Instance.Settings)!;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            propertyMap[property.Name] = property == targetProperty ? item.IsChecked : property.GetValue(current);
            propertyInfoMap[property.Name] = property;
        }

        var constructors = type.GetConstructors();
        foreach (var constructor in constructors) {
            var parameters = constructor.GetParameters().ToDictionary(x => x.Name!, y => y, StringComparer.InvariantCultureIgnoreCase);
            if (!parameters.Keys.All(propertyMap.ContainsKey)) {
                continue;
            }

            if (parameters.Keys.Any(x => !propertyMap.ContainsKey(x))) {
                continue;
            }

            var constructorParams = parameters.Select(x => propertyMap[x.Key]).ToArray();
            var newSettings = Activator.CreateInstance(type, constructorParams);
            foreach (var (properyKeyName, properyValue) in propertyMap) {
                if (!parameters.ContainsKey(properyKeyName)) {
                    propertyInfoMap[properyKeyName].SetValue(newSettings, properyValue);
                }
            }

            currentProperty.SetValue(SnuggleCore.Instance.Settings, newSettings);
            SnuggleCore.Instance.SaveOptions();
        }
    }

    private void PopulateGameOptions() {
        var selected = SnuggleCore.Instance.Settings.Options.Game;

        GameOptions.Visibility = Visibility.Collapsed;

        if (selected is UnityGame.PokemonUnite) {
            SetPokemonUniteOptionValues();
        } else {
            GameOptions.Items.Clear();
        }
    }

    private void SetPokemonUniteOptionValues() {
        GameOptions.Visibility = Visibility.Visible;
        GameOptions.Header = UnityGameItems[UnityGame.PokemonUnite].Header + " Options";

        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        var optionsMenuItem = new MenuItem { Tag = "PokemonUnite_Version", Header = "_Version" };
        GameOptions.Items.Add(optionsMenuItem);
        BuildEnumMenu(optionsMenuItem, PokemonUniteVersionItems, new[] { uniteOptions.GameVersion }, UpdatePokemonUniteVersion, CancelPokemonUniteVersionEvent);
    }

    private static void BuildEnumMenu<T>(ItemsControl menu, IDictionary<T, MenuItem> items, IReadOnlyCollection<T> currentValue, RoutedEventHandler @checked, RoutedEventHandler @unchecked) where T : struct, Enum {
        var descriptions = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(x => (T) x.GetValue(null)!, x => x.GetCustomAttribute<DescriptionAttribute>()?.Description ?? SplitName(x.Name));
        foreach (var value in Enum.GetValues<T>()) {
            var item = new MenuItem {
                Tag = value, Header = "_" + descriptions[value], IsChecked = currentValue.Any(x => x.Equals(value)), IsCheckable = true,
            };
            item.Checked += @checked;
            item.Unchecked += @unchecked;
            menu.Items.Add(item);
            items[value] = item;
        }
    }

    private void PopulateItemTypes() {
        var instance = SnuggleCore.Instance;
        Filters.Items.Clear();
        var letters = new Dictionary<char, MenuItem>();
        foreach (var item in instance.Objects.DistinctBy(x => x.ClassId).Select(x => x.ClassId).OrderBy(x => ((Enum) x).ToString("G"))) {
            var name = SplitName(((Enum) item).ToString("G"));
            var menuItem = new MenuItem { Tag = item, Header = "_" + name, IsCheckable = true, IsChecked = instance.Filters.Contains(item) };
            menuItem.Click += ToggleFilter;
            var letter = char.ToUpper(name[0]);
            if (!letters.TryGetValue(letter, out var letterMenuItem)) {
                letterMenuItem = new MenuItem { Header = $"_{letter}" };
                letters[letter] = letterMenuItem;
                Filters.Items.Add(letterMenuItem);
            }

            letterMenuItem.Items.Add(menuItem);
        }

        Filters.Visibility = Filters.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PopulateRecentItems() {
        var instance = SnuggleCore.Instance;
        RecentItems.Items.Clear();
        foreach (var item in instance.Settings.RecentFiles.Select(recentFile => new MenuItem { Tag = recentFile, Header = "_" + recentFile })) {
            item.Click += LoadDirectoryOrFile;
            RecentItems.Items.Add(item);
        }

        if (!RecentItems.Items.IsEmpty && instance.Settings.RecentDirectories.Count > 0) {
            RecentItems.Items.Add(new Separator());
        }

        foreach (var item in instance.Settings.RecentDirectories.Select(recentDirectory => new MenuItem { Tag = recentDirectory, Header = "_" + recentDirectory })) {
            item.Click += LoadDirectoryOrFile;
            RecentItems.Items.Add(item);
        }

        RecentItems.Visibility = RecentItems.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void ToggleFilter(object sender, RoutedEventArgs e) {
        var tag = ((FrameworkElement) sender).Tag;
        var instance = SnuggleCore.Instance;
        if (instance.Filters.Contains(tag)) {
            instance.Filters.Remove(tag);
        } else {
            instance.Filters.Add(tag);
        }

        instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
    }

    private static void LoadDirectoryOrFile(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem { Tag: string directory }) {
            return;
        }

        SnuggleFile.LoadDirectoriesAndFiles(directory);
    }

    private static void CancelGameEvent(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        if ((UnityGame) menuItem.Tag == SnuggleCore.Instance.Settings.Options.Game) {
            menuItem.IsChecked = true;
        }

        e.Handled = true;
    }

    private void UpdateGame(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var game = SnuggleCore.Instance.Settings.Options.Game;
        var tag = (UnityGame) menuItem.Tag;
        if (tag == game) {
            return;
        }

        SnuggleCore.Instance.SetOptions(SnuggleCore.Instance.Settings.Options with { Game = tag });
        UnityGameItems[game].IsChecked = false;
        PopulateGameOptions();
        e.Handled = true;
    }

    private void AddRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Add(tag);
        SnuggleCore.Instance.SaveOptions();
        e.Handled = true;
    }

    private void RemoveRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Remove(tag);
        SnuggleCore.Instance.SaveOptions();
        e.Handled = true;
    }

    private void ToggleCacheData(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings.Options with { CacheData = enabled });
    }

    private void ToggleCacheDataLZMA(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings.Options with { CacheDataIfLZMA = enabled });
    }

    private void ToggleLightMode(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { LightMode = enabled });
        ResourceLocator.SetColorScheme(Application.Current.Resources, instance.Settings.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
    }

    private void LoadDirectories(object sender, RoutedEventArgs e) {
        SnuggleFile.LoadDirectories();
    }

    private void LoadFiles(object sender, RoutedEventArgs e) {
        SnuggleFile.LoadFiles();
    }

    private void ExitTrampoline(object sender, RoutedEventArgs e) {
        Application.Current.MainWindow?.Close();
    }

    private void ResetTrampoline(object sender, RoutedEventArgs e) {
        SnuggleCore.Instance.Reset();
    }

    private void FreeMemory(object sender, RoutedEventArgs e) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction(
            "FreeMemory",
            _ => {
                foreach (var bundle in instance.Collection.Bundles) {
                    bundle.ClearCache();
                }

                foreach (var (_, file) in instance.Collection.Files) {
                    file.Free();
                }

                SnuggleTextureFile.ClearMemory();

                AssetCollection.Collect();
            },
            true);
    }

    private void DumpGameObjectTree(object sender, RoutedEventArgs e) {
        using var selection = new CommonSaveFileDialog {
            DefaultFileName = "gametree.txt",
            Filters = { new CommonFileDialogFilter("JSON File", ".json") },
            InitialDirectory = SnuggleCore.Instance.Settings.LastSaveDirectory,
            Title = "Select file to save to",
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        using var file = new FileStream(selection.FileName, FileMode.Create);
        using var writer = new StreamWriter(file);
        var nodes = BuildTreeNode(SnuggleCore.Instance.Collection.GameObjectTree);
        TreePrinter.PrintTree(writer, nodes);
    }

    private List<TreePrinter.TreeNode> BuildTreeNode(IEnumerable<GameObject?> gameObjects) {
        return gameObjects.Where(x => x != null).Select(x => new TreePrinter.TreeNode(x!.Name, BuildTreeNode(x.Children.Select(y => y.Value)))).ToList();
    }

    private void ExtractRaw(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Raw, (sender as MenuItem)?.Tag == null);
    }

    private void ExtractConvert(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Convert, (sender as MenuItem)?.Tag == null);
    }

    private void ExtractSerialize(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Serialize, (sender as MenuItem)?.Tag == null);
    }

    private void UpdatePokemonUniteVersion(object sender, RoutedEventArgs e) {
        var version = (UniteVersion) ((MenuItem) sender).Tag;
        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        instance.SetOptions(UnityGame.PokemonUnite, uniteOptions with { GameVersion = version });

        foreach (var (itemVersion, item) in PokemonUniteVersionItems) {
            if (itemVersion == version) {
                continue;
            }

            item.IsChecked = false;
        }

        e.Handled = true;
    }

    private void CancelPokemonUniteVersionEvent(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        if (menuItem.Tag?.Equals(uniteOptions.Version) == true) {
            menuItem.IsChecked = true;
        }

        e.Handled = true;
    }
}
