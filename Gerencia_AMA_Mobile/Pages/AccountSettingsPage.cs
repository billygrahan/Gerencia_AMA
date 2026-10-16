using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public sealed class AccountSettingsPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Func<Task> _onLogout;
    private readonly Entry _name;
    private readonly Entry _email;
    private readonly Entry _password;
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Muted = Color.FromArgb("#82919C");
    private static readonly Color Teal = Color.FromArgb("#00A896");

    public AccountSettingsPage(ApiClient api, Func<Task> onLogout)
    {
        _api = api;
        _onLogout = onLogout;
        Title = "Minha conta";
        BackgroundColor = Color.FromArgb("#F5F8FA");
        _name = new Entry { Text = api.CurrentUser?.Nome, Placeholder = "Nome completo", TextColor = Ink };
        _email = new Entry { Text = api.CurrentUser?.Email, Placeholder = "seu@email.com", Keyboard = Keyboard.Email, TextColor = Ink };
        _password = new Entry { Placeholder = "Deixe vazio para manter a senha", IsPassword = true, TextColor = Ink };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(48), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        var back = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, BackgroundColor = Colors.White, TextColor = Ink, CornerRadius = 14, FontSize = 25, Padding = 0 };
        back.Clicked += async (_, _) => await Navigation.PopModalAsync();
        header.Add(back, 0);
        header.Add(new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2, Children = { new Label { Text = "Minha conta", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = api.IsTreasurer ? "Perfil de tesoureiro" : "Perfil de membro", FontSize = 12, TextColor = Muted } } }, 1);

        var save = new Button { Text = "Salvar configurações", BackgroundColor = Teal, TextColor = Colors.White, CornerRadius = 14, HeightRequest = 50, FontAttributes = FontAttributes.Bold };
        save.Clicked += async (_, _) => await Save();
        var logout = new Button { Text = "Sair da conta", BackgroundColor = Color.FromArgb("#FCEDED"), TextColor = Color.FromArgb("#C95050"), CornerRadius = 14, HeightRequest = 48, FontAttributes = FontAttributes.Bold };
        logout.Clicked += async (_, _) => await SignOut();

        var form = new VerticalStackLayout { Spacing = 16 };
        form.Add(Field("Nome", _name));form.Add(Field("E-mail", _email));form.Add(Field("Nova senha", _password));
        form.Add(new Label { Text = "Seu perfil de acesso é definido pela tesouraria.", FontSize = 12, TextColor = Muted, Margin = new Thickness(0, 2) });
        form.Add(save);

        var access = new VerticalStackLayout { Spacing = 10, Children = { new Label { Text = "ACESSO", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Muted }, new Label { Text = api.IsTreasurer ? "Você pode administrar membros, campanhas e comprovantes." : "Você pode consultar seu calendário, contribuições e comprovantes.", FontSize = 13, TextColor = Ink, LineBreakMode = LineBreakMode.WordWrap } } };
        var body = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 26), Spacing = 18, Children = { header, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E6ECEF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = 18, Content = form }, new Border { BackgroundColor = Color.FromArgb("#E6F7F4"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 16, Content = access }, logout } };
        Content = new ScrollView { Content = body };
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_email.Text))
        {
            await DisplayAlertAsync("Dados obrigatórios", "Informe seu nome e e-mail.", "OK");
            return;
        }
        try
        {
            await _api.UpdateMyProfile(_name.Text.Trim(), _email.Text.Trim(), _password.Text);
            _password.Text = string.Empty;
            await DisplayAlertAsync("Dados atualizados", "Suas configurações foram salvas.", "OK");
        }
        catch (Exception ex) { await DisplayAlertAsync("Não foi possível salvar", ex.Message, "OK"); }
    }

    private async Task SignOut()
    {
        var confirm = await DisplayAlertAsync("Sair da conta", "Deseja encerrar sua sessão neste dispositivo?", "Sair", "Cancelar");
        if (!confirm) return;
        _api.Logout();
        await Navigation.PopModalAsync();
        await _onLogout();
    }

    private static View Field(string title, Entry entry) => new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E8EC"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 0), Content = entry } } };
}
