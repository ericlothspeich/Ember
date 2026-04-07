using Ember.Architecture;
using IfritParticles;
using IfritParticles.Profiles;

namespace Ember.Tests;

public class EmitterNamingTests
{
    // EditorContext.GenerateEmitterName requires a full Game instance.
    // Mirror the logic here for testing the naming algorithm.
    private static string GenerateEmitterName(List<ParticleEmitter> emitters)
    {
        int index = 0;
        while (true)
        {
            string name = "Emitter" + index;
            bool exists = false;
            for (int i = 0; i < emitters.Count; i++)
            {
                if (emitters[i].Name == name)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists) return name;
            index++;
        }
    }

    private static ParticleEmitter CreateEmitter(string name)
    {
        return new ParticleEmitter(10) { Profile = new PointProfile(), Name = name };
    }

    [Fact]
    public void FirstEmitter_IsEmitter0()
    {
        var emitters = new List<ParticleEmitter>();
        Assert.Equal("Emitter0", GenerateEmitterName(emitters));
    }

    [Fact]
    public void SecondEmitter_IsEmitter1()
    {
        var emitters = new List<ParticleEmitter> { CreateEmitter("Emitter0") };
        Assert.Equal("Emitter1", GenerateEmitterName(emitters));
    }

    [Fact]
    public void ThirdEmitter_IsEmitter2()
    {
        var emitters = new List<ParticleEmitter>
        {
            CreateEmitter("Emitter0"),
            CreateEmitter("Emitter1")
        };
        Assert.Equal("Emitter2", GenerateEmitterName(emitters));
    }

    [Fact]
    public void FillsGap_WhenFirstDeleted()
    {
        var emitters = new List<ParticleEmitter>
        {
            CreateEmitter("Emitter1"),
            CreateEmitter("Emitter2")
        };
        Assert.Equal("Emitter0", GenerateEmitterName(emitters));
    }

    [Fact]
    public void FillsGap_WhenMiddleDeleted()
    {
        var emitters = new List<ParticleEmitter>
        {
            CreateEmitter("Emitter0"),
            CreateEmitter("Emitter2")
        };
        Assert.Equal("Emitter1", GenerateEmitterName(emitters));
    }
}
