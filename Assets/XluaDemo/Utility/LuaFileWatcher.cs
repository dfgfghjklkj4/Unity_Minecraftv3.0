using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using XLua;

[CSharpCallLua]
public delegate void ReloadDelegate(string path);

public  class LuaFileWatcher
{
    private static ReloadDelegate ReloadFunction;
    
    private static HashSet<string> _changedFiles = new HashSet<string>();
    
    public static void CreateLuaFileWatcher(LuaEnv luaEnv)
    {
        var scriptPath = Path.Combine(Application.streamingAssetsPath, "LuaScripts");
        var directoryWatcher =
            new DirectoryWatcher(scriptPath, new FileSystemEventHandler(LuaFileOnChanged));
        ReloadFunction = luaEnv.Global.Get<ReloadDelegate>("hotfix");
      
     //   #if UNITY_EDITOR
   //EditorApplication.update -= Reload;
     //EditorApplication.update += Reload;
//#endif
    
    }

    private static void LuaFileOnChanged(object obj, FileSystemEventArgs args)
    {
        Debug.Log( args.FullPath);
     //   GameLaunch.instanc.T.text=args.FullPath;
        var fullPath = args.FullPath;
        var luaFolderName = "LuaScripts";
        var requirePath = fullPath.Replace(".lua", "");
        Debug.Log("Re00----  " + requirePath);
        var luaScriptIndex = requirePath.IndexOf(luaFolderName) + luaFolderName.Length + 1;
        Debug.Log("Re11----  " + luaScriptIndex);
        requirePath = requirePath.Substring(luaScriptIndex);
        Debug.Log("Re11----Substring    " + requirePath);
        requirePath = requirePath.Replace('\\','.');
        Debug.Log("Re11----requirePath    " + requirePath);
        Debug.Log("Re  "+requirePath );
        _changedFiles.Add(requirePath);
        
    }

    public static void Reload()
    {     
        
      
        if (_changedFiles.Count == 0)
        {  
          //  Debug.Log("Reload:00000000" );
            return;
        }

        foreach (var file in _changedFiles)
        {
         //   GameLaunch.instanc.T.text=file;
            ReloadFunction(file);
            Debug.Log("Reload:   " + file);
        }
        _changedFiles.Clear();
    }
}