"""
Auto-generated stub for Unity IRobotAPI.
Edit your own scripts, but **не правьте этот файл вручную**.
"""
from typing import List, Any

class Robot:

    # Датчики лидаров
    Lidars: List[Any]  #: List`1

    # Ручное управление
    ManualControl: bool  #: Boolean

    # Ориентация робота
    YawDeg: float  #: Single

    # Список значений энкодеров колес
    WheelRPM: List[Any]  #: Single[]

    Position: Any  #: Vector3

    # Облако точек
    PointCloud: List[Any]  #: List`1

    # Движение
    def SetMotorPower(self, left: float, right: float) -> Any: ...

    # Тормоз
    def Brake(self, power: float) -> Any: ...

# runtime instance (Unity заменит его настоящим объектом)
robot = Robot()
