namespace SadViewer.Models
{
    // ==============================================================
    // SADConfig
    // ⚠ Si ya existe en otro archivo, ELIMÍNALA de aquí
    // ==============================================================
    /// <summary>
    /// DTO que representa la configuración persistida en <c>SADConfig.json</c>.
    /// </summary>
    public class SADConfig
    {
        /// <summary>Puerto COM por defecto.</summary>
        public string COM { get; set; } = "COM1";

        /// <summary>Velocidad en baudios por defecto.</summary>
        public int RATE { get; set; } = 115200;

        /// <summary>Carpeta relativa o absoluta donde residen los archivos de BD.</summary>
        public string DatabaseFolder { get; set; } = "SAD";

        /// <summary>Nombre del archivo HRL.</summary>
        public string HRLFile { get; set; } = "BS.hrl";

        /// <summary>Nombre del archivo GHT.</summary>
        public string GHTFile { get; set; } = "BS.ght";
    }
}