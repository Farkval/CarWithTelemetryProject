using System.Collections.Generic;

namespace Assets.Scripts.Consts
{
    public class GameObjectNameConst
    {
        public static readonly List<string> ToolsToggles = new()
        {
            "RaiseToggle",
            "PitToggle",
            "GrassToggle",
            "MudToggle",
            "IceToggle",
            "WaterToggle",
            "GravelToggle"
        };

        public const string BASE_CAR_CAMERA = "BaseCamera";
    }
}
