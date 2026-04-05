// Particle.fx — GPU instanced billboard particle shader
// Stream 0: per-vertex quad geometry (POSITION0, TEXCOORD0)
// Stream 1: per-instance data (POSITION1, TEXCOORD1-4, COLOR1)
//
// All particles from all emitters are drawn in a single DrawInstancedPrimitives call.
// Each particle's UVOffset + UVScale selects its region in the shared texture atlas.
// Depth occlusion with opaque geometry is handled automatically by DepthStencilState.DepthRead.

float4x4 View;
float4x4 Projection;
texture Texture;

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

struct VSInput
{
    // Per-vertex (stream 0)
    float3 Position      : POSITION0;
    float2 TexCoord      : TEXCOORD0;

    // Per-instance (stream 1)
    float3 InstPosition  : POSITION1;
    float  InstRotation  : TEXCOORD1;
    float2 InstScale     : TEXCOORD2;
    float4 InstColor     : COLOR1;
    float2 InstUVOffset  : TEXCOORD3;
    float2 InstUVScale   : TEXCOORD4;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    // Apply rotation to quad vertex
    float s = sin(input.InstRotation);
    float c = cos(input.InstRotation);
    float2 rotated = float2(
        input.Position.x * c - input.Position.y * s,
        input.Position.x * s + input.Position.y * c
    );

    // Scale the rotated vertex
    float3 localPos = float3(rotated * input.InstScale, 0.0);

    // Billboard: offset along camera right and up vectors
    float3 right = float3(View[0][0], View[1][0], View[2][0]);
    float3 up    = float3(View[0][1], View[1][1], View[2][1]);

    float3 worldPos = input.InstPosition
                    + right * localPos.x
                    + up    * localPos.y;

    float4 viewPos = mul(float4(worldPos, 1.0), View);
    output.Position = mul(viewPos, Projection);

    // Map quad UV (0-1) into atlas region via UVScale + UVOffset
    output.TexCoord = input.TexCoord * input.InstUVScale + input.InstUVOffset;
    output.Color = input.InstColor;

    return output;
}

float4 PS(VSOutput input) : SV_TARGET
{
    float4 texColor = tex2D(TextureSampler, input.TexCoord);
    return texColor * input.Color;
}

technique Default
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
    }
}
