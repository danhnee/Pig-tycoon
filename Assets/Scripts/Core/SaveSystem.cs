using System;
using System.IO;
using Newtonsoft.Json;

namespace PigTycoon.Core
{
    /// <summary>
    /// Lưu / tải trạng thái game dạng JSON.
    /// Một đường tuần tự hóa duy nhất (Newtonsoft.Json — gói chính thức com.unity.nuget.newtonsoft-json,
    /// Sổ tay v3.0 mục 15.1) cho cả Unity và bộ test ngoài Unity, để định dạng file được kiểm thử
    /// chính là định dạng game ghi ra. Trước đây test dùng System.Text.Json còn Unity dùng
    /// DataContractJsonSerializer, hai định dạng khác nhau và System.Text.Json không có trong
    /// hồ sơ .NET Standard 2.1 của Unity.
    /// </summary>
    public static class SaveSystem
    {
        public const int CurrentSaveVersion = 1;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            Culture = System.Globalization.CultureInfo.InvariantCulture
        };

        public static string Serialize(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return JsonConvert.SerializeObject(data, Settings);
        }

        public static bool TryDeserialize(string json, out GameSaveData data, out string error)
        {
            data = null!;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON rỗng.";
                return false;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<GameSaveData>(json, Settings);
                if (parsed == null)
                {
                    error = "Không đọc được dữ liệu lưu.";
                    return false;
                }
                data = parsed;
                return true;
            }
            catch (JsonException ex)
            {
                // Lỗi định dạng được trả về cho người gọi qua `error`, không nuốt im lặng.
                error = ex.Message;
                return false;
            }
        }

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

            // Ghi ra file tạm rồi thay thế, tránh để lại file lưu hỏng khi bị ngắt giữa chừng.
            string tempPath = fullPath + ".tmp";
            File.WriteAllText(tempPath, Serialize(data));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            File.Move(tempPath, fullPath);
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
                data = JsonConvert.DeserializeObject<GameSaveData>(json, Settings);
            }
            catch (JsonException ex)
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
