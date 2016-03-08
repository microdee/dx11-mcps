﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;

namespace DX11.Mcps.Utils
{
    public delegate void RegistryChangedHandler(ParticleSystemRegistry sender, ParticleSystemRegistryEventArgs e);

    public class ParticleSystemRegistry : Dictionary<string, ParticleSystemData>
    {
        public const string PARTICLESYSTEM_ENUM = "DX11.Mcps.Core.ParticleSystemNames";
        public const string DEFAULT_ENUM = "Default System";
        private static ParticleSystemRegistry _instance;
        public event RegistryChangedHandler Changed;

        public static ParticleSystemRegistry Instance
        {
            get { return _instance ?? (_instance = new ParticleSystemRegistry()); }
        }

        public ParticleSystemRegistry() : base()
        {
            Add( DEFAULT_ENUM, new ParticleSystemData("DefaultId"));
            UpdateEnums();
        }

        public void Add(string particleSystemName, string particleSystemRegisterNodeId)
        {

            if (!Instance.ContainsKey(particleSystemName)){
                Instance[particleSystemName] = new ParticleSystemData(particleSystemRegisterNodeId);
            }
            else
            {
                Instance[particleSystemName].AddNodeId(particleSystemRegisterNodeId);
            }

            UpdateEnums();

            if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false));
        }

        public void Remove(ParticleSystemData particleSystemData)
        {
            var item = Instance.First(kvp => kvp.Value == particleSystemData);
            Instance.Remove(item.Key);

            UpdateEnums();

            if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true));
        }

        public void SetShaderVariables (string particleSystemName, string shaderRegisterNodeId, IEnumerable<string> variables)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetShaderVariables(shaderRegisterNodeId, variables);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true));
            }
        }

        public void RemoveShaderVariables (string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveShaderVariables(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false));
                }
            }
            catch (Exception) { }
        }

        public void SetDefines(string particleSystemName, string shaderRegisterNodeId, IEnumerable<string> additionalDefines)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetDefines(shaderRegisterNodeId, additionalDefines);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true));
            }
        }

        public void RemoveDefines(string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveDefines(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false));
                }
            }
            catch (Exception){}
        }

        public void SetEmitterSize(string particleSystemName, string shaderRegisterNodeId, int emitterSize)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetEmitterSize(shaderRegisterNodeId, emitterSize);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true));
            }
        }

        public void RemoveEmitterSize(string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveEmitterSize(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false));
                }
            }
            catch (Exception) { }
        }

        public ParticleSystemData GetByParticleSystemId (string particleSystemRegisterNodeId)
        {
            foreach (ParticleSystemData psd in Instance.Values)
            {
                if (psd.HasNodeId(particleSystemRegisterNodeId)) return psd;
            }
            return null;
        }

        public ParticleSystemData GetByShaderId (string shaderRegisterNodeId)
        {
            foreach (ParticleSystemData psd in Instance.Values)
            {
                if (psd.HasShaderNodeId(shaderRegisterNodeId)) return psd;
            }
            return null;
        }

        public string GetParticleSystemName (ParticleSystemData particleSystemData)
        {
            var item = Instance.First(kvp => kvp.Value == particleSystemData);
            return item.Key;
        }

        private void UpdateEnums()
        {
            EnumManager.UpdateEnum(ParticleSystemRegistry.PARTICLESYSTEM_ENUM, "", this.Keys.ToArray());
        }

    }

    public class ParticleSystemData
    {
        public List<string> ParticleSystemRegisterNodeIds = new List<string>();
        public Dictionary<string, List<string>> ShaderVariables = new Dictionary<string,List<string>>();
        public Dictionary<string, List<string>> Defines = new Dictionary<string, List<string>>();
        public Dictionary<string, int> EmitterSizes = new Dictionary<string, int>();
        
        public string StructureDefinition { get; protected set; }
        public int Stride { get; protected set; }
        public int ElementCount { get; protected set; }

        public ParticleSystemData(string particleSystemRegisterNodeId)
        {
            this.ParticleSystemRegisterNodeIds.Add(particleSystemRegisterNodeId);
            this.StructureDefinition = "";
            this.Stride = 0;
            this.ElementCount = 0;
        }

        public void AddNodeId (string particleSystemRegisterNodeId ){
            this.ParticleSystemRegisterNodeIds.Add(particleSystemRegisterNodeId);
        }

        public void RemoveNodeId(string particleSystemRegisterNodeId){
            this.ParticleSystemRegisterNodeIds.Remove(particleSystemRegisterNodeId);
        }

        public bool HasNodeId(string particleSystemRegisterNodeId)
        {
            return this.ParticleSystemRegisterNodeIds.Contains(particleSystemRegisterNodeId);
        }

        public bool IsEmpty()
        {
            return this.ParticleSystemRegisterNodeIds.Count == 0;
        }

        public void SetShaderVariables (string shaderRegisterNodeId, IEnumerable<string> variables){
            this.ShaderVariables[shaderRegisterNodeId] = variables.ToList();
            this.UpdateStructure();
        }

        public void RemoveShaderVariables (string shaderRegisterNodeId){
            this.ShaderVariables.Remove(shaderRegisterNodeId);
            this.UpdateStructure();
        }

        public void SetDefines(string shaderRegisterNodeId, IEnumerable<string> additionalDefines)
        {
            this.Defines[shaderRegisterNodeId] = additionalDefines.ToList();
        }

        public void RemoveDefines(string shaderRegisterNodeId)
        {
            this.Defines.Remove(shaderRegisterNodeId);
        }

        public List<string> GetDefines()
        {
            List<string> defines = (
                               from definelist in Defines.Values
                               from define in definelist
                               select define
                           ).Distinct().ToList();
            return defines;
        }

        public List<string> GetDefines(string shaderRegisterNodeId)
        {
            if (this.Defines.ContainsKey(shaderRegisterNodeId))
                return this.Defines[shaderRegisterNodeId];
            else return new List<string>();
        }

        public void SetEmitterSize(string shaderRegisterNodeId, int size)
        {
            this.EmitterSizes[shaderRegisterNodeId] = size;
            UpdateElementCount();
        }

        public void RemoveEmitterSize(string shaderRegisterNodeId)
        {
            if (this.EmitterSizes.ContainsKey(shaderRegisterNodeId))
            {
                this.EmitterSizes.Remove(shaderRegisterNodeId);
                UpdateElementCount();
            }
        }

        public bool HasShaderNodeId(string shaderRegisterNodeId)
        {
            return this.ShaderVariables.ContainsKey(shaderRegisterNodeId);
        }

        private void UpdateStructure(){
            var variables = (
                                from shaderVariables in ShaderVariables.Values
                                    from shaderVariable in shaderVariables
                                    select shaderVariable
                            ).Distinct().ToList();

            StructHelper sh = new StructHelper(variables);

            this.StructureDefinition = string.Join(" ", sh.StructureDefinition);
            this.Stride = sh.Stride;
        }

        public int GetEmitterOffset(string shaderRegisterNodeId)
        {
            int offset = 0;

            foreach (var kvp in EmitterSizes)
            {
                if (kvp.Key == shaderRegisterNodeId) return offset;
                else offset += kvp.Value;
            }

            return offset;
        }

        private void UpdateElementCount()
        {
            int count = 0;
            foreach (int size in EmitterSizes.Values)
            {
                count += size;
            }
            this.ElementCount = count;
        }
                
    }

    public class ParticleSystemRegistryEventArgs : EventArgs
    {
        public ParticleSystemRegistryEventArgs(bool isShaderEvent)
        {
            this.isShaderEvent = isShaderEvent;
        }
        private bool isShaderEvent;
        public bool IsShaderEvent
        {
            get { return isShaderEvent; }
        }
    }
    
}
