using System;
using System.Windows.Markup;
using ReduxInstaller.Services;

namespace ReduxInstaller.Utilities
{
    public class LocExtension : MarkupExtension
    {
        private readonly string _key;

        public LocExtension(string key)
        {
            _key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return LocalizationService.Instance.GetString(_key);
        }
    }
}