namespace Ifrit.Interpolators
{
    public interface IInterpolator
    {
        string Name { get; set; }
        bool Enabled { get; set; }
        void Interpolate(ref Particle particle, float normalizedAge);
    }
}
