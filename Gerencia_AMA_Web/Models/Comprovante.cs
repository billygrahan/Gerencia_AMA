namespace ApiTesourariaAMA.Models;

public class Comprovante
{
    public int Id { get; set; }
    public string UrlCloudinary { get; set; } = string.Empty; // Link retornado pelo Cloudinary
    public string PublicIdCloudinary { get; set; } = string.Empty; // ID para gerenciamento no Cloudinary
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
}