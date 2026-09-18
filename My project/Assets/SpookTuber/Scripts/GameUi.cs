using UnityEngine;
namespace SpookTuber
{
    public static class GameUi
    {
        static Font body,heading;
        public static Font Body=>body?body:body=Resources.Load<Font>("Fonts/ChakraPetch-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Heading=>heading?heading:heading=Resources.Load<Font>("Fonts/ChakraPetch-SemiBold")??Body;
    }
}
