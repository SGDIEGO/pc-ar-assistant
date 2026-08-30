namespace PCAR.Recognition
{
    /// <summary>
    /// FASE 8: reconocimiento SIMULADO. No usa camara.
    /// Devuelve siempre el componente RAM como si la IA lo hubiera identificado,
    /// para poder desarrollar Info/UI/Procedimientos/AR sin depender de un modelo real.
    /// </summary>
    public class MockRecognitionService : IRecognitionService
    {
        public RecognitionResult Recognize()
        {
            return RecognitionResult.Found("ram", 0.94f);
        }
    }
}
