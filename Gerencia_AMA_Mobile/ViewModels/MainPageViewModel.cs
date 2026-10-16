namespace Gerencia_AMA;

/// <summary>
/// Dados compartilhados pelas telas da aplicação. As páginas cuidam da apresentação;
/// este ViewModel mantém a sessão e os dados carregados da API.
/// </summary>
public sealed class MainPageViewModel
{
    public ApiClient Api { get; } = new();
    public List<Debito> Debits { get; set; } = [];
    public List<TipoDoacao> Types { get; set; } = [];
    public List<Usuario> Users { get; set; } = [];

    public async Task LoadMemberAsync()
    {
        Debits = await Api.MyDebits();
        Types = await Api.DonationTypes();
        if (Api.IsTreasurer) Users = await Api.Users();
    }

    public async Task LoadTreasuryAsync()
    {
        Users = await Api.Users();
        Debits = await Api.AllDebits();
        Types = await Api.DonationTypes();
    }

    public async Task RefreshMemberDebitsAsync() => Debits = await Api.MyDebits();
    public async Task RefreshTreasuryDebitsAsync() => Debits = await Api.AllDebits();
    public async Task RefreshUsersAsync() => Users = await Api.Users();
}
