using System;
using System.IO;
using System.Text;
#if !UNITY_5_3_OR_NEWER
using System.Text.Json;
#else
using System.Runtime.Serialization.Json;
#endif

namespace PigTycoon.Core
{
    public static class SaveSystem
    {
#if !UNITY_5_3_OR_NEWER
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };
#endif

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

#if !UNITY_5_3_OR_NEWER
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(fullPath, json);
#else
            var ser = new DataContractJsonSerializer(typeof(GameSaveData));
            using var ms = new MemoryStream();
            ser.WriteObject(ms, data);
            File.WriteAllText(fullPath, Encoding.UTF8.GetString(ms.ToArray()));
#endif
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
#if !UNITY_5_3_OR_NEWER
                data = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions);
#else
                var ser = new DataContractJsonSerializer(typeof(GameSaveData));
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                data = ser.ReadObject(ms) as GameSaveData;
#endif
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException($"File lưu game không đúng định dạng JSON: {ex.Message}", ex);
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
                ex is InvalidDataException)
            {
                data = null;
                return false;
            }
        }
    }
}
