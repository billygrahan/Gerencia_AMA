using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public partial class MainPage
{
private async Task ShowAdmin()
    {
        _showingTreasury = true;
        await Run(async () => { await ViewModel.LoadTreasuryAsync(); RenderAdmin(); });
    }

private void RenderAdmin()
    {
        Root.BackgroundColor = Canvas;
        var page = new VerticalStackLayout { Padding = new Thickness(18, 15, 18, 30), Spacing = 16 };
        var adminHeader = new Grid { ColumnDefinitions = Columns(45, Star, 45) };
        adminHeader.Add(BackButton(ShowMemberHome), 0); adminHeader.Add(Label("Tesouraria", 21, Ink, true, 1, TextAlignment.Center), 1); adminHeader.Add(ProfileAvatar(), 2); page.Add(adminHeader);
        page.Add(MonthSelector());
        page.Add(Label("Membros, tesoureiros e contribuições", 13, Muted));
        var search = Entry("Buscar membro ou campanha"); search.TextChanged += (_, e) => { _search = e.NewTextValue ?? ""; RenderAdminList(page); };
        page.Add(new Border { Stroke = Color.FromArgb("#E1E9ED"), StrokeThickness = 1, BackgroundColor = Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(13, 0), Content = search });
        var filters = new HorizontalStackLayout { Spacing = 8 };
        foreach (var filter in new[] { "Todos", "Em dia", "Pendentes", "Expirando" })
        {
            var local = filter; var button = Button(filter, _filter == filter ? Ink : Colors.White, _filter == filter ? Colors.White : Muted); button.FontSize = 12; button.Padding = new Thickness(14, 9); button.Clicked += (_, _) => { _filter = local; RenderAdmin(); }; filters.Add(button);
        }
        page.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = filters });
        var monthDebits = ViewModel.Debits.Where(d => d.ReferenceMonth.Year == _selectedMonth.Year && d.ReferenceMonth.Month == _selectedMonth.Month).ToList();
        var total = ViewModel.Users.Count; var pending = monthDebits.Count(x => x.StatusName == "Em aberto");
        page.Add(new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 10, Children = { MiniStat("Usuários", total.ToString(), "👥"), MiniStat("Pendentes no mês", pending.ToString(), "◷", 1) } });
        page.Add(Label("Usuários", 17, Ink, true));
        var adminActions = new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 9 };
        adminActions.Add(Button("＋ Novo membro", Colors.White, Teal, async () => await AdminCreateUser()), 0);
        adminActions.Add(Button("＋ Novo débito", Teal, Colors.White, async () => await AdminCreateDebit()), 1);
        page.Add(adminActions);
        page.Add(Button("⚙  Gerenciar tipos de doação", Color.FromArgb("#E6F7F5"), Teal, async () => await ShowDonationTypes()));
        var list = new VerticalStackLayout { Spacing = 10 }; page.Add(list);
        var scroll = new ScrollView { Content = page }; Root.Children.Clear(); Root.Children.Add(scroll);
        FillAdminList(list);
    }

private void RenderAdminList(VerticalStackLayout page)
    {
        if (page.Children.LastOrDefault() is VerticalStackLayout list) FillAdminList(list);
    }

private void FillAdminList(VerticalStackLayout list)
    {
        list.Clear();
        IEnumerable<Usuario> users = ViewModel.Users;
        if (!string.IsNullOrWhiteSpace(_search)) users = users.Where(u => u.Nome.Contains(_search, StringComparison.OrdinalIgnoreCase) || u.Email.Contains(_search, StringComparison.OrdinalIgnoreCase) || ViewModel.Debits.Any(d => d.UsuarioId == u.Id && d.NomeTipoDoacao.Contains(_search, StringComparison.OrdinalIgnoreCase)));
        foreach (var user in users)
        {
            var debts = ViewModel.Debits.Where(d => d.UsuarioId == user.Id && d.ReferenceMonth.Year == _selectedMonth.Year && d.ReferenceMonth.Month == _selectedMonth.Month).ToList();
            if (_filter == "Em dia" && (debts.Count == 0 || debts.Any(d => d.StatusName != "Pago"))) continue;
            if (_filter == "Pendentes" && !debts.Any(d => d.StatusName == "Em aberto")) continue;
            if (_filter == "Expirando" && !debts.Any(d => ViewModel.Types.Any(t => t.Id == d.TipoDoacaoId && t.DataExpiracao >= DateTime.UtcNow && t.DataExpiracao <= DateTime.UtcNow.AddDays(30)))) continue;
            var unpaid = debts.Where(d => d.StatusName != "Pago").Sum(d => d.Valor);
            var role = user.RoleName == "Tesoureiro" ? "Tesoureiro · também membro" : "Membro";
            var detail = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { Label(user.Nome, 14, Ink, true), Label(user.Email + " · " + role, 10, Muted) } };
            var trailing = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center, Children = { Label(unpaid.ToString("C", new System.Globalization.CultureInfo("pt-BR")), 13, unpaid > 0 ? Color.FromArgb("#CE6C44") : Teal, true, 1, TextAlignment.End), Label(debts.Count + " lançamentos", 10, Muted, false, 1, TextAlignment.End) } };
            var row = new Grid { ColumnDefinitions = Columns(40, Star, 95), ColumnSpacing = 10, Children = { Avatar(user.Nome), detail, trailing } }; row.SetColumn(detail, 1); row.SetColumn(trailing, 2);
            var actions = new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 8 };
            actions.Add(Button("Ver doações", Color.FromArgb("#F2F7F7"), Ink, async () => await ShowUserDebits(user)), 0);
            actions.Add(Button("Editar cadastro", Color.FromArgb("#E6F7F5"), Teal, async () => await AdminEditUser(user)), 1);
            list.Add(Card(new VerticalStackLayout { Spacing = 12, Children = { row, actions } }));
        }
        if (list.Children.Count == 0) list.Add(Label("Nenhum membro encontrado com esses filtros.", 13, Muted));
    }

