namespace Assets.Scripts.Garage.Interfaces
{
    /// <summary>
    /// Метка для компонентов, у которых есть метод ApplySettings(),
    /// и которые хотят, чтобы его вызывали сразу после правки поля.
    /// </summary>
    public interface IApplySettings
    {
        void ApplySettings();
    }
}
