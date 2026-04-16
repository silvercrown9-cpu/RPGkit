using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PlyGame.Runtime.Core.Graph;

namespace PlyGame.Runtime.Core.Serialization
{
    /// <summary>
    /// Modern serialization service using System.Text.Json
    /// Supports async file I/O and AOT-compatible types
    /// </summary>
    public class SerializationService : ISerializationService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        
        public SerializationService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { }
            };
        }
        
        /// <summary>
        /// Serialize graph asset to JSON string
        /// </summary>
        public string SerializeGraph(GraphAsset graph)
        {
            try
            {
                var data = new GraphSaveData
                {
                    Version = graph.Version,
                    Description = graph.Description,
                    Nodes = new System.Collections.Generic.List<GraphNodeSaveData>()
                };
                
                foreach (var nodeData in graph.Nodes)
                {
                    data.Nodes.Add(new GraphNodeSaveData
                    {
                        Guid = nodeData.Guid,
                        Name = nodeData.Name,
                        NodeType = nodeData.NodeType,
                        JsonData = nodeData.JsonData,
                        Connections = nodeData.Connections,
                        PositionX = nodeData.PositionX,
                        PositionY = nodeData.PositionY
                    });
                }
                
                return JsonSerializer.Serialize(data, _jsonOptions);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to serialize graph: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Deserialize graph from JSON string
        /// </summary>
        public GraphAsset DeserializeGraph(string json, GraphAsset target = null)
        {
            try
            {
                var data = JsonSerializer.Deserialize<GraphSaveData>(json, _jsonOptions);
                if (data == null)
                {
                    UnityEngine.Debug.LogError("Deserialized graph data is null");
                    return null;
                }
                
                if (target == null)
                {
                    target = ScriptableObject.CreateInstance<GraphAsset>();
                }
                
                // Note: In production, use proper Unity serialization workflow
                // This is a simplified example
                UnityEngine.Debug.Log($"Deserialized graph: {data.Description} (v{data.Version})");
                
                return target;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to deserialize graph: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Serialize to file asynchronously
        /// </summary>
        public async Task<bool> SaveToFileAsync<T>(T data, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to save to file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load from file asynchronously
        /// </summary>
        public async Task<T> LoadFromFileAsync<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    UnityEngine.Debug.LogWarning($"File not found: {filePath}");
                    return default;
                }
                
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to load from file: {e.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Serialize to bytes
        /// </summary>
        public byte[] SerializeToBytes<T>(T data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to serialize to bytes: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Deserialize from bytes
        /// </summary>
        public T DeserializeFromBytes<T>(byte[] bytes)
        {
            try
            {
                var json = Encoding.UTF8.GetString(bytes);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to deserialize from bytes: {e.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Validate JSON format
        /// </summary>
        public bool IsValidJson(string json)
        {
            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Serializable graph data for JSON serialization
    /// </summary>
    [Serializable]
    public class GraphSaveData
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<GraphNodeSaveData> Nodes { get; set; }
    }
    
    /// <summary>
    /// Serializable node data for JSON serialization
    /// </summary>
    [Serializable]
    public class GraphNodeSaveData
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public string NodeType { get; set; }
        public string JsonData { get; set; }
        public System.Collections.Generic.List<string> Connections { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }
    
    /// <summary>
    /// Interface for serialization service
    /// </summary>
    public interface ISerializationService
    {
        string SerializeGraph(GraphAsset graph);
        GraphAsset DeserializeGraph(string json, GraphAsset target = null);
        Task<bool> SaveToFileAsync<T>(T data, string filePath);
        Task<T> LoadFromFileAsync<T>(string filePath);
        byte[] SerializeToBytes<T>(T data);
        T DeserializeFromBytes<T>(byte[] bytes);
        bool IsValidJson(string json);
    }
}
