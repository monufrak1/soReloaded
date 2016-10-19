using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

class ActivePlayer
{
    private static PlayerIndex playerIndex;
    public static PlayerIndex PlayerIndex
    {
        get { return playerIndex; }
        set { playerIndex = value; }
    }

    private static bool fullHDEnabled;
    public static bool FullHDEnabled
    {
        get { return fullHDEnabled; }
        set { fullHDEnabled = value; }
    }

    private static GameObjects.PlayerProfile profile;
    public static GameObjects.PlayerProfile Profile
    {
        get { return profile; }
        set { profile = value; }
    }
}
