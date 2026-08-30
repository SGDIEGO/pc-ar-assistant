using System;

namespace PCAR.Recognition
{
    /// <summary>
    /// FASE 8: resultado de un reconocimiento visual.
    /// Unico contrato entre el reconocimiento (mock o IA) y el resto de la app.
    /// </summary>
    [Serializable]
    public struct RecognitionResult
    {
        public bool success;
        public string componentId;
        public float confidence;
        public string message;

        public static RecognitionResult Found(string componentId, float confidence)
        {
            return new RecognitionResult
            {
                success = true,
                componentId = componentId,
                confidence = confidence,
                message = null
            };
        }

        public static RecognitionResult NotFound(string message)
        {
            return new RecognitionResult
            {
                success = false,
                componentId = null,
                confidence = 0f,
                message = message
            };
        }
    }

    /// <summary>
    /// FASE 8: contrato del servicio de reconocimiento.
    /// Se implementa hoy con MockRecognitionService; en Fase 14 se reemplaza
    /// por AIRecognitionService SIN tocar el resto de la aplicacion.
    /// </summary>
    public interface IRecognitionService
    {
        RecognitionResult Recognize();
    }
}
