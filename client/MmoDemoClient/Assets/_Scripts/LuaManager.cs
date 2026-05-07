using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

namespace MmoDemo.Client
{
    public class LuaManager : IDisposable
    {
        private Script _script;
        private readonly Dictionary<string, object> _bridges = new();
        private readonly HashSet<Type> _registeredBridgeTypes = new();

        public void Start()
        {
            _script = new Script();
            foreach (var kv in _bridges)
                ApplyBridge(kv.Key, kv.Value);
        }

        public void RegisterBridge(string name, object instance)
        {
            _bridges[name] = instance;
            if (_script != null) ApplyBridge(name, instance);
        }

        public DynValue DoString(string script)
        {
            if (_script == null) return DynValue.Nil;
            try { return _script.DoString(script); }
            catch (Exception e) { Debug.LogError($"[Lua] Error: {e.Message}"); return DynValue.Nil; }
        }

        public DynValue DoFile(string path)
        {
            if (_script == null) return DynValue.Nil;
            try { return _script.DoFile(path); }
            catch (Exception e) { Debug.LogError($"[Lua] Error: {e.Message}"); return DynValue.Nil; }
        }

        public object Call(string funcName, params object[] args)
        {
            if (_script == null) return null;
            try
            {
                var fn = _script.Globals.Get(funcName);
                if (fn.IsNil()) return null;
                return _script.Call(fn, args);
            }
            catch (Exception e) { Debug.LogError($"[Lua] Call '{funcName}' error: {e.Message}"); return null; }
        }

        public void Reload()
        {
            _script = new Script();
            foreach (var kv in _bridges)
                ApplyBridge(kv.Key, kv.Value);
        }

        public void Dispose()
        {
            _bridges.Clear();
            _script = null;
        }

        private void ApplyBridge(string name, object instance)
        {
            if (_script == null || instance == null) return;

            try
            {
                var type = instance.GetType();
                if (_registeredBridgeTypes.Add(type))
                    UserData.RegisterType(type);

                _script.Globals[name] = UserData.Create(instance);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lua] Bridge '{name}' failed: {e.Message}");
            }
        }
    }
}
