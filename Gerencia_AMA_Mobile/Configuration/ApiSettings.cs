namespace Gerencia_AMA;

/// <summary>Edite a URL aqui para apontar para outro ambiente.</summary>
public static class ApiSettings
{
    // Em aparelho físico, use o IP da máquina na mesma rede Wi-Fi.
    // Para o emulador Android, use 10.0.2.2 no lugar do IP abaixo.
    public static readonly Uri BaseAddress = new("http://192.168.18.234:5192/");
}
