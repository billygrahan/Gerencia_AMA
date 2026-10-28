using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public partial class MainPage
{
private void ShowMemberHome()
    {
        _showingTreasury = false;
        Root.BackgroundColor = Canvas;
        var content = new VerticalStackLayout { Padding = new Thickness(20, 15, 20, 30), Spacing = 20 };
        var header = new Grid { ColumnDefinitions = Columns(Star, 48), Children =
        {
            new VerticalStackLayout { Spacing = 3, Children = { Label("Olá, " + (ViewModel.Api.CurrentUser?.Nome.Split(' ')[0] ?? "membro"), 23, Ink, true), Label(_selectedMonth.ToString("MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR")), 13, Muted) } },
            ProfileAvatar()
        }};
        content.Add(header);
        content.Add(MonthSelector());
        var personalDebits = ViewModel.Debits.Where(x => x.UsuarioId == ViewModel.Api.CurrentUser?.Id).ToList();
        var monthDebits = personalDebits.Where(x => x.ReferenceMonth.Year == _selectedMonth.Year && x.ReferenceMonth.Month == _selectedMonth.Month).ToList();
        var paid = monthDebits.Where(x => x.StatusName == "Pago").ToList();
        var outstanding = monthDebits.Where(x => x.StatusName != "Pago").ToList();
        var summary = new VerticalStackLayout { Spacing = 17 };
        summary.Add(new Grid { ColumnDefinitions = Columns(Star, 36), Children = { Label("RESUMO · " + _selectedMonth.ToString("MMM/yyyy", new System.Globalization.CultureInfo("pt-BR")).ToUpperInvariant(), 11, Colors.White, true, 1.3), Label("▦", 23, Colors.White) } });
        summary.Add(new Label { Text = paid.Sum(x => x.Valor).ToString("C", new System.Globalization.CultureInfo("pt-BR")), FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
        var monthStatus = monthDebits.Count == 0 ? "Nenhuma contribuição lançada neste mês" : outstanding.Count == 0 ? "Tudo certo com suas contribuições" : $"{outstanding.Count} contribuição(ões) aguardando pagamento";
        summary.Add(new Label { Text = monthStatus, FontSize = 13, TextColor = Color.FromArgb("#D5F5F0") });
        summary.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#4FC2B7") });
        summary.Add(new Grid { ColumnDefinitions = Columns(Star, Star), Children = { Stat("Pagas", paid.Count.ToString()), Stat("Em aberto", outstanding.Count.ToString(), 1) } });
        content.Add(Card(summary, Teal, 22));
        var actions = new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 12 };
        var donate = Button("↑  Enviar comprovante", Colors.White, Teal); donate.BorderColor = Color.FromArgb("#DCE7EA"); donate.BorderWidth = 1; donate.Clicked += async (_, _) => await CreateMyDonation(); actions.Add(donate);
        if (ViewModel.Api.IsTreasurer) { var admin = Button("Painel tesouraria  →", Color.FromArgb("#E5F6F4"), Teal); admin.FontSize = 13; admin.Clicked += async (_, _) => await ShowAdmin(); actions.Add(admin, 1); }
        else { var account = Button("Minha conta", Colors.White, Ink); account.BorderColor = Color.FromArgb("#DCE7EA"); account.BorderWidth = 1; account.Clicked += async (_, _) => await OpenAccountSettings(); actions.Add(account, 1); }
        content.Add(actions);
        var availableTypes = ViewModel.Types.Where(t => IsDonationTypeOpen(t, _selectedMonth)).ToList();
        if (availableTypes.Count > 0)
        {
            content.Add(Label("Campanhas neste mês", 17, Ink, true));
            foreach (var type in availableTypes)
            {
                var debit = monthDebits.FirstOrDefault(d => d.TipoDoacaoId == type.Id);
                var stateText = debit == null ? "Disponível" : debit.StatusName;
                var stateColor = debit?.StatusName switch { "Pago" => Color.FromArgb("#208B69"), "Rejeitado" => Color.FromArgb("#C95050"), "Em aberto" => Color.FromArgb("#BE8423"), _ => Teal };
                var row = new Grid { ColumnDefinitions = Columns(Star, 110), ColumnSpacing = 8, Children = { new VerticalStackLayout { Spacing = 3, Children = { Label(type.Nome, 14, Ink, true), Label(type.Descricao ?? "Contribuição mensal", 11, Muted) } }, Label(stateText, 11, stateColor, true, 0, TextAlignment.End) } };
                row.SetColumn(row.Children[1], 1);
                var campaign = Card(row);
                if (debit == null)
                {
                    var gesture = new TapGestureRecognizer();gesture.Tapped += async (_, _) => await CreateMyDonation();campaign.GestureRecognizers.Add(gesture);
                }
                content.Add(campaign);
            }
        }
        content.Add(new Grid { ColumnDefinitions = Columns(Star, 80), Children = { Label("Suas contribuições", 19, Ink, true), Label(personalDebits.Count + " registros", 12, Muted, false, 1, TextAlignment.End) } });
        if (monthDebits.Count == 0) content.Add(Card(new VerticalStackLayout { Spacing = 7, Children = { Label("Nenhuma doação neste mês", 15, Ink, true), Label("Escolha outra competência no calendário acima ou registre uma campanha disponível.", 13, Muted) } }));
        else foreach (var debit in monthDebits.OrderByDescending(d => d.DataCriacao)) content.Add(DebitCard(debit, false));
        var scroll = new ScrollView { Content = content }; Root.Children.Clear(); Root.Children.Add(scroll);
    }

private async Task CreateMyDonation()
    {
        var personalDebits = ViewModel.Debits.Where(x => x.UsuarioId == ViewModel.Api.CurrentUser?.Id).ToList();
        var pending = personalDebits.Where(x => x.ReferenceMonth.Year == _selectedMonth.Year && x.ReferenceMonth.Month == _selectedMonth.Month && x.StatusName != "Pago").ToList();
        if (pending.Count == 0) { await DisplayAlertAsync("Sem pendências neste mês", "Os débitos da campanha são gerados automaticamente. Não há uma doação em aberto para anexar comprovante neste mês.", "OK"); return; }
        var options = pending.Select(x => $"{x.NomeTipoDoacao} · {x.Valor:C} · #{x.Id}").ToArray();
        var selected = await DisplayActionSheetAsync("Escolha a doação", "Cancelar", null, options);
        var index = Array.IndexOf(options, selected);
        if (index < 0) return;
        await PickProof(pending[index].Id);
    }

private static bool IsDonationTypeOpen(TipoDoacao type, DateTime month)
    {
        var reference = new DateTime(month.Year, month.Month, 1);
        return type.OpeningMonth <= reference && reference <= new DateTime(type.DataExpiracao.Year, type.DataExpiracao.Month, 1);
    }
}
