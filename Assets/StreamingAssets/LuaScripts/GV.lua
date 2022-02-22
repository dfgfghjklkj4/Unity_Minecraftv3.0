function InitMono_GameLogicCenter(monotable)
    --  if monotable.Start ~= nil then
          StartTabel[monotable] = monotable
     -- end
      if monotable.haveUpdate then
          UpdateTabel[monotable] = monotable
      end
  end

  UpdateTabel = {}
  StartTabel = {}
 ChangeTabel = {}