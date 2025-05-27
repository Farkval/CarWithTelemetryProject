using System.IO;
using UnityEditor;
using UnityEngine;

public static class RobotStubGenerator
{
    private const string OutputPath = "Assets/UserScripts/robot.py";

    [MenuItem("Tools/Generate Robot Stub")]
    public static void GenerateStub()
    {
        var code = @$"
class ImageFormat:
    PNG = 0  # int
    JPEG = 1  # int

class ILidarPoint:
    """"""Точка лидарного облака.""""""
    WorldPosition = None  # Tuple[float, float, float]
    Distance = 0.0        # float

class ILidar:
    """"""Лидар-сенсор.""""""
    @property
    def PointCloud(self):
        # возвращает List[ILidarPoint]
        return []  # List[ILidarPoint]

class ICameraSensor:
    """"""Камера-сенсор.""""""
    Width = 0   # int
    Height = 0  # int
    def CaptureTexture(self):
        # -> Texture2D
        pass
    def CaptureImageBytes(self, format=None):
        # format: ImageFormat, -> bytes
        return b""""

class IEncoders:
    """"""Энкодеры колёс.""""""
    WheelRPM = []  # List[float]

class IGps:
    """"""GPS-позиция.""""""
    Position = None  # Tuple[float, float, float]

class ICompass:
    """"""Курс робота (yaw).""""""
    YawDeg = 0.0  # float

class IMotion:
    """"""Команды движения.""""""
    def SetMotorPower(self, left, right):  # left: float, right: float
        pass
    def Brake(self, power=1.0):  # power: float
        pass

class IRobotAPI(IMotion, IEncoders, IGps, ICompass):
    """"""Главный интерфейс робота.""""""
    Lidars = []      # List[ILidar]
    Cameras = []     # List[ICameraSensor]
    ManualControl = True  # bool
";
        File.WriteAllText(OutputPath, code);
        AssetDatabase.Refresh();
        Debug.Log("robot.py stub generated successfully");
    }
}