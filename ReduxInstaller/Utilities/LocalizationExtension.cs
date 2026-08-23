using System;
using System.Windows.Data;
using ReduxInstaller.Services;

namespace ReduxInstaller.Utilities
{
    public class LocalizationExtension : Binding
    {
        public LocalizationExtension(string key) : base("GetString")
        {
            Source = LocalizationService.Instance;
            Mode = BindingMode.OneWay;
            ConverterParameter = key;
        }
    }
}