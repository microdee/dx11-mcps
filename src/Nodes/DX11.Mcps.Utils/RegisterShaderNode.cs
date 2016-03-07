using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;

using VVVV.Core.Logging;

namespace DX11.Mcps.Utils
{
    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Mcps.Core", Version = "Shader", Help = "Adds mandatory variables of a shader to the particle system registry and outputs all composited variables for the particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class RegisterShaderNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {

        [Input("Structure", DefaultString = "")]
        public IDiffSpread<string> FVariables;

        [Input("EmitCount", DefaultValue = 0, IsSingle=true)]
        public IDiffSpread<int> FEmitCount;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Output("StructureDefinition", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FStructureDefinition;

        [Output("Element Count", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCount;

        [Import]
        protected IPluginHost2 PluginHost;

        [Import()]
        public ILogger FLogger;

        private string ShaderNodeId = "";
        private bool _ParticleSystemChanged = false;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;
            this.ShaderNodeId = this.PluginHost.GetNodePath(false);
        }

        public void Evaluate(int SpreadMax)
        {
            if (_ParticleSystemChanged)
            {
                UpdateShaderVariables();
                UpdateEmitterSize();
                _ParticleSystemChanged = false;
            }

            if (FParticleSystemName.IsChanged)
            {
                UpdateShaderVariables();
                UpdateEmitterSize();
            }

            if (FVariables.IsChanged)
            {
                SetShaderVariables();
            }
            if (FEmitCount.IsChanged)
            {
                SetEmitterSize();
            }
        }

        public void Dispose()
        {
            ParticleSystemRegistry.Instance.Changed -= HandleRegistryChange;
            RemoveShaderVariables();
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            if (!e.IsShaderEvent) _ParticleSystemChanged = true;
            UpdateOutputPins();
        }

        private void SetShaderVariables ()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetShaderVariables(FParticleSystemName[0], this.ShaderNodeId, FVariables);
        }

        private void RemoveShaderVariables()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveShaderVariables(this.ShaderNodeId);
        }

        private void UpdateShaderVariables()
        {
            RemoveShaderVariables();
            SetShaderVariables();
        }

        private void SetEmitterSize()
        {
            if (FEmitCount[0] > 0)
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetEmitterSize(FParticleSystemName[0], this.ShaderNodeId, FEmitCount[0]);
            }
        }

        private void RemoveEmitterSize()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveEmitterSize(this.ShaderNodeId);
        }

        private void UpdateEmitterSize()
        {
            RemoveEmitterSize();
            SetEmitterSize();
        }

        private void UpdateOutputPins()
        {
            var particleSystemData = ParticleSystemRegistry.Instance.GetByShaderId(this.ShaderNodeId);
            if (particleSystemData != null)
            {
                FStructureDefinition.SliceCount = 3;
                FStructureDefinition[0] = "COMPOSITESTRUCT=" + particleSystemData.StructureDefinition;
                FStructureDefinition[1] = "COMPOSITESTRUCTAVAILABLE=1";
                FStructureDefinition[2] = "MAXPARTICLECOUNT=" + particleSystemData.ElementCount;

                if (FEmitCount[0] > 0) // this is an emitter, so we output an offset too
                {
                    FStructureDefinition.SliceCount = 4;
                    FStructureDefinition[3] = "EMITTEROFFSET=" + particleSystemData.GetEmitterOffset(this.ShaderNodeId);
                }

                FStructureDefinition.Flush();

                FElementCount[0] = particleSystemData.ElementCount;
                FElementCount.Flush();
            }
            
        }

    }
}
