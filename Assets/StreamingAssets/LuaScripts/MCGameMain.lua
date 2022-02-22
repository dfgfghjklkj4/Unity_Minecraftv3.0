require("BaseClass")


local GameLogicCenter =  require("GameLogicCenter")
gameLogicCenter = {}










 function Start_MCGameMain()



    print("MCGameMain start...")
    gameLogicCenter= GameLogicCenter.New()
    gameLogicCenter:Start_GameLogicCenter()


end

local function TestFunc()
    print("call from self...")
  
end

function Update_MCGameMain()
  gameLogicCenter:Update_GameLogicCenter()
end






--MCGameMain.Start = Start_MCGameMain
--MCGameMain.Update= Update_MCGameMain
--MCGameMain.TestFunc = TestFunc
--StartTabel["MCGameMain"]=TestFunc
--StartTabel.MCGameMain=TestFunc
--UpdateTabel["MCGameMain"]=Update
return MCGameMain