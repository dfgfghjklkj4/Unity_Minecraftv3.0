using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;
using XLua;

public class GameLaunch : MonoBehaviour
{
    public static GameLaunch instanc;
    public Text T;
  //  private Action luaStart;
   // private Action luaUpdate;
    private Action luaOnDestroy;

    List<Action> luaStart = new List<Action>();
    List<Action> luaUpdate=new List<Action>();
    public void TTT()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

        }
        Debug.Log("TTTEST");
    }

    private void Start0000()
    {
        instanc = this;
        string luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCGameMain" + ".lua";
        string strLuaContent = File.ReadAllText(luaPath);
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);
        Action act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start");
        if (act1 != null)
        {
            print(123);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update");
        if (act1 != null)
        {
            print(456);
            luaUpdate.Add(act1);
        }

        luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCluaTest" + ".lua";


        strLuaContent = File.ReadAllText(luaPath);
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);


        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start");
        if (act1 != null)
        {
            print(1230);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update");
        if (act1 != null)
        {
            print(4560);
            luaUpdate.Add(act1);
        }

        //  XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        LuaFileWatcher.CreateLuaFileWatcher(XLuaManager.Instance.luaEnv);
        for (int i = 0; i < luaStart.Count; i++)
        {
            luaStart[i]();
        }
    }
    private LuaTable scriptEnv;
    private void Start00()
    {
        scriptEnv = XLuaManager.Instance.luaEnv.NewTable();
        instanc = this;
        string luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCGameMain" + ".lua";
        string strLuaContent = File.ReadAllText(luaPath);


        XLuaManager.Instance.LoadScript("MCGameMain");
        XLuaManager.Instance.SafeDoString("MCGameMain.Start_MCGameMain()");
        Action luaAwake = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");

        // XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        // print(strLuaContent);
        //  XLuaManager.Instance.luaEnv.DoString(strLuaContent, "MCGameMain", scriptEnv);
     //   Action luaAwake;
     //   scriptEnv.Get("Start", out luaAwake);
     //   Action luaAwake = scriptEnv.Get<Action>("Start");
        luaAwake();
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);
        Action act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");
        if (act1 != null)
        {
            print(123);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCGameMain");
        if (act1 != null)
        {
            print(456);
            luaUpdate.Add(act1);
        }

        luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCluaTest" + ".lua";


        strLuaContent = File.ReadAllText(luaPath);
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);


        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCluaTest");
        if (act1 != null)
        {
            print(1230);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCluaTest");
        if (act1 != null)
        {
            print(4560);
            luaUpdate.Add(act1);
        }

        //  XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        LuaFileWatcher.CreateLuaFileWatcher(XLuaManager.Instance.luaEnv);
        for (int i = 0; i < luaStart.Count; i++)
        {
            luaStart[i]();
        }
    }

    private void Start()
    {
        scriptEnv = XLuaManager.Instance.luaEnv.NewTable();
        instanc = this;
        string luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCGameMain" + ".lua";
        string strLuaContent = File.ReadAllText(luaPath);


        //  XLuaManager.Instance.LoadScript("MCGameMain");
        // XLuaManager.Instance.SafeDoString("MCGameMain.Start_MCGameMain()");
        //  Action luaAwake = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");

        // XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        // print(strLuaContent);
        //  XLuaManager.Instance.luaEnv.DoString(strLuaContent, "MCGameMain", scriptEnv);
        //   Action luaAwake;
        //   scriptEnv.Get("Start", out luaAwake);
        //   Action luaAwake = scriptEnv.Get<Action>("Start");
        //    luaAwake();
        XLuaManager.Instance.SafeDoString(strLuaContent);
       // XLuaManager.Instance.luaEnv.LoadString(strLuaContent);
        Action act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");
        if (act1 != null)
        {
            print(123);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCGameMain");
        if (act1 != null)
        {
            print(456);
            luaUpdate.Add(act1);
        }

        luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCluaTest" + ".lua";


        strLuaContent = File.ReadAllText(luaPath);
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);


        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCluaTest");
        if (act1 != null)
        {
            print(1230);
         //   luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCluaTest");
        if (act1 != null)
        {
            print(4560);
          //  luaUpdate.Add(act1);
        }

        //  XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        LuaFileWatcher.CreateLuaFileWatcher(XLuaManager.Instance.luaEnv);
        for (int i = 0; i < luaStart.Count; i++)
        {
            luaStart[i]();
        }
    }

    private void Start000()
    {
        scriptEnv = XLuaManager.Instance.luaEnv.NewTable();
        instanc = this;
        string luaPath = Application.streamingAssetsPath + "/LuaScripts/" + "MCGameMain" + ".lua";
        string strLuaContent = File.ReadAllText(luaPath);


      XLuaManager.Instance.LoadScript("MCGameMain");
    XLuaManager.Instance.SafeDoString("MCGameMain.Start_MCGameMain()");
     Action luaAwake = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");

        // XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        // print(strLuaContent);
        //  XLuaManager.Instance.luaEnv.DoString(strLuaContent, "MCGameMain", scriptEnv);
        //   Action luaAwake;
        //   scriptEnv.Get("Start", out luaAwake);
        //   Action luaAwake = scriptEnv.Get<Action>("Start");
    luaAwake();
        XLuaManager.Instance.luaEnv.LoadString(strLuaContent);
        Action act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Start_MCGameMain");
        if (act1 != null)
        {
            print(123);
            luaStart.Add(act1);
        }
        act1 = XLuaManager.Instance.luaEnv.Global.Get<Action>("Update_MCGameMain");
        if (act1 != null)
        {
            print(456);
            luaUpdate.Add(act1);
        }

    

        //  XLuaManager.Instance.Startup();
        //   XLuaManager.Instance.StartGame();
        LuaFileWatcher.CreateLuaFileWatcher(XLuaManager.Instance.luaEnv);
        for (int i = 0; i < luaStart.Count; i++)
        {
            luaStart[i]();
        }
    }

    private void Update()
    {LuaFileWatcher.Reload();
        for (int i = 0; i < luaUpdate.Count; i++)
        {
            luaUpdate[i]();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            XLuaManager.Instance.SafeDoString("GameMain.TestFunc()");
        }
    }

}
