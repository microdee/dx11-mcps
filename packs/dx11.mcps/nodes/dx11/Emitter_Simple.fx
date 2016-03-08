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
RWStructuredBuffer<uint> CounterBuffer : COUNTERBUFFER; // counts the number of emitted particles
RWStructuredBuffer<uint> IndexBuffer : INDEXBUFFER; // stores indices of written particles

StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
StructuredBuffer<float3> VelocityBuffer <string uiname="Velocity Buffer";>;
StructuredBuffer<float3> ForceBuffer <string uiname="Force Buffer";>;
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

cbuffer cbuf
{
    uint EmitCount = 0;
	uint EmitterSize = 0;
	bool Emit = false;
}

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Main(csin input)
{
	
	if(input.DTID.x >= EmitterSize) return;
	
	if (Emit) {
		
		uint slotIndex = EMITTEROFFSET + input.DTID.x;

		float currentLifespan = ParticleBuffer[slotIndex].lifespan;
		if ( currentLifespan <= 0.0f){
			
			
			uint particleCounter = CounterBuffer.IncrementCounter();
			if (particleCounter > EmitCount -1 ) return;
			
			uint size, stride;
			Particle p = (Particle) 0;

			PositionBuffer.GetDimensions(size,stride);
			p.position = PositionBuffer[particleCounter % size];

			VelocityBuffer.GetDimensions(size,stride);
			p.velocity = VelocityBuffer[particleCounter % size];
			
			ForceBuffer.GetDimensions(size,stride);
			p.force = ForceBuffer[particleCounter % size];

			LifespanBuffer.GetDimensions(size,stride);
			p.lifespan = LifespanBuffer[particleCounter % size];

			ParticleBuffer[slotIndex] = p;
			IndexBuffer[EMITTEROFFSET + particleCounter] = slotIndex;
		}
	}
}

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	if (Emit) {
		uint particleCounter = CounterBuffer.IncrementCounter();
		CounterBuffer[0] = particleCounter;
	}
}

technique11 EmitParticles { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Main() ) );} }
technique11 UpdateCounter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }
