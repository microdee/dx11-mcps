#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float4 color;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> CounterBuffer : COUNTERBUFFER;
RWStructuredBuffer<uint> IndexBuffer : INDEXBUFFER;

StructuredBuffer<float4> ColorBuffer <string uiname="Color Buffer";>;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSMain(csin input)
{
	if(input.DTID.x >= CounterBuffer[0]) return;
	uint slotIndex = IndexBuffer[input.DTID.x];
	
	uint size, stride;
	ColorBuffer.GetDimensions(size,stride);
	ParticleBuffer[slotIndex].color =  ColorBuffer[input.DTID.x % size];
}

technique11 csmain { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMain() ) );} }
