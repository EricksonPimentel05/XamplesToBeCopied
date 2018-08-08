﻿namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Converters;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Converters;

    using BooleanToVisibilityConverter = TomsToolbox.Wpf.Converters.BooleanToVisibilityConverter;

    public static class ColumnManager
    {
        [NotNull]
        private const string NeutralCultureKeyString = ".";
        [NotNull]
        private static readonly BitmapImage _codeReferencesImage = new BitmapImage(new Uri("/ResXManager.View;component/Assets/references.png", UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Identifies the ResourceFileExists attached property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty ResourceFileExistsProperty =
            DependencyProperty.RegisterAttached("ResourceFileExists", typeof(bool), typeof(ColumnManager), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the CellAnnotations attached property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty CellAnnotationsProperty =
            DependencyProperty.RegisterAttached("CellAnnotations", typeof(ICollection<string>), typeof(ColumnManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Identifies the IsCellInvariant attached property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty IsCellInvariantProperty =
            DependencyProperty.RegisterAttached("IsCellInvariant", typeof(bool), typeof(ColumnManager), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, IsCellInvariant_Changed));

        [NotNull]
        public static readonly DependencyProperty SelectedCellsProperty =
            DependencyProperty.RegisterAttached("SelectedCells", typeof(IList<DataGridCellInfo>), typeof(ColumnManager));

        public static void SetupColumns([NotNull] this DataGrid dataGrid, [NotNull] ResourceManager resourceManager, [NotNull] ResourceViewModel resourceViewModel, [NotNull] Configuration configuration)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(resourceManager != null);
            Contract.Requires(resourceViewModel != null);
            Contract.Requires(configuration != null);

            var dataGridEvents = dataGrid.GetAdditionalEvents();

            dataGridEvents.ColumnVisibilityChanged -= DataGrid_ColumnVisibilityChanged;
            dataGridEvents.ColumnVisibilityChanged += DataGrid_ColumnVisibilityChanged;

            dataGrid.CurrentCellChanged -= DataGrid_CurrentCellChanged;
            dataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;

            var columns = dataGrid.Columns;

            if (columns.Count == 0)
            {
                columns.Add(CreateKeyColumn());
                columns.Add(CreateIndexColumn(resourceViewModel, configuration));
                columns.Add(CreateCodeReferencesColumn(dataGrid));
            }

            var languageColumns = columns.Skip(3).ToArray();

            IEnumerable<CultureKey> cultureKeys = resourceManager.Cultures;

            var disconnectedColumns = languageColumns.Where(col => cultureKeys.All(cultureKey => !Equals(col.GetCultureKey(), cultureKey)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedcultureKeys = cultureKeys.Where(cultureKey => languageColumns.All(col => !Equals(col.GetCultureKey(), cultureKey)));

            foreach (var cultureKey in addedcultureKeys)
            {
                Contract.Assume(cultureKey != null);
                dataGrid.AddLanguageColumn(configuration, cultureKey);
            }
        }

        public static void CreateNewLanguageColumn([NotNull] this DataGrid dataGrid, [NotNull] Configuration configuration, [CanBeNull] CultureInfo culture)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(configuration != null);

            var cultureKey = new CultureKey(culture);

            dataGrid.AddLanguageColumn(configuration, cultureKey);

            var key = cultureKey.ToString(NeutralCultureKeyString);

            HiddenLanguageColumns = HiddenLanguageColumns.Where(col => !string.Equals(col, key, StringComparison.OrdinalIgnoreCase));
        }

        [NotNull]
        private static DataGridTextColumn CreateKeyColumn()
        {
            Contract.Ensures(Contract.Result<DataGridTextColumn>() != null);

            return new DataGridTextColumn
            {
                Header = new ColumnHeader(Resources.Key, ColumnType.Key),
                Binding = new Binding(nameof(ResourceTableEntry.Key)) { ValidatesOnDataErrors = true },
                Width = 200,
                CanUserReorder = false,
            };
        }

        [NotNull]
        private static DataGridTextColumn CreateIndexColumn([CanBeNull] ResourceViewModel resourceViewModel, [CanBeNull] Configuration configuration)
        {
            Contract.Ensures(Contract.Result<DataGridTextColumn>() != null);

            var elementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
                    new Setter(TextBlock.PaddingProperty, new Thickness(2, 0, 2, 0)),
                    new Setter(FrameworkElement.ToolTipProperty, Resources.IndexColumnToolTip)
                }
            };

            var columnHeader = new ColumnHeader("#", ColumnType.Other)
            {
                ToolTip = Resources.IndexColumnHeaderToolTip,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                ElementStyle = elementStyle,
                Width = 26,
                Binding = new Binding(@"Index"),
                CanUserReorder = false,
            };

            column.SetIsFilterVisible(false);

            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(nameof(Settings.IsIndexColumnVisible)) { Source = Settings.Default, Converter = BooleanToVisibilityConverter.Default });
            BindingOperations.SetBinding(column, DataGridColumn.IsReadOnlyProperty, new MultiBinding
            {
                Converter = LogicalMultiValueConverter.Or,
                Bindings =
                {
                    new Binding(nameof(ResourceViewModel.SelectedEntities) + ".Count") { Source = resourceViewModel, Converter = BinaryOperationConverter.Inequality, ConverterParameter = 1 },
                    new Binding(nameof(Configuration.SortFileContentOnSave)) { Source = configuration }
                }
            });

            return column;
        }

        [NotNull]
        private static Image CreateCodeReferencesImage()
        {
            Contract.Ensures(Contract.Result<Image>() != null);

            return new Image
            {
                Source = _codeReferencesImage,
                SnapsToDevicePixels = true
            };
        }

        [NotNull]
        private static DataGridColumn CreateCodeReferencesColumn([NotNull] FrameworkElement dataGrid)
        {
            Contract.Requires(dataGrid != null);
            Contract.Ensures(Contract.Result<DataGridColumn>() != null);

            var elementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center)
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(@"CodeReferences.Count"),
                        Value = null,
                        Setters =
                        {
                            new Setter(UIElement.OpacityProperty, 0.3)
                        }
                    }
                }
            };

            var columnHeader = new ColumnHeader(CreateCodeReferencesImage(), ColumnType.Other)
            {
                ToolTip = Resources.CodeReferencesToolTip,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var cellStyle = new Style(typeof(DataGridCell))
            {
                Setters =
                {
                    new Setter(ToolTipService.ShowDurationProperty, int.MaxValue),
                    new Setter(FrameworkElement.ToolTipProperty, new CodeReferencesToolTip(dataGrid.GetExportProvider()))
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(@"CodeReferences.Count") { Converter = BinaryOperationConverter.GreaterThan, ConverterParameter = 50},
                        Value = true,
                        Setters =
                        {
                            new Setter(FrameworkElement.ToolTipProperty, Resources.CodeReferencesTooManyDetailsToolTip)
                        }
                    }
                }

            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                CellStyle = cellStyle,
                ElementStyle = elementStyle,
                Binding = new Binding(@"CodeReferences.Count") { FallbackValue = "?" },
                Width = DataGridLength.SizeToHeader,
                CanUserReorder = false,
                CanUserResize = false,
                IsReadOnly = true,
            };

            column.SetIsFilterVisible(false);
            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(@"IsFindCodeReferencesEnabled") { Source = Model.Properties.Settings.Default, Converter = BooleanToVisibilityConverter.Default });

            return column;
        }

        private static void AddLanguageColumn([NotNull] this DataGrid dataGrid, [NotNull] Configuration configuration, [NotNull] CultureKey cultureKey)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(configuration != null);
            Contract.Requires(cultureKey != null);

            var columns = dataGrid.Columns;

            var key = cultureKey.ToString(NeutralCultureKeyString);

            var culture = cultureKey.Culture;
            var languageBinding = culture != null
                ? new Binding { Source = culture }
                : new Binding("NeutralResourcesLanguage") { Source = configuration };

            languageBinding.Converter = CultureToXmlLanguageConverter.Default;
            // It's important to explicitly set the converter culture here, else we will get a binding error, because here the source for the converter culture is the target of the binding.
            languageBinding.ConverterCulture = CultureInfo.InvariantCulture;

            var flowDirectionBinding = culture != null
                ? new Binding("TextInfo.IsRightToLeft") { Source = culture }
                : new Binding("NeutralResourcesLanguage.TextInfo.IsRightToLeft") { Source = configuration };

            flowDirectionBinding.Converter = IsRightToLeftToFlowDirectionConverter.Default;

            var cellStyle = new Style(typeof(DataGridCell), dataGrid.CellStyle);
            cellStyle.Setters.Add(new Setter(ResourceFileExistsProperty, new Binding(@"FileExists[" + key + @"]")));

            var commentCellStyle = new Style(typeof(DataGridCell), cellStyle);
            commentCellStyle.Setters.Add(new Setter(CellAnnotationsProperty, new Binding(@"CommentAnnotations[" + key + @"]")));

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(configuration, cultureKey),
                Binding = new Binding(@"Comments[" + key + @"]"),
                MinWidth = 50,
                CellStyle = commentCellStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Visibility = VisibleCommentColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Hidden
            };

            columns.AddLanguageColumn(commentColumn, languageBinding, flowDirectionBinding);

            var textCellStyle = new Style(typeof(DataGridCell), cellStyle)
            {
                Setters =
                {
                    new Setter(CellAnnotationsProperty, new Binding(@"ValueAnnotations[" + key + @"]")),
                    new Setter(IsCellInvariantProperty, new Binding(@"IsItemInvariant[" + key + @"]")),
                }
            };

            var textElementStyle = new Style(typeof(TextBlock))
            {
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(nameof(Settings.IsWrapLinesEnabled)) { Source = Settings.Default },
                        Value = true,
                        Setters = { new Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow ) }
                    }
                }
            };

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(configuration, cultureKey),
                Binding = new Binding(@"Values[" + key + @"]"),
                MinWidth = 120,
                CellStyle = textCellStyle,
                ElementStyle = textElementStyle,
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                Visibility = HiddenLanguageColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Hidden : Visibility.Visible
            };

            columns.AddLanguageColumn(column, languageBinding, flowDirectionBinding);
        }

        private static void AddLanguageColumn([NotNull][ItemNotNull] this ICollection<DataGridColumn> columns, [NotNull] DataGridBoundColumn column, [NotNull] Binding languageBinding, [CanBeNull] Binding flowDirectionBinding)
        {
            Contract.Requires(columns != null);
            Contract.Requires(languageBinding != null);
            Contract.Requires(column != null);

            column.SetElementStyle(languageBinding, flowDirectionBinding);
            column.SetEditingElementStyle(languageBinding, flowDirectionBinding);
            columns.Add(column);
        }

        private static void DataGrid_ColumnVisibilityChanged([NotNull] object sender, [NotNull] EventArgs e)
        {
            Contract.Requires(sender != null);

            var dataGrid = (DataGrid)sender;

            VisibleCommentColumns = UpdateColumnSettings<CommentHeader>(VisibleCommentColumns, dataGrid, col => col.Visibility == Visibility.Visible);
            HiddenLanguageColumns = UpdateColumnSettings<LanguageHeader>(HiddenLanguageColumns, dataGrid, col => col.Visibility != Visibility.Visible);
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> UpdateColumnSettings<T>([NotNull][ItemNotNull] IEnumerable<string> current, [NotNull] DataGrid dataGrid, [NotNull] Func<DataGridColumn, bool> includePredicate)
            where T : LanguageColumnHeaderBase
        {
            Contract.Requires(current != null);
            Contract.Requires(dataGrid != null);
            Contract.Requires(includePredicate != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            Func<DataGridColumn, bool> excludePredicate = col => !includePredicate(col);

            return current.Concat(GetColumnKeys<T>(dataGrid, includePredicate))
                .Except(GetColumnKeys<T>(dataGrid, excludePredicate))
                .Distinct();
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> GetColumnKeys<T>([NotNull] DataGrid dataGrid, [NotNull] Func<DataGridColumn, bool> predicate)
            where T : LanguageColumnHeaderBase
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(predicate != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            return dataGrid.Columns
                .Where(predicate)
                .Select(col => col.Header)
                .OfType<T>()
                .Select(hdr => hdr.CultureKey.ToString(NeutralCultureKeyString));
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> VisibleCommentColumns
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return (Settings.Default.VisibleCommentColumns ?? string.Empty).Split(',');
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.VisibleCommentColumns = string.Join(",", value);
            }
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<string> HiddenLanguageColumns
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return (Settings.Default.HiddenLanguageColumns ?? string.Empty).Split(',');
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.HiddenLanguageColumns = string.Join(",", value);
            }
        }

        private static void DataGrid_CurrentCellChanged([NotNull] object sender, [NotNull] EventArgs eventArgs)
        {
            var dataGrid = (DataGrid)sender;
            // postpone update, SelectedCells is updates *after* the current cell has changed.
            dataGrid?.Dispatcher?.BeginInvoke(() =>
            {
                dataGrid.SetValue(SelectedCellsProperty, dataGrid.SelectedCells?.ToArray());
            });
        }

        private static void IsCellInvariant_Changed([CanBeNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = d?.TryFindAncestorOrSelf<DataGrid>();

            if (dataGrid != null)
            {
                // force an update of the selected cells property, else the value converter won't get triggered.
                DataGrid_CurrentCellChanged(dataGrid, EventArgs.Empty);
            }
        }

        private class IsRightToLeftToFlowDirectionConverter : IValueConverter
        {
            [NotNull]
            public static readonly IValueConverter Default = new IsRightToLeftToFlowDirectionConverter();

            [NotNull]
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                Contract.Ensures(Contract.Result<object>() != null);

                return true.Equals(value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}