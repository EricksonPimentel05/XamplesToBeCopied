namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public static class ExtensionMethods
    {
        [CanBeNull]
        public static CultureKey GetCultureKey([NotNull] this DataGridColumn column)
        {
            Contract.Requires(column != null);

            return (column.Header as ILanguageColumnHeader)?.CultureKey;
        }

        [CanBeNull]
        public static CultureInfo GetCulture([NotNull] this DataGridColumn column)
        {
            Contract.Requires(column != null);

            return column.GetCultureKey()?.Culture;
        }

        public static void SetEditingElementStyle([NotNull] this DataGridBoundColumn column, [CanBeNull] Binding languageBinding, [CanBeNull] Binding flowDirectionBinding)
        {
            Contract.Requires(column != null);

            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            var setters = textBoxStyle.Setters;

            // ReSharper disable AssignNullToNotNullAttribute
            setters.Add(new EventSetter(UIElement.PreviewKeyDownEvent, (KeyEventHandler)EditingElement_PreviewKeyDown));
            setters.Add(new Setter(TextBoxBase.AcceptsReturnProperty, true));

            setters.Add(new Setter(Spellcheck.IsEnabledProperty, true));
            setters.Add(new Setter(FrameworkElement.LanguageProperty, languageBinding));

            setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, flowDirectionBinding));
            // ReSharper restore AssignNullToNotNullAttribute

            textBoxStyle.Seal();

            column.EditingElementStyle = textBoxStyle;
        }

        public static void SetElementStyle([NotNull] this DataGridBoundColumn column, [CanBeNull] Binding languageBinding, [CanBeNull] Binding flowDirectionBinding)
        {
            Contract.Requires(column != null);

            var elementStyle = new Style(typeof(TextBlock), column.ElementStyle);
            var setters = elementStyle.Setters;

            // ReSharper disable AssignNullToNotNullAttribute
            setters.Add(new Setter(FrameworkElement.LanguageProperty, languageBinding));
            setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, flowDirectionBinding));
            // ReSharper restore AssignNullToNotNullAttribute

            elementStyle.Seal();
            column.ElementStyle = elementStyle;
        }

        private static void EditingElement_PreviewKeyDown([NotNull] object sender, [NotNull] KeyEventArgs e)
        {
            Contract.Requires(sender != null);

            if (e.Key != Key.Return)
                return;

            e.Handled = true;
            var editingElement = (TextBox)sender;

            if (IsKeyDown(Key.LeftCtrl) || IsKeyDown(Key.RightCtrl))
            {
                // Ctrl+Return adds a new line
                editingElement.SelectedText = Environment.NewLine;
                editingElement.SelectionLength = 0;
                editingElement.SelectionStart += Environment.NewLine.Length;
            }
            else
            {
                // Return without Ctrl: Forward to parent, grid should move focused cell down.
                var parent = (FrameworkElement)editingElement.Parent;
                if (parent == null)
                    return;

                // ReSharper disable once AssignNullToNotNullAttribute
                var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Return)
                {
                    RoutedEvent = UIElement.KeyDownEvent
                };

                parent.RaiseEvent(args);
            }
        }

        private static bool IsKeyDown(this Key key)
        {
            return (Keyboard.GetKeyStates(key) & KeyStates.Down) != 0;
        }

        public static bool IsItemInvariant(this DataGridCellInfo info)
        {
            var col = info.Column?.Header as ILanguageColumnHeader;

            if (col?.ColumnType != ColumnType.Language)
                return false;

            if (!(info.Item is ResourceTableEntry item))
                return false;

            if (item.IsItemInvariant.GetValue(col.CultureKey))
            {
                return true;
            }

            return false;
        }

        public static bool IsOfColumnType(this DataGridCellInfo cell, [NotNull] params ColumnType[] columnTypes)
        {
            Contract.Requires(columnTypes != null);
            return columnTypes.Any(columnType => columnType == (cell.Column?.Header as ILanguageColumnHeader)?.ColumnType);
        }

    }
}