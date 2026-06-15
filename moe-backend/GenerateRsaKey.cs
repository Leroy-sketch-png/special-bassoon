using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        var rsa = RSA.Create(2048);
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        Directory.CreateDirectory(@"c:\Users\c-leroy.phan\Downloads\final_agile\moe-backend\keys");
        File.WriteAllText(@"c:\Users\c-leroy.phan\Downloads\final_agile\moe-backend\keys\singpass_private.pem", privateKeyPem);
        Console.WriteLine("RSA key generated successfully.");
    }
}
