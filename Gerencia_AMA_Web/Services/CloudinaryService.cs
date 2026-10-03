using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ApiTesourariaAMA.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<(string Url, string PublicId)?> UploadArquivoAsync(IFormFile arquivo)
    {
        if (arquivo.Length == 0) return null;

        await using var stream = arquivo.OpenReadStream();

        // Aceita imagens ou arquivos raw (PDF)
        var isPdf = arquivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

        RawUploadParams uploadParams;

        if (isPdf)
        {
            uploadParams = new RawUploadParams
            {
                File = new FileDescription(arquivo.FileName, stream),
                Folder = "comprovantes_igreja"
            };
        }
        else
        {
            uploadParams = new ImageUploadParams
            {
                File = new FileDescription(arquivo.FileName, stream),
                Folder = "comprovantes_igreja"
            };
        }

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
        {
            throw new Exception(result.Error.Message);
        }

        return (result.SecureUrl.ToString(), result.PublicId);
    }
}