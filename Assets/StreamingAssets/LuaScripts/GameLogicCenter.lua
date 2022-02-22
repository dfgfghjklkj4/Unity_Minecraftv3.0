require("GV")
require("Mono")
 GameLogicCenter = BaseClass("GameLogicCenter")

 
 --加载和创建一个脚本实例
  --local MCluaTest =  require("MCluaTest")
  --local mCluaTest = MCluaTest.New()

 function GameLogicCenter:Start_GameLogicCenter()
   
    print("GameLogicCenter start...")

end



function GameLogicCenter:Update_GameLogicCenter()

  if  _G.ChangeTabel ~= nil then
    for k, v in pairs( _G.ChangeTabel) do
    v:SetActive(true)
    end
    _G.ChangeTabel = {}
end


  if  _G.StartTabel ~= nil then
    for k, v in pairs( _G.StartTabel) do
      v:Start()
    end
    _G.StartTabel = {}
end



 for k, v in pairs( _G.UpdateTabel) do
   v:Update()
end


end




local function OnReload(self)
  print('call onReload from: ',self.__cname)
  if self.__ctype == ClassType.class then
 --     print("this is a class not a instance")
      for k,v in pairs(self.instances) do
          print("call instance reload: ",k)
          if v.OnReload ~= nil then
              v:OnReload()
          end
      end
  else
      if self.__ctype == ClassType.instance then
          print("this is a instance  GameLogicCenter")
        --  MCGameMain.playerFunc = self.TestFunc
      end
  end
end



function GameLogicCenter:SetActive(act)
  
end

GameLogicCenter.OnReload = OnReload

--GameLogicCenter.Start = Start_GameLogicCenter
--GameLogicCenter.Update= Update_GameLogicCenter


return GameLogicCenter