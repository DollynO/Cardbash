using System;
using System.Collections.Generic;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts;

public class WorldContext : IContext, IDisposable
{
    public WorldSettings settings;
    public IList<PlayerCharacter> players;

    public void Dispose()
    {
        // TODO release managed resources here
    }
}