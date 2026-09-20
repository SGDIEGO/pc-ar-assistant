public static class PCComponentsData
{
    public static ComponentInfo GetCPU()
    {
        return new ComponentInfo
        {
            componentName = "CPU - Procesador",
            buttonLabel = "CPU",
            description = "La CPU (Unidad Central de Procesamiento) es el cerebro de la computadora. Es el componente encargado de interpretar y ejecutar las instrucciones de los programas: realiza cálculos, toma decisiones y coordina a todos los demás componentes del sistema.",
            function = "Ejecuta las instrucciones del sistema operativo y de las aplicaciones. Su velocidad se mide en GHz (gigahertzios) y determina la rapidez con la que la PC procesa información. Incluye varios núcleos que trabajan en paralelo para mejorar el rendimiento.",
            specifications = "Marca/modelo: Intel Core o AMD Ryzen.\nNúcleos: 6 a 16 en CPUs modernas.\nVelocidad: 2.5 a 5.7 GHz.\nSocket: LGA o AM5 (zócalo sobre la placa madre).\nTDP: 65 a 125 W (el disipador debe ser adecuado).",
            assembleSteps = new[]
            {
                "Abre la palanca del zócalo del procesador en la placa madre.",
                "Alinea las muescas del CPU con las del zócalo (hay un indicador en una esquina).",
                "Coloca el CPU con cuidado sobre el zócalo sin ejercer presión.",
                "Baja y asegura la palanca del zócalo hasta que encaje.",
                "Aplica pasta térmica en el centro del IHS (cubierta metálica).",
                "Coloca el disipador o sistema de refrigeración y fija sus anclajes.",
                "Conecta el cable del ventilador al conector de la CPU en la placa."
            },
            disassembleSteps = new[]
            {
                "Apaga la computadora y desconéctala de la corriente.",
                "Desconecta el cable del ventilador del conector de la CPU.",
                "Desmonta el sistema de refrigeración aflojando sus anclajes.",
                "Gira con cuidado el disipador para despegar del IHS (si hace falta, calienta ligeramente).",
                "Limpia la pasta térmica restante con alcohol isopropílico.",
                "Abre la palanca del zócalo.",
                "Retira el CPU sujetándolo por los bordes, sin tocar los pines/conectores."
            }
        };
    }

    public static ComponentInfo GetRAM()
    {
        return new ComponentInfo
        {
            componentName = "RAM - Memoria de acceso aleatorio",
            buttonLabel = "RAM",
            description = "La memoria RAM almacena temporalmente los datos y programas que la CPU necesita en el momento. Es una memoria rápida y volátil: su contenido se borra cuando se apaga la computadora por eso se usa como área de trabajo del sistema.",
            function = "Guarda las instrucciones y datos en uso para que el procesador los lea casi instantáneamente. Más capacidad (GB) permite ejecutar más programas a la vez sin que la PC se detenga o se ponga lenta.",
            specifications = "Tipo: DDR4 o DDR5.\nCapacidad: 8, 16 o 32 GB por módulo.\nVelocidad: 3200 a 6400 MT/s.\nFormato: DIMM (PC de escritorio).\nInstalar en canales: usar los slots A2/B2 para modo dual.",
            assembleSteps = new[]
            {
                "Revisa la posición de la muesca del módulo DIMM para que coincida con el slot.",
                "Abre los clips (seguros) de los extremos del slot de memoria.",
                "Alinea el módulo de memoria sobre el slot presionando desde arriba.",
                "Presiona con firmeza y de forma pareja en ambos extremos hasta que los clips hagan clic.",
                "Repite para cada módulo. Usa la configuración de canales recomendada de la placa."
            },
            disassembleSteps = new[]
            {
                "Apaga la computadora y desconéctala de la corriente.",
                "Presiona hacia afuera los clips de ambos extremos del slot de RAM.",
                "El módulo se desprende solo: sácalo hacia arriba sujetándolo por los bordes.",
                "Guárdalo en una bolsa antiestática o superficie aislante."
            }
        };
    }

    public static ComponentInfo GetSSD()
    {
        return new ComponentInfo
        {
            componentName = "SSD M.2 NVMe",
            buttonLabel = "SSD",
            description = "El SSD (unidad de estado sólido) es el almacenamiento principal donde se instalan el sistema operativo, programas y archivos. No tiene partes móviles: usa memoria flash, por lo que es muy rápido, silencioso y resistente a golpes.",
            function = "Almacena los datos de forma permanente. En formato M.2 NVMe se conecta directamente a la placa madre y alcanza velocidades de lectura muy superiores a los discos mecánicos, reduciendo el tiempo de arranque y de carga de aplicaciones.",
            specifications = "Formato: M.2 2280 (22 mm x 80 mm).\nInterfaz: PCIe NVMe (Gen 3, 4 o 5).\nCapacidad: 256 GB a 2 TB.\nVelocidad lectura: 2000 a 12000 MB/s.\nActualización: firmware vía app del fabricante.",
            assembleSteps = new[]
            {
                "Asegura el tornillo de montaje del slot M.2 en la placa madre.",
                "Alinea la muesca del conector del SSD con la del slot M.2.",
                "Inserta el SSD en un ángulo de 30 grados hasta que entre por completo.",
                "Baja el extremo libre del SSD hasta la posición horizontal.",
                "Fija el SSD con el tornillo de montaje o anclaje de la placa.",
                "Opcional: coloca el disipador térmico M.2 sobre el SSD."
            },
            disassembleSteps = new[]
            {
                "Apaga la computadora y desconéctala de la corriente.",
                "Retira el disipador térmico M.2 si lo tiene la placa.",
                "Quita el tornillo que fija el extremo libre del SSD.",
                "Levanta ligeramente el SSD, retíralo del conector en ángulo hacia arriba.",
                "Guárdalo en bolsa antiestática."
            }
        };
    }

    public static string MainMenuTitle
    {
        get { return "Componentes de una PC"; }
    }

    public static string MainMenuBody
    {
        get { return "Toca sobre la placa madre (CPU, RAM o SSD) para ver su información y los pasos de ensamblaje y desensamblaje.\n\nArrastra con un dedo para mover la placa; usa dos dedos para girar o hacer zoom."; }
    }
}