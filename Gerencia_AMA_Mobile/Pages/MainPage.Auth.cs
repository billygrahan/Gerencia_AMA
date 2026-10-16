using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public partial class MainPage
{
private async void ShowSplash()
    {
        var logo = new Image { Source = "dotnet_bot.png", HeightRequest = 116, WidthRequest = 116, Aspect = Aspect.AspectFit, Opacity = 0 };
        var name = new Label { Text = "AMA", FontSize = 31, FontAttributes = FontAttributes.Bold, TextColor = Ink, HorizontalTextAlignment = TextAlignment.Center, Opacity = 0 };
        var subtitle = new Label { Text = "GESTÃO DE DOAÇÕES", FontSize = 11, CharacterSpacing = 2.1, TextColor = Muted, HorizontalTextAlignment = TextAlignment.Center, Opacity = 0 };
        Root.Children.Clear(); Root.BackgroundColor = Colors.White;
        Root.Children.Add(new VerticalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Children = { logo, name, subtitle } });
        await Task.WhenAll(logo.FadeToAsync(1, 650), name.FadeToAsync(1, 700), subtitle.FadeToAsync(1, 800));
        await Task.Delay(1700);
        await Task.WhenAll(logo.FadeToAsync(0, 450), name.FadeToAsync(0, 450), subtitle.FadeToAsync(0, 450));
        ShowLogin();
    }

private void ShowLogin()
    {
        Root.BackgroundColor = Canvas;
        var email = Entry("seu@email.com", Keyboard.Email);
        var password = Entry("Sua senha", isPassword: true);
        var body = new VerticalStackLayout { Padding = new Thickness(24, 35), Spacing = 20 };
        body.Add(Brand());
        body.Add(new VerticalStackLayout { Spacing = 6, Margin = new Thickness(0, 20, 0, 0), Children =
        {
            Label("Bem-vindo de volta", 27, Ink, true), Label("Entre para acompanhar suas contribuições.", 14, Muted)
        }});
        body.Add(Field("E-mail", email)); body.Add(Field("Senha", password));
        var login = Button("Entrar", Teal);
        login.Clicked += async (_, _) => await Run(async () => { await ViewModel.Api.Login(email.Text?.Trim() ?? "", password.Text ?? ""); await LoadHome(); });
        body.Add(login);
        var register = LinkButton("Criar minha conta"); register.Clicked += async (_, _) => await ShowRegister(); body.Add(register);
        body.Add(new Label { Text = "Suas contribuições, em um só lugar.", FontSize = 12, TextColor = Muted, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 28, 0, 0) });
        Root.Children.Clear(); Root.Children.Add(new ScrollView { Content = body });
    }

private async Task ShowRegister()
    {
        var nome = Entry("Nome completo"); var email = Entry("seu@email.com", Keyboard.Email); var senha = Entry("Mínimo 6 caracteres", isPassword: true);
        var submit = Button("Criar conta", Teal);
        submit.Clicked += async (_, _) => await Run(async () => { await ViewModel.Api.Register(nome.Text?.Trim() ?? "", email.Text?.Trim() ?? "", senha.Text ?? ""); await DisplayAlertAsync("Conta criada", "Agora você já pode entrar no AMA.", "Entrar"); ShowLogin(); });
        var body = new VerticalStackLayout { Padding = new Thickness(24, 35), Spacing = 17, Children =
        {
            Brand(), Label("Sua conta AMA", 26, Ink, true), Label("Crie seu acesso de membro para acompanhar suas contribuições.", 14, Muted),
            Field("Nome completo", nome), Field("E-mail", email), Field("Senha", senha), submit
        }};
        var back = LinkButton("Voltar para entrar"); back.Clicked += (_, _) => ShowLogin(); body.Add(back);
        Root.Children.Clear(); Root.Children.Add(new ScrollView { Content = body });
    }

private async Task LoadHome()
    {
        await Run(async () =>
        {
            await ViewModel.LoadMemberAsync();
            ShowMemberHome();
        });
    }
}
