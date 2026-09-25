using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.Config;
using System;

namespace Nikse.SubtitleEdit.Features.Tools.SpeakerProfiles;

public class SpeakerProfilesWindow : Window
{
    private static readonly SpeakerGender[] GenderOptions = Enum.GetValues<SpeakerGender>();

    public SpeakerProfilesWindow(SpeakerProfilesViewModel vm)
    {
        UiUtil.InitializeWindow(this, GetType().Name);
        Title = Se.Language.Tools.SpeakerProfiles.Title;
        Width = 760;
        Height = 480;
        MinWidth = 620;
        MinHeight = 340;
        CanResize = true;

        vm.Window = this;
        DataContext = vm;

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
            },
            Margin = UiUtil.MakeWindowMargin(),
            RowSpacing = 12,
        };

        root.Add(BuildHeader(vm), 0);
        root.Add(BuildTable(vm), 1);
        root.Add(UiUtil.MakeButtonBar(
            UiUtil.MakeButtonOk(vm.OkCommand),
            UiUtil.MakeButtonCancel(vm.CancelCommand)), 2);

        Content = root;
        KeyDown += (_, e) => vm.OnKeyDown(e);
        Closing += (_, _) => UiUtil.SaveWindowPosition(this);
        Loaded += (_, _) => UiUtil.RestoreWindowPosition(this);
    }

    private static Control BuildHeader(SpeakerProfilesViewModel vm)
    {
        return new StackPanel
        {
            Spacing = 3,
            Children =
            {
                new TextBlock
                {
                    Text = Se.Language.Tools.SpeakerProfiles.Title,
                    FontSize = UiUtil.ScaledFontSize(16),
                    FontWeight = FontWeight.SemiBold,
                },
                new TextBlock
                {
                    Text = Se.Language.Tools.SpeakerProfiles.Subtitle,
                    Foreground = UiUtil.GetTextColor(0.65d),
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Foreground = UiUtil.GetTextColor(0.55d),
                    [!TextBlock.TextProperty] = new Binding(nameof(vm.SummaryText)),
                },
            },
        };
    }

    private static Border BuildTable(SpeakerProfilesViewModel vm)
    {
        var table = TableViewExtras.MakeTableView(multiSelect: false);
        table[!TableView.ItemsSourceProperty] = new Binding(nameof(vm.Rows));
        TableViewExtras.BindSelectedItem(table, vm, nameof(vm.SelectedRow));

        table.Columns.Add(new SeTableViewColumn
        {
            Header = Se.Language.General.Name,
            Width = new GridLength(1, GridUnitType.Star),
            CellTheme = UiUtil.TableViewNoPaddingCellTheme,
            HeaderTheme = UiUtil.TableViewColumnHeaderTheme,
            CellTemplate = new FuncDataTemplate<SpeakerProfileRow>((_, _) => new TextBox
            {
                Margin = new Thickness(4, 2),
                [!TextBox.TextProperty] = new Binding(nameof(SpeakerProfileRow.Name)) { Mode = BindingMode.TwoWay },
            }),
        });
        table.Columns.Add(new SeTableViewColumn
        {
            Header = Se.Language.General.Lines,
            Binding = new Binding(nameof(SpeakerProfileRow.LineCount)),
            Width = new GridLength(70),
            CellTheme = UiUtil.TableViewCellTheme,
            HeaderTheme = UiUtil.TableViewColumnHeaderTheme,
        });
        table.Columns.Add(new SeTableViewColumn
        {
            Header = Se.Language.Tools.SpeakerProfiles.Gender,
            Width = new GridLength(150),
            CellTheme = UiUtil.TableViewNoPaddingCellTheme,
            HeaderTheme = UiUtil.TableViewColumnHeaderTheme,
            CellTemplate = new FuncDataTemplate<SpeakerProfileRow>((_, _) => new ComboBox
            {
                Margin = new Thickness(4, 2),
                ItemsSource = GenderOptions,
                [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(SpeakerProfileRow.Gender)) { Mode = BindingMode.TwoWay },
            }),
        });
        table.Columns.Add(new SeTableViewColumn
        {
            Header = Se.Language.Tools.SpeakerProfiles.Confidence,
            Binding = new Binding(nameof(SpeakerProfileRow.ConfidenceDisplay)),
            Width = new GridLength(100),
            CellTheme = UiUtil.TableViewCellTheme,
            HeaderTheme = UiUtil.TableViewColumnHeaderTheme,
        });
        table.Columns.Add(new SeTableViewColumn
        {
            Header = Se.Language.Tools.SpeakerProfiles.Source,
            Binding = new Binding(nameof(SpeakerProfileRow.Source)),
            Width = new GridLength(110),
            CellTheme = UiUtil.TableViewCellTheme,
            HeaderTheme = UiUtil.TableViewColumnHeaderTheme,
        });

        return new Border
        {
            BorderBrush = UiUtil.GetBorderBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = table,
        };
    }
}
