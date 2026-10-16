using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public sealed class DonationDetailsPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Muted = Color.FromArgb("#82919C");
    private static readonly Color Teal = Color.FromArgb("#00A896");

    public DonationDetailsPage(Debito debit, TipoDoacao? type)
    {
        Title = "Detalhes da doação";
        BackgroundColor = Color.FromArgb("#F5F8FA");
        var body = new VerticalStackLayout { Padding = new Thickness(20, 15, 20, 28), Spacing = 15 };
        var close = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, BackgroundColor = Colors.White, TextColor = Ink, CornerRadius = 14, FontSize = 25, Padding = 0 };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var pageHeader = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(48), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 9 };
        pageHeader.Add(close, 0);
        pageHeader.Add(new Label { Text = "Detalhes da doação", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalTextAlignment = TextAlignment.Center }, 1);
        body.Add(pageHeader);
        var statusColor = debit.StatusName switch { "Pago" => Color.FromArgb("#208B69"), "Rejeitado" => Color.FromArgb("#C95050"), _ => Color.FromArgb("#BE8423") };
        var statusBg = debit.StatusName switch { "Pago" => Color.FromArgb("#E7F6EF"), "Rejeitado" => Color.FromArgb("#FCEDED"), _ => Color.FromArgb("#FFF4DC") };
        var overview = new VerticalStackLayout { Spacing = 11, Children =
        {
            new Label { Text = debit.NomeTipoDoacao, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ink },
            new Label { Text = debit.Valor.ToString("C", new System.Globalization.CultureInfo("pt-BR")), FontSize = 31, FontAttributes = FontAttributes.Bold, TextColor = Teal },
            new Border { BackgroundColor = statusBg, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = new Thickness(11, 6), HorizontalOptions = LayoutOptions.Start, Content = new Label { Text = debit.StatusName, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = statusColor } }
        }};
        body.Add(new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E6ECEF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = 18, Content = overview });
        body.Add(new Label { Text = "INFORMAÇÕES", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.3, TextColor = Muted });
        body.Add(InfoCard("Mês de referência", debit.ReferenceMonth.ToString("MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"))));
        body.Add(InfoCard("Data do lançamento", debit.DataCriacao.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm")));
        body.Add(InfoCard("Campanha válida até", type?.DataExpiracao.ToString("MM/yyyy") ?? "—"));
        body.Add(new Label { Text = "COMPROVANTE", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.3, TextColor = Muted, Margin = new Thickness(0, 6, 0, 0) });
        if (string.IsNullOrWhiteSpace(debit.UrlComprovante))
        {
            body.Add(new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E6ECEF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 18, Content = new Label { Text = "Ainda não há comprovante anexado a esta doação.", FontSize = 12, TextColor = Muted } });
        }
        else if (IsImage(debit.UrlComprovante))
        {
            var image = new Image { Source = ImageSource.FromUri(new Uri(debit.UrlComprovante)), Aspect = Aspect.AspectFit, HeightRequest = 280 };
            body.Add(new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E6ECEF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 9, Content = image });
        }
        else
        {
            var open = new Button { Text = "Abrir comprovante PDF  ↗", BackgroundColor = Color.FromArgb("#E6F7F5"), TextColor = Teal, CornerRadius = 13, HeightRequest = 49, FontAttributes = FontAttributes.Bold };
            open.Clicked += async (_, _) => await Launcher.Default.OpenAsync(debit.UrlComprovante);
            body.Add(open);
        }
        Content = new ScrollView { Content = body };
    }

    private static View InfoCard(string title, string value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        grid.Add(new Label { Text = title, FontSize = 11, TextColor = Muted }, 0);
        grid.Add(new Label { Text = value, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Ink, HorizontalTextAlignment = TextAlignment.End }, 1);
        return new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E6ECEF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 14, Content = grid };
    }
    private static bool IsImage(string url) => new[] { ".jpg", ".jpeg", ".png", ".webp" }.Any(ext => url.Contains(ext, StringComparison.OrdinalIgnoreCase));
}
