using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;
using XLua;

public class luaStart : MonoBehaviour
{
    public static luaStart instanc;
    //  private Action luaStart;
    // private Action luaUpdate;
    private Action luaOnDestroy;

    List<Action> StartInLua = new List<Action>();
    List<Action> Update_InLua = new List<Action>();
    public void TTT()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

        }
        Debug.Log("TTTEST");
    }

    private LuaTable scriptEnv;

    private void Start()
    {
        scriptEnv = XLuaManager.Instance.luaEnv.NewTable();
        instanc = this;
        string luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCGameMain" + ".lua";
        string strLuaContent = File.ReadAllText(luaPath);


        XLuaManager.Instance.SafeDoString(strLuaContent);
        // XLuaManager.Instance.luaEnv.LoadString(strLuaContent);
        Action act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");
        if (act1 != null)
        {
            print(123);
            StartInLua.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCGameMain");
        if (act1 != null)
        {
            print(456);
            Update_InLua.Add(act1);
        }

      
        LuaFileWatcher.CreateLuaFileWatcher(XLuaManager.Instance.luaEnv);
        for (int i = 0; i < StartInLua.Count; i++)
        {
            StartInLua[i]();
        }
    }


    private void Update()
    {
        LuaFileWatcher.Reload();
        for (int i = 0; i < Update_InLua.Count; i++)
        {
            Update_InLua[i]();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            XLuaManager.Instance.SafeDoString("GameMain.TestFunc()");
        }
    }

}
