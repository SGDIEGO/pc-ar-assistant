using System;
using System.Collections.Generic;

namespace PCAR.Components
{
    /// <summary>
    /// FASE 7: informacion completa de un componente de PC.
    /// Modelo de datos puro, desacoplado de Unity, para serializar a JSON
    /// (la fuente de verdad vive en Assets/Resources/Data/Components/*.json).
    /// </summary>
    [Serializable]
    public class ComponentInfo
    {
        public string id;
        public string name;
        public string description;
        public string function;

        public List<string> specifications = new List<string>();
        public List<string> warnings = new List<string>();

        public List<AssemblyStep> assemblySteps = new List<AssemblyStep>();
        public List<AssemblyStep> disassemblySteps = new List<AssemblyStep>();
    }
}
