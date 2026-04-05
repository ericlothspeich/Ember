namespace Ifrit.Modifiers
{
    public interface IModifier
    {
        string Name { get; set; }
        bool Enabled { get; set; }
        void Update(ref Particle particle, float dt);
    }
}
