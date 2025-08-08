using System.Security.Cryptography;
using System.Text;

namespace Tranzor.Utils;

public class StringUtils
{
    private const string NumericChars = "0123456789";
    private const string FullChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                    "abcdefghijklmnopqrstuvwxyz" +
                                    "0123456789" +
                                    "#$%*&_+=^?/";
    private const string ExclusiveChars = "abcdefghijklmnopqrstuvwxyz" +
                                         "0123456789";

    private static readonly Random random = new Random();

    public static string GenerateExclusiveCode(int length = 8)
    {
        StringBuilder result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(0, ExclusiveChars.Length);
            result.Append(ExclusiveChars[index]);
        }

        return result.ToString();
    }

    public static string GenerateCode(int length = 8)
    {
        StringBuilder result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(0, FullChars.Length);
            result.Append(FullChars[index]);
        }

        return result.ToString();
    }

    public static string GenerateNumberCode(int length = 8)
    {
        StringBuilder result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(0, NumericChars.Length);
            result.Append(NumericChars[index]);
        }

        return result.ToString();
    }

    public static string GenerateHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}