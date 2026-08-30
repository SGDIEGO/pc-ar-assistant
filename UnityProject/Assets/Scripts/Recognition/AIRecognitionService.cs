namespace PCAR.Recognition
{
    /// <summary>
    /// FASE 14: punto de integracion de un modelo real de vision artificial.
    /// Quien integre el modelo de IA solo implementa este contrato; el resto de
    /// la app (informacion, guias, UI) no se toca. Ejemplo de salida del modelo:
    /// { "component": "RAM", "confidence": 0.94 }.
    /// </summary>
    public interface IRecognitionModel
    {
        RecognitionResult Recognize();
    }

    /// <summary>
    /// FASE 14: servicio de reconocimiento por IA.
    /// Reemplaza a MockRecognitionService SIN evaluarlo (el resto de la app solo
    /// conoce IRecognitionService). Si no hay modelo real conectado (o la confianza
    /// es demasiado baja), devuelve el mensaje estandar de "no identificado".
    /// </summary>
    public class AIRecognitionService : IRecognitionService
    {
        private readonly IRecognitionModel _model;
        private readonly float _minConfidence;

        public const string NotFoundMessage =
            "Componente no identificado. Intente acercarse o mejorar la iluminacion.";

        public AIRecognitionService(IRecognitionModel model, float minConfidence = 0.6f)
        {
            _model = model;
            _minConfidence = minConfidence;
        }

        public RecognitionResult Recognize()
        {
            if (_model == null)
            {
                // Aun no hay modelo de vision conectado (integracion pendiente).
                return RecognitionResult.NotFound(NotFoundMessage);
            }

            var result = _model.Recognize();
            if (!result.success)
            {
                return RecognitionResult.NotFound(result.message ?? NotFoundMessage);
            }

            if (result.confidence < _minConfidence)
            {
                return RecognitionResult.NotFound(NotFoundMessage);
            }

            return result;
        }
    }
}
