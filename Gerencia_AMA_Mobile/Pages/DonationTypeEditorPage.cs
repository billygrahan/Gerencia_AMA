using Microsoft.Maui.Controls.Shapes;
using System.Globalization;

namespace Gerencia_AMA;

public sealed record DonationTypeDraft(string Name, string? Description, decimal Amount, bool AllUsers, List<int> UserIds, DateTime OpeningMonth, DateTime ExpirationMonth);

public sealed class DonationTypeEditorPage : ContentPage
{
    private readonly Entry _name = new() { Placeholder = "Ex.: Oferta missionária", MaxLength = 100, TextColor = Color.FromArgb("#172B3A") };
    private readonly Entry _description = new() { Placeholder = "Objetivo desta campanha", MaxLength = 255, TextColor = Color.FromArgb("#172B3A") };
    private readonly Entry _amount = new() { Placeholder = "0,00", Keyboard = Keyboard.Numeric, TextColor = Color.FromArgb("#172B3A") };
    private readonly DatePicker _expiration;
    private readonly DatePicker _opening;
    private readonly Picker _audience;
    private readonly TaskCompletionSource<DonationTypeDraft?> _completion = new();
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Teal = Color.FromArgb("#00A896");

    private readonly List<(Usuario User, CheckBox Check)> _memberChecks = [];

    public DonationTypeEditorPage(TipoDoacao? current = null, IReadOnlyList<Usuario>? members = null)
    {
        Title = current == null ? "Novo tipo" : "Editar tipo";
        BackgroundColor = Color.FromArgb("#F5F8FA");
        if (current != null) { _name.Text = current.Nome; _description.Text = current.Descricao; _amount.Text = current.ValorPadrao > 0 ? current.ValorPadrao.ToString("0.00", CultureInfo.CurrentCulture) : string.Empty; }
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _opening = new DatePicker { Date = current?.OpeningMonth.Date ?? thisMonth, MinimumDate = new DateTime(1900, 1, 1), MaximumDate = new DateTime(2100, 12, 31), Format = "MMMM yyyy", TextColor = Ink };
        _expiration = new DatePicker { Date = current?.DataExpiracao.Date ?? thisMonth.AddMonths(1), MinimumDate = current?.OpeningMonth.Date ?? thisMonth, MaximumDate = new DateTime(2100, 12, 31), Format = "MMMM yyyy", TextColor = Ink };
        _opening.DateSelected += (_, e) => { var selected = e.NewDate ?? thisMonth; if (_expiration.Date < selected) _expiration.Date = selected; _expiration.MinimumDate = new DateTime(selected.Year, selected.Month, 1); };

        _audience = new Picker { Title = "Quem recebe esta campanha?", ItemsSource = new List<string> { "Todos os membros", "Somente membros selecionados" }, SelectedIndex = current?.TodosUsuarios == false ? 1 : 0, TextColor = Ink };
        var memberSelection = new VerticalStackLayout { Spacing = 2, IsVisible = _audience.SelectedIndex == 1 };
        foreach (var member in members ?? [])
        {
            var check = new CheckBox { IsChecked = current?.UsuariosIds.Contains(member.Id) == true, Color = Teal, VerticalOptions = LayoutOptions.Center };
            _memberChecks.Add((member, check));
            memberSelection.Add(new HorizontalStackLayout { Spacing = 6, Children = { check, new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = member.Nome + (member.RoleName == "Tesoureiro" ? " · Tesoureiro" : ""), FontSize = 13, TextColor = Ink }, new Label { Text = member.Email, FontSize = 10, TextColor = Color.FromArgb("#82919C") } } } } });
        }
        _audience.SelectedIndexChanged += (_, _) => memberSelection.IsVisible = _audience.SelectedIndex == 1;

        var close = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, BackgroundColor = Colors.White, TextColor = Ink, CornerRadius = 14, FontSize = 25, Padding = 0 };
        close.Clicked += async (_, _) => await Finish(null);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(48), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        header.Add(close, 0);header.Add(new Label { Text = current == null ? "Novo tipo de doação" : "Editar campanha", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalTextAlignment = TextAlignment.Center }, 1);
        var save = new Button { Text = "Salvar campanha", BackgroundColor = Teal, TextColor = Colors.White, CornerRadius = 14, HeightRequest = 50, FontAttributes = FontAttributes.Bold };
        save.Clicked += async (_, _) => await Save();
        var body = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 28), Spacing = 17, Children = { header, new Border { BackgroundColor = Color.FromArgb("#E6F7F4"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 16, Content = new Label { Text = "Os débitos mensais usam o valor padrão. Selecione os membros que devem receber esta campanha.", FontSize = 12, TextColor = Ink } }, Field("Nome da campanha", _name), Field("Descrição", _description), Field("Valor mensal por membro", _amount), Field("Primeiro mês aberto", _opening), Field("Último mês aberto", _expiration), Field("Destinatários", _audience), memberSelection, save } };
        Content = new ScrollView { Content = body };
    }

    public async Task<DonationTypeDraft?> PresentAsync(Page parent)
    {
        await parent.Navigation.PushModalAsync(new NavigationPage(this));
        return await _completion.Task;
    }

    protected override void OnDisappearing() { base.OnDisappearing(); _completion.TrySetResult(null); }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { await DisplayAlertAsync("Informe um nome", "O tipo de doação precisa de um nome.", "OK"); return; }
        var openingDate = _opening.Date ?? DateTime.Today;
        var expirationDate = _expiration.Date ?? openingDate;
        var opening = new DateTime(openingDate.Year, openingDate.Month, 1);
        var expiration = new DateTime(expirationDate.Year, expirationDate.Month, 1);
        if (expiration < opening) { await DisplayAlertAsync("Período inválido", "O último mês aberto precisa ser igual ou posterior ao primeiro.", "OK"); return; }
        if (!decimal.TryParse(_amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) && !decimal.TryParse(_amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)) { await DisplayAlertAsync("Valor inválido", "Informe o valor mensal da campanha.", "OK"); return; }
        if (amount <= 0) { await DisplayAlertAsync("Valor inválido", "O valor mensal precisa ser maior que zero.", "OK"); return; }
        var allUsers = _audience.SelectedIndex == 0;
        var selectedIds = _memberChecks.Where(item => item.Check.IsChecked).Select(item => item.User.Id).ToList();
        if (!allUsers && selectedIds.Count == 0) { await DisplayAlertAsync("Selecione membros", "Escolha ao menos um membro para esta campanha.", "OK"); return; }
        await Finish(new DonationTypeDraft(_name.Text.Trim(), string.IsNullOrWhiteSpace(_description.Text) ? null : _description.Text.Trim(), amount, allUsers, selectedIds, opening, expiration));
    }

    private async Task Finish(DonationTypeDraft? result) { _completion.TrySetResult(result); await Navigation.PopModalAsync(); }
    private static View Field(string title, View editor) => new VerticalStackLayout { Spacing = 7, Children = { new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E8EC"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(12, 2), Content = editor } } };
}
