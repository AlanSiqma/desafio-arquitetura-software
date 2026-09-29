using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DesafioArquiteturaSoftware.Application;

public static class RequestHash
{
    public static string Calculate(
        object request)
    {
        var json = JsonSerializer.Serialize(request);

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(bytes);
    }
}