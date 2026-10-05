namespace NewsHub.API;

/// <summary>
/// Helper gọn nhẹ nạp các biến môi trường từ file .env ở môi trường Local Development
/// mà không cần cài đặt thêm external package.
/// </summary>
public static class DotEnvLoader
{
    public static void Load()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDir != null)
        {
            var envPath = Path.Combine(currentDir.FullName, ".env");
            if (File.Exists(envPath))
            {
                LoadFile(envPath);
                return;
            }

            currentDir = currentDir.Parent;
        }
    }

    private static void LoadFile(string filePath)
    {
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            // Loại bỏ dấu nháy kép hoặc đơn nếu có
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Chỉ set nếu môi trường chưa có giá trị này (ưu tiên biến môi trường OS/Cloud)
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
