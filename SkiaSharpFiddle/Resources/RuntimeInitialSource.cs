in fragmentProcessor color_map;

uniform float scale;
uniform half exp;
uniform float3 in_colors0;

half4 main(float2 p)
{
    half4 texColor = sample(color_map, p);
    if (length(abs(in_colors0 - pow(texColor.rgb, half3(exp)))) < scale)
        discard;
    return texColor;
}
