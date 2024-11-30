sampler2D implicitInputSampler : register(s0);

#define const1 (16.0/255.0)
#define const2 (255.0/219.0)

float4 main(float2 tex : TEXCOORD0) : COLOR
{
   return((tex2D( implicitInputSampler, tex ) - const1 ) * const2);
}