using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public partial class MainPage
{
private View DebitCard(Debito debit, bool admin)
    {
        var statusColor = debit.StatusName switch { "Pago" => Color.FromArgb("#208B69"), "Rejeitado" => Color.FromArgb("#C95050"), _ => Color.FromArgb("#BE8423") };
        var statusBg = debit.StatusName switch { "Pago" => Color.FromArgb("#E7F6EF"), "Rejeitado" => Color.FromArgb("#FCEDED"), _ => Color.FromArgb("#FFF4DC") };
        var status = new Border { BackgroundColor = statusBg, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = new Thickness(10, 5), Content = Label(debit.StatusName, 10, statusColor, true) };
        var donationType = ViewModel.Types.FirstOrDefault(t => t.Id == debit.TipoDoacaoId);
        var timeline = "Referência: " + debit.ReferenceMonth.ToString("MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));
        if (donationType != null) timeline += "  ·  campanha até " + donationType.DataExpiracao.ToString("MM/yyyy");
        var detail = new VerticalStackLayout { Spacing = 5, Children = { Label(debit.NomeTipoDoacao, 15, Ink, true), Label(timeline, 11, Muted) } };
        var header = new Grid { ColumnDefinitions = Columns(Star, 92), Children = { detail, status } }; Grid.SetColumn(status, 1);
        var stack = new VerticalStackLayout { Spacing = 12, Children = { header, new Grid { ColumnDefinitions = Columns(Star, 100), Children = { Label(debit.Valor.ToString("C", new System.Globalization.CultureInfo("pt-BR")), 19, Ink, true), Label(debit.UrlComprovante == null ? "Sem comprovante" : "Comprovante anexado", 10, Teal, false, 1, TextAlignment.End) } } } };
        var details = Button("Ver detalhes da doação  →", Colors.White, Teal, async () => await OpenDonationDetails(debit)); details.HeightRequest = 42; details.BorderColor = Color.FromArgb("#E1E9ED"); details.BorderWidth = 1; stack.Add(details);
        if (debit.StatusName != "Pago")
        {
            var upload = Button(admin ? "Anexar comprovante" : "Enviar comprovante", Color.FromArgb("#E6F7F5"), Teal, async () => await PickProof(debit.Id, admin));
            if (admin && debit.StatusName != "Rejeitado") { var reject = Button("Rejeitar", Color.FromArgb("#FCEDED"), Color.FromArgb("#C95050"), async () => { await Run(async () => { await ViewModel.Api.Reject(debit.Id); await ViewModel.RefreshTreasuryDebitsAsync(); RenderAdmin(); }); }); stack.Add(new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 8, Children = { upload, reject } }); }
            else stack.Add(upload);
        }
        if (admin) stack.Add(Label("Membro: " + debit.NomeUsuario, 11, Muted));
        return Card(stack);
    }

private async Task PickProof(int debitId, bool admin = false)
    {
        try
        {
            var allowedFiles = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "image/jpeg", "image/png", "application/pdf" } },
                { DevicePlatform.iOS, new[] { "public.jpeg", "public.png", "com.adobe.pdf" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".pdf" } },
                { DevicePlatform.MacCatalyst, new[] { "public.jpeg", "public.png", "com.adobe.pdf" } }
            });
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecione seu comprovante", FileTypes = allowedFiles });
            if (file == null) return;
            await Run(async () => { await ViewModel.Api.UploadProof(debitId, file.FullPath, admin); await DisplayAlertAsync("Comprovante enviado", "O comprovante foi anexado e ficará aguardando revisão da tesouraria.", "OK"); if (admin) await ShowAdmin(); else { await ViewModel.RefreshMemberDebitsAsync(); ShowMemberHome(); } });
        }
        catch (Exception ex) { await DisplayAlertAsync("Não foi possível enviar", ex.Message, "OK"); }
    }

private async Task OpenDonationDetails(Debito debit)
    {
        await Navigation.PushModalAsync(new NavigationPage(new DonationDetailsPage(debit, ViewModel.Types.FirstOrDefault(t => t.Id == debit.TipoDoacaoId))));
    }

