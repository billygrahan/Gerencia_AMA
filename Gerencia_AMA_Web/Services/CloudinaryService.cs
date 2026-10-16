using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ApiTesourariaAMA.Models;

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

    /// <summary>
    /// Apaga um arquivo do Cloudinary usando o seu PublicId.
    /// </summary>
    public async Task<bool> DeletarArquivoAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId)) return false;

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);

        return result.Result == "ok";
    }

    /// <summary>
    /// Processa o upload (ou reupload) de comprovante. Caso já exista um comprovante associado,
    /// remove o arquivo correspondente do Cloudinary antes de criar/atualizar o registro.
    /// </summary>
    public async Task<Comprovante> ProcessarComprovanteAsync(IFormFile arquivo, Comprovante? comprovanteExistente = null)
    {
        // 1. Caso seja um reupload (já exista um comprovante), remove o arquivo antigo do Cloudinary
        if (comprovanteExistente != null && !string.IsNullOrEmpty(comprovanteExistente.PublicIdCloudinary))
        {
            await DeletarArquivoAsync(comprovanteExistente.PublicIdCloudinary);
        }

        // 2. Faz o upload do novo arquivo
        var uploadResult = await UploadArquivoAsync(arquivo);

        if (uploadResult == null)
            throw new InvalidOperationException("Não foi possível processar o upload do arquivo.");

        // 3. Se já existia a entidade Comprovante, reutiliza atualizando as propriedades; caso contrário, cria uma nova
        if (comprovanteExistente != null)
        {
            comprovanteExistente.UrlCloudinary = uploadResult.Value.Url;
            comprovanteExistente.PublicIdCloudinary = uploadResult.Value.PublicId;
            comprovanteExistente.DataEnvio = DateTime.UtcNow;
            return comprovanteExistente;
        }

        return new Comprovante
        {
            UrlCloudinary = uploadResult.Value.Url,
            PublicIdCloudinary = uploadResult.Value.PublicId,
            DataEnvio = DateTime.UtcNow
        };
    }
}