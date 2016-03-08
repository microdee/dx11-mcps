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

        [Input("Defines")]
        public IDiffSpread<string> FDefines;

        [Input("EmitCount", DefaultValue = 0, IsSingle=true)]
        public IDiffSpread<int> FEmitCount;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Output("Defines", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutDefines;

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
                UpdateDefines();
                UpdateEmitterSize();
                UpdateShaderVariables();
                _ParticleSystemChanged = false;
            }

            if (FParticleSystemName.IsChanged)
            {
                UpdateDefines();
                UpdateEmitterSize();
                UpdateShaderVariables();
            }

            if (FVariables.IsChanged)
            {
                SetShaderVariables();
            }

            if (FDefines.IsChanged)
            {
                SetDefines();
            }

            if (FEmitCount.IsChanged)
            {
                SetEmitterSize();
            }
        }

        public void Dispose()
        {
            ParticleSystemRegistry.Instance.Changed -= HandleRegistryChange;
            RemoveDefines();
            RemoveEmitterSize();
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

        private void SetDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetDefines(FParticleSystemName[0], this.ShaderNodeId, FDefines);
        }

        private void RemoveDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveDefines(this.ShaderNodeId);
        }

        private void UpdateDefines()
        {
            RemoveDefines();
            SetDefines();
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
                FOutDefines.SliceCount = 0;
                FOutDefines.Add("COMPOSITESTRUCT=" + particleSystemData.StructureDefinition);
                FOutDefines.Add("MAXPARTICLECOUNT=" + particleSystemData.ElementCount);

                foreach (string define in particleSystemData.GetDefines())
                {
                    if (define != "") FOutDefines.Add(define);
                }
                
                if (FEmitCount[0] > 0) // this is an emitter, so we output an offset too
                {
                    FOutDefines.Add("EMITTEROFFSET=" + particleSystemData.GetEmitterOffset(this.ShaderNodeId));
                }

                FOutDefines.Flush();

                FElementCount[0] = particleSystemData.ElementCount;
                FElementCount.Flush();
            }
            
        }

    }
}
