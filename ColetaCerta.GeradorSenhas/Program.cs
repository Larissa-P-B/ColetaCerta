using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var contexto = new object();

GerarHash("admin", "Admin@12345");
GerarHash("user", "User@12345");

Console.WriteLine("Pressione ENTER para sair.");
Console.WriteLine(
    "CHAVE JWT: " +
    Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

Console.ReadLine();

void GerarHash(string login, string senha)
{
    var hash = hasher.HashPassword(contexto, senha);

    var resultado = hasher.VerifyHashedPassword(
        contexto,
        hash,
        senha);

    Console.WriteLine($"LOGIN: {login}");
    Console.WriteLine($"HASH: {hash}");
    Console.WriteLine($"VERIFICACAO: {resultado}");
    Console.WriteLine();
}