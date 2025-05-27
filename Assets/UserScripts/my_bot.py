import robot
from math import sin, cos

class EightDriver:
    def __init__(self):
        self.t = 0.0

    def update(self, robot: robot.IRobotAPI, dt: float):
        self.t += dt
        robot.SetMotorPower(1, -1)
        
        # безопасная работа с лидаром
        if robot.Lidars:
            pts = robot.Lidars[0].PointCloud
            if pts:
                dmin = min(pt.Distance for pt in pts)
                print(f"Nearest obstacle: {dmin:.2f} m")

bot = EightDriver()

def update(robot, dt):
    bot.update(robot, dt)