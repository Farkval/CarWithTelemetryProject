# Assets/UserScripts/my_bot.py
# Функция update(robot, dt) вызывается каждый кадр Unity.

import math

def update(robot, dt):
    """
    robot : IRobotAPI  (см. свойства)
      • robot.SetMotorPower(left, right)  –1..1
      • robot.Brake(power)               0..1
      • robot.Position  (Vector3)
      • robot.YawDeg    (float)
      • robot.WheelRPM  (list[float])
      • robot.Lidars    (list[ILidar])
    dt    : float  – прошедшее время, сек
    """
    print("Едем едем едем быстро быстро")
    # пример: едем прямо 3 секунды, потом тормозим
    if update.t < 3:
        robot.SetMotorPower(0.4, 0.4)
    else:
        robot.Brake()
    update.t += dt

update.t = 0
