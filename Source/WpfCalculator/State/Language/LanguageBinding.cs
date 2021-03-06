﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xaml;

namespace WpfCalculator
{
    public class LanguageBinding : MarkupExtension
    {
        private PropertyPath? _keyProperty;
        private object? _value;

        private DependencyObject? _targetObject;
        private DependencyProperty? _targetProperty;

        public PropertyPath? Key
        {
            get => _keyProperty;
            set
            {
                _keyProperty = value;
                Uri = _keyProperty != null ? new ResourceUri(_keyProperty.Path) : null;
            }
        }

        public ResourceUri? Uri { get; private set; }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                return null;

            var targetProvider = serviceProvider.GetService<IProvideValueTarget>();
            if (targetProvider == null)
                throw new ArgumentException($"Missing {nameof(IProvideValueTarget)} service.", nameof(serviceProvider));

            _targetObject = targetProvider.TargetObject as DependencyObject;
            _targetProperty = targetProvider.TargetProperty as DependencyProperty;

            object? langProviderResource = null;
            if (DesignerProperties.GetIsInDesignMode(_targetObject))
            {
                if (_targetObject is FrameworkElement element)
                    langProviderResource = element.TryFindResource(App.LanguageProviderKey);
            }
            else
            {
                langProviderResource = App.Instance.TryFindResource(App.LanguageProviderKey);
            }

            if (!(langProviderResource is AppLanguageProvider langProvider))
                return _value = "[No Language Provider]";

            langProvider.PropertyChanged += LanguageData_PropertyChange;
            Refresh(langProvider);

            // TODO: add options for turning this on/off
            if (DesignerProperties.GetIsInDesignMode(_targetObject) && Uri != null)
            {
                var block = new TextBlock();

                Run AddRun(string text, Color color)
                {
                    var run = new Run(text)
                    {
                        Foreground = new SolidColorBrush(color)
                    };
                    block.Inlines.Add(run);
                    return run;
                }

                if (Uri.Segments.Length == 1)
                {
                    AddRun(Uri.Segments[0], Colors.Black);
                }
                else
                {
                    AddRun(Uri.Segments[^2] + ResourceUri.PathSeparator, Colors.Gray).FontSize *= 0.75;
                    AddRun(Uri.Segments[^1], Colors.Black);
                }

                _value = block;
                return _value;
            }

            return _value;
        }

        private void LanguageData_PropertyChange(object sender, EventArgs e)
        {
            var langProvider = (AppLanguageProvider)sender;
            Refresh(langProvider);

            if (_targetObject != null)
                _targetObject.SetValue(_targetProperty, _value);
        }

        private void Refresh(AppLanguageProvider languageProvider)
        {
            if (Uri == null)
            {
                _value = "[Null Key]";
                return;
            }

            try
            {
                _value = languageProvider.GetValue(Uri);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _value = "[Invalid Key]";
            }
        }
    }
}