private View Brand() => new HorizontalStackLayout { Spacing = 11, VerticalOptions = LayoutOptions.Center, Children = { new Border { WidthRequest = 42, HeightRequest = 42, BackgroundColor = Teal, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 13 }, Content = new Label { Text = "A", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } }, new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { Label("AMA", 18, Ink, true), Label("GESTÃO DE DOAÇÕES", 9, Muted, true, 1.5) } } } };

private View ProfileAvatar()
    {
        var avatar = Avatar(ViewModel.Api.CurrentUser?.Nome ?? "A");
        var tap = new TapGestureRecognizer();tap.Tapped += async (_, _) => await ShowAccountMenu();avatar.GestureRecognizers.Add(tap);
        return avatar;
    }

private View MonthSelector()
    {
        var title = Label(_selectedMonth.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture), 13, Ink, true, 0, TextAlignment.Center);
        var previous = Button("‹", Colors.White, Ink);previous.WidthRequest = 40;previous.HeightRequest = 38;previous.Clicked += (_, _) => { _selectedMonth = _selectedMonth.AddYears(-1); RefreshMonthlyView(); };
        var next = Button("›", Colors.White, Ink);next.WidthRequest = 40;next.HeightRequest = 38;next.Clicked += (_, _) => { _selectedMonth = _selectedMonth.AddYears(1); RefreshMonthlyView(); };
        var yearHeader = new Grid { ColumnDefinitions = Columns(40, Star, 40), Children = { previous, title, next } };yearHeader.SetColumn(title, 1);yearHeader.SetColumn(next, 2);
        var months = new HorizontalStackLayout { Spacing = 8 };
        Border? selectedCard = null;
        for (var month = 1; month <= 12; month++)
        {
            var date = new DateTime(_selectedMonth.Year, month, 1);
            var visibleDebits = _showingTreasury ? ViewModel.Debits : ViewModel.Debits.Where(d => d.UsuarioId == ViewModel.Api.CurrentUser?.Id).ToList();
            var records = visibleDebits.Where(d => d.ReferenceMonth.Year == date.Year && d.ReferenceMonth.Month == date.Month).ToList();
            var pending = records.Count(d => d.StatusName == "Em aberto");
            var typeOpen = ViewModel.Types.Any(t => t.OpeningMonth <= date && date <= new DateTime(t.DataExpiracao.Year, t.DataExpiracao.Month, 1));
            var hint = pending > 0 ? $"{pending} pend." : records.Count > 0 ? "Em dia" : typeOpen ? "Aberta" : "Fechada";
            var selected = date.Year == _selectedMonth.Year && date.Month == _selectedMonth.Month;
            var card = new Border { WidthRequest = 82, BackgroundColor = selected ? Teal : Colors.White, Stroke = selected ? Teal : Color.FromArgb("#E1E9ED"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 13 }, Padding = new Thickness(9, 8), Content = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center, Children = { Label(date.ToString("MMM", new System.Globalization.CultureInfo("pt-BR")).ToUpperInvariant(), 11, selected ? Colors.White : Ink, true, 0, TextAlignment.Center), Label(hint, 9, selected ? Color.FromArgb("#D5F5F0") : pending > 0 ? Color.FromArgb("#BE8423") : Muted, false, 0, TextAlignment.Center) } } };
            if (selected) selectedCard = card;
            var tap = new TapGestureRecognizer();tap.Tapped += (_, _) => { _selectedMonth = date; RefreshMonthlyView(); };card.GestureRecognizers.Add(tap);months.Add(card);
        }
        var monthStrip = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = months };
        monthStrip.Loaded += async (_, _) => { if (selectedCard != null) await monthStrip.ScrollToAsync(selectedCard, ScrollToPosition.Center, false); };
        return new VerticalStackLayout { Spacing = 8, Children = { yearHeader, monthStrip } };
    }

private void RefreshMonthlyView()
    {
        if (_showingTreasury) RenderAdmin();
        else ShowMemberHome();
    }

