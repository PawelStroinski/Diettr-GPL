﻿using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows;

namespace Dietphone.Tools
{
    public static class UIExtensionMethods
    {
        public static bool IsFocused(this Control control)
        {
            return FocusManager.GetFocusedElement() == control;
        }

        public static void UniversalGroupPickerItemTap(this RadJumpList list, GroupPickerItemTapEventArgs e)
        {
            var groups = list.Groups;
            foreach (var group in groups)
            {
                if (object.Equals(e.DataItem, group.Key))
                {
                    e.DataItemToNavigate = group;
                    return;
                }
            }
        }

        public static void ForceRefresh(this RadListPicker picker, PerformanceProgressBar progressBar)
        {
            progressBar.IsIndeterminate = true;
            picker.Dispatcher.BeginInvoke(() =>
            {
                picker.IsEnabled = false;
                var items = picker.ItemsSource;
                var item = picker.SelectedItem;
                picker.ItemsSource = null;
                picker.ItemsSource = items;
                picker.SelectedItem = item;
                picker.IsEnabled = true;
                progressBar.IsIndeterminate = false;
            });
        }

        public static void QuicklyCollapse(this RadListPicker picker)
        {
            var isAnimationEnabled = picker.IsAnimationEnabled;
            picker.IsAnimationEnabled = false;
            picker.IsExpanded = false;
            picker.IsAnimationEnabled = isAnimationEnabled;
        }

        public static ApplicationBarIconButton GetIcon(this PhoneApplicationPage page, int whichIcon)
        {
            var appBar = page.ApplicationBar;
            var icons = appBar.Buttons;
            return icons[whichIcon] as ApplicationBarIconButton;
        }

        public static Visibility ToVisibility(this bool visible)
        {
            if (visible)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public static InputScope GetInputScope(this InputScopeNameValue kind)
        {
            return new InputScope
            {
                Names = 
                { 
                    new InputScopeName() 
                    { 
                        NameValue = kind
                    } 
                }
            };
        }

        public static bool IsDarkTheme(this PhoneApplicationPage page)
        {
            var themeVisibility = (Visibility)page.Resources["PhoneDarkThemeVisibility"];
            return themeVisibility == Visibility.Visible;
        }

        public static void HideColumnWithIndex(this Grid grid, byte columnIndex)
        {
            var column = grid.ColumnDefinitions[columnIndex];
            var width = new GridLength(0);
            column.Width = width;
        }
    }
}