private async Task ShowUserDebits(Usuario user)
    {
        var debts = ViewModel.Debits.Where(d => d.UsuarioId == user.Id && d.ReferenceMonth.Year == _selectedMonth.Year && d.ReferenceMonth.Month == _selectedMonth.Month).OrderByDescending(d => d.DataCriacao).ToList();
        if (debts.Count == 0) { await DisplayAlertAsync(user.Nome, $"Nenhuma doação em {_selectedMonth:MM/yyyy}.", "Fechar"); return; }
        var debtOptions = debts.Select(d => $"{d.NomeTipoDoacao} · {d.Valor:C} · {d.StatusName} · #{d.Id}").ToArray();
        var selected = await DisplayActionSheetAsync(user.Nome + " · contribuições", "Voltar", null, debtOptions);
        var index = Array.IndexOf(debtOptions, selected);
        if (index < 0) return;
        var debit = debts[index];
        var actions = new List<string> { "Ver detalhes" };
        if (!string.IsNullOrWhiteSpace(debit.UrlComprovante)) actions.Add("Abrir comprovante");
        if (debit.StatusName != "Pago") actions.Add("Anexar comprovante");
        if (debit.StatusName == "Em aberto" && !string.IsNullOrWhiteSpace(debit.UrlComprovante)) actions.Add("Aprovar pagamento");
        if (debit.StatusName != "Pago") actions.Add("Rejeitar comprovante");
        var action = await DisplayActionSheetAsync(debit.NomeTipoDoacao + " · " + debit.StatusName, "Voltar", null, actions.ToArray());
        if (action == "Ver detalhes") await OpenDonationDetails(debit);
        else if (action == "Anexar comprovante") await PickProof(debit.Id, true);
        else if (action == "Aprovar pagamento") await Run(async () => { await ViewModel.Api.Approve(debit.Id); await ViewModel.RefreshTreasuryDebitsAsync(); RenderAdmin(); });
        else if (action == "Rejeitar comprovante") await Run(async () => { await ViewModel.Api.Reject(debit.Id); await ViewModel.RefreshTreasuryDebitsAsync(); RenderAdmin(); });
        else if (action == "Abrir comprovante" && !string.IsNullOrWhiteSpace(debit.UrlComprovante)) await Launcher.Default.OpenAsync(debit.UrlComprovante);
    }

private async Task AdminCreateDebit()
    {
        if (ViewModel.Users.Count == 0 || ViewModel.Types.Count == 0) { await DisplayAlertAsync("Cadastros necessários", "Cadastre pelo menos um membro e um tipo de doação na API antes de lançar um débito.", "OK"); return; }
        var userOptions = ViewModel.Users.Select(u => $"{u.Nome}{(u.RoleName == "Tesoureiro" ? " · Tesoureiro" : "")} · #{u.Id}").ToArray();
        var personName = await DisplayActionSheetAsync("Selecionar usuário", "Cancelar", null, userOptions);
        var personIndex = Array.IndexOf(userOptions, personName); if (personIndex < 0) return;
        var person = ViewModel.Users[personIndex];
        var submission = await new DonationFormPage(ViewModel.Types, ViewModel.Debits.Where(d => d.UsuarioId == person.Id).ToList(), _selectedMonth, person.Id).PresentAsync(this);
        if (submission == null) return;
        await Run(async () => { await ViewModel.Api.CreateDebit(person.Id, submission.DonationTypeId, submission.Amount, submission.ReferenceMonth); await ViewModel.RefreshTreasuryDebitsAsync(); RenderAdmin(); });
    }

    private async Task ShowDonationTypes()
    {
        _showingTreasury = true;
        await Run(async () => { await ViewModel.LoadTreasuryAsync(); RenderDonationTypes(); });
    }

