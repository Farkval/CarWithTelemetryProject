using Assets.Scripts.Robot.Api.Attributes;
using UnityEngine;

namespace Assets.Scripts.Robot.Api.Interfaces
{
    /// <summary>
    /// Универсальный интерфейс «камерного» сенсора.
    /// Позволяет захватить текущий вид в виде Texture2D или байтов закодированного изображения.
    /// </summary>
    public enum ImageFormat
    {
        PNG,
        JPEG
    }

    public interface ICameraSensor
    {
        /// <summary>
        /// Ширина изображения в пикселях.
        /// </summary>
        [RobotApi]
        int Width { get; }

        /// <summary>
        /// Высота изображения в пикселях.
        /// </summary>
        [RobotApi]
        int Height { get; }

        /// <summary>
        /// Захватывает текущий кадр в Texture2D.
        /// </summary>
        /// <returns>Объект Texture2D с пикселями.</returns>
        Texture2D CaptureTexture();

        /// <summary>
        /// Захватывает текущий кадр и возвращает массив байтов в выбранном формате.
        /// </summary>
        /// <param name="format">Формат кодирования (PNG или JPEG).</param>
        /// <returns>Массив байтов закодированного изображения.</returns>
        [RobotApi]
        byte[] CaptureImageBytes(ImageFormat format = ImageFormat.PNG);
    }
}
