
using System;
using System.IO;
using System.Text.Json;

namespace PigTycoon.Core
{
    public static class SaveSystem
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };

        public static void Save(GameSaveData data, string filePath)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn lưu không hợp lệ.", nameof(filePath));

            string fullPath = Path.GetFullPath(filePath);
            string? directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(fullPath, json);
        }

        public static GameSaveData Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn lưu không hợp lệ.", nameof(filePath));

            string fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Không tìm thấy file lưu game.", fullPath);

            string json = File.ReadAllText(fullPath);

            GameSaveData? data;

            try
            {
                data = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("File lưu game không đúng định dạng JSON.", ex);
            }

            return data ?? throw new InvalidDataException("File lưu game không có dữ liệu.");
        }

        public static bool TryLoad(string filePath, out GameSaveData? data)
        {
            try
            {
                data = Load(filePath);
                return true;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is ArgumentException ||
                ex is JsonException)
            {
                data = null;
                return false;
            }
        }
    }
}
