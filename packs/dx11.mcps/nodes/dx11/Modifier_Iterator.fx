#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
		float lifespan;
		float age;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

cbuffer cbuf
{
    bool AddForce = true;
};

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Iterate(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT) return;

	uint slotIndex = input.DTID.x;

	ParticleBuffer[slotIndex].age += mcpsTime.y;
	ParticleBuffer[slotIndex].lifespan -= mcpsTime.y;

	if(AddForce)
	{
		ParticleBuffer[slotIndex].velocity += ParticleBuffer[slotIndex].force;
	}
	
	ParticleBuffer[slotIndex].position += ParticleBuffer[slotIndex].velocity * mcpsTime.y;
}

technique11 Iterate { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Iterate() ) );} }
