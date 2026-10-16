using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public sealed record DonationSubmission(int DonationTypeId, decimal Amount, DateTime ReferenceMonth);

public sealed class DonationFormPage : ContentPage
{
    private static readonly Color Teal = Color.FromArgb("#00A896");
    private static readonly Color Ink = Color.FromArgb("#172B3A");
    private static readonly Color Muted = Color.FromArgb("#82919C");
    private readonly IReadOnlyList<TipoDoacao> _types;
    private readonly IReadOnlyList<Debito> _debits;
    private readonly Picker _monthPicker;
    private readonly Picker _typePicker;
    private readonly Entry _amount;
    private readonly Label _availability;
    private readonly TaskCompletionSource<DonationSubmission?> _completion = new();
    private IReadOnlyList<DateTime> _months = [];

    public DonationFormPage(IReadOnlyList<TipoDoacao> types, IReadOnlyList<Debito> debits, DateTime? initialMonth = null)
    {
        _types = types;
        _debits = debits;
        Title = "Nova doação";
        BackgroundColor = Color.FromArgb("#F5F8FA");

        var today = DateTime.Today;
        _months = Enumerable.Range(-24, 49).Select(offset => new DateTime(today.Year, today.Month, 1).AddMonths(offset)).ToArray();
        var defaultMonth = new DateTime((initialMonth ?? today).Year, (initialMonth ?? today).Month, 1);
        _monthPicker = new Picker { Title = "Selecione o mês", ItemsSource = _months.Select(FormatMonth).ToList(), SelectedIndex = Array.IndexOf(_months.ToArray(), defaultMonth), TextColor = Ink };
        _typePicker = new Picker { Title = "Selecione o tipo de doação", TextColor = Ink };
        _amount = new Entry { Placeholder = "Ex.: 50,00", Keyboard = Keyboard.Numeric, TextColor = Ink };
        _availability = new Label { FontSize = 12, TextColor = Muted, LineBreakMode = LineBreakMode.WordWrap };

        _monthPicker.SelectedIndexChanged += (_, _) => RefreshTypes();
        _typePicker.SelectedIndexChanged += (_, _) => RefreshAvailability();
        RefreshTypes();

        var close = new Button { Text = "‹", WidthRequest = 44, HeightRequest = 44, BackgroundColor = Colors.White, TextColor = Ink, CornerRadius = 14, FontSize = 25, Padding = 0 };
        close.Clicked += async (_, _) => await Finish(null);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(48), new(GridLength.Star) }, ColumnSpacing = 8, Children = { close, new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { new Label { Text = "Fazer uma doação", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Label { Text = "Escolha o mês, a contribuição e o valor.", FontSize = 12, TextColor = Muted } } } } };
        header.SetColumn(header.Children[1], 1);

        var body = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 24), Spacing = 18 };
        body.Add(header);
        body.Add(new Border { BackgroundColor = Color.FromArgb("#E6F7F4"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Padding = 17, Content = new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = "CONTRIBUIÇÃO MENSAL", FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.2, TextColor = Teal }, new Label { Text = "Cada tipo pode ser registrado uma vez por mês.", FontSize = 13, TextColor = Ink } } } });
        body.Add(Field("Mês de referência", _monthPicker));
        body.Add(Field("Tipo de doação", _typePicker));
        body.Add(_availability);
        body.Add(Field("Valor da doação", _amount));
        var submit = new Button { Text = "Continuar  →", BackgroundColor = Teal, TextColor = Colors.White, CornerRadius = 14, HeightRequest = 52, FontAttributes = FontAttributes.Bold };
        submit.Clicked += async (_, _) => await Submit();
        body.Add(submit);
        Content = new ScrollView { Content = body };
    }

    public async Task<DonationSubmission?> PresentAsync(Page parent)
    {
        await parent.Navigation.PushModalAsync(new NavigationPage(this) { BarBackgroundColor = Color.FromArgb("#F5F8FA"), BarTextColor = Ink });
        return await _completion.Task;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(null);
    }

    private void RefreshTypes()
    {
        if (_monthPicker.SelectedIndex < 0 || _monthPicker.SelectedIndex >= _months.Count) return;
        var month = _months[_monthPicker.SelectedIndex];
        var openTypes = _types.Where(t => IsOpen(t, month)).ToList();
        _typePicker.ItemsSource = openTypes.Select(t => t.Nome).ToList();
        _typePicker.SelectedIndex = -1;
        RefreshAvailability();
        if (openTypes.Count == 0) _availability.Text = "Não há tipos de doação abertos para este mês.";
    }

    private void RefreshAvailability()
    {
        if (_monthPicker.SelectedIndex < 0 || _typePicker.SelectedIndex < 0) return;
        var month = _months[_monthPicker.SelectedIndex];
        var type = _types.Where(t => IsOpen(t, month)).ElementAtOrDefault(_typePicker.SelectedIndex);
        if (type == null) return;
        var exists = _debits.Any(d => d.TipoDoacaoId == type.Id && d.ReferenceMonth.Year == month.Year && d.ReferenceMonth.Month == month.Month);
        _availability.Text = exists ? "Você já tem uma doação deste tipo neste mês." : "Disponível para registrar neste mês.";
        _availability.TextColor = exists ? Color.FromArgb("#C95050") : Color.FromArgb("#208B69");
    }

    private async Task Submit()
    {
        if (_monthPicker.SelectedIndex < 0 || _typePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Selecione a contribuição", "Escolha um mês e um tipo de doação disponível.", "OK");
            return;
        }
        var month = _months[_monthPicker.SelectedIndex];
        var type = _types.Where(t => IsOpen(t, month)).ElementAtOrDefault(_typePicker.SelectedIndex);
        if (type == null) return;
        if (_debits.Any(d => d.TipoDoacaoId == type.Id && d.ReferenceMonth.Year == month.Year && d.ReferenceMonth.Month == month.Month))
        {
            await DisplayAlertAsync("Doação já registrada", "Já existe uma doação deste tipo para o mês selecionado.", "OK");
            return;
        }
        var amountText = _amount.Text?.Trim().Replace(',', '.');
        if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            await DisplayAlertAsync("Valor inválido", "Informe um valor maior que zero.", "OK");
            return;
        }
        await Finish(new DonationSubmission(type.Id, amount, month));
    }

    private async Task Finish(DonationSubmission? result)
    {
        _completion.TrySetResult(result);
        await Navigation.PopModalAsync();
    }

    private static bool IsOpen(TipoDoacao type, DateTime month)
    {
        var start = type.OpeningMonth;
        var end = new DateTime(type.DataExpiracao.Year, type.DataExpiracao.Month, 1);
        var reference = new DateTime(month.Year, month.Month, 1);
        return start <= reference && reference <= end;
    }

    private static string FormatMonth(DateTime month) => month.ToString("MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));

    private static View Field(string label, View input) => new VerticalStackLayout { Spacing = 7, Children = { new Label { Text = label, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ink }, new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E1E8EC"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 13 }, Padding = new Thickness(13, 2), Content = input } } };
}
