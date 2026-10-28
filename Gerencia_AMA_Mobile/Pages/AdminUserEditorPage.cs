using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public sealed record AdminUserDraft(string Name, string Email, string? NewPassword, int Profile);

public sealed class AdminUserEditorPage : ContentPage
{
    private readonly Entry _name;
    private readonly Entry _email;
    private readonly Entry _password;
    private readonly Picker _profile;
    private readonly TaskCompletionSource<AdminUserDraft?> _completion = new();
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Muted = Color.FromArgb("#82919C");
    private static readonly Color Teal = Color.FromArgb("#00A896");

    public AdminUserEditorPage(Usuario user)
    {
        Title = "Editar usuário";
        BackgroundColor = Color.FromArgb("#F5F8FA");
        _name = new Entry { Text = user.Nome, Placeholder = "Nome completo", TextColor = Ink };
        _email = new Entry { Text = user.Email, Placeholder = "email@exemplo.com", Keyboard = Keyboard.Email, TextColor = Ink };
        _password = new Entry { Placeholder = "Em branco para manter a senha atual", IsPassword = true, TextColor = Ink };
        _profile = new Picker { ItemsSource = new List<string> { "Membro e contribuinte", "Tesoureiro e contribuinte" }, SelectedIndex = user.RoleName == "Tesoureiro" ? 1 : 0, TextColor = Ink };

        var back = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, BackgroundColor = Colors.White, TextColor = Ink, CornerRadius = 14, FontSize = 25, Padding = 0 };
        back.Clicked += async (_, _) => await Finish(null);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(48), new(GridLength.Star) }, ColumnSpacing = 10 };
        header.Add(back, 0);
        header.Add(new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = "Editar usuário", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = "Atualize os dados e o perfil de acesso", FontSize = 12, TextColor = Muted } } }, 1);

        var save = new Button { Text = "Salvar alterações", BackgroundColor = Teal, TextColor = Colors.White, CornerRadius = 14, HeightRequest = 50, FontAttributes = FontAttributes.Bold };
        save.Clicked += async (_, _) => await Save();
        var card = new VerticalStackLayout { Spacing = 15, Children = { Field("Nome", _name), Field("E-mail", _email), Field("Nova senha", _password), Field("Perfil", _profile), save } };
        var body = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 28), Spacing = 17, Children = { header, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E9ED"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = 18, Content = card } } };
        Content = new ScrollView { Content = body };
    }

    public async Task<AdminUserDraft?> PresentAsync(Page parent)
    {
        await parent.Navigation.PushModalAsync(new NavigationPage(this));
        return await _completion.Task;
    }

    protected override void OnDisappearing() { base.OnDisappearing(); _completion.TrySetResult(null); }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || _name.Text.Trim().Length < 3 || string.IsNullOrWhiteSpace(_email.Text) || !_email.Text.Contains('@'))
        {
            await DisplayAlertAsync("Confira os dados", "Informe nome e e-mail válidos.", "OK");
            return;
        }
        if (!string.IsNullOrWhiteSpace(_password.Text) && _password.Text.Length < 6)
        {
            await DisplayAlertAsync("Senha muito curta", "Use pelo menos 6 caracteres ou deixe o campo vazio para manter a senha atual.", "OK");
            return;
        }
        await Finish(new AdminUserDraft(_name.Text.Trim(), _email.Text.Trim(), _password.Text, _profile.SelectedIndex == 1 ? 2 : 1));
    }

    private async Task Finish(AdminUserDraft? result) { _completion.TrySetResult(result); await Navigation.PopModalAsync(); }

    private static View Field(string title, View editor) => new VerticalStackLayout { Spacing = 7, Children = { new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E8EC"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 2), Content = editor } } };
}
