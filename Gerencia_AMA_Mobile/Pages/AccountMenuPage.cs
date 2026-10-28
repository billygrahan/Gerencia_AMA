using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public enum AccountMenuAction { None, Settings, OpenTreasury, MemberArea, ManageUsers, ManageTypes, Logout }

public sealed class AccountMenuPage : ContentPage
{
    private readonly TaskCompletionSource<AccountMenuAction> _completion = new();
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Muted = Color.FromArgb("#82919C");
    private static readonly Color Teal = Color.FromArgb("#00A896");

    public AccountMenuPage(Usuario user, bool isTreasurer, bool showingTreasury)
    {
        Title = "Conta";
        BackgroundColor = Color.FromArgb("#F3F7F8");
        NavigationPage.SetHasNavigationBar(this, false);

        var close = new Button { Text = "×", WidthRequest = 42, HeightRequest = 42, BackgroundColor = Colors.White, TextColor = Muted, CornerRadius = 14, FontSize = 24, Padding = 0, HorizontalOptions = LayoutOptions.End };
        close.Clicked += async (_, _) => await Finish(AccountMenuAction.None);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(44) }, Children = { new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = "Sua conta", FontSize = 23, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = "Perfil, preferências e acesso", FontSize = 12, TextColor = Muted } } }, close } };
        header.SetColumn(close, 1);

        var avatar = new Border { WidthRequest = 54, HeightRequest = 54, BackgroundColor = Color.FromArgb("#DDF3F0"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 27 }, Content = new Label { Text = string.IsNullOrWhiteSpace(user.Nome) ? "A" : user.Nome[..1].ToUpperInvariant(), FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Teal, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        var identity = new HorizontalStackLayout { Spacing = 13, VerticalOptions = LayoutOptions.Center, Children = { avatar, new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = user.Nome, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = user.Email, FontSize = 11, TextColor = Muted }, new Label { Text = isTreasurer ? "Tesoureiro" : "Membro", FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Teal } } } } };
        var identityCard = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E4EBED"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = 16, Content = identity };

        var actions = new VerticalStackLayout { Spacing = 9 };
        actions.Add(ActionButton("⚙", "Configurações da conta", "Atualize seu nome, e-mail e senha", AccountMenuAction.Settings));
        if (isTreasurer)
        {
            actions.Add(ActionButton("♙", "Gerenciar usuários", "Editar cadastros e perfis", AccountMenuAction.ManageUsers));
            actions.Add(ActionButton("✳", "Gerenciar campanhas", "Valores, períodos e destinatários", AccountMenuAction.ManageTypes));
            actions.Add(ActionButton("▦", showingTreasury ? "Voltar à área do membro" : "Abrir painel da tesouraria", "Alternar entre as áreas do sistema", showingTreasury ? AccountMenuAction.MemberArea : AccountMenuAction.OpenTreasury));
        }
        var logout = new Button { Text = "Sair da conta", BackgroundColor = Color.FromArgb("#FCEDED"), TextColor = Color.FromArgb("#C95050"), CornerRadius = 14, HeightRequest = 49, FontAttributes = FontAttributes.Bold };
        logout.Clicked += async (_, _) => await Finish(AccountMenuAction.Logout);

        var body = new VerticalStackLayout { Padding = new Thickness(20, 25, 20, 28), Spacing = 20, Children = { header, identityCard, new Label { Text = "ACESSO RÁPIDO", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Muted }, actions, logout } };
        Content = new ScrollView { Content = body };
    }

    public async Task<AccountMenuAction> PresentAsync(Page parent)
    {
        await parent.Navigation.PushModalAsync(new NavigationPage(this) { BarBackgroundColor = Color.FromArgb("#F3F7F8"), BarTextColor = Ink });
        return await _completion.Task;
    }

    protected override void OnDisappearing() { base.OnDisappearing(); _completion.TrySetResult(AccountMenuAction.None); }

    private View ActionButton(string icon, string title, string subtitle, AccountMenuAction action)
    {
        var iconTile = new Border { WidthRequest = 38, HeightRequest = 38, BackgroundColor = Color.FromArgb("#E6F7F4"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = new Label { Text = icon, FontSize = 17, TextColor = Teal, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        var titleBlock = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = subtitle, FontSize = 10, TextColor = Muted } } };
        var content = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(38), new(GridLength.Star), new(20) }, ColumnSpacing = 12, VerticalOptions = LayoutOptions.Center, Children = { iconTile, titleBlock, new Label { Text = "›", FontSize = 22, TextColor = Muted, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End } } };
        content.SetColumn(titleBlock, 1);content.SetColumn(content.Children[2], 2);
        var card = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E4EBED"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 13, Content = content };
        var tap = new TapGestureRecognizer();tap.Tapped += async (_, _) => await Finish(action);card.GestureRecognizers.Add(tap);
        return card;
    }

    private async Task Finish(AccountMenuAction action) { _completion.TrySetResult(action); await Navigation.PopModalAsync(); }
}
