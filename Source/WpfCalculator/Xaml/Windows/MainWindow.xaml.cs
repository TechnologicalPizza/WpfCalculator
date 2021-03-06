﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WpfCalculator.Expressions;

namespace WpfCalculator
{
    // TODO: settings:
    //  * smooth scroll in field list (quality vs performance)
    //  * show result values with base10 exponent (default) or just a lot of zeroes (+ max decimal count)
    //  * result value with either comma or dot

    public partial class MainWindow : Window
    {
        private BindingList<ListViewItem> FieldItemList { get; } =
            new BindingList<ListViewItem>();

        private LanguageWindow? _languageWindow;
        private AppStateProvider _stateProvider;

        public AppState State => _stateProvider.State;

        public MainWindow()
        {
            InitializeComponent();

            _stateProvider = App.Instance.StateProvider;

            FieldListView.ItemsSource = FieldItemList;

            AddNewEquationField(State, null);
            AddNewField(State);

            var menuFieldOptions = (ContextMenu)FindResource("Menu_FieldOptions");
            menuFieldOptions.Opened += MenuFieldOptions_Opened;
            menuFieldOptions.Closed += MenuFieldOptions_Closed;
        }

        private IContextMenuOperand? TryGetFieldFromContextMenu(ContextMenu menu)
        {
            var target = menu.PlacementTarget;
            if (menu.PlacementTarget is ListViewItem listItem)
                target = listItem.Content as UIElement;

            return target as IContextMenuOperand;
        }

        private void MenuFieldOptions_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var field = TryGetFieldFromContextMenu(menu);
            if (field != null)
            {
                foreach (var menuItem in menu.Items.OfType<MenuItem>())
                    field.RegisterContextMenuItem(menuItem);
            }
        }

        private void MenuFieldOptions_Closed(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var field = TryGetFieldFromContextMenu(menu);
            if (field != null)
            {
                foreach (var menuItem in menu.Items.OfType<MenuItem>())
                    field.UnregisterContextMenuItem(menuItem);
            }
        }

        #region ActionButton Click handlers

        private void AddNewEquationField(AppState state, EquationSet equation)
        {
            var field = new EquationField();
            FieldItemList.Add(new ListViewItem() { Content = field, ContextMenu = field.ContextMenu });
        }

        private void AddNewField(AppState state)
        {
            var field = new ExpressionField();
            field.InputBox.VariableName = state.GenerateFieldName().ToString();

            var listItem = new ListViewItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = field,
                ContextMenu = field.ContextMenu
            };

