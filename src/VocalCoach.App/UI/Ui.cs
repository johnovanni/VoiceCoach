namespace VocalCoach.App.UI;

/// <summary>Small factory helpers so pages built in C# stay short and consistent.</summary>
public static class Ui
{
    public static Label Title(string text, double size = 26) => new()
    {
        Text = text, FontSize = size, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text,
    };

    public static Label Heading(string text) => new()
    {
        Text = text, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text,
    };

    public static Label Body(string text) => new() { Text = text, FontSize = 15, TextColor = Theme.Text, LineHeight = 1.2 };

    public static Label Muted(string text, double size = 13) => new() { Text = text, FontSize = size, TextColor = Theme.Muted };

    public static Border Card(View content, Color? background = null, double padding = 16) => new()
    {
        Content = content,
        Padding = padding,
        BackgroundColor = background ?? Theme.Surface,
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
    };

    public static Button Primary(string text, Action onClick)
    {
        var b = new Button
        {
            Text = text, BackgroundColor = Theme.Accent, TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, CornerRadius = 12, HeightRequest = 48, FontSize = 16,
        };
        b.Clicked += (_, _) => onClick();
        return b;
    }

    public static Button Secondary(string text, Action onClick)
    {
        var b = new Button
        {
            Text = text, BackgroundColor = Theme.SurfaceRaised, TextColor = Theme.Text,
            CornerRadius = 12, HeightRequest = 44, FontSize = 15,
        };
        b.Clicked += (_, _) => onClick();
        return b;
    }

    public static Border Chip(string text, Color color) => new()
    {
        Content = new Label { Text = text, FontSize = 12, TextColor = color, FontAttributes = FontAttributes.Bold },
        Padding = new Thickness(10, 4),
        BackgroundColor = color.WithAlpha(0.15f),
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
        HorizontalOptions = LayoutOptions.Start,
    };

    public static View Bullets(IEnumerable<string> items, string bullet = "•")
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        foreach (var item in items)
        {
            stack.Add(new Grid
            {
                ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)],
                ColumnSpacing = 8,
                Children = { new Label { Text = bullet, TextColor = Theme.Accent, FontSize = 15 }, Body(item).Column(1) },
            });
        }
        return stack;
    }

    /// <summary>Scrollable page body with standard padding.</summary>
    public static ScrollView Page(params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 12, 16, 24) };
        foreach (var c in children) stack.Add(c);
        return new ScrollView { Content = stack };
    }

    public static string Stars(int difficulty) => new string('●', difficulty) + new string('○', 3 - difficulty);

    public static T Column<T>(this T view, int column) where T : BindableObject
    {
        Grid.SetColumn(view, column);
        return view;
    }

    public static T Row<T>(this T view, int row) where T : BindableObject
    {
        Grid.SetRow(view, row);
        return view;
    }

    /// <summary>Configure a view inline: <c>Ui.Muted("x").Also(l => l.HorizontalTextAlignment = ...)</c>.</summary>
    public static T Also<T>(this T view, Action<T> configure)
    {
        configure(view);
        return view;
    }

    public static T Tap<T>(this T view, Action onTap) where T : View
    {
        var gesture = new TapGestureRecognizer();
        gesture.Tapped += (_, _) => onTap();
        view.GestureRecognizers.Add(gesture);
        return view;
    }
}
