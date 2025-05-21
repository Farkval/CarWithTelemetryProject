namespace Assets.Scripts.MapEditor
{
    // порядок индексов важен — мы сериализуем в byte[]
    public enum SurfaceType : byte
    {
        Grass = 0,
        Mud = 1,
        Gravel = 2,
        Water = 3,
        Ice = 4
    }
}
