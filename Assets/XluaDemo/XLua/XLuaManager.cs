using System;
using System.IO;
using UnityEngine;
using XLua;

[Hotfix]
[LuaCallCSharp]
public class XLuaManager : MonoSingleton<XLuaManager>
{
    public  const string luaScriptsFolder = "LuaScripts";
    private const string gameMainScriptName = "GameMain";
    private const string MCgameMainScriptName = "MCGameMain";
    private const string hotfixScriptName = "Hotfix";
    
  //  private LuaEnv _luaEnv;

    private Action<float> _luaUpdate = null;

    private static string _luaScriptsFullPath;

    public  LuaEnv luaEnv;
  

    protected override void Init()
    {
        _luaScriptsFullPath = Path.Combine(Application.streamingAssetsPath, luaScriptsFolder);
        base.Init();
        luaEnv = new LuaEnv();
        luaEnv.AddLoader(CustomLoader);
        LoadScript(hotfixScriptName);
    }

    public void StartGame()
    {
        LoadScript(gameMainScriptName);
        SafeDoString("GameMain.Start()");
        _luaUpdate = luaEnv.Global.Get<Action<float>>("Update");
    }

    public void StartGameMC()
    {
        LoadScript(MCgameMainScriptName);
        SafeDoString("MCGameMain.Start()");
        _luaUpdate = luaEnv.Global.Get<Action<float>>("Update");
    }

    public void LoadScript(string scriptName)
    {
        SafeDoString(string.Format("require('{0}')",scriptName));
    }
    
    public void SafeDoString(string scriptContent)
    {
        if (luaEnv != null)
        {
            try
            {
                luaEnv.DoString(scriptContent);
            }
            catch (System.Exception ex)
            {
                string msg = string.Format("xLua exception : {0}\n {1}", ex.Message, ex.StackTrace);
                Debug.LogError(msg, null);
            }
        }
    }
    
    public static byte[] CustomLoader(ref string filepath)
    {
        string scriptPath = string.Empty;
        filepath = filepath.Replace(".", "/") + ".lua";

        scriptPath = Path.Combine(_luaScriptsFullPath, filepath);
        return FileOperation.SafeReadAllBytes(scriptPath);
    }

    private void Update()
    {
        if (luaEnv != null)
        {
            luaEnv.Tick();
        }

        if (_luaUpdate != null)
        {
            _luaUpdate(Time.deltaTime);
        }
    }

    public override void Dispose()
    {
        if (luaEnv != null)
        {
            luaEnv.Dispose();
            luaEnv = null;
        }
    }
}
