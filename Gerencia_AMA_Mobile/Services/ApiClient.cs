using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Gerencia_AMA;

public sealed class ApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = ApiSettings.BaseAddress, Timeout = TimeSpan.FromSeconds(20) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public Usuario? CurrentUser { get; private set; }
    public bool IsTreasurer => CurrentUser?.RoleName == "Tesoureiro";

    public void Logout()
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<LoginResult> Login(string email, string senha)
    {
        var response = await _http.PostAsJsonAsync("api/Auth/login", new { email, senha });
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
        var result = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions) ?? throw new ApiException("Resposta de login inválida.");
        AccessToken = result.AccessToken; RefreshToken = result.RefreshToken; CurrentUser = result.Usuario;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        return result;
    }

    public async Task Register(string nome, string email, string senha)
    {
        var response = await _http.PostAsJsonAsync("api/Auth/register", new { nome, email, senha });
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    public async Task<List<Debito>> MyDebits() => await Get<List<Debito>>("api/DebitosMembros/me") ?? [];
    public async Task<List<TipoDoacao>> DonationTypes() => await Get<List<TipoDoacao>>("api/TiposDoacao") ?? [];
    public async Task<List<Usuario>> Users() => await Get<List<Usuario>>("api/Usuarios") ?? [];
    public async Task<List<Debito>> AllDebits() => await Get<List<Debito>>("api/DebitosMembros") ?? [];

    public async Task UpdateMyProfile(string nome, string email, string? novaSenha)
    {
        await Put("api/Auth/me", new { nome, email, novaSenha = string.IsNullOrWhiteSpace(novaSenha) ? null : novaSenha });
        if (CurrentUser != null) CurrentUser = CurrentUser with { Nome = nome, Email = email };
    }

    public async Task<TipoDoacao?> CreateDonationType(string nome, string? descricao, decimal valorPadrao, DateTime mesAbertura, DateTime dataExpiracao, bool todosUsuarios, List<int> usuariosIds) =>
        await Post<TipoDoacao>("api/TiposDoacao", new { nome, descricao, valorPadrao, mesAbertura, dataExpiracao, todosUsuarios, usuariosIds });

    public async Task UpdateDonationType(int id, string nome, string? descricao, decimal valorPadrao, DateTime mesAbertura, DateTime dataExpiracao, bool todosUsuarios, List<int> usuariosIds) =>
        await Put($"api/TiposDoacao/{id}", new { nome, descricao, valorPadrao, mesAbertura, dataExpiracao, todosUsuarios, usuariosIds });

    public async Task DeleteDonationType(int id)
    {
        var response = await _http.DeleteAsync($"api/TiposDoacao/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.DeleteAsync($"api/TiposDoacao/{id}");
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    public async Task<Debito?> CreateMyDebit(int tipoId, decimal valor, DateTime mesReferencia) =>
        await Post<Debito>("api/DebitosMembros/meus-debitos", new { tipoDoacaoId = tipoId, valor, mesReferencia = new DateTime(mesReferencia.Year, mesReferencia.Month, 1) });

    public async Task<Debito?> CreateDebit(int userId, int tipoId, decimal valor, DateTime mesReferencia) =>
        await Post<Debito>("api/DebitosMembros", new { usuarioId = userId, tipoDoacaoId = tipoId, valor, mesReferencia = new DateTime(mesReferencia.Year, mesReferencia.Month, 1) });

    public async Task CreateUser(string nome, string email, string senha)
    {
        var response = await _http.PostAsJsonAsync("api/Usuarios", new { nome, email, senha });
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    public async Task UpdateUser(int id, string nome, string email, string? novaSenha, int perfil) =>
        await Put($"api/Usuarios/{id}", new { nome, email, novaSenha = string.IsNullOrWhiteSpace(novaSenha) ? null : novaSenha, perfil });

    public async Task UploadProof(int debitId, string filePath, bool asTreasurer = false)
    {
        var url = asTreasurer ? $"api/DebitosMembros/{debitId}/anexar-comprovante" : $"api/DebitosMembros/meus-debitos/{debitId}/anexar-comprovante";
        var response = await SendProof(url, filePath);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await SendProof(url, filePath);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    public async Task Reject(int debitId)
    {
        var response = await _http.PatchAsync($"api/DebitosMembros/{debitId}/rejeitar", null);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.PatchAsync($"api/DebitosMembros/{debitId}/rejeitar", null);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    public async Task Approve(int debitId)
    {
        var response = await _http.PatchAsync($"api/DebitosMembros/{debitId}/aprovar", null);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.PatchAsync($"api/DebitosMembros/{debitId}/aprovar", null);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }

    private async Task<T?> Get<T>(string path)
    {
        var response = await _http.GetAsync(path);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.GetAsync(path);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
    private async Task<T?> Post<T>(string path, object payload)
    {
        var response = await _http.PostAsJsonAsync(path, payload);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.PostAsJsonAsync(path, payload);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
    private async Task Put(string path, object payload)
    {
        var response = await _http.PutAsJsonAsync(path, payload);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await RefreshSession()) response = await _http.PutAsJsonAsync(path, payload);
        if (!response.IsSuccessStatusCode) throw new ApiException(await ReadError(response));
    }
    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try { using var json = JsonDocument.Parse(body); return json.RootElement.TryGetProperty("erro", out var error) ? error.GetString() ?? "Não foi possível concluir a solicitação." : $"Falha ({(int)response.StatusCode}): {body}"; }
        catch { return string.IsNullOrWhiteSpace(body) ? $"Falha ({(int)response.StatusCode})." : body; }
    }

    private async Task<HttpResponseMessage> SendProof(string url, string filePath)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(filePath);
        var file = new StreamContent(stream);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        file.Headers.ContentType = new MediaTypeHeaderValue(extension == ".pdf" ? "application/pdf" : extension == ".png" ? "image/png" : "image/jpeg");
        content.Add(file, "Arquivo", Path.GetFileName(filePath));
        return await _http.PostAsync(url, content);
    }

    private async Task<bool> RefreshSession()
    {
        if (string.IsNullOrWhiteSpace(AccessToken) || string.IsNullOrWhiteSpace(RefreshToken)) return false;
        try
        {
            var response = await _http.PostAsJsonAsync("api/Auth/refresh-token", new { accessToken = AccessToken, refreshToken = RefreshToken });
            if (!response.IsSuccessStatusCode) return false;
            var result = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions);
            if (result == null) return false;
            AccessToken = result.AccessToken; RefreshToken = result.RefreshToken; CurrentUser = result.Usuario;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            return true;
        }
        catch { return false; }
    }
}
