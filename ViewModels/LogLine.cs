namespace SadViewer.ViewModels
{
    /// <summary>
    /// Niveles de severidad posibles para una línea de log.
    /// </summary>
    /// <remarks>
    /// Se usan para colorear y filtrar las entradas en la vista
    /// (ver <c>LevelToBrushConverter</c>).
    /// </remarks>
    public enum LogLevel { Info, Ok, Warn, Error }

    /// <summary>
    /// Representa una entrada individual del registro de logs mostrado en la UI.
    /// </summary>
    /// <remarks>
    /// Contiene la marca de tiempo, el nivel de severidad y el mensaje.
    /// Expone además propiedades de solo lectura ya formateadas
    /// (<see cref="TimeText"/> y <see cref="LevelText"/>) para el binding directo en XAML.
    /// </remarks>
    public class LogLine
    {
        /// <summary>Momento en que se generó la entrada. Por defecto, la hora actual.</summary>
        public DateTime Time { get; set; } = DateTime.Now;

        /// <summary>Nivel de severidad de la entrada. Por defecto, <see cref="LogLevel.Info"/>.</summary>
        public LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>Texto del mensaje. Por defecto, cadena vacía.</summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Hora formateada como <c>HH:mm:ss</c> (24 h) para mostrar en la UI.
        /// </summary>
        public string TimeText => Time.ToString("HH:mm:ss");

        /// <summary>
        /// Nombre del nivel en mayúsculas (por ejemplo, <c>INFO</c>, <c>WARN</c>).
        /// Se usa <c>ToUpperInvariant</c> para evitar diferencias culturales.
        /// </summary>
        public string LevelText => Level.ToString().ToUpperInvariant();

        /// <summary>
        /// Devuelve una representación textual de la línea de log,
        /// útil para depuración o exportación a texto plano.
        /// </summary>
        /// <returns>Cadena con formato <c>[HH:mm:ss] NIVEL Mensaje</c>.</returns>
        public override string ToString()
            => $"[{TimeText}] {LevelText,-5} {Message}"; // El ancho -5 alinea el nivel en columnas
    }
}