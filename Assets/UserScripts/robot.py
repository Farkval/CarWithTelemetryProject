"""
Описание Python-API для взаимодействия с виртуальным роботом в симуляторе.
Содержит определения интерфейсов сенсоров, привода и общего API робота.
"""
class Vector3:
    """
    Структура для представления 3D-вектора или координаты в пространстве.
    Поля:
        x (float): координата по оси X
        y (float): координата по оси Y
        z (float): координата по оси Z
    """
    x: float
    y: float
    z: float


class IDetectedObjectInfo:
    """
    Интерфейс описывает данные об объекте, обнаруженном камерой.
    Атрибуты:
        Name (str): название или метка объекта
        position (Vector3): мировая позиция объекта
        distance (float): расстояние до объекта (в метрах)
        viziblePercent (float): доля видимой области объекта (0.0–1.0)
    """
    Name: str
    position: Vector3
    distance: float
    viziblePercent: float


class ILidarPoint:
    """
    Интерфейс для точки облака LiDAR.
    Атрибуты:
        WorldPosition (Vector3): мировая координата точки отражения
        Distance (float): расстояние от сенсора до точки
    """
    WorldPosition: Vector3
    Distance: float


class ICameraSensor:
    """
    Интерфейс для камеры, обнаруживает объекты в кадре.
    Свойства:
        DetectedObjects: список объектов IDetectedObjectInfo
    """
    @property
    def DetectedObjects(self) -> list[IDetectedObjectInfo]:
        ...  # реализуется в симуляторе


class ILidar:
    """
    Интерфейс для LiDAR-датчика.
    Свойства:
        PointCloud: облако точек (List[ILidarPoint])
    """
    @property
    def PointCloud(self) -> list[ILidarPoint]:
        ...  # реализуется в симуляторе


class IEncoders:
    """
    Интерфейс для энкодеров колёс.
    Свойства:
        WheelRPM: текущие обороты каждого колеса (List[float])
    """
    @property
    def WheelRPM(self) -> list[float]:
        ...  # реализуется в симуляторе


class IGps:
    """
    Интерфейс для GPS-модуля.
    Свойства:
        Position: координаты робота в мировых единицах (Vector3)
    """
    @property
    def Position(self) -> Vector3:
        ...  # реализуется в симуляторе


class ICompass:
    """
    Интерфейс для компаса/гирокомпаса.
    Свойства:
        YawDeg: угол ориентации на север в градусах
    """
    @property
    def YawDeg(self) -> float:
        ...  # реализуется в симуляторе


class IMotion:
    """
    Интерфейс управления движением робота.
    Методы:
        SetMotorPower(left, right): задать мощность моторов
        SetSteerAngle(steer): задать угол поворота рулевого управления
        Brake(power): задать силу торможения
    Свойства:
        CurrentSpeed: текущая скорость
        CurrentSteerAngle: текущий угол поворота колес
    """
    def SetMotorPower(self, left: float, right: float) -> None:
        ...  # реализуется в симуляторе

    def SetSteerAngle(self, steer: float) -> None:
        ...  # реализуется в симуляторе

    def Brake(self, power: float = 1.0) -> None:
        ...  # реализуется в симуляторе

    @property
    def CurrentSpeed(self) -> float:
        ...  # реализуется в симуляторе

    @property
    def CurrentSteerAngle(self) -> float:
        ...  # реализуется в симуляторе


class IRobotAPI(IMotion, IEncoders, IGps, ICompass):
    """
    Основной интерфейс робота, объединяет управление и сенсоры.

    Атрибуты:
        Lidars (List[ILidar]): список LiDAR-датчиков
        Cameras (List[ICameraSensor]): список камер
        ManualControl (bool): флаг ручного управления (True — ручное, False — скрипт)
    """
    Lidars: list[ILidar]
    Cameras: list[ICameraSensor]
    ManualControl: bool
