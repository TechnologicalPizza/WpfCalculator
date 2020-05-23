﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Effects;
using Miniräknare.Expressions;

namespace Miniräknare
{
    // TODO: settings:
    //  * smooth scroll in field list (quality vs performance)
    //  * show result values with base10 exponent (default) or just a lot of zeroes (+ max decimal count)
    //  * result value with either comma or dot

    public partial class MainWindow : Window
    {
        private static readonly ReadOnlyString _alphabet = "abcdefghijklmnopqrstuvwxyz";

        private BindingList<ListViewItem> FieldItemList { get; } =
            new BindingList<ListViewItem>();

        public static Dictionary<ReadOnlyString, ExpressionBox> GlobalExpressions { get; } =
            new Dictionary<ReadOnlyString, ExpressionBox>();

        public static ListView FieldList { get; private set; }

        private LanguageWindow _languageWindow;

        public MainWindow()
        {
            InitializeComponent();

            FieldList = FieldListView;
            FieldListView.ItemsSource = FieldItemList;

            AddNewEquationField(null);
            AddNewField();

            var menuFieldOptions = (ContextMenu)FindResource("Menu_FieldOptions");
            menuFieldOptions.Opened += MenuFieldOptions_Opened;
            menuFieldOptions.Closed += MenuFieldOptions_Closed;
        }

        private IContextMenuOperand TryGetFieldFromContextMenu(ContextMenu menu)
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

        #region GenerateFieldName

        private ReadOnlyString GenerateFieldName()
        {
            const int maxLength = 10;
            var alphabet = _alphabet.Span;
            int targetIndex = alphabet.Length;

            int length = 1;
            int lastIndex = maxLength - 1;

            var nameBuffer = new char[maxLength];
            Span<int> indices = stackalloc int[maxLength];
            Memory<char> name;

            bool tryGet = true;
            do
            {
                for (int i = 0; i < length; i++)
                    nameBuffer[maxLength - i - 1] = alphabet[indices[maxLength - i - 1]];
                name = nameBuffer.AsMemory(maxLength - length, length);

                if (!GlobalExpressions.ContainsKey(name))
                    break;

                indices[^1]++;

                for (int i = indices.Length; i-- > 0;)
                {
                    if (indices[i] != targetIndex)
                        continue;

                    if (i - 1 < 0)
                    {
                        if (indices[i] == targetIndex)
                        {
                            tryGet = false;
                            break;
                        }
                    }
                    else
                    {
                        indices[i - 1]++;
                    }

                    if (i < lastIndex)
                    {
                        length++;
                        if (length > maxLength)
                            throw new Exception("Max name length reached.");

                        lastIndex = i;
                    }
                    indices[i] = 0;
                }
            } while (tryGet);

            return name;
        }

        #endregion

        #region ActionButton Click handlers

        private void AddNewEquationField(EquationSet equation)
        {
            var field = new EquationField();
            FieldItemList.Add(new ListViewItem() { Content = field, ContextMenu = field.ContextMenu });
        }

        private void AddNewField()
        {
            var field = new ExpressionField();
            field.InputBox.VariableName = GenerateFieldName().ToString();

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
            AddNewField();
        }

        private void AddNewFunctionField_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        #endregion

        private void FieldList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var list = (ListView)sender;
            list.UnselectAll();
        }

        #region MenuItem Click handlers

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Language_Click(object sender, RoutedEventArgs e)
        {
            if (_languageWindow == null)
            {
                _languageWindow = new LanguageWindow();
                _languageWindow.Closed += LanguageWindow_Closed;
            }

            _languageWindow.Show();
            _languageWindow.Focus();
        }

        private void LanguageWindow_Closed(object sender, EventArgs e)
        {
            _languageWindow = null;
        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
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
    }
}
