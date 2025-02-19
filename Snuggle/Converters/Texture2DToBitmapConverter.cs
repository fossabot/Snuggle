﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Interfaces;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class Texture2DToBitmapConverter : MarkupExtension, IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not ITexture texture ? null : new TaskCompletionNotifier<BitmapSource?>(texture, ConvertTexture(texture, Dispatcher.CurrentDispatcher));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private static async Task<BitmapSource?> ConvertTexture(ITexture texture, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeTexture",
            _ => {
                texture.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                var memory = SnuggleTextureFile.LoadCachedTexture(texture);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new RGBABitmapSource(memory, texture.Width, texture.Height, texture.Depth));
            },
            true);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