            FieldItemList.Add(listItem);
        }

        private void AddNewField_Click(object sender, RoutedEventArgs e)
        {
            AddNewField(State);
        }

        private void AddNewEquationField_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.ContextMenu.IsOpen = true;
        }

        #endregion

        private void FieldList_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListView list))
                return;

            list.UnselectAll();
        }

        #region MenuItem Click handlers

        private void MenuItem_Exit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Language_Click(object? sender, RoutedEventArgs e)
        {
            if (_languageWindow == null)
            {
                _languageWindow = new LanguageWindow();
                _languageWindow.Closed += LanguageWindow_Closed;
            }

            _languageWindow.Show();
            _languageWindow.Focus();
        }

        private void LanguageWindow_Closed(object? sender, EventArgs e)
        {
            _languageWindow = null;
        }

        #endregion

        private void Window_Closing(object? sender, CancelEventArgs e)
        {

            // TODO: check if there is stuff to save
        }

        private void MenuItem_View_Theme_Click(object sender, RoutedEventArgs e)
        {
            var inverseShaderResource = FindResource("Shader_Inverse");
            if (!(inverseShaderResource is Effect inverseShader))
                throw new Exception("Missing inverse shader resource.");

            Effect = Effect != inverseShader ? inverseShader : null;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            string query = box.Text;
            var equations = App.Instance.Equations;
            var languageProvider = (AppLanguageProvider)box.FindResource(App.LanguageProviderKey);

            bool checkFallbackLanguage = true;
            bool searchByName = true;
            bool searchByKey = true;
            bool searchByPath = true;
            var keyComparison = StringComparison.OrdinalIgnoreCase;

            var primaryLanguage = languageProvider.Language;
            var fallbackLanguage = languageProvider.FallbackLanguage;

            var included = new Dictionary<(string, string[]), AppLanguage.Entry>();
            var translatedNames = new List<string?>();

            foreach (string equationKey in equations.Keys)
            {
                string[] keySegments = equationKey
                    .Substring(App.ContentPath.Length)
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                string specificEquationKey = equationKey
                    .Substring(App.EquationsPath.Length).TrimStart('/');

                string valueKey = Path.GetFileNameWithoutExtension(keySegments[^1]);

                CheckLanguage(primaryLanguage);
                if (checkFallbackLanguage)
                    CheckLanguage(fallbackLanguage);

                void CheckLanguage(AppLanguage language)
                {
                    translatedNames.Clear();

                    AppLanguage.EntryList? currentList = language.Entries;

                    bool breakSearch = false;
                    for (int i = 0; i < keySegments.Length - 1; i++)
                    {
                        currentList.Names.TryGetValue(keySegments[i], out var currentListName);
                        translatedNames.Add(currentListName?.Value as string);

                        if (!currentList.SubLists.TryGetValue(keySegments[i], out currentList))
                        {
                            breakSearch = true;
                            break;
                        }
                    }

                    if (breakSearch || currentList == null)
                        return;

                    currentList.Values.TryGetValue(valueKey, out var lastValueEntry);
                    translatedNames.Add(lastValueEntry?.Value as string);

                    if (currentList.Values.TryGetValue(valueKey, out var targetValue))
                    {
                        (string, string[]) key = (equationKey, keySegments);

                        for (int i = 1; i < keySegments.Length; i++)
                        {
                            bool include = false;
                            if (searchByName)
                            {
                                string? publicName = translatedNames[i];
                                if (publicName != null && publicName.Contains(query, keyComparison))
                                    include = true;
                            }
                            if (!include && searchByKey)
                            {
                                if (keySegments[i].Contains(query, keyComparison))
                                    include = true;
                            }

                            if (include)
                            {
                                included.TryAdd(key, targetValue);
                                break;
                            }
                        }

                        if (searchByPath && specificEquationKey.Contains(query))
                            included.TryAdd(key, targetValue);
                    }
                }
            }

            var boxGrid = (Grid)box.Parent;
            var boxMenuItem = (MenuItem)boxGrid.TemplatedParent;
            var boxMenu = (ContextMenu)boxMenuItem.Parent;

            var containerItem = (MenuItem)LogicalTreeHelper.FindLogicalNode(
                boxMenu, "ActionItemPickerMenuItem");

            var searchResultViewer = (ScrollViewer)containerItem.Template.FindName(
                "Action_SearchResultViewer", containerItem);
            var searchResultMenu = (Menu)searchResultViewer.Content;

            var staticChoiceViewer = (ScrollViewer)containerItem.Template.FindName(
                "Action_StaticChoiceViewer", containerItem);
            var staticChoiceMenu = (Menu)searchResultViewer.Content;

            if (string.IsNullOrEmpty(query))
            {
                searchResultViewer.IsEnabled = false;
                staticChoiceViewer.IsEnabled = true;

                searchResultMenu.ItemsSource = null;
                // TODO: update static choices
            }
            else
            {
                searchResultViewer.IsEnabled = true;
                staticChoiceViewer.IsEnabled = false;

                searchResultMenu.ItemsSource = included.Select(
                    x => new ActionEquationChoice(x.Key.Item1, x.Key.Item2, x.Value));
            }
        }

        private void Action_ItemPickerGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = (Grid)sender;
            grid.MaxHeight = Math.Max(0, ActualHeight - 100);
        }
    }
}