private async Task ShowAccountMenu()
    {
        var user = ViewModel.Api.CurrentUser;
        if (user == null) return;
        var action = await new AccountMenuPage(user, ViewModel.Api.IsTreasurer, _showingTreasury).PresentAsync(this);
        if (action == AccountMenuAction.Settings) await OpenAccountSettings();
        else if (action == AccountMenuAction.ManageTypes) await ShowDonationTypes();
        else if (action == AccountMenuAction.ManageUsers || action == AccountMenuAction.OpenTreasury) await ShowAdmin();
        else if (action == AccountMenuAction.MemberArea) ShowMemberHome();
        else if (action == AccountMenuAction.Logout)
        {
            if (await DisplayAlertAsync("Sair da conta", "Deseja encerrar sua sessão neste dispositivo?", "Sair", "Cancelar"))
            {
                ViewModel.Api.Logout();
                ShowLogin();
            }
        }
    }

private async Task OpenAccountSettings()
    {
        var settings = new AccountSettingsPage(ViewModel.Api, async () => { ShowLogin(); await Task.CompletedTask; });
        await Navigation.PushModalAsync(new NavigationPage(settings));
    }

private View Avatar(string name) => new Border { WidthRequest = 40, HeightRequest = 40, BackgroundColor = Color.FromArgb("#DDF3F0"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Content = new Label { Text = name.Length == 0 ? "A" : name[..1].ToUpperInvariant(), TextColor = Teal, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };

private View Card(View content, Color? background = null, double radius = 18) => new Border { Stroke = Color.FromArgb("#E9EEF1"), StrokeThickness = background == null ? 1 : 0, BackgroundColor = background ?? Colors.White, StrokeShape = new RoundRectangle { CornerRadius = radius }, Padding = 16, Content = content };

private View Field(string label, Entry entry) => new VerticalStackLayout { Spacing = 7, Children = { Label(label, 13, Ink, true), new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E8EC"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 13 }, Padding = new Thickness(14, 0), Content = entry } } };

private Entry Entry(string placeholder, Keyboard? keyboard = null, bool isPassword = false) => new() { Placeholder = placeholder, Keyboard = keyboard ?? Keyboard.Default, IsPassword = isPassword, TextColor = Ink, PlaceholderColor = Muted, BackgroundColor = Colors.Transparent, HeightRequest = 50 };

private Label Label(string text, double size, Color color, bool bold = false, double spacing = 0, TextAlignment alignment = TextAlignment.Start) => new() { Text = text, FontSize = size, TextColor = color, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, CharacterSpacing = spacing, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = alignment };

private Button Button(string text, Color bg, Color? fg = null, Func<Task>? action = null) { var b = new Button { Text = text, BackgroundColor = bg, TextColor = fg ?? Colors.White, CornerRadius = 13, HeightRequest = 49, FontSize = 14, FontAttributes = FontAttributes.Bold, Padding = new Thickness(12, 0) }; if (action != null) b.Clicked += async (_, _) => await action(); return b; }

private Button LinkButton(string text) => new() { Text = text, BackgroundColor = Colors.Transparent, TextColor = Teal, FontAttributes = FontAttributes.Bold, FontSize = 14 };

private View Stat(string name, string number, int column = 0) { var v = new VerticalStackLayout { Spacing = 3, Children = { Label(name, 11, Color.FromArgb("#D5F5F0")), Label(number, 18, Colors.White, true) } }; Grid.SetColumn(v, column); return v; }

private View MiniStat(string title, string value, string icon, int col = 0) { var v = Card(new Grid { ColumnDefinitions = Columns(36, Star), ColumnSpacing = 8, Children = { Label(icon, 18, Teal), new VerticalStackLayout { Spacing = 3, Children = { Label(title, 11, Muted), Label(value, 19, Ink, true) } } } }); if (v is Border { Content: Grid g } && g.Children.Count > 1) g.SetColumn(g.Children[1], 1); Grid.SetColumn(v, col); return v; }

private Button BackButton(Action action) { var b = Button("‹", Colors.White, Ink); b.Clicked += (_, _) => action(); return b; }

private static ColumnDefinitionCollection Columns(params GridLength[] widths) { var c = new ColumnDefinitionCollection(); foreach (var w in widths) c.Add(new ColumnDefinition(w)); return c; }

private async Task Run(Func<Task> action) { try { await action(); } catch (Exception ex) { await DisplayAlertAsync("Não foi possível conectar", ex.Message + "\n\nVerifique se a API está ativa e se ApiSettings.cs aponta para o endereço correto.", "OK"); } }
}