private void RenderDonationTypes()
    {
        Root.BackgroundColor = Canvas;
        var page = new VerticalStackLayout { Padding = new Thickness(18, 15, 18, 28), Spacing = 15 };
        var header = new Grid { ColumnDefinitions = Columns(45, Star, 45) };
        header.Add(BackButton(RenderAdmin), 0);header.Add(Label("Tipos de doação", 19, Ink, true, 0, TextAlignment.Center), 1);header.Add(ProfileAvatar(), 2);
        page.Add(header);
        page.Add(Label("Campanhas e períodos de abertura", 13, Muted));
        page.Add(Button("＋ Novo tipo de doação", Teal, Colors.White, async () => await EditDonationType(null)));
        foreach (var type in ViewModel.Types.OrderBy(t => t.DataExpiracao))
        {
            var scope = type.TodosUsuarios ? "Todos os usuários" : $"{type.UsuariosIds.Count} usuário(s) selecionado(s)";
            var info = new VerticalStackLayout { Spacing = 4, Children = { Label(type.Nome, 15, Ink, true), Label(type.Descricao ?? "Sem descrição", 11, Muted), Label(type.ValorPadrao.ToString("C", new System.Globalization.CultureInfo("pt-BR")) + " · " + scope, 11, Teal, true), Label(type.OpeningMonth.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR")) + " a " + type.DataExpiracao.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR")), 11, Muted) } };
            var edit = Button("Editar", Color.FromArgb("#E6F7F5"), Teal, async () => await EditDonationType(type));
            var remove = Button("Excluir", Color.FromArgb("#FCEDED"), Color.FromArgb("#C95050"), async () => await DeleteDonationType(type));
            edit.FontSize = 11;remove.FontSize = 11;edit.HeightRequest = 40;remove.HeightRequest = 40;
            var actions = new Grid { ColumnDefinitions = Columns(Star, Star), ColumnSpacing = 8 };actions.Add(edit, 0);actions.Add(remove, 1);
            page.Add(Card(new VerticalStackLayout { Spacing = 12, Children = { info, actions } }));
        }
        if (ViewModel.Types.Count == 0) page.Add(Card(Label("Ainda não há tipos de doação. Crie a primeira campanha para ela aparecer no calendário mensal.", 13, Muted)));
        Root.Children.Clear();Root.Children.Add(new ScrollView { Content = page });
    }

private async Task EditDonationType(TipoDoacao? current)
    {
        var draft = await new DonationTypeEditorPage(current, ViewModel.Users).PresentAsync(this);
        if (draft == null) return;
        await Run(async () =>
        {
            if (current == null) await ViewModel.Api.CreateDonationType(draft.Name, draft.Description, draft.Amount, draft.OpeningMonth, draft.ExpirationMonth, draft.AllUsers, draft.UserIds);
            else await ViewModel.Api.UpdateDonationType(current.Id, draft.Name, draft.Description, draft.Amount, draft.OpeningMonth, draft.ExpirationMonth, draft.AllUsers, draft.UserIds);
            ViewModel.Types = await ViewModel.Api.DonationTypes();
            RenderDonationTypes();
        });
    }

private async Task DeleteDonationType(TipoDoacao type)
    {
        if (ViewModel.Debits.Any(d => d.TipoDoacaoId == type.Id))
        {
            await DisplayAlertAsync("Campanha com histórico", "Há doações registradas para este tipo. Edite o prazo para encerrá-la e preserve o histórico.", "OK");
            return;
        }
        if (!await DisplayAlertAsync("Excluir tipo", $"Deseja excluir {type.Nome}?", "Excluir", "Cancelar")) return;
        await Run(async () => { await ViewModel.Api.DeleteDonationType(type.Id); ViewModel.Types = await ViewModel.Api.DonationTypes(); RenderDonationTypes(); });
    }

private async Task AdminCreateUser()
    {
        var nome = Entry("Nome completo"); var email = Entry("membro@email.com", Keyboard.Email); var password = Entry("Mínimo 6 caracteres", isPassword: true);
        var submit = Button("Cadastrar membro", Teal);
        submit.Clicked += async (_, _) => await Run(async () =>
        {
            await ViewModel.Api.CreateUser(nome.Text?.Trim() ?? "", email.Text?.Trim() ?? "", password.Text ?? "");
            await ViewModel.RefreshUsersAsync(); RenderAdmin();
        });
        var back = LinkButton("Voltar ao painel"); back.Clicked += (_, _) => RenderAdmin();
        var header = new Grid { ColumnDefinitions = Columns(45, Star) };
        header.Add(BackButton(RenderAdmin), 0); header.Add(Label("Novo membro", 21, Ink, true, 1, TextAlignment.Center), 1);
        var body = new VerticalStackLayout { Padding = new Thickness(22, 15, 22, 30), Spacing = 17, Children =
        {
            header,
            Label("Cadastre uma conta de membro da igreja.", 14, Muted), Field("Nome", nome), Field("E-mail", email), Field("Senha inicial", password), submit, back
        }};
        Root.Children.Clear(); Root.Children.Add(new ScrollView { Content = body });
    }

    private async Task AdminEditUser(Usuario user)
    {
        var draft = await new AdminUserEditorPage(user).PresentAsync(this);
        if (draft == null) return;
        await Run(async () =>
        {
            await ViewModel.Api.UpdateUser(user.Id, draft.Name, draft.Email, draft.NewPassword, draft.Profile);
            await ViewModel.RefreshUsersAsync();
            RenderAdmin();
        });
    }
}
