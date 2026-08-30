using PCAR.Assembly;
using PCAR.Components;
using PCAR.Recognition;
using PCAR.UI;
using UnityEngine;

namespace PCAR.Core
{
    /// <summary>
    /// FASE 10: orquesta el flujo del MVP.
    /// Pantalla de informacion <-> guia de procedimiento (ensamblaje/desensamblaje).
    /// Un solo GameObject con ambos OnGUI; se habilita uno a la vez.
    /// </summary>
    public class PCARFlowController : MonoBehaviour
    {
        public RamInfoScreen infoScreen;
        public AssemblyGuideController guide;

        private IRecognitionService _recognition;

        private void Awake()
        {
            _recognition = RecognitionServiceFactory.Create();
        }

        private void Start()
        {
            if (infoScreen == null)
            {
                infoScreen = GetComponent<RamInfoScreen>();
            }

            if (guide == null)
            {
                guide = GetComponent<AssemblyGuideController>();
            }

            if (guide == null)
            {
                gameObject.AddComponent<AssemblyGuideController>();
                guide = GetComponent<AssemblyGuideController>();
            }

            guide.enabled = false;
            infoScreen.enabled = true;

            // La pantalla de informacion se carga con el resultado del reconocimiento.
            var result = _recognition.Recognize();
            if (result.success)
            {
                infoScreen.component = ComponentRepository.Get(result.componentId);
            }

            infoScreen.OnAssembleRequested += StartAssembly;
            infoScreen.OnDisassembleRequested += StartDisassembly;
            guide.OnExitRequested += ShowInfo;
            guide.OnCompleted += ShowInfo;
        }

        private void StartAssembly()
        {
            var component = ComponentRepository.GetRam();
            if (component == null)
            {
                return;
            }

            guide.Configure(component.assemblySteps);
            ShowGuide();
        }

        private void StartDisassembly()
        {
            var component = ComponentRepository.GetRam();
            if (component == null)
            {
                return;
            }

            guide.Configure(component.disassemblySteps);
            ShowGuide();
        }

        private void ShowGuide()
        {
            infoScreen.enabled = false;
            guide.enabled = true;
        }

        private void ShowInfo()
        {
            guide.enabled = false;
            infoScreen.enabled = true;
        }
    }
}
