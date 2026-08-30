using System;

namespace PCAR.Components
{
    /// <summary>
    /// FASE 7: un paso de ensamblaje/desensamblaje.
    /// Modelo de datos puro (independiente de Unity) para poder
    /// serializarlo a JSON y reutilizarlo en la guia.
    /// </summary>
    [Serializable]
    public class AssemblyStep
    {
        public int stepNumber;
        public string title;
        public string description;
        public string warning;
        public string arHint;
    }
}
