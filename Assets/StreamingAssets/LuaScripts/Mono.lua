
--Mono = {}



function  SetActiveMono(act,compoment)
    if act==true then

        InitMono_GameLogicCenter(compoment)
      

        compoment. enable=true
        compoment.haveStart = true
        compoment.haveUpdate =  true
    
else
    compoment. enable=false
    compoment.haveStart = false
    compoment.haveUpdate =  false
end
end


function  AwakeMono(act,compoment)
    if act==true then

        InitMono_GameLogicCenter(compoment)
      

        compoment. enable=true
        compoment.haveStart = true
        compoment.haveUpdate =  true
    
else
    compoment. enable=false
    compoment.haveStart = false
    compoment.haveUpdate =  false
end
end


function  OnEnableMono(act,compoment)
    if act==true then

        InitMono_GameLogicCenter(compoment)
      

        compoment. enable=true
        compoment.haveStart = true
        compoment.haveUpdate =  true
    
else
    compoment. enable=false
    compoment.haveStart = false
    compoment.haveUpdate =  false
end
end

function  StartMono(act,compoment)
    if act==true then

        InitMono_GameLogicCenter(compoment)
      

        compoment. enable=true
        compoment.haveStart = true
        compoment.haveUpdate =  true
    
else
    compoment. enable=false
    compoment.haveStart = false
    compoment.haveUpdate =  false
end
end
