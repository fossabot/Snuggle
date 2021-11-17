﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Snuggle.Core.Options;

namespace Snuggle.Handlers;

public record SnuggleOptions(
    SnuggleCoreOptions Options,
    ObjectDeserializationOptions ObjectOptions,
    BundleSerializationOptions BundleOptions,
    FileSerializationOptions FileOptions,
    bool WriteNativeTextures,
    bool UseContainerPaths,
    bool GroupByType,
    string NameTemplate,
    bool LightMode,
    bool BubbleGameObjectsDown,
    bool BubbleGameObjectsUp,
    bool DisplayRelationshipLines,
    bool DisplayWireframe) {
    private const int LatestVersion = 6;

    public List<string> RecentFiles { get; set; } = new();
    public List<string> RecentDirectories { get; set; } = new();
    public string LastSaveDirectory { get; set; } = string.Empty;
    public HashSet<RendererType> EnabledRenders { get; set; } = Enum.GetValues<RendererType>().ToHashSet();

    public static SnuggleOptions Default { get; } = new(
        SnuggleCoreOptions.Default,
        ObjectDeserializationOptions.Default,
        BundleSerializationOptions.Default,
        FileSerializationOptions.Default,
        true,
        true,
        true,
        "{0}.{1:G}_{2:G}.bytes", // 0 = Name, 1 = PathId, 2 = Type
        false,
        true,
        false,
        true,
        false);

    public int Version { get; set; } = LatestVersion;

    public static SnuggleOptions FromJson(string json) {
        try {
            var settings = JsonSerializer.Deserialize<SnuggleOptions>(json, SnuggleCoreOptions.JsonOptions) ?? Default;

            if (settings.Options.NeedsMigration()) {
                settings = settings with { Options = settings.Options.Migrate() };
            }

            if (settings.BundleOptions.NeedsMigration()) {
                settings = settings with { BundleOptions = settings.BundleOptions.Migrate() };
            }

            if (settings.FileOptions.NeedsMigration()) {
                settings = settings with { FileOptions = settings.FileOptions.Migrate() };
            }

            if (settings.ObjectOptions.NeedsMigration()) {
                settings = settings with { ObjectOptions = settings.ObjectOptions.Migrate() };
            }

            return settings.NeedsMigration() ? settings.Migrate() : settings;
        } catch {
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SnuggleCoreOptions.JsonOptions);

    public bool NeedsMigration() => Version < LatestVersion;

    public SnuggleOptions Migrate() {
        var settings = this;

        if (settings.Version < 2) {
            settings = settings with { RecentDirectories = new List<string>(), RecentFiles = new List<string>(), LastSaveDirectory = string.Empty, Version = 2 };
        }

        if (settings.Version < 3) {
            settings = settings with { LightMode = false, Version = 3 };
        }

        if (settings.Version < 4) {
            settings = settings with { EnabledRenders = Enum.GetValues<RendererType>().ToHashSet(), BubbleGameObjectsDown = true, BubbleGameObjectsUp = true };
        }

        if (settings.Version < 5) {
            settings = settings with { DisplayRelationshipLines = true };
        }

        if (settings.Version < 6) {
            settings = settings with { DisplayWireframe = false };
        }

        return settings with { Version = LatestVersion };
    }
}
