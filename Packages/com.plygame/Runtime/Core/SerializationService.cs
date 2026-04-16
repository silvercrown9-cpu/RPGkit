using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Runtime.Core.Serialization
{
    /// <summary>
    /// Современный сервис сериализации на базе System.Text.Json
    /// </summary>
    public static class SerializationService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new Vector2Converter(), new ColorConverter() }
        };
        
        /// <summary>
        /// Сериализация объекта в JSON строку
        /// </summary>
        public static string ToJson<T>(T obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка сериализации: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Десериализация из JSON строки
        /// </summary>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка десериализации: {ex.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Асинхронное сохранение в файл
        /// </summary>
        public static async Task SaveToFileAsync<T>(T obj, string filePath)
        {
            try
            {
                var json = ToJson(obj);
                if (json == null) return;
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                Debug.Log($"Данные сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка сохранения файла: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Асинхронная загрузка из файла
        /// </summary>
        public static async Task<T> LoadFromFileAsync<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Файл не найден: {filePath}");
                    return default;
                }
                
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка загрузки файла: {ex.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Сохранение в PlayerPrefs
        /// </summary>
        public static void SaveToPlayerPrefs<T>(T obj, string key)
        {
            try
            {
                var json = ToJson(obj);
                if (json != null)
                {
                    PlayerPrefs.SetString(key, json);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка сохранения в PlayerPrefs: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Загрузка из PlayerPrefs
        /// </summary>
        public static T LoadFromPlayerPrefs<T>(string key, T defaultValue = default)
        {
            try
            {
                if (PlayerPrefs.HasKey(key))
                {
                    var json = PlayerPrefs.GetString(key);
                    return FromJson<T>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка загрузки из PlayerPrefs: {ex.Message}");
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Глубокое клонирование через сериализацию
        /// </summary>
        public static T DeepClone<T>(T obj)
        {
            var json = ToJson(obj);
            return FromJson<T>(json);
        }
    }
    
    /// <summary>
    /// Конвертер для Vector2
    /// </summary>
    public class Vector2Converter : System.Text.Json.Serialization.JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0, y = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return new Vector2(x, y);
                    
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();
                        
                        if (propertyName == "x") x = reader.GetSingle();
                        else if (propertyName == "y") y = reader.GetSingle();
                    }
                }
            }
            return Vector2.zero;
        }
        
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteEndObject();
        }
    }
    
    /// <summary>
    /// Конвертер для Color
    /// </summary>
    public class ColorConverter : System.Text.Json.Serialization.JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float r = 0, g = 0, b = 0, a = 1;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return new Color(r, g, b, a);
                    
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();
                        
                        if (propertyName == "r") r = reader.GetSingle();
                        else if (propertyName == "g") g = reader.GetSingle();
                        else if (propertyName == "b") b = reader.GetSingle();
                        else if (propertyName == "a") a = reader.GetSingle();
                    }
                }
            }
            return Color.white;
        }
        
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.r);
            writer.WriteNumber("g", value.g);
            writer.WriteNumber("b", value.b);
            writer.WriteNumber("a", value.a);
            writer.WriteEndObject();
        }
    }
}
