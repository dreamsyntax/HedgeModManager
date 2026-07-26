using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using HedgeModManager.UI.Models;
using HedgeModManager.UI.ViewModels;
using HedgeModManager.UI.ViewModels.Settings;

namespace HedgeModManager.UI.Controls.Settings;

public partial class Settings : UserControl
{
    public static readonly StyledProperty<UIGame?> GameProperty =
        AvaloniaProperty.Register<Settings, UIGame?>(nameof(Game));

    public static readonly StyledProperty<ModProfile?> ProfileProperty =
        AvaloniaProperty.Register<Settings, ModProfile?>(nameof(Profile));

    public SettingsBase? MainSettingsControl { get; set; }
    public SettingsViewModel ViewModel { get; set; } = new ();

    public UIGame? Game
    {
        get => GetValue(GameProperty);
        set => SetValue(GameProperty, value);
    }

    public ModProfile? Profile
    {
        get => GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    public Settings()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null)
            return;

        var settingsMain = new SettingsMain();
        settingsMain.ViewModel.MainViewModel = viewModel;
        settingsMain.Settings = this;

        settingsMain.Bind(SettingsMain.GameProperty, new Binding()
        {
            Source = viewModel,
            Path = "SelectedGame"
        });
        settingsMain.Bind(SettingsMain.ProfileProperty, new Binding()
        {
            Source = viewModel,
            Path = "SelectedProfile"
        });

        MainSettingsControl = settingsMain;
        Panels.Children.Clear();
        Panels.Children.Add(MainSettingsControl);
        UpdateTitle();
    }

    private static TranslateTransform GetTranslate(Control control)
    {
        if (control.RenderTransform is not TranslateTransform tt)
        {
            tt = new TranslateTransform();
            control.RenderTransform = tt;
        }
        return tt;
    }

    private static Animation CreateSlideAnimation(double toX)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new LinearEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, toX)
                    }
                }
            }
        };
    }

    // TODO: Needs better handling
    public async Task SwitchPanel(SettingsBase? control)
    {
        if (control != null && control == Panels.Children.LastOrDefault())
            return;

        if (control == null && MainSettingsControl == Panels.Children.LastOrDefault())
            return;

        double width = ScrollViewerPanel.Viewport.Width;
        if (width <= 0)
            width = Bounds.Width;

        if (MainSettingsControl != null &&
            (control == MainSettingsControl || control == null))
        {
            var main = MainSettingsControl;
            var current = Panels.Children.OfType<SettingsBase>().FirstOrDefault(c => c != main);

            Panels.Children.Insert(0, main);
            UpdateTitle();

            if (current != null)
            {
                var mainTT = GetTranslate(main);
                var currentTT = GetTranslate(current);

                mainTT.X = -width;
                currentTT.X = 0;

                _ = CreateSlideAnimation(0).RunAsync(mainTT);
                await CreateSlideAnimation(width).RunAsync(currentTT);

                mainTT.X = 0;
                current.RenderTransform = null;
                Panels.Children.Remove(current);
            }
        }
        else if (control != null)
        {
            var main = Panels.Children.OfType<SettingsBase>().FirstOrDefault();

            Panels.Children.Add(control);
            control.Settings = this;
            control.SettingsParent = main;
            UpdateTitle();

            var newTT = GetTranslate(control);
            newTT.X = width;

            if (main != null)
            {
                var mainTT = GetTranslate(main);
                mainTT.X = 0;
                _ = CreateSlideAnimation(-width).RunAsync(mainTT);
            }

            await CreateSlideAnimation(0).RunAsync(newTT);
            // TODO
            newTT.X = 0;

            if (main != null)
            {
                Panels.Children.Remove(main);
                main.RenderTransform = null;
            }
        }
    }

    public void UpdateTitle()
    {
        TextBlock directoryTextBlock = new()
        {
            Text = ">",
            Margin = new Thickness(0, 0, 18, 0),
            FontSize = 30,
            FontWeight = FontWeight.Bold
        };
        TitlePanel.Children.Clear();
        foreach (var child in Panels.Children)
        {
            if (child is not SettingsBase settings)
                continue;

            TextBlock textBlock = new()
            {
                Margin = new Thickness(0, 0, 18, 0),
                FontSize = 30,
                FontWeight = FontWeight.Bold
            };
            textBlock.Bind(
                TextBlock.TextProperty,
                new DynamicResourceExtension(settings.Title)
            );
            textBlock.PointerReleased += async (s, e) =>
            {
                await SwitchPanel(settings);
            };

            if (Panels.Children.IndexOf(child) != 0)
                TitlePanel.Children.Add(directoryTextBlock);
            TitlePanel.Children.Add(textBlock);
        }
    }
}