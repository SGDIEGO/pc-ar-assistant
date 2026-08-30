namespace PCAR.Recognition
{
    /// <summary>
    /// FASE 14: fabrica de servicios de reconocimiento.
    /// UNICO punto donde se decide que servicio usar (mock durante desarrollo,
    /// IA cuando el modelo este listo). Cambiar `UseAI` a true activa la IA sin
    /// modificar el resto de la aplicacion.
    /// </summary>
    public static class RecognitionServiceFactory
    {
        /// <summary>
        /// false -> MockRecognitionService (reconoce siempre RAM para desarrollo).
        /// true  -> AIRecognitionService (necesita un IRecognitionModel conectado).
        /// </summary>
        public const bool UseAI = false;

        /// <summary>Modelo de IA a usar cuando UseAI sea true (null = aun no conectado).</summary>
        public static IRecognitionModel Model { get; set; }

        public static IRecognitionService Create()
        {
            if (UseAI)
            {
                return new AIRecognitionService(Model);
            }

            return new MockRecognitionService();
        }
    }
}
