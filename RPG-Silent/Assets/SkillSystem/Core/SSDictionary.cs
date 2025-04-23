using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SkillSystem.Core
{
    public static class SSDictionary
    {
        private static readonly ConcurrentDictionary<string, object> _dictionary = new ConcurrentDictionary<string, object>();

        public static SSFunction CopyFunction; 
        public static void Add<T>(string key, T value)
        {
            if (!_dictionary.TryAdd(key, value))
            {
                throw new Exception($"[Skill System] Key '{key}' already exists.");
            }
        }

        public static void ForceAdd<T>(string key, T value)
        {
            if (!_dictionary.TryAdd(key, value))
            {
                _dictionary[key] = value;
            }
        }
        
        public static bool Remove(string key)
        {
            return _dictionary.TryRemove(key, out _);
        }
        
        public static T Get<T>(string key)
        {
            if (_dictionary.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    throw new Exception($"[SkillSystem] Unable to find a valid {typeof(T)} for key: {key}");
                }
            }
            throw new KeyNotFoundException($"[SkillSystem] Key '{key}' not found.");
        }
        
        public static bool TryGet<T>(string key, out T value, bool throwOnFailure = false)
        {
            try
            {
                value = Get<T>(key);
                return true;
            }
            catch
            {
                if (throwOnFailure)
                {
                    throw new Exception($"[SkillSystem] Failed to retrieve value for key '{key}' with type {typeof(T)}.");
                }
                value = default;
                return false;
            }
        }
        
        public static bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }
        
        public static void Clear()
        {
            _dictionary.Clear();
        }
        
        public static IEnumerable<string> GetAllKeys()
        {
            return _dictionary.Keys;
        }

        public static IEnumerable<object> GetAllValues()
        {
            return _dictionary.Values;
        }
    }
}