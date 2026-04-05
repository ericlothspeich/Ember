using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public interface IEmissionProfile
    {
        void Emit(Random random, out Vector2 position, out Vector2 direction);
    }
}